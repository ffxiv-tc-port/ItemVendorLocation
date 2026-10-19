using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GrandCompany = FFXIVClientStructs.FFXIV.Client.UI.Agent.GrandCompany;

namespace ItemVendorLocation;
public static class Dictionaries
{
    // ENpcBase IDs that are generic service handlers (Lost Property Administrator 1006004-1006006,
    // Reward Manager 1017613-1017615), not real vendors a player can visit to purchase an item.
    // AddAchievementItem() and quest-reward parsing attribute some items to these as a stand-in
    // "location", which is misleading when surfaced as a purchasable source - excluded here so
    // those items report as having no known vendor instead.
    public static readonly HashSet<uint> KnownNonVendorNpcIds = new()
    {
        1006004, 1006005, 1006006,
        1017613, 1017614, 1017615,
    };

    // SpecialShop 的 ItemCost 代號 -> 真正的貨幣 Item id，只在 UseCurrencyType == 16 時適用。
    // 代號 3 與 5 刻意留空：台服 7.20 沒有任何 type 16 商店用到，對不到真值。
    // 查不到時 ConvertCurrency 回 null 讓呼叫端顯示「未知」，不要猜一個值填進來。
    public static readonly Dictionary<uint, uint> Currencies = new()
        {
            { 1, 28 },
            { 2, 33913 },
            { 4, 33914 },
            { 6, 41784 },
            { 7, 41785 },
        };

    public static readonly Dictionary<uint, uint> ShbFateShopNpc = new()
    {
        { 1027998, 1769957 },
        { 1027538, 1769958 },
        { 1027385, 1769959 },
        { 1027497, 1769960 },
        { 1027892, 1769961 },
        { 1027665, 1769962 },
        { 1027709, 1769963 },
        { 1027766, 1769964 },
    };

    public static readonly Dictionary<byte, uint> GcVendorIdMap = new()
    {
        { 0, 0 },
        { 1, 1002387 },
        { 2, 1002393 },
        { 3, 1002390 },
    };

    public static readonly Dictionary<GrandCompany, uint> OicVendorIdMap = new()
    {
        { GrandCompany.Maelstrom, 1002389 },
        { GrandCompany.TwinAdder, 1000165 },
        { GrandCompany.ImmortalFlames, 1003925 },
        { GrandCompany.None, 0 },
    };
}
