using Foundation.Infrastructure.Find.Facets;

namespace Foundation.Features.Search.Category;

public class CategoryPartialComponent : AsyncPartialContentComponent<GenericNode>
{
    private readonly ISearchViewModelFactory _viewModelFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CategoryPartialComponent(ISearchViewModelFactory viewModelFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _viewModelFactory = viewModelFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    [AcceptVerbs(new string[] { "GET", "POST" })]
    protected override async Task<IViewComponentResult> InvokeComponentAsync(GenericNode currentContent)
    {
        var viewmodel = await GetSearchModelAsync(currentContent);
        return View("_Category", viewmodel);
    }

    protected virtual async Task<SearchViewModel<GenericNode>> GetSearchModelAsync(GenericNode currentContent)
    {
        return await _viewModelFactory.CreateAsync(currentContent, _httpContextAccessor.HttpContext.Request.Query["facets"].ToString(), 0, new FilterOptionViewModel
        {
            FacetGroups = new List<FacetGroupOption>(),
            Page = 1,
            PageSize = currentContent.PartialPageSize
        });
    }
}