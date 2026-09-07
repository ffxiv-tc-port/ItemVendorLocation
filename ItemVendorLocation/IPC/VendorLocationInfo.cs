using System.Collections.Generic;

namespace ItemVendorLocation.IPC;

/// <summary>
/// <c>ItemVendorLocation.GetItemVendorsWorld</c>回傳的一筆商人資料。
/// </summary>
/// <remarks>
/// 🔴 <b>這是跨外掛的資料契約。</b>消費端請宣告一個<b>成員名逐字相同</b>的鏡像型別去接
/// （CallGate 在兩邊型別不同時走 JSON 來回轉換）。名字打錯的失敗形式是<b>那個欄位靜默變成
/// 預設值</b>，不是例外——所以不要「差不多就好」地改名。
/// 消費端的鏡像型別缺欄位是安全的（Newtonsoft 忽略多出來的鍵），多欄位也安全（維持預設值）。
///
/// 📌 要加欄位可以直接加（舊消費端不受影響）；要<b>改既有欄位的型別或語意</b>請開一個新的端點名，
/// 不要同名改形狀。
/// </remarks>
public class VendorLocationInfo
{
    /// <summary>問的是哪一個物品（＝呼叫時傳進來的 item id，原樣回拋方便非同步比對）。</summary>
    public uint ItemId { get; set; }

    /// <summary>商人的 ENpcBase／ENpcResident 列號。</summary>
    public uint NpcId { get; set; }

    /// <summary>商人名稱（客戶端語言）。</summary>
    public string NpcName { get; set; } = "";

    /// <summary>商店名稱；沒有就是空字串（<b>不是 null</b>）。</summary>
    public string ShopName { get; set; } = "";

    /// <summary>
    /// 取得管道：<c>GilShop</c>／<c>SpecialShop</c>／<c>GcShop</c>／<c>Achievement</c>／
    /// <c>FcShop</c>／<c>QuestReward</c>／<c>CollectableExchange</c> 其中之一。
    /// </summary>
    /// <remarks>
    /// 🔑 刻意送<b>列舉的名字字串</b>而不是數值——數值會在列舉插入新成員時整批位移，
    /// 那種錯是靜默的。
    /// </remarks>
    public string SourceType { get; set; } = "";

    /// <summary>
    /// 這個商人有沒有已知的所在位置。
    /// </summary>
    /// <remarks>
    /// 🔑 <see langword="false"/> 時底下所有座標欄位都是 0，<b>那個 0 沒有意義</b>，
    /// 請依這個旗標判斷，不要把 0 當成「在原點」。
    /// </remarks>
    public bool HasLocation { get; set; }

    /// <summary>所在區域的 TerritoryType 列號。</summary>
    public uint TerritoryTypeId { get; set; }

    /// <summary>所在地圖的 Map 列號。</summary>
    public uint MapId { get; set; }

    /// <summary>世界座標 X。</summary>
    public float WorldX { get; set; }

    /// <summary>
    /// 世界座標 Z。
    /// </summary>
    /// <remarks>
    /// 🔴 這是<b>世界座標的 Z</b>，不是地圖上的 Y。<c>Lifestream.GoToMapPoint</c> 的第三個參數
    /// 要的正是這個值。IVL 內部沒有存世界 Y（高度）——資料來源 LGB 的 Level 只取了 X 與 Z，
    /// 需要高度請自己向 vnavmesh 問這個 XZ 底下的地板。
    /// </remarks>
    public float WorldZ { get; set; }

    /// <summary>地圖座標 X（遊戲內地圖上顯示的那組數字）。</summary>
    public float MapX { get; set; }

    /// <summary>地圖座標 Y。</summary>
    public float MapY { get; set; }

    /// <summary>
    /// <see cref="MapX"/>／<see cref="MapY"/> 算得出來嗎。
    /// </summary>
    /// <remarks>
    /// 換算要讀 Map 表的 SizeFactor／Offset，理論上有可能查不到。查不到時這裡是
    /// <see langword="false"/> 而兩個欄位是 0 ——同樣<b>不要把那個 0 當座標</b>。
    /// 世界座標（<see cref="WorldX"/>／<see cref="WorldZ"/>）不受影響，照 <see cref="HasLocation"/> 判斷。
    /// </remarks>
    public bool MapCoordinatesKnown { get; set; }

    /// <summary>要付的代價；可能是空清單（例如任務獎勵）。</summary>
    public List<VendorCostInfo> Costs { get; set; } = [];
}

/// <summary>
/// 一筆代價。<c>3 個「亞拉戈詩學神典石」</c>就是 <c>Amount = 3</c>、
/// <c>CurrencyName = "亞拉戈詩學神典石"</c>。
/// </summary>
/// <remarks>🔴 同 <see cref="VendorLocationInfo"/>：成員名是跨外掛契約。</remarks>
public class VendorCostInfo
{
    /// <summary>數量。</summary>
    public uint Amount { get; set; }

    /// <summary>貨幣／材料名稱（客戶端語言）。</summary>
    public string CurrencyName { get; set; } = "";
}
