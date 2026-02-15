using JetBrains.Annotations;

// These attributes tell Rider/ReSharper where to find Razor views for the feature folder convention.
// They mirror the patterns in FeatureViewLocationExpander.cs.
// {0} = view name, {1} = controller name (without "Controller" suffix)

// Shared and common view locations
[assembly: AspMvcViewLocationFormat("~/Features/Shared/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Shared/Views/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Shared/Views/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Shared/Views/Header/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Shared/Views/ElementBlocks/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Blocks/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Blocks/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Cms/Views/{1}/{0}.cshtml")]

// Single-level features where the folder name matches the controller name
// e.g., HomeController -> Features/Home/Index.cshtml
[assembly: AspMvcViewLocationFormat("~/Features/{1}/{0}.cshtml")]

// Nested feature folders: ~/Features/{Parent}/{ControllerName}/{ViewName}.cshtml
// When adding a new parent feature folder, add an entry here.
[assembly: AspMvcViewLocationFormat("~/Features/Blog/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/CatalogContent/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/CatalogContent/DynamicCatalogContent/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Checkout/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Events/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/LandingPages/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Locations/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Markets/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/MyAccount/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/MyOrganization/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/NamedCarts/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/People/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Preview/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Recommendations/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Search/{1}/{0}.cshtml")]

// Single-level features where the folder name differs from the controller name.
// e.g., StandardCategoryController -> Features/Category/Index.cshtml
// These need a hardcoded parent since {1} won't match the folder.
[assembly: AspMvcViewLocationFormat("~/Features/Category/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Collection/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Login/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Markets/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/NewProducts/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Preview/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Sales/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("~/Features/Stores/{0}.cshtml")]
