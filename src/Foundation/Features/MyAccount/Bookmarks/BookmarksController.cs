namespace Foundation.Features.MyAccount.Bookmarks;

/// <summary>
/// A page to list all bookmarks belonging to a customer
/// </summary>
public class BookmarksController : PageController<BookmarksPage>
{
    private readonly IBookmarksService _bookmarksService;

    public BookmarksController(IBookmarksService bookmarksService)
    {
        _bookmarksService = bookmarksService;
    }

    public ActionResult Index(BookmarksPage currentPage)
    {
        var model = new BookmarksViewModel(currentPage)
        {
            Bookmarks = _bookmarksService.Get(),
            CurrentContent = currentPage
        };

        return View(model);
    }

}