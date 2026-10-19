using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ItemVendorLocation;

public enum ResultsViewType : byte
{
    Single = 1,
    Multiple = 2,
}

public enum CollectablesShopIconIndex : uint
{
    // 這個列舉是**職業名 → 收藏品交易所分頁節點 ID**的對照表,而且刻意讓每個職業有兩個名字
    // (工匠名 `Carpenter` 與工藝名 `Carpentry`)共用同一個值 —— 消費端拿使用者看到的字串比對,
    // 兩種寫法都要能命中。配對不可拆散。

    // 🔴 **每個成員都寫上顯式值**:沒有初始值的成員會從 0 或前一個成員 +1 遞增,中間插入新項目就整批位移一格 ——
    // 不會編譯錯、不會丟例外,只是指到隔壁職業的分頁(消費端 HighlightMenus.ResolveCollectablesShopNodeId 用
    // Enum.GetNames/GetValues 按索引取值,更看不出哪裡錯)。新增項目時請照樣寫顯式值。
    Carpenter = 3, Carpentry = 3,
    Blacksmith = 4, Blacksmithing = 4,
    Armoer = 5, Armoring = 5,
    Goldsmith = 6, Goldsmithing = 6,
    Leatherworker = 7, Leatherworking = 7,
    Weaver = 8, Clothcrafting = 8,
    Alchemist = 9, Alchemy = 9,
    Culinarian = 10, Cooking = 10,
    Miner = 11,
    Botanist = 12,
    Fisher = 13,
}