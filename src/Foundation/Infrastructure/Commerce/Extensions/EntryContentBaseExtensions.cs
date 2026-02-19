using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Find.Commerce.Services.Internal;
using Foundation.Infrastructure.Cms;
using Mediachase.Commerce.InventoryService;
using Mediachase.Commerce.Markets;
using System.Diagnostics.CodeAnalysis;

namespace Foundation.Infrastructure.Commerce.Extensions;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Extension methods")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Extension methods")]
public static class EntryContentBaseExtensions
{
    private const int _maxHistory = 10;
    private const string _delimiter = "^!!^";

    private static readonly Lazy<IInventoryService> _inventoryService = new(() => ServiceLocator.Current.GetInstance<IInventoryService>());

    private static readonly Lazy<ReferenceConverter> _referenceConverter = new(() => ServiceLocator.Current.GetInstance<ReferenceConverter>());

    private static readonly Lazy<IPriceService> _priceService = new(() => ServiceLocator.Current.GetInstance<IPriceService>());

    private static readonly Lazy<UrlResolver> _urlResolver = new(() => ServiceLocator.Current.GetInstance<UrlResolver>());

    private static readonly Lazy<ICookieService> _cookieService = new(() => ServiceLocator.Current.GetInstance<ICookieService>());

    private static readonly Lazy<ICurrentMarket> _currentMarket = new(() => ServiceLocator.Current.GetInstance<ICurrentMarket>());

    private static readonly Lazy<IMarketService> _marketService = new(() => ServiceLocator.Current.GetInstance<IMarketService>());

    private static readonly Lazy<IRelationRepository> _relationRepository = new(() => ServiceLocator.Current.GetInstance<IRelationRepository>());

    private static readonly Lazy<IContentLoader> _contentLoader = new(() => ServiceLocator.Current.GetInstance<IContentLoader>());

    public static IEnumerable<Inventory> Inventories(this EntryContentBase entryContentBase)
    {
        switch (entryContentBase)
        {
            case ProductContent productContent:
                {
                    var variations = _contentLoader.Value
                        .GetItems(productContent.GetVariants(_relationRepository.Value), productContent.Language)
                        .OfType<VariationContent>();
                    return variations.SelectMany(x => x.GetStockPlacement());
                }
            case PackageContent packageContent:
                return packageContent.ContentLink.GetStockPlacements();
            case VariationContent variationContent:
                return variationContent.ContentLink.GetStockPlacements();
            default:
                return [];
        }
    }

    public static decimal DefaultPrice(this EntryContentBase entryContentBase)
    {
        var market = _marketService.Value.GetAllMarkets()
            .FirstOrDefault(x => x.DefaultLanguage.Name.Equals(entryContentBase.Language.Name));

        if (market == null)
        {
            return 0m;
        }

        var minPrice = new Price();
        switch (entryContentBase)
        {
            case ProductContent productContent:
                {
                    var variationLinks = productContent.GetVariants(_relationRepository.Value);
                    foreach (var variationLink in variationLinks)
                    {
                        var defaultPrice =
                            variationLink.GetDefaultPrice(market.MarketId, market.DefaultCurrency, DateTime.UtcNow);

                        if ((defaultPrice.UnitPrice.Amount < minPrice.UnitPrice.Amount && defaultPrice.UnitPrice.Amount > 0) ||
                            minPrice.UnitPrice.Amount == 0)
                        {
                            minPrice = defaultPrice;
                        }
                    }

                    return minPrice.UnitPrice.Amount;
                }
            case PackageContent packageContent:
                return packageContent.ContentLink
                    .GetDefaultPrice(market.MarketId, market.DefaultCurrency, DateTime.UtcNow)?.UnitPrice
                    .Amount ?? 0m;
            case VariationContent variationContent:
                return variationContent.ContentLink
                    .GetDefaultPrice(market.MarketId, market.DefaultCurrency, DateTime.UtcNow)?.UnitPrice
                    .Amount ?? 0m;
            default:
                return 0m;
        }
    }

    public static IEnumerable<Price> Prices(this EntryContentBase entryContentBase)
    {
        var market = _currentMarket.Value.GetCurrentMarket();

        if (market == null)
        {
            return [];
        }

        var priceFilter = new PriceFilter { CustomerPricing = [CustomerPricing.AllCustomers] };

        switch (entryContentBase)
        {
            case ProductContent productContent:
                {
                    var variationLinks = productContent.GetVariants();
                    return variationLinks.GetPrices(market.MarketId, priceFilter);
                }
            case PackageContent packageContent:
                return packageContent.ContentLink.GetPrices(market.MarketId, priceFilter);
            case VariationContent variationContent:
                return variationContent.ContentLink.GetPrices(market.MarketId, priceFilter);
            default:
                return [];
        }
    }

    public static IEnumerable<VariationContent> VariationContents(this ProductContent productContent)
    {
        return _contentLoader.Value
            .GetItems(productContent.GetVariants(_relationRepository.Value), productContent.Language)
            .OfType<VariationContent>();
    }

    public static IEnumerable<string> Outline(this EntryContentBase productContent)
    {
        var nodes = _contentLoader.Value
            .GetItems(productContent.GetNodeRelations().Select(x => x.Parent), productContent.Language)
            .OfType<NodeContent>();

        return nodes.Select(x => GetOutlineForNode(x.Code));
    }

    public static int SortOrder(this EntryContentBase productContent)
    {
        var node = productContent.GetNodeRelations().FirstOrDefault();
        return node?.SortOrder ?? 0;
    }

    public static CatalogKey GetCatalogKey(this EntryContentBase productContent) => new(productContent.Code);

    public static CatalogKey GetCatalogKey(this ContentReference contentReference) => new(_referenceConverter.Value.GetCode(contentReference));

    public static ItemCollection<Inventory> GetStockPlacements(this ContentReference contentLink)
    {
        var code = contentLink.ToReferenceWithoutVersion().GetCode();
        return string.IsNullOrEmpty(code)
            ? []
            : new ItemCollection<Inventory>(_inventoryService.Value.QueryByEntry([code]).Select(x =>
                new Inventory(x) { ContentLink = contentLink }));
    }

    public static Price GetDefaultPrice(this ContentReference contentLink, MarketId marketId, Currency currency, DateTime validOn)
    {
        var catalogKey = new CatalogKey(_referenceConverter.Value.GetCode(contentLink));

        var priceValue = _priceService.Value.GetPrices(marketId, validOn, catalogKey, new() { Currencies = [currency] })
            .OrderBy(x => x.UnitPrice).FirstOrDefault();
        return priceValue == null ? new() : new Price(priceValue);
    }

    public static IEnumerable<Price> GetPrices(this ContentReference entryContents,
        MarketId marketId, PriceFilter priceFilter) => new[] { entryContents }.GetPrices(marketId, priceFilter);

    public static IEnumerable<Price> GetPrices(this IEnumerable<ContentReference> entryContents, MarketId marketId, PriceFilter priceFilter)
    {
        var customerPricingList = priceFilter.CustomerPricing != null
            ? priceFilter.CustomerPricing.Where(x => x != null).ToList()
            : Enumerable.Empty<CustomerPricing>().ToList();

        var entryContentsList = entryContents.Where(x => x != null).ToList();

        var catalogKeys = entryContentsList.Select(GetCatalogKey).ToList();
        IEnumerable<IPriceValue> priceCollection;
        if (marketId == MarketId.Empty && (customerPricingList.Count == 0 ||
                                           customerPricingList.Any(x => string.IsNullOrEmpty(x.PriceCode))))
        {
            priceCollection = _priceService.Value.GetCatalogEntryPrices(catalogKeys);
        }
        else
        {
            var customerPricingWithPriceCode =
                customerPricingList.Where(x => !string.IsNullOrEmpty(x.PriceCode)).ToList();
            if (customerPricingWithPriceCode.Count != 0)
            {
                priceFilter.CustomerPricing = customerPricingWithPriceCode;
            }

            priceCollection = _priceService.Value.GetPrices(marketId, DateTime.UtcNow, catalogKeys, priceFilter).ToList();

            // if the entry has no price without sale code
            if (!priceCollection.Any())
            {
                priceCollection = _priceService.Value.GetCatalogEntryPrices(catalogKeys)
                    .Where(x => x.ValidFrom <= DateTime.Now && (!x.ValidUntil.HasValue || x.ValidUntil.Value >= DateTime.Now))
                    .Where(x => x.MarketId == marketId);
            }
        }

        return priceCollection.Select(x => new Price(x));
    }

    public static string GetCode(this ContentReference contentLink) => _referenceConverter.Value.GetCode(contentLink);

    public static EntryContentBase GetEntryContent(this CatalogKey catalogKey)
    {
        var entryContentLink = _referenceConverter.Value
            .GetContentLink(catalogKey.CatalogEntryCode, CatalogContentType.CatalogEntry);

        return _contentLoader.Value.Get<EntryContentBase>(entryContentLink);
    }

    public static IEnumerable<VariationContent> GetAllVariants(this ContentReference contentLink)
    {
        return contentLink.GetAllVariants<VariationContent>();
    }

    public static IEnumerable<T> GetAllVariants<T>(this ContentReference contentLink) where T : VariationContent
    {
        switch (_referenceConverter.Value.GetContentType(contentLink))
        {
            case CatalogContentType.CatalogNode:
                var children = _contentLoader.Value.GetChildren<CatalogContentBase>(contentLink, [LanguageLoaderOption.FallbackWithMaster()]).ToList();

                var variants = children.OfType<T>().ToList();
                var products = children.OfType<ProductContent>();
                foreach (var productContent in products)
                {
                    variants.AddRange(productContent.GetVariants()
                        .Select(c => _contentLoader.Value.Get<T>(c)));
                }

                return variants;

            case CatalogContentType.CatalogEntry:

                var entryContent = _contentLoader.Value.Get<EntryContentBase>(contentLink);

                switch (entryContent)
                {
                    case ProductContent p:
                        return p.GetVariants().Select(c => _contentLoader.Value.Get<T>(c));
                    case T entryContentBase:
                        return [entryContentBase];
                }

                break;
        }

        return [];
    }

    private static string GetOutlineForNode(string nodeCode)
    {
        if (string.IsNullOrEmpty(nodeCode))
        {
            return "";
        }

        var outline = nodeCode;
        var currentNode = _contentLoader.Value.Get<NodeContent>(_referenceConverter.Value.GetContentLink(nodeCode));
        var parent = _contentLoader.Value.Get<CatalogContentBase>(currentNode.ParentLink);
        while (!ContentReference.IsNullOrEmpty(parent.ParentLink))
        {
            outline = parent switch
            {
                CatalogContent catalog => string.Format("{1}/{0}", outline, catalog.Name),
                NodeContent parentNode => string.Format("{1}/{0}", outline, parentNode.Code),
                _ => outline,
            };

            parent = _contentLoader.Value.Get<CatalogContentBase>(parent.ParentLink);
        }

        return outline;
    }

    public static string GetUrl(this EntryContentBase entry) => entry.GetUrl(_relationRepository.Value, _urlResolver.Value);

    public static string GetUrl(this EntryContentBase entry, IRelationRepository linksRepository, UrlResolver urlResolver)
    {
        var productLink = entry is VariationContent
            ? entry.GetParentProducts(linksRepository).FirstOrDefault()
            : entry.ContentLink;

        if (productLink == null)
        {
            return string.Empty;
        }

        var urlBuilder = new UrlBuilder(urlResolver.GetUrl(productLink));

        if (entry.Code != null && entry is VariationContent)
        {
            urlBuilder.QueryCollection.Add("variationCode", entry.Code);
        }

        return urlBuilder.ToString();
    }

    public static void AddBrowseHistory(this EntryContentBase entry)
    {
        var history = _cookieService.Value.Get("BrowseHistory");
        var values = string.IsNullOrEmpty(history)
            ? []
            : history.Split([_delimiter], StringSplitOptions.RemoveEmptyEntries).ToList();

        if (values.Contains(entry.Code))
        {
            return;
        }

        if (values.Count != 0)
        {
            if (values.Count == _maxHistory)
            {
                values.RemoveAt(0);
            }
        }

        values.Add(entry.Code);

        _cookieService.Value.Set("BrowseHistory", string.Join(_delimiter, values));
    }

    public static IList<EntryContentBase> GetBrowseHistory()
    {
        var entryCodes = _cookieService.Value.Get("BrowseHistory");
        if (string.IsNullOrEmpty(entryCodes))
        {
            return new List<EntryContentBase>();
        }

        var contentLinks = _referenceConverter.Value.GetContentLinks(entryCodes.Split([
            _delimiter,
        ], StringSplitOptions.RemoveEmptyEntries));

        return _contentLoader.Value.GetItems(contentLinks.Select(x => x.Value), [])
            .OfType<EntryContentBase>()
            .ToList();
    }
}