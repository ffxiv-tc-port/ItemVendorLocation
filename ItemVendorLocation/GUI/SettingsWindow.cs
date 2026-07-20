using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using Dalamud.Interface.Components;
using Lumina.Excel.Sheets;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Dalamud.Interface.Style;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace ItemVendorLocation.GUI;

public class SettingsWindow : Window
{
    public SettingsWindow() : base("物品購買地點設定")
    {
        RespectCloseHotkey = true;

        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(740, 200);
    }

    public override void Draw()
    {
#if DEBUG
        ImGui.SetNextItemWidth(200f);
        var num = Service.Configuration.BuildDebugVendorInfo;
        if (ImGui.InputInt("NPC ID", ref num))
        {
            Service.Configuration.BuildDebugVendorInfo = num;
            Service.Configuration.Save();
        }
        if (ImGui.Button("Build Debug Vendor Info"))
        {
            Service.Plugin.ItemLookup.BuildDebugVendorInfo((uint)num);
        }
        ImGui.SameLine();
        if (ImGui.Button("Build NPC location")) // DEBUG-only, not translated
        {
            Service.Plugin.ItemLookup.BuildDebugNpcLocation((uint)num);
        }
#endif
        var filterDuplicates = Service.Configuration.FilterDuplicates;
        if (ImGui.Checkbox("篩選重複項目", ref filterDuplicates))
        {
            Service.Configuration.FilterDuplicates = filterDuplicates;
            Service.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(@"勾選後，將依地點篩選重複的商人");

        var filterGCResults = Service.Configuration.FilterGCResults;
        if (ImGui.Checkbox("篩選軍隊結果", ref filterGCResults))
        {
            Service.Configuration.FilterGCResults = filterGCResults;
            Service.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(@"勾選後，只顯示您所屬軍隊的商人");

        var filterNPCsWithNoLocation = Service.Configuration.FilterNPCsWithNoLocation;
        if (ImGui.Checkbox("篩選無地點的結果", ref filterNPCsWithNoLocation))
        {
            Service.Configuration.FilterNPCsWithNoLocation = filterNPCsWithNoLocation;
            Service.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(@"勾選後，只顯示有地點資訊的NPC");

        var showShopName = Service.Configuration.ShowShopName;
        if (ImGui.Checkbox("顯示商店資訊", ref showShopName))
        {
            Service.Configuration.ShowShopName = showShopName;
            Service.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(@"勾選後，將顯示商店名稱資訊，例如「購買魔法師裝備 - 購買裝備 (Lv. 20-29)」");

        var highlightSelectedNpc = Service.Configuration.HighlightSelectedNpc;
        if (ImGui.Checkbox("醒目提示所選NPC", ref highlightSelectedNpc))
        {
            Service.Configuration.HighlightSelectedNpc = highlightSelectedNpc;
            Service.Framework.Run(() => Service.HighlightObject.ToggleHighlight(highlightSelectedNpc));
            Service.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(@"勾選後，將在畫面上出現販售上次搜尋物品的NPC時進行醒目提示");
        ImGui.SameLine();
        var highlightColorNames = Enum.GetNames<ObjectHighlightColor>();
        var highlightColorValues = Enum.GetValues<ObjectHighlightColor>();
        var selectedHighlightColor = Array.IndexOf(highlightColorValues, Service.Configuration.HighlightColor);
        ImGui.SetNextItemWidth(150f);
        if (ImGui.Combo("醒目提示顏色", ref selectedHighlightColor, highlightColorNames, highlightColorNames.Length))
        {
            Service.Configuration.HighlightColor = (ObjectHighlightColor)selectedHighlightColor;
            Service.Configuration.Save();
        }

        var highlightMenuSelections = Service.Configuration.HighlightMenuSelections;
        if (ImGui.Checkbox("醒目提示選單項目", ref highlightMenuSelections))
        {
            Service.Configuration.HighlightMenuSelections = highlightMenuSelections;
            Service.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(@"勾選後，將醒目提示選單項目以便更容易找到物品。

注意：如果您搜尋另一個由已開啟選單的商人所販售的物品，
這將導致先前的物品與新的物品同時被醒目提示。這是可以修正的，但唯一的方法
是每次都以原始顏色重新繪製所有未醒目提示的項目。醒目提示是每一
影格都會執行的，而我不願意為這種我認為很蠢的情況多加一個每影格迴圈。");
        ImGui.SameLine();
        // this part seems dumb to me, but it works
        var selectedShopHighlightColor = Service.Configuration.ShopHighlightColor;
        ImGui.SetNextItemWidth(150f);
        selectedShopHighlightColor = ImGuiComponents.ColorPickerWithPalette(1, "醒目提示顏色", selectedShopHighlightColor, ImGuiColorEditFlags.NoAlpha);
        if (selectedShopHighlightColor != Service.Configuration.ShopHighlightColor)
        {
            Service.Configuration.ShopHighlightColor = selectedShopHighlightColor;
            Service.Configuration.Save();
        }

        ImGui.SetNextItemWidth(200f);
        int maxSearchResults = Service.Configuration.MaxSearchResults;
        if (ImGui.InputInt("最大搜尋結果數", ref maxSearchResults))
        {
            if (maxSearchResults is <= 50 and >= 1)
            {
                Service.Configuration.MaxSearchResults = (ushort)maxSearchResults;
                Service.Configuration.Save();
            }
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(@"使用文字指令時的最大搜尋結果數量，以避免聊天欄洗版。

最大允許值為50。");

        var resultsViewTypeNames = Enum.GetNames<ResultsViewType>();
        var resultsViewTypeValues = Enum.GetValues<ResultsViewType>();
        var selectedResultsViewType = Array.IndexOf(resultsViewTypeValues, Service.Configuration.ResultsViewType);
        ImGui.SetNextItemWidth(200f);
        if (ImGui.Combo("結果顯示方式", ref selectedResultsViewType, resultsViewTypeNames, resultsViewTypeNames.Length))
        {
            Service.Configuration.ResultsViewType = resultsViewTypeValues[selectedResultsViewType];
            Service.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(@"插件如何顯示商人地點的搜尋結果。

「Single」將選取第一個結果並印在您的聊天欄中。

「Multiple」將以彈出視窗顯示結果。若保持此設定，插件將維持先前的運作方式不變。");

        var uiColors = Service.DataManager.GetExcelSheet<UIColor>().DistinctBy(i => i.ClassicFF).ToList();
        int npcNameChatColor = Service.Configuration.NPCNameChatColor;
        ImGui.SetNextItemWidth(200f);
        // my lame way to allow selection of colors as defined in the UIColor sheet
        if (ImGui.BeginCombo("NPC名稱文字顏色", ""))
        {
            foreach (var color in uiColors)
            {
                var isChecked = Service.Configuration.NPCNameChatColor == color.RowId;
                var reversedColors = ImGui.ColorConvertU32ToFloat4(color.ClassicFF);
                // Seems like the above function reverses the order of the bytes
                // There's got to be a better way to do this, but brain no working :P
                Vector4 correctColors = new()
                {
                    X = reversedColors.W,
                    Y = reversedColors.Z,
                    Z = reversedColors.Y,
                    W = reversedColors.X,
                };
                if (ImGui.Checkbox($"###{color.RowId}", ref isChecked))
                {
                    Service.Configuration.NPCNameChatColor = (ushort)uiColors.Find(i => i.ClassicFF == ImGui.ColorConvertFloat4ToU32(reversedColors)).RowId;
                    Service.Configuration.Save();
                }
                ImGui.SameLine();
                _ = ImGui.ColorEdit4($"", ref correctColors, ImGuiColorEditFlags.None | ImGuiColorEditFlags.NoInputs);
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(@"使用 /pvendor 搜尋時，NPC名稱的聊天文字顏色。");

        var keyNames = Service.KeyState.GetValidVirtualKeys().Select(i => i.GetFancyName()).ToArray();
        keyNames = [.. keyNames.Prepend("None")];
        var keyValues = Service.KeyState.GetValidVirtualKeys().ToArray();
        keyValues = [.. keyValues.Prepend(VirtualKey.NO_KEY)];
        var selectedKey = Array.IndexOf(keyValues, Service.Configuration.SearchDisplayModifier);
        ImGui.SetNextItemWidth(200f);
        if (ImGui.Combo("結果顯示方式修飾鍵", ref selectedKey, keyNames, keyNames.Length))
        {
            Service.Configuration.SearchDisplayModifier = keyValues[selectedKey];
            Service.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(@"按住時將變更結果顯示方式。");
    }
}