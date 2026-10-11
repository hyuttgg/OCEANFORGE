using System;
using System.Collections.Generic;

namespace OceanForge.Contracts.Catalog;

public enum BloxItemCategory
{
    Fruit,
    Sword,
    Gun,
    FightingStyle,
    Accessory,
    Material,
    Race
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary,
    Mythical
}

public class BloxCatalogItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public BloxItemCategory Category { get; set; }
    public ItemRarity Rarity { get; set; }
    public string Sea { get; set; } = "All"; // Sea 1, Sea 2, Sea 3, All
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AcquisitionMethod { get; set; } = string.Empty;
    public long BasePriceBeli { get; set; }
    public long BasePriceFragments { get; set; }
    public string BuffStats { get; set; } = string.Empty;
}

public class CatalogFilterRequest
{
    public BloxItemCategory? Category { get; set; }
    public ItemRarity? Rarity { get; set; }
    public string? Sea { get; set; }
    public string? SearchQuery { get; set; }
}
