using System;
using System.Collections.Generic;
using Dalamud.Plugin.Ipc;

namespace ItemVendorLocation.IPC;

public class ItemVendorLocationIpc : IDisposable
{
    private readonly ICallGateProvider<uint, bool, HashSet<(uint npcId, uint territory, (float x, float y))>?> _getItemInfoProvider;
    private readonly ICallGateProvider<uint, object?> _openUiWithItemId;
    private readonly ICallGateProvider<uint, (uint territory, (float x, float y))?> _getVendorLocation;
    /// <summary>
    /// <c>ItemVendorLocation.GetItemVendorsWorld(uint itemId)</c>
    /// -&gt; <c>List&lt;VendorLocationInfo&gt;?</c>
    /// </summary>
    /// <remarks>
    /// 既有的 <c>GetItemVendors</c> 只給地圖座標而且用巢狀 tuple，消費端很難接；這個端點給的是
    /// <b>世界座標</b>（可以直接餵 <c>Lifestream.GoToMapPoint</c>）＋商人名／商店名／代價，
    /// 型別是有名字的類別，鏡像型別接得乾淨。
    /// 🔴 舊端點刻意<b>原封不動</b>——既有消費端還在用，改形狀要開新名字不是同名改型別。
    /// </remarks>
    private readonly ICallGateProvider<uint, List<VendorLocationInfo>?> _getItemVendorsWorld;
    //private readonly ICallGateProvider<uint, List<uint>?> _getVendorItems;

    public ItemVendorLocationIpc()
    {
        _getItemInfoProvider = Service.Interface.GetIpcProvider<uint, bool, HashSet<(uint npcId, uint territory, (float x, float y))>?>("ItemVendorLocation.GetItemVendors");
        _openUiWithItemId = Service.Interface.GetIpcProvider<uint, object?>("ItemVendorLocation.OpenVendorResults");
        _getVendorLocation = Service.Interface.GetIpcProvider<uint, (uint territory, (float x, float y))?>("ItemVendorLocation.GetVendorLocation");
        _getItemVendorsWorld = Service.Interface.GetIpcProvider<uint, List<VendorLocationInfo>?>("ItemVendorLocation.GetItemVendorsWorld");
        //_getVendorItems = Service.Interface.GetIpcProvider<uint, List<uint>?>("ItemVendorLocation.GetVendorItems");

        RegisterFunctions();
    }

    public void Dispose()
    {
        _getItemInfoProvider.UnregisterFunc();
        _openUiWithItemId.UnregisterFunc();
        _getVendorLocation.UnregisterFunc();
        _getItemVendorsWorld.UnregisterFunc();
        //_getVendorItems.UnregisterFunc();
    }

    private void RegisterFunctions()
    {
        _getItemInfoProvider.RegisterFunc(GetItemVendors);
        _openUiWithItemId.RegisterFunc(OpenVendorResult);
        _getVendorLocation.RegisterFunc(GetVendorLocation);
        _getItemVendorsWorld.RegisterFunc(GetItemVendorsWorld);
        //_getVendorItems.RegisterFunc(GetVendorItems);
    }

    /// <summary>
    /// Allows other plugins to open the IVL results window for a specific item.
    /// </summary>
    /// <param name="itemId">Item ID the window will show results for.</param>
    /// <returns>null</returns>
    private object? OpenVendorResult(uint itemId)
    {
        var itemInfo = Service.Plugin.ItemLookup.GetItemInfo(itemId);
        if (itemInfo == null)
            return null;

        Service.VendorResultsUi.SetItemToDisplay(itemInfo);
        Service.VendorResultsUi.IsOpen = true;

        return null;
    }

    /// <summary>
    /// Allows other plugins to request vendor locations for an item.
    /// </summary>
    /// <param name="itemId">Item ID to get vendor locations for.</param>
    /// <param name="filterNoLocation">If true, will not return vendors that don't have a location.</param>
    /// <returns>HashSet where each row contains an npc ID, the territory ID of where that npc is, and the x, y coordinates where the npc can be found.</returns>
    private static HashSet<(uint npcId, uint territory, (float x, float y))>? GetItemVendors(uint itemId, bool filterNoLocation)
    {
        var itemInfo = Service.Plugin.ItemLookup.GetItemInfo(itemId);
        if (itemInfo == null)
            return null;

        var vendors = new HashSet<(uint npcId, uint territory, (float x, float y))>();

        foreach (var npcInfo in itemInfo.NpcInfos)
        {
            if (npcInfo.Location != null)
            {
                var location = npcInfo.Location;
                vendors.Add((npcInfo.Id, location.TerritoryType, (location.MapX, location.MapY)));
            }
            else if(!filterNoLocation)
            {
                vendors.Add((npcInfo.Id, 0, (0, 0)));
            }
        }

        return vendors;
    }

    /// <summary>
    /// Allows other plugins to get the location of an npc.
    /// </summary>
    /// <param name="npcId">npc ID to get a location for.</param>
    /// <returns>The territory and x, y  coordinates of the NPC if location exists. Null otherwise.</returns>
    private static (uint territory, (float x, float y))? GetVendorLocation(uint npcId)
    {
        var npcLocation = Service.Plugin.ItemLookup.GetNpcLocation(npcId);
        if (npcLocation == null)
            return null;

        return (npcLocation.TerritoryType, (npcLocation.MapX, npcLocation.MapY));
    }
    /// <summary>
    /// 給別的外掛用的商人查詢：一次拿到<b>世界座標</b>與人／店／代價。
    /// </summary>
    /// <param name="itemId">要查的物品 id。</param>
    /// <returns>
    /// <see langword="null"/> 代表 <b>IVL 完全不認得這個物品</b>（沒有任何商人資料）；
    /// 空清單代表認得但這次沒有可回報的商人。兩者刻意分開，消費端才分得出「查不到」與「沒有」。
    /// 每一筆的座標請先看 <see cref="VendorLocationInfo.HasLocation"/>——沒有位置時座標是 0，
    /// 那個 0 不是原點。
    /// </returns>
    /// <remarks>
    /// 🔴 <b>IPC 端點跑在呼叫端的執行緒上</b>，所以這裡只做純資料查詢：
    /// <list type="bullet">
    /// <item><c>_itemDataMap</c>／<c>_npcLocations</c> 在 <c>ItemLookup</c> 建構子裡一次建好，
    /// 之後只讀不寫（<c>BuildDebugVendorInfo</c> 是 <c>#if DEBUG</c>，Release 沒有），
    /// 而 IPC 是在 <c>ItemLookup</c> 建好之後才註冊的。</item>
    /// <item><c>ItemInfo.NpcInfos</c> 這條清單也不會被改——套用篩選的
    /// <c>EntryPoint.ContextMenuCallback</c> 先複製一份再 <c>RemoveAll</c>。
    /// 🔴 <b>那個複製是這個端點的執行緒安全前提</b>，不要改回共用同一個 List。</item>
    /// <item>Lumina 的表快取是 FrozenDictionary／ConcurrentDictionary，讀取本身安全。</item>
    /// </list>
    /// 沒有任何遊戲狀態存取（不碰原生指標、不碰 UIState），所以不需要繞到 framework 執行緒。
    /// </remarks>
    private static List<VendorLocationInfo>? GetItemVendorsWorld(uint itemId)
    {
        var itemInfo = Service.Plugin.ItemLookup.GetItemInfo(itemId);
        if (itemInfo?.NpcInfos == null)
            return null;

        var results = new List<VendorLocationInfo>(itemInfo.NpcInfos.Count);
        var sourceType = itemInfo.Type.ToString();

        foreach (var npcInfo in itemInfo.NpcInfos)
        {
            var entry = new VendorLocationInfo
            {
                ItemId = itemId,
                NpcId = npcInfo.Id,
                NpcName = npcInfo.Name ?? "",
                ShopName = npcInfo.ShopName ?? "",
                SourceType = sourceType,
            };

            if (npcInfo.Costs != null)
            {
                foreach (var cost in npcInfo.Costs)
                {
                    entry.Costs.Add(new() { Amount = cost.Item1, CurrencyName = cost.Item2 ?? "" });
                }
            }

            var location = npcInfo.Location;
            if (location != null)
            {
                entry.HasLocation = true;
                entry.TerritoryTypeId = location.TerritoryType;
                entry.MapId = location.MapId;

                // 🔴 NpcLocation 建構時傳的是 (level.X, level.Z)，所以 Y 存的是世界座標 Z。
                entry.WorldX = location.X;
                entry.WorldZ = location.Y;

                try
                {
                    // MapX/MapY 要解 Map 表的 SizeFactor／Offset。解不開就把
                    // MapCoordinatesKnown 留成 false，讓消費端看得見「不知道」，
                    // 而不是收到一組看起來很合理的 (0, 0)。
                    entry.MapX = location.MapX;
                    entry.MapY = location.MapY;
                    entry.MapCoordinatesKnown = true;
                }
                catch (Exception exception)
                {
                    entry.MapX = 0;
                    entry.MapY = 0;
                    entry.MapCoordinatesKnown = false;
                    Service.PluginLog.Information(
                        exception,
                        $"[ItemVendorLocation] 算不出 npc {npcInfo.Id}（territory {location.TerritoryType}）的地圖座標，該筆只給世界座標。");
                }
            }

            results.Add(entry);
        }

        return results;
    }

    //private static List<uint>? GetVendorItems(uint npcId)
    //{
    //    var npcInfo = Service.Plugin.ItemLookup.GetVendorInfo(npcId);
    //    if (npcInfo == null)
    //        return null;

    //    // TODO: We don't have an easy way to take an npcId and get a list of items
    //}
}
