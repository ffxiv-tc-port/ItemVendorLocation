using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ItemVendorLocation;

public enum ResultsViewType : byte
{
    Single = 1,
    Multiple = 2,
}

public enum CollectablesShopIconIndex : uint
{
    // 注意:原本寫成「Carpenter, Carpentry = 3」——C# 會把沒有初始值的第一個成員定為 0,
    // 於是 Carpenter = 0(不是 3),用「Carpenter」查到的節點 ID 是 0。明確寫死 3。
    Carpenter = 3, Carpentry = 3,
    Blacksmith, Blacksmithing = 4,
    Armoer, Armoring = 5,
    Goldsmith, Goldsmithing = 6,
    Leatherworker, Leatherworking = 7,
    Weaver, Clothcrafting = 8,
    Alchemist, Alchemy = 9,
    Culinarian, Cooking = 10,
    Miner = 11,
    Botanist = 12,
    Fisher = 13,
}