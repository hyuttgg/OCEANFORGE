using System;
using System.Collections.Generic;
using System.Linq;
using OceanForge.Contracts.Catalog;

namespace OceanForge.Application.Catalog;

public interface IBloxFruitsCatalogService
{
    IReadOnlyList<BloxCatalogItem> GetAllItems();
    IReadOnlyList<BloxCatalogItem> GetItemsByCategory(BloxItemCategory category);
    IReadOnlyList<BloxCatalogItem> FilterItems(CatalogFilterRequest request);
    BloxCatalogItem? GetItemById(string id);
}

public class BloxFruitsWikiCatalogService : IBloxFruitsCatalogService
{
    private readonly List<BloxCatalogItem> _items = [];

    public BloxFruitsWikiCatalogService()
    {
        InitializeCatalog();
    }

    private void InitializeCatalog()
    {
        // ═════════════════════════════════════════════════════════════════════
        // 1. TRÁI ÁC QUỶ (BLOX FRUITS)
        // ═════════════════════════════════════════════════════════════════════
        _items.Add(new BloxCatalogItem
        {
            Id = "fruit-kitsune",
            Name = "Kitsune Fruit",
            Category = BloxItemCategory.Fruit,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/9/9f/KitsuneFruit.png/revision/latest",
            Description = "Trái Thần Thoại hệ Beast mạnh bậc nhất Blox Fruits, biến thân thành Cửu Vĩ Hồ với tốc độ di chuyển kinh hoàng và chiêu thức AOE diện rộng.",
            AcquisitionMethod = "Blox Fruit Dealer (8,000,000 Beli / 4,000 Robux) hoặc đổi từ Kitsune Shrine",
            BasePriceBeli = 8000000,
            BasePriceFragments = 4000,
            BuffStats = "+30% Speed, Giảm 20% sát thương nhận vào, Hồi thể lực liên tục"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "fruit-dragon",
            Name = "Dragon Fruit (West/East)",
            Category = BloxItemCategory.Fruit,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/7/7b/DragonFruit.png/revision/latest",
            Description = "Trái Thần Thoại hệ Biến Hình Rồng với sát thương rực lửa, khả năng bay lượn và kháng sát thương tối thượng.",
            AcquisitionMethod = "Blox Fruit Dealer (10,000,000 Beli / 5,000 Robux) sau Update Rework",
            BasePriceBeli = 10000000,
            BasePriceFragments = 5000,
            BuffStats = "+60% Sát thương hỏa ngục, Kháng 40% sát thương vật lý khi hóa rồng"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "fruit-leopard",
            Name = "Leopard Fruit",
            Category = BloxItemCategory.Fruit,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/2/23/LeopardFruit.png/revision/latest",
            Description = "Trái hệ Zoan báo hoa mai với combo cận chiến liên hoàn tốc độ cao, phá vỡ thế phòng thủ của kẻ địch.",
            AcquisitionMethod = "Blox Fruit Dealer (5,000,000 Beli / 3,000 Robux)",
            BasePriceBeli = 5000000,
            BasePriceFragments = 3000,
            BuffStats = "+40% Tốc độ đánh, +15% Sát thương cận chiến"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "fruit-dough",
            Name = "Dough Fruit (Awakened)",
            Category = BloxItemCategory.Fruit,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/3/3a/DoughFruit.png/revision/latest",
            Description = "Trái Mochi Thức Tỉnh huyền thoại cho PVP với các chiêu khống chế làm choáng (Stun Lock) và chiêu Nắm Đấm Bánh Donut.",
            AcquisitionMethod = "Thức tỉnh qua Dough Raid (Cần Dough Chip từ Cake Scientist)",
            BasePriceBeli = 2800000,
            BasePriceFragments = 18500,
            BuffStats = "Ken Haki bypass, Stun liên tục, AOE kéo mục tiêu"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "fruit-buddha",
            Name = "Buddha Fruit (Awakened)",
            Category = BloxItemCategory.Fruit,
            Rarity = ItemRarity.Legendary,
            Sea = "Sea 2",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/0/07/BuddhaFruit.png/revision/latest",
            Description = "Vua cày cấp (PVE Farming): Biến thành Phật Vàng khổng lồ với tầm đánh cận chiến siêu xa và giảm tới 50% sát thương.",
            AcquisitionMethod = "Buddha Raid (Sea 2 / Sea 3)",
            BasePriceBeli = 1200000,
            BasePriceFragments = 14500,
            BuffStats = "+500% Tầm đánh Melee/Sword, Giảm 50% sát thương nhận vào"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "fruit-portal",
            Name = "Portal Fruit",
            Category = BloxItemCategory.Fruit,
            Rarity = ItemRarity.Legendary,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/8/87/PortalFruit.png/revision/latest",
            Description = "Trái dịch chuyển tức thời mở cổng không gian đi khắp các đảo và chiều không gian Dimensional Rift để bắt cóc đối thủ.",
            AcquisitionMethod = "Blox Fruit Dealer (1,900,000 Beli / 2,000 Robux)",
            BasePriceBeli = 1900000,
            BasePriceFragments = 2000,
            BuffStats = "Dịch chuyển toàn bản đồ tức thời, Tàng hình không gian"
        });

        // ═════════════════════════════════════════════════════════════════════
        // 2. KIẾM HUYỀN THOẠI & THẦN THOẠI (SWORDS)
        // ═════════════════════════════════════════════════════════════════════
        _items.Add(new BloxCatalogItem
        {
            Id = "sword-cdk",
            Name = "Cursed Dual Katana (CDK)",
            Category = BloxItemCategory.Sword,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/8/82/CursedDualKatana.png/revision/latest",
            Description = "Song kiếm nguyền rủa tối thượng kết hợp giữa Yama và Tushita. Sở hữu chiêu thức lướt trảm Slayer of Goliath xé toạc không gian.",
            AcquisitionMethod = "Hoàn thành Scroll Trials tại Haunted Castle với Yama 350+ và Tushita 350+ Mastery",
            BasePriceBeli = 0,
            BasePriceFragments = 0,
            BuffStats = "Combo 2 đòn lướt Stun, Sát thương chém cao nhất game"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "sword-shark-anchor",
            Name = "Shark Anchor",
            Category = BloxItemCategory.Sword,
            Rarity = ItemRarity.Legendary,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/4/4e/SharkAnchor.png/revision/latest",
            Description = "Mỏ neo cá mập khổng lồ trục vớt từ đáy biển sâu, có khả năng quăng dây kéo hút đối thủ và tạo xoáy nước cuốn bay kẻ thù.",
            AcquisitionMethod = "Chế tạo Monster Magnet và tiêu diệt Terror Shark 195k HP tại Danger Level 6",
            BasePriceBeli = 0,
            BasePriceFragments = 0,
            BuffStats = "Kéo đối phương về phía mình, AOE xoáy nước biển sâu"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "sword-ttk",
            Name = "True Triple Katana (TTK)",
            Category = BloxItemCategory.Sword,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 2",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/a/a2/TrueTripleKatana.png/revision/latest",
            Description = "Tam kiếm tuyệt kỹ của Roronoa Zoro, hợp nhất từ Shisui, Wando và Saddi.",
            AcquisitionMethod = "Mua từ Mysterious Man trên đỉnh Green Zone (2,000,000 Beli mỗi thanh kiếm + 300 Mastery)",
            BasePriceBeli = 6000000,
            BasePriceFragments = 0,
            BuffStats = "Chém xoáy bão 3 luồng khí kiếm diện rộng"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "sword-fox-lamp",
            Name = "Fox Lamp",
            Category = BloxItemCategory.Sword,
            Rarity = ItemRarity.Legendary,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/1/1d/FoxLamp.png/revision/latest",
            Description = "Đèn hồ ly lửa xanh ma mị thu thập từ đảo Kitsune Island, bắn ra ngọn lửa ma quái và tạo làn khói mê hoặc.",
            AcquisitionMethod = "Cúng 15–25 Azure Embers tại đền Kitsune Shrine",
            BasePriceBeli = 0,
            BasePriceFragments = 0,
            BuffStats = "Đốt cháy sinh lực theo thời gian, Hóa giải phòng thủ"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "sword-dark-blade",
            Name = "Dark Blade (Yoru V3)",
            Category = BloxItemCategory.Sword,
            Rarity = ItemRarity.Mythical,
            Sea = "All",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/6/68/DarkBlade.png/revision/latest",
            Description = "Hắc Kiếm Yoru của Kiếm Khách Mạnh Nhất Thế Giới Dracule Mihawk, có thể nâng cấp lên V2 và V3 với hào quang phát sáng.",
            AcquisitionMethod = "Gamepass 1,200 Robux hoặc nhận quà tặng từ Admin",
            BasePriceBeli = 0,
            BasePriceFragments = 0,
            BuffStats = "Nhát chém khí kiếm xuyên thấu toàn màn hình"
        });

        // ═════════════════════════════════════════════════════════════════════
        // 3. SÚNG (GUNS)
        // ═════════════════════════════════════════════════════════════════════
        _items.Add(new BloxCatalogItem
        {
            Id = "gun-soul-guitar",
            Name = "Soul Guitar",
            Category = BloxItemCategory.Gun,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/b/bc/SoulGuitar.png/revision/latest",
            Description = "Cây đàn ghi-ta linh hồn thần thoại duy nhất trong game: Hút máu hồi phục sinh lực và bắn ra cột sáng thánh thần stun đối thủ.",
            AcquisitionMethod = "Giải mã bí ẩn trăng rằm tại Haunted Castle (500 Bones, 250 Ectoplasm, 1 Dark Fragment, 5000 Frags)",
            BasePriceBeli = 0,
            BasePriceFragments = 5000,
            BuffStats = "Hồi phục 15% máu khi gây sát thương, Stun diện rộng"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "gun-serpent-bow",
            Name = "Serpent Bow",
            Category = BloxItemCategory.Gun,
            Rarity = ItemRarity.Legendary,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/3/36/SerpentBow.png/revision/latest",
            Description = "Cung mãng xà bắn ra những mũi tên mang độc tố nọc rắn ăn mòn và trói buộc đối thủ.",
            AcquisitionMethod = "Tiêu diệt Island Empress tại Hydra Island",
            BasePriceBeli = 0,
            BasePriceFragments = 0,
            BuffStats = "Sát thương độc bào mòn, Gây choáng tức thì"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "gun-acidum-rifle",
            Name = "Acidum Rifle",
            Category = BloxItemCategory.Gun,
            Rarity = ItemRarity.Rare,
            Sea = "Sea 2",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/a/ab/AcidumRifle.png/revision/latest",
            Description = "Súng bắn tỉa axit hủy diệt thể lực và Ken Haki của đối phương cực kỳ hiệu quả trong combo PVP.",
            AcquisitionMethod = "Mở rương bí mật tại Factory sau cuộc đột kích Factory Raid",
            BasePriceBeli = 0,
            BasePriceFragments = 0,
            BuffStats = "Phá Ken Haki tức thì, Sát thương axit ăn mòn"
        });

        // ═════════════════════════════════════════════════════════════════════
        // 4. VÕ THUẬT (FIGHTING STYLES)
        // ═════════════════════════════════════════════════════════════════════
        _items.Add(new BloxCatalogItem
        {
            Id = "style-godhuman",
            Name = "Godhuman",
            Category = BloxItemCategory.FightingStyle,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/2/2e/Godhuman.png/revision/latest",
            Description = "Đỉnh cao võ học loài người kết hợp 5 trường phái võ thuật: Superhuman, Death Step, Sharkman Karate, Electric Claw và Dragon Talon.",
            AcquisitionMethod = "Gặp Ancient Monk tại Floating Turtle (Cần 400 Mastery trên 5 loại võ + nguyên liệu biển)",
            BasePriceBeli = 5000000,
            BasePriceFragments = 5000,
            BuffStats = "Combo lướt bất khả xâm phạm, Cú đấm thần thánh Seventh Dragon phá giáp"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "style-sanguine-art",
            Name = "Sanguine Art",
            Category = BloxItemCategory.FightingStyle,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/5/5a/SanguineArt.png/revision/latest",
            Description = "Nghệ thuật Huyết Ma Đạo của ma cà rồng biển sâu: Hút máu trực tiếp từ tim đối thủ để hồi phục toàn bộ thể lực và sinh mệnh.",
            AcquisitionMethod = "Gặp Shafi tại Tiki Outpost (Cần Leviathan Heart, 20 Demonic Wisps, 20 Vampire Fangs, 5M Beli, 5k Frags)",
            BasePriceBeli = 5000000,
            BasePriceFragments = 5000,
            BuffStats = "Hút máu cực mạnh khi đánh trúng, Tầm thi triển đòn đánh xa"
        });

        // ═════════════════════════════════════════════════════════════════════
        // 5. PHỤ KIỆN (ACCESSORIES)
        // ═════════════════════════════════════════════════════════════════════
        _items.Add(new BloxCatalogItem
        {
            Id = "acc-leviathan-shield",
            Name = "Leviathan Shield",
            Category = BloxItemCategory.Accessory,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/c/c5/LeviathanShield.png/revision/latest",
            Description = "Khiên vảy thủy quái Leviathan tối thượng: Miễn nhiễm sát thương nước biển và tăng máu/phòng thủ lên mức cực đại.",
            AcquisitionMethod = "Chế tạo tại Beast Hunter NPC (1 Mirror Fractal, 30 Leviathan Scales, 10 Electric Wings, 20 Mutant Teeth)",
            BasePriceBeli = 0,
            BasePriceFragments = 0,
            BuffStats = "+1,200 HP, +15% Kháng sát thương Melee/Sword, +90% Kháng sát thương nước biển"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "acc-kitsune-ribbon",
            Name = "Kitsune Ribbon",
            Category = BloxItemCategory.Accessory,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/d/df/KitsuneRibbon.png/revision/latest",
            Description = "Dải lụa hồ ly thần bí tăng tốc độ hồi chiêu và thể lực lên đỉnh điểm.",
            AcquisitionMethod = "Nhận thưởng khi cúng Azure Embers tại Kitsune Island",
            BasePriceBeli = 0,
            BasePriceFragments = 0,
            BuffStats = "+2,500 Energy, +15% Tốc độ di chuyển, +10% Hồi chiêu chiêu thức"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "acc-pale-scarf",
            Name = "Pale Scarf",
            Category = BloxItemCategory.Accessory,
            Rarity = ItemRarity.Legendary,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/f/f4/PaleScarf.png/revision/latest",
            Description = "Khăn choàng của Dough King Katakuri, tăng 2 lượt né tránh Ken Haki và tăng 15% sát thương kiếm/trái.",
            AcquisitionMethod = "Đánh bại Dough King hoặc Cake Prince tại Sea of Treats",
            BasePriceBeli = 0,
            BasePriceFragments = 0,
            BuffStats = "+2 Lượt né Ken Haki, +15% Sát thương Trái ác quỷ & Kiếm, Tầm nhìn Haki x10"
        });

        // ═════════════════════════════════════════════════════════════════════
        // 6. NGUYÊN LIỆU BIỂN SÂU (MATERIALS)
        // ═════════════════════════════════════════════════════════════════════
        _items.Add(new BloxCatalogItem
        {
            Id = "mat-leviathan-heart",
            Name = "Leviathan Heart",
            Category = BloxItemCategory.Material,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/1/14/LeviathanHeart.png/revision/latest",
            Description = "Trái tim rực cháy của thủy quái Leviathan dưới đáy biển sâu Danger 6. Kéo về bờ bằng mỏ neo tàu Beast Hunter.",
            AcquisitionMethod = "Tiêu diệt Leviathan và dùng mũi móc cẩu Beast Hunter kéo tim về Tiki Outpost",
            BasePriceBeli = 0,
            BasePriceFragments = 0,
            BuffStats = "Nguyên liệu chế tạo Sanguine Art và Thuyền Săn Quái Thú"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "mat-terror-eyes",
            Name = "Terror Eyes",
            Category = BloxItemCategory.Material,
            Rarity = ItemRarity.Rare,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/2/2d/TerrorEyes.png/revision/latest",
            Description = "Mắt quỷ của cá mập Terror Shark phát sáng trong bóng đêm đại dương.",
            AcquisitionMethod = "Tiêu diệt Terror Shark tại vùng biển nguy hiểm Danger 3-6",
            BasePriceBeli = 0,
            BasePriceFragments = 0,
            BuffStats = "Nguyên liệu chế tạo Monster Magnet và Terror Jaw"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "mat-dark-fragment",
            Name = "Dark Fragment",
            Category = BloxItemCategory.Material,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 2",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/5/52/DarkFragment.png/revision/latest",
            Description = "Mảnh vỡ bóng tối rơi ra khi tiêu diệt trùm Darkbeard (Râu Đen).",
            AcquisitionMethod = "Tiêu diệt Raid Boss Darkbeard triệu hồi bằng Fist of Darkness",
            BasePriceBeli = 0,
            BasePriceFragments = 0,
            BuffStats = "Nguyên liệu chế tạo Soul Guitar và nâng cấp vũ khí"
        });

        // ═════════════════════════════════════════════════════════════════════
        // 7. CÁC TỘC BIỂN CẢ (RACES & AWAKENING V4)
        // ═════════════════════════════════════════════════════════════════════
        _items.Add(new BloxCatalogItem
        {
            Id = "race-cyborg-v4",
            Name = "Tộc Cyborg V4 (Người Máy)",
            Category = BloxItemCategory.Race,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/c/c2/CyborgV4.png/revision/latest",
            Description = "Thức tỉnh V4 Đền Thời Gian: Phóng điện Energy Control giật liên hoàn mọi kẻ địch xung quanh và vô hiệu hóa Ken Haki của đối thủ.",
            AcquisitionMethod = "Trial of Machines tại Temple of Time dưới ánh trăng tròn",
            BasePriceBeli = 2500000,
            BasePriceFragments = 4000,
            BuffStats = "Xung điện giật lan AOE, Hút máu đối thủ chuyển thành khiên năng lượng"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "race-ghoul-v4",
            Name = "Tộc Ghoul V4 (Ngạ Quỷ)",
            Category = BloxItemCategory.Race,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/d/d6/GhoulV4.png/revision/latest",
            Description = "Thức tỉnh V4 Đền Thời Gian: Màn đêm bóng tối bao trùm làm mù mắt kẻ địch, giảm 40% thời gian hồi chiêu và hút máu cực mạnh.",
            AcquisitionMethod = "Trial of Carnage tại Temple of Time",
            BasePriceBeli = 0,
            BasePriceFragments = 4000,
            BuffStats = "Giảm 40% Cooldown chiêu thức, Che mù tầm nhìn đối phương, Hồi sinh mệnh"
        });

        _items.Add(new BloxCatalogItem
        {
            Id = "race-angel-v4",
            Name = "Tộc Angel V4 (Thiên Thần)",
            Category = BloxItemCategory.Race,
            Rarity = ItemRarity.Mythical,
            Sea = "Sea 3",
            ImageUrl = "https://static.wikia.nocookie.net/roblox-blox-piece/images/f/f0/AngelV4.png/revision/latest",
            Description = "Thức tỉnh V4 Đền Thời Gian: Đôi cánh thiên thần bay lượn trên không, hào quang thiên thần làm bất động và giải trừ khống chế.",
            AcquisitionMethod = "Trial of Kings tại Temple of Time",
            BasePriceBeli = 0,
            BasePriceFragments = 4000,
            BuffStats = "Bay tự do, Hào quang trừng phạt làm choáng kẻ áp sát"
        });
    }

    public IReadOnlyList<BloxCatalogItem> GetAllItems() => _items;

    public IReadOnlyList<BloxCatalogItem> GetItemsByCategory(BloxItemCategory category)
    {
        return _items.Where(i => i.Category == category).ToList();
    }

    public IReadOnlyList<BloxCatalogItem> FilterItems(CatalogFilterRequest request)
    {
        var query = _items.AsEnumerable();

        if (request.Category.HasValue)
            query = query.Where(i => i.Category == request.Category.Value);

        if (request.Rarity.HasValue)
            query = query.Where(i => i.Rarity == request.Rarity.Value);

        if (!string.IsNullOrWhiteSpace(request.Sea) && request.Sea != "All")
            query = query.Where(i => i.Sea.Equals(request.Sea, StringComparison.OrdinalIgnoreCase) || i.Sea == "All");

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            string q = request.SearchQuery;
            query = query.Where(i => i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                     i.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                     i.BuffStats.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }

    public BloxCatalogItem? GetItemById(string id)
    {
        return _items.FirstOrDefault(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}
