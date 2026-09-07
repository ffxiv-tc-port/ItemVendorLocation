using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Numerics;

namespace ItemVendorLocation;

[Serializable]
public class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public ResultsViewType ResultsViewType { get; set; } = ResultsViewType.Multiple;
    public ushort NPCNameChatColor { get; set; } = 67;
    public bool FilterGCResults { get; set; } = false;
    public bool FilterNPCsWithNoLocation { get; set; } = false;
    public bool FilterDuplicates { get; set; } = true;
    public bool ShowShopName { get; set; } = false;
    public ushort MaxSearchResults { get; set; } = 5;
    public bool HighlightSelectedNpc { get; set; } = true;
    public ObjectHighlightColor HighlightColor { get; set; } = ObjectHighlightColor.Red;
    public bool HighlightMenuSelections { get; set; } = true;
    public Vector4 ShopHighlightColor { get; set; } = ImGuiColors.DalamudRed;
    public VirtualKey SearchDisplayModifier { get; set; } = VirtualKey.NO_KEY;

    /// <summary>
    /// 商人結果視窗按下「前往」時，允不允許 Lifestream 用飛行坐騎跑最後一段。
    /// </summary>
    /// <remarks>
    /// 這是新增的設定，沒有「既有行為」可以沿用；預設 true 與 Mappy 的「移動時使用飛行坐騎」一致。
    /// 不可飛的區域由 Lifestream 自己退回地面路線，不會因此失敗。
    /// </remarks>
    public bool TravelUseFlying { get; set; } = true;
#if DEBUG
    public int BuildDebugVendorInfo { get; set; } = 0;
#endif
    public void Save()
    {
        Service.Interface.SavePluginConfig(this);
    }
}