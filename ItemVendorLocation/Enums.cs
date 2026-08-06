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
    //
    // 🔴 這裡踩過一次,而且是靜默的:原本寫成「Carpenter, Carpentry = 3」,C# 會把沒有初始值的
    // 第一個成員定為 0,於是 Carpenter = 0(不是 3),用「Carpenter」查到的節點 ID 是 0。
    // 🔴 修完那一次之後,底下 7 組仍然靠「前一個成員的顯式值 +1」自動遞增湊出正確配對值
    // (`Blacksmith` 沒有初始值,靠 `Carpentry = 3` 遞增成 4 才對上 `Blacksmithing = 4`)。
    // 只要在中間插入任何一個新項目,整批就會集體位移一格 —— 一樣不會編譯錯、不會丟例外,
    // 只是從此指到隔壁職業的分頁。消費端 HighlightMenus.GetCollectablesShopNodeId 是用
    // Enum.GetNames/GetValues 按索引取值,更看不出哪裡錯了。
    // ⇒ **每個成員都寫上顯式值**,讓插入新項目不再能改動既有成員。新增項目時請照樣寫顯式值。
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