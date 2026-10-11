using OceanForge.Application.Catalog;
using OceanForge.Contracts.Catalog;
using Xunit;

namespace OceanForge.Application.Tests;

public class BloxFruitsCatalogServiceTests
{
    private readonly IBloxFruitsCatalogService _catalogService = new BloxFruitsWikiCatalogService();

    [Fact]
    public void GetAllItems_ContainsAllSevenWikiCategories()
    {
        var items = _catalogService.GetAllItems();
        Assert.NotEmpty(items);

        // Verify all 7 categories are present
        Assert.Contains(items, i => i.Category == BloxItemCategory.Fruit);
        Assert.Contains(items, i => i.Category == BloxItemCategory.Sword);
        Assert.Contains(items, i => i.Category == BloxItemCategory.Gun);
        Assert.Contains(items, i => i.Category == BloxItemCategory.FightingStyle);
        Assert.Contains(items, i => i.Category == BloxItemCategory.Accessory);
        Assert.Contains(items, i => i.Category == BloxItemCategory.Material);
        Assert.Contains(items, i => i.Category == BloxItemCategory.Race);
    }

    [Fact]
    public void FilterItems_ByFruitAndMythical_ReturnsKitsuneAndDragon()
    {
        var filtered = _catalogService.FilterItems(new CatalogFilterRequest
        {
            Category = BloxItemCategory.Fruit,
            Rarity = ItemRarity.Mythical
        });

        Assert.NotEmpty(filtered);
        Assert.Contains(filtered, i => i.Name.Contains("Kitsune"));
        Assert.Contains(filtered, i => i.Name.Contains("Dragon"));
    }

    [Fact]
    public void GetItemById_WithKnownSword_ReturnsCdk()
    {
        var cdk = _catalogService.GetItemById("sword-cdk");
        Assert.NotNull(cdk);
        Assert.Equal("Cursed Dual Katana (CDK)", cdk.Name);
        Assert.Equal(ItemRarity.Mythical, cdk.Rarity);
    }
}
