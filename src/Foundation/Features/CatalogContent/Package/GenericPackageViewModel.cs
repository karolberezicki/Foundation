using EPiServer.Personalization.Commerce.Tracking;

namespace Foundation.Features.CatalogContent.Package;

public class GenericPackageViewModel : PackageViewModelBase<GenericPackage>, IEntryViewModelBase
{
    public GenericPackageViewModel()
    {
    }

    public GenericPackageViewModel(GenericPackage fashionPackage) : base(fashionPackage)
    {
    }

    public IEnumerable<Recommendation> AlternativeProducts { get; set; }
    public IEnumerable<Recommendation> CrossSellProducts { get; set; }
}