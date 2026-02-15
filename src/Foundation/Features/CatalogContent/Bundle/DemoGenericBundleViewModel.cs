using EPiServer.Personalization.Commerce.Tracking;

namespace Foundation.Features.CatalogContent.Bundle;

public class DemoGenericBundleViewModel : GenericBundleViewModel, IEntryViewModelBase
{
    public IEnumerable<Recommendation> AlternativeProducts { get; set; }
    public IEnumerable<Recommendation> CrossSellProducts { get; set; }
}