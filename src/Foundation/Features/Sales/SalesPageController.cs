using Foundation.Features.Search;
using Foundation.Infrastructure.Cms.Settings;

namespace Foundation.Features.Sales;

public class SalesPageController : PageController<SalesPage>
{
    private readonly ISearchService _searchService;
    private readonly ISettingsService _settingsService;

    public SalesPageController(ISearchService searchService,
        ISettingsService settingsService)
    {
        _searchService = searchService;
        _settingsService = settingsService;
    }

    public async Task<ActionResult> Index(SalesPage currentPage, int page = 1)
    {
        var searchSettings = _settingsService.GetSiteSettings<SearchSettings>();
        var result = await _searchService.SearchOnSaleAsync(currentPage, searchSettings?.SearchCatalog ?? 0, page, 12);
        var model = new SalesPageViewModel(currentPage)
        {
            ProductViewModels = result.Products,
            PageNumber = page,
            Pages = result.Pages
        };

        return View(model);
    }
}