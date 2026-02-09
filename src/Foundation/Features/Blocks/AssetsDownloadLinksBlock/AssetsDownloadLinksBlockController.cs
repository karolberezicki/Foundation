using EPiServer.Framework.Blobs;
using System.IO;

namespace Foundation.Features.Blocks.AssetsDownloadLinksBlock
{
    [ApiController]
    [Route("[controller]")]
    public class AssetsDownloadLinksBlockController : ControllerBase
    {
        private readonly IContentLoader _contentLoader;

        public AssetsDownloadLinksBlockController(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        [HttpGet("Download/{contentLinkId}")]
        public IActionResult Download(int contentLinkId)
        {
            if (_contentLoader.Get<IContent>(new ContentReference(contentLinkId))
                is MediaData { BinaryData: FileBlob { FilePath: not null } blob } mediaData)
            {
                var routeSegment = mediaData.RouteSegment;
                var extension = Path.GetExtension(blob.FilePath);
                var fileName = routeSegment.EndsWith(extension) ? routeSegment : routeSegment + extension;

                HttpContext.Response.Headers.Append("content-disposition", "attachment;filename=" + Path.GetFileName(fileName));
                return File(System.IO.File.ReadAllBytes(blob.FilePath), "application/octet-stream");
            }

            return null;
        }
    }
}