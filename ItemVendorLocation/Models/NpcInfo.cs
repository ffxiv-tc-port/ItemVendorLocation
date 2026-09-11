using System;
using System.Collections.Generic;

namespace ItemVendorLocation.Models
{
    public class NpcInfo
    {
        public uint Id;
        public string? Name;
        public string? ShopName;

        /// <summary>
        /// 這個商人賣這件東西時,所屬商店表的列號(0 ＝ 建表時拿不到)。
        /// </summary>
        /// <remarks>
        /// 🔴 單看這個數字沒有意義 —— 各張商店表的列號會互撞,
        /// 必須配 <see cref="ShopSheetName"/> 一起解讀。
        ///
        /// ⚠️ 同一個商人可能透過好幾家店賣同一件東西,而 <c>AddItem_Internal</c> 對
        /// 「同一個 (item, npc)」只留<b>第一筆</b>(後續 <c>NpcInfos.Find</c> 命中就不再加),
        /// 所以這裡存的是<b>第一個被掃到的店</b>,不是全部。
        /// </remarks>
        public uint ShopId;

        /// <summary>
        /// <see cref="ShopId"/> 是哪一張表的列號;拿不到時為 <see langword="null"/>。
        /// 值域見 <see cref="ShopSheets"/>。
        /// </summary>
        public string? ShopSheetName;

        public List<Tuple<uint, string>>? Costs;
        public NpcLocation? Location;
    }

    /// <summary>
    /// <see cref="NpcInfo.ShopSheetName"/> 的值域。
    /// </summary>
    /// <remarks>
    /// 🔴 這是跨外掛契約的一部分(會原樣送進 <c>GetItemVendorsWorld</c>),
    /// <b>字串內容不要改</b>。刻意用常數而不是在各呼叫點寫字面值 ——
    /// 字面值打錯不會編譯錯,失敗形式是消費端靜默分不出是哪張表。
    /// </remarks>
    public static class ShopSheets
    {
        public const string GilShop = "GilShop";
        public const string SpecialShop = "SpecialShop";
        public const string GcShop = "GCShop";
        public const string FccShop = "FccShop";
        public const string CollectablesShop = "CollectablesShop";
        public const string QuestClassJobReward = "QuestClassJobReward";
    }
}
