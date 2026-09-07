using System;
using System.Linq;
using Dalamud.Plugin.Ipc;

namespace ItemVendorLocation.IPC;

/// <summary>
/// Lifestream 現在能不能接受一次「帶我過去」的要求。
/// </summary>
/// <remarks>
/// 🔑 刻意做成四態而不是把「問不到」摺成「可以按」——UI 上「不知道」要看得見。
/// 零值是 <see cref="NotInstalled"/>，所以 <c>default</c> 落在最保守的那一態。
/// </remarks>
public enum LifestreamStatus
{
    /// <summary>沒安裝，或安裝了但沒載入。</summary>
    NotInstalled = 0,

    /// <summary>裝了也載入了，但問不到狀態（多半是版本太舊、沒有這些端點）。</summary>
    Unknown = 1,

    /// <summary>正在跑別的行程，這時候插隊沒有意義。</summary>
    Busy = 2,

    /// <summary>可以接受要求。</summary>
    Ready = 3,
}

/// <summary>
/// ItemVendorLocation 對 Lifestream 的單向消費端。
///
/// 只有在使用者親手按下商人結果視窗那一列的「前往」按鈕時才會呼叫，
/// 沒有任何自動或事件驅動的呼叫鏈。
///
/// 🔴 下面這些字串是跨外掛的行為契約，對應 Lifestream/Lifestream/IPC/IPCProvider.cs：
///   Lifestream.IsBusy() -&gt; bool
///   Lifestream.GoToMapPoint(uint territoryId, float worldX, float worldZ, bool fly) -&gt; bool
/// 改名字要兩邊一起改，否則失敗形式是「按鈕靜默變成灰字」而不是報錯。
/// 📌 同一組端點在 Mappy/Mappy/Controllers/LifestreamIpc.cs 也有一份消費端，
///    參數順序與型別以那份與 Lifestream 本體為準，不要各自發明。
/// </summary>
public class LifestreamIpc
{
    private const string LifestreamInternalName = "Lifestream";

    private readonly ICallGateSubscriber<bool> _isBusy;

    /// <summary>
    /// 「走到這張圖上的這個世界座標」。territory／worldX／worldZ／要不要飛。
    /// </summary>
    /// <remarks>
    /// 🔴 第三個參數是世界座標 <b>Z</b>，不是地圖上的 Y。
    /// ItemVendorLocation 的 <see cref="Models.NpcLocation"/> 建構時傳的是
    /// <c>(level.X, level.Z)</c>，所以 <c>NpcLocation.Y</c> 存的就是世界 Z，直接對上。
    /// 回 false 代表 <b>Lifestream 一件事都沒有排</b>（忙碌中、人不能動、沒有 vnavmesh、
    /// 沒有可用的乙太之光…），呼叫端不要傻等。
    /// </remarks>
    private readonly ICallGateSubscriber<uint, float, float, bool, bool> _goToMapPoint;

    public LifestreamIpc()
    {
        _isBusy = Service.Interface.GetIpcSubscriber<bool>("Lifestream.IsBusy");
        _goToMapPoint = Service.Interface.GetIpcSubscriber<uint, float, float, bool, bool>("Lifestream.GoToMapPoint");
    }

    /// <summary>
    /// Lifestream 是否已安裝且載入。沒載入時連 IPC 都不要呼叫。
    /// </summary>
    public static bool IsInstalled
        => Service.Interface.InstalledPlugins.Any(plugin => plugin is { InternalName: LifestreamInternalName, IsLoaded: true });

    /// <summary>
    /// 問一次 Lifestream 現在的狀態。
    /// </summary>
    /// <remarks>
    /// ⚠️ 這會做一次跨外掛呼叫，<b>不要在每一列都叫</b>——呼叫端請每一畫格問一次、整個表格共用。
    /// 舊版 Lifestream 沒有這些端點時 <see cref="ICallGateSubscriber{TRet}.InvokeFunc"/> 會擲
    /// <c>IpcNotReadyError</c>，這裡收斂成 <see cref="LifestreamStatus.Unknown"/>。
    /// </remarks>
    public LifestreamStatus QueryStatus()
    {
        if (!IsInstalled)
        {
            return LifestreamStatus.NotInstalled;
        }

        try
        {
            return _isBusy.InvokeFunc() ? LifestreamStatus.Busy : LifestreamStatus.Ready;
        }
        catch (Exception)
        {
            // 每一畫格都會走到這裡（按鈕要畫成灰字），所以刻意不寫 log，
            // 否則舊版 Lifestream 的使用者會被以幀率洗版。
            return LifestreamStatus.Unknown;
        }
    }

    /// <summary>
    /// 請 Lifestream 把角色送到指定區域的世界座標。
    /// </summary>
    /// <param name="territoryId">目標的 TerritoryType 列號。</param>
    /// <param name="worldX">世界座標 X。</param>
    /// <param name="worldZ">世界座標 Z（<b>不是地圖上的 Y</b>）。</param>
    /// <param name="fly">允許使用飛行坐騎；不可飛的區域由 Lifestream 自己退回用走的。</param>
    /// <returns>
    /// <see langword="true"/> 代表 Lifestream 真的排了工作；
    /// <see langword="false"/> 代表<b>它一件事都沒排</b>（含未安裝、舊版沒有這個端點、
    /// 忙碌中、人不能動、沒裝 vnavmesh），呼叫端要讓使用者知道什麼都沒發生。
    /// </returns>
    public bool TryGoToMapPoint(uint territoryId, float worldX, float worldZ, bool fly)
    {
        if (territoryId is 0)
        {
            return false;
        }

        if (!IsInstalled)
        {
            return false;
        }

        try
        {
            var accepted = _goToMapPoint.InvokeFunc(territoryId, worldX, worldZ, fly);

            Service.PluginLog.Information(
                $"[ItemVendorLocation] 要求 Lifestream 移動到 territory {territoryId} 的 ({worldX:F1}, {worldZ:F1})，" +
                $"允許飛行={fly}，Lifestream 回應={accepted}。");

            return accepted;
        }
        catch (Exception exception)
        {
            // 舊版 Lifestream 沒有這個端點時走到這裡（IpcNotReadyError）。這是使用者親手按下按鈕
            // 之後才會發生的一次性事件，不是每幀路徑，所以寫 Information 讓使用者回報得出來。
            Service.PluginLog.Information(exception, "[ItemVendorLocation] 呼叫 Lifestream.GoToMapPoint 失敗（可能是 Lifestream 版本太舊）。");
            return false;
        }
    }
}
