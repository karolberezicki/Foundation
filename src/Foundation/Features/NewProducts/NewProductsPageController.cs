using Foundation.Features.Search;
using Foundation.Infrastructure.Cms.Settings;

namespace Foundation.Features.NewProducts;

public class NewProductsPageController : PageController<NewProductsPage>
{
    private readonly ISearchService _searchService;
    private readonly ISettingsService _settingsService;

    public NewProductsPageController(ISearchService searchService,
        ISettingsService settingsService)
    {
        _searchService = searchService;
        _settingsService = settingsService;
    }

    public async Task<ActionResult> Index(NewProductsPage currentPage, int page = 1)
    {
        var searchsettings = _settingsService.GetSiteSettings<SearchSettings>();
        var result = await _searchService.SearchNewProductsAsync(currentPage, searchsettings?.SearchCatalog ?? 0, page);
        var model = new NewProductsPageViewModel(currentPage)
        {
            ProductViewModels = result.Products,
            PageNumber = page,
            Pages = result.Pages
        };

        return View(model);
    }
}