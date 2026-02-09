#Requires -Version 7.0

param(
    [string]$AppName,
    [string]$SqlServer,
    [string]$DbUser,
    [string]$DbPassword
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot

# Check for admin privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)
if (-not $isAdmin) {
    Write-Error "setup.ps1 script must be run in an elevated (admin) PowerShell prompt"
    exit 1
}

try { $host.UI.RawUI.BufferSize = New-Object System.Management.Automation.Host.Size(120, 5000) } catch {}

$RootPath = $PSScriptRoot
$SourcePath = Join-Path $RootPath "src"

# Prompt for parameters if not provided via command line
if (-not $AppName) {
    do {
        $AppName = Read-Host "Enter your app name (required)"
        $SqlServer = Read-Host "Enter your SQL server name (optional, press Enter for default (.) local server)"
        $DbUser = Read-Host "Enter database user name (required)"
        $DbPassword = Read-Host "Enter database password (required)"

        if (-not $AppName -or -not $DbUser -or -not $DbPassword) {
            Write-Host "Parameters missing, application name, database user and database password are required"
            pause
            Clear-Host
        }
    } while (-not $AppName -or -not $DbUser -or -not $DbPassword)
}

if (-not $SqlServer) { $SqlServer = "." }

Clear-Host
Write-Host "Your application name is: $AppName"
Write-Host "Your SQL server name is: $SqlServer"
Write-Host "Your database user is: $DbUser"
Write-Host "Waiting 15 seconds..."
Start-Sleep -Seconds 15

$cmsDb = "$AppName.Cms"
$commerceDb = "$AppName.Commerce"
$user = $DbUser
$password = $DbPassword

Clear-Host
Write-Host @"
######################################################################
#     Grab a tea or coffee, this could take around 5 to 10 mins      #
######################################################################
#                                                                    #
#                         (  )   (   )  )                            #
#                          ) (   )  (  (                             #
#                          ( )  (    ) )                             #
#                          _____________                             #
#                         |_____________| ___                        #
#                         |             |/ _ \                       #
#                         |             | | |                        #
#                         | Optimizely  |_| |                        #
#                      ___|             |\___/                       #
#                     /    \___________/    \                         #
#                     \_____________________/                        #
#                                                                    #
######################################################################
"@

Write-Host "## Building Foundation please check the Build\Logs directory if you receive errors"

# Getting MSBuild path
Write-Host "## Getting MSBuildPath ##"
$vswherePath = Join-Path $RootPath "build\vswhere.exe"
$installDir = & $vswherePath -latest -products * -requires Microsoft.Component.MSBuild -property installationPath

$msBuildPath = $null
foreach ($version in @("15.0", "14.0")) {
    $candidate = Join-Path $installDir "MSBuild\$version\Bin\MSBuild.exe"
    if (Test-Path $candidate) {
        $msBuildPath = $candidate
        break
    }
}
if (-not $msBuildPath) {
    $msBuildPath = Join-Path $installDir "MSBuild\Current\Bin\MSBuild.exe"
}
Write-Host "msbuild.exe path: $msBuildPath"

$logsDir = Join-Path $RootPath "Build\Logs"
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null

$buildLog = Join-Path $logsDir "Build.log"
$databaseLog = Join-Path $logsDir "Database.log"

# NPM Install
Write-Host "## NPM Install ##"
"## NPM Install" | Out-File $buildLog
Push-Location (Join-Path $SourcePath "Foundation")
npm ci
if ($LASTEXITCODE -ne 0) {
    Write-Error "npm ci failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}
npm run dev
Pop-Location

# Clean and build
Write-Host "## Clean and build ##"
"## Clean and build ##" | Add-Content $buildLog
& $msBuildPath (Join-Path $RootPath "Foundation.sln") /t:Clean,Build 2>&1 | Add-Content $buildLog

# SQL commands
$sqlArgs = @("-S", $SqlServer, "-U", $DbUser, "-P", $DbPassword)
Write-Host "## sqlcmd -S $SqlServer -U $DbUser ##"

function Invoke-Sqlcmd-Custom {
    param([string[]]$ExtraArgs)
    & sqlcmd @sqlArgs @ExtraArgs
}

# Dropping databases
Write-Host "## Dropping databases ##"
"## Dropping databases ##" | Out-File $databaseLog
Invoke-Sqlcmd-Custom -ExtraArgs @("-Q", "EXEC msdb.dbo.sp_delete_database_backuphistory N'$cmsDb'") 2>&1 | Add-Content $databaseLog
Invoke-Sqlcmd-Custom -ExtraArgs @("-Q", "if db_id('$cmsDb') is not null ALTER DATABASE [$cmsDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE") 2>&1 | Add-Content $databaseLog
Invoke-Sqlcmd-Custom -ExtraArgs @("-Q", "if db_id('$cmsDb') is not null DROP DATABASE [$cmsDb]") 2>&1 | Add-Content $databaseLog
Invoke-Sqlcmd-Custom -ExtraArgs @("-Q", "EXEC msdb.dbo.sp_delete_database_backuphistory N'$commerceDb'") 2>&1 | Add-Content $databaseLog
Invoke-Sqlcmd-Custom -ExtraArgs @("-Q", "if db_id('$commerceDb') is not null ALTER DATABASE [$commerceDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE") 2>&1 | Add-Content $databaseLog
Invoke-Sqlcmd-Custom -ExtraArgs @("-Q", "if db_id('$commerceDb') is not null DROP DATABASE [$commerceDb]") 2>&1 | Add-Content $databaseLog

# Dropping user
Write-Host "## Dropping user ##"
"## Dropping user ##" | Add-Content $databaseLog
Invoke-Sqlcmd-Custom -ExtraArgs @("-Q", "if exists (select loginname from master.dbo.syslogins where name = '$user') EXEC sp_droplogin @loginame='$user'") 2>&1 | Add-Content $databaseLog

# Create databases via dotnet-episerver
dotnet tool update EPiServer.Net.Cli --global --add-source https://nuget.optimizely.com/feed/packages.svc/
dotnet-episerver create-cms-database ".\src\Foundation\Foundation.csproj" -S "$SqlServer" -U $DbUser -P $DbPassword -du $DbUser -dp $DbPassword --database-name "$AppName.Cms"
dotnet-episerver create-commerce-database ".\src\Foundation\Foundation.csproj" -S "$SqlServer" -U $DbUser -P $DbPassword --database-name "$AppName.Commerce" --reuse-cms-user

# Installing foundation configuration
Write-Host "## Installing foundation configuration ##"
"## Installing foundation configuration ##" | Add-Content $databaseLog
Invoke-Sqlcmd-Custom -ExtraArgs @("-d", $commerceDb, "-b", "-i", "Build\SqlScripts\FoundationConfigurationSchema.sql", "-v", "appname=$AppName") 2>&1 | Add-Content $databaseLog

# Installing unique coupon schema
Write-Host "## Installing unique coupon schema ##"
"## Installing unique coupon schema ##" | Add-Content $databaseLog
Invoke-Sqlcmd-Custom -ExtraArgs @("-d", $commerceDb, "-b", "-i", "Build\SqlScripts\UniqueCouponSchema.sql") 2>&1 | Add-Content $databaseLog

# Generate resetup script
Write-Host "Run resetup.ps1 to resetup solution"
$resetupContent = @"
Clear-Host
Write-Host "######################################################################"
Write-Host "#           Rebuild the current application from default             #"
Write-Host "######################################################################"
Write-Host "#                                                                    #"
Write-Host "#       NOTE: This will **DROP** the existing DB                     #"
Write-Host "#             and resetup so use with caution!!                      #"
Write-Host "#                                                                    #"
Write-Host "#       Ctrl+C NOW if you are unsure!                                #"
Write-Host "#                                                                    #"
Write-Host "######################################################################"
pause
& "`$PSScriptRoot\setup.ps1" -AppName "$AppName" -SqlServer "$SqlServer" -DbUser "$DbUser" -DbPassword "$DbPassword"
"@
$resetupContent | Out-File (Join-Path $RootPath "resetup.ps1") -Encoding utf8

pause
