using System.Linq;
using CheapLoc;
using Dalamud.Interface.Windowing;
using ItemVendorLocation.Models;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using ImGuiNET;

namespace ItemVendorLocation.GUI;

public class VendorResultsWindow : Window
{
    private ItemInfo _itemToDisplay;

    public VendorResultsWindow() : base(Loc.Localize("VendorResultsWindowTitle", "Item Vendor Location"))
    {
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new(409, 120),
            MaximumSize = new(-1, -1),
        };
    }

    private void DrawTableRow(NpcInfo npcInfo, string shopName, NpcLocation location, string costStr)
    {
        ImGui.TableNextRow();
        _ = ImGui.TableNextColumn();
#if DEBUG
        ImGui.Text(npcInfo.Id.ToString());
        _ = ImGui.TableNextColumn();
#endif
        ImGui.Text(npcInfo.Name);
        _ = ImGui.TableNextColumn();
        if (Service.Configuration.ShowShopName && _itemToDisplay.HasShopNames())
        {
            ImGui.Text(shopName ?? "");
            _ = ImGui.TableNextColumn();
        }

        if (location != null)
        {
            if (location.TerritoryType == 282)
            {
                ImGui.Text(Loc.Localize("PlayerHousing", "Player Housing"));
            }
            else
            {
                // The <i>Endeavor</i> fix
                string placeString = location.TerritoryExcel.PlaceName.Value.Name.ExtractText();
                placeString = placeString.Replace("\u0002", "");
                placeString = placeString.Replace("\u001a", "");
                placeString = placeString.Replace("\u0003", "");
                placeString = placeString.Replace("\u0001", "");

                placeString = $"{placeString} ({location.MapX:F1}, {location.MapY:F1})";

                // need to use an ID here, the armorer/blacksmith vendors have the same location, resulting in a problem otherwise
                if (ImGui.Button($"{placeString}###{npcInfo.Id}"))
                {
                    Service.HighlightObject.SetNpcInfo([npcInfo]);
                    _ = Service.GameGui.OpenMapWithMapLink(new(location.TerritoryType, location.MapId, location.MapX, location.MapY, 0f));
                }

                var isHoveringButton = ImGui.IsItemHovered();

                if (isHoveringButton)
                {
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                    {
                        ImGui.SetClipboardText(string.Format(Loc.Localize("ClipboardFormat", "{0} -> {1}@{2}, costs {3}"), _itemToDisplay.Name, npcInfo.Name, placeString, costStr));
                        Service.NotificationManager.AddNotification(new()
                        {
                            Content = Loc.Localize("CopiedToClipboard", "Copied vendor info to clipboard"),
                            Title = "ItemVendorLocation",
                            Type = NotificationType.Success,
                        });
                    }
                }
            }
        }
        else
        {
            ImGui.Text(Loc.Localize("NoLocation", "No location"));
        }

        _ = ImGui.TableNextColumn();

        ImGui.Text(costStr);

        if (_itemToDisplay.Type == ItemType.Achievement)
        {
            _ = ImGui.TableNextColumn();
            ImGui.Text(_itemToDisplay.AchievementDescription);
        }
    }

    public override void PreOpenCheck()
    {
        if (_itemToDisplay != null)
        {
            return;
        }

        IsOpen = false;
    }

    public override void Draw()
    {
        ImGui.Text($"{_itemToDisplay.Name} {Loc.Localize("VendorListLabel", "Vendor list:")}");
        ImGuiComponents.HelpMarker(Loc.Localize("RightClickCopyHelp", "You can right-click the button to copy vendor info to clipboard"));

        var columnCount = 3;
#if DEBUG
        columnCount++;
#endif
        if (_itemToDisplay.Type == ItemType.Achievement)
        {
            columnCount++;
        }

        if (Service.Configuration.ShowShopName && _itemToDisplay.HasShopNames())
        {
            columnCount++;
        }

        if (!ImGui.BeginChild("VendorListChild"))
            return;
        if (!ImGui.BeginTable("Vendors", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new(-1, -1)))
            return;
        ImGui.TableSetupScrollFreeze(0, 1);
#if DEBUG
        ImGui.TableSetupColumn("NPC ID");
#endif
        ImGui.TableSetupColumn(Loc.Localize("ColumnNpcName", "NPC Name"));
        if (Service.Configuration.ShowShopName && _itemToDisplay.HasShopNames())
        {
            ImGui.TableSetupColumn(Loc.Localize("ColumnShopName", "Shop Name"));
        }

        ImGui.TableSetupColumn(Loc.Localize("ColumnLocation", "Location"));
        ImGui.TableSetupColumn(_itemToDisplay.Type == ItemType.CollectableExchange ? Loc.Localize("ColumnExchangeRate", "Exchange Rate") : Loc.Localize("ColumnCost", "Cost"));

        if (_itemToDisplay.Type == ItemType.Achievement)
        {
            ImGui.TableSetupColumn(Loc.Localize("ColumnObtainRequirement", "Obtain Requirement"));
        }

        ImGui.TableHeadersRow();

        foreach (var npcInfo in _itemToDisplay.NpcInfos)
        {
            string costStr;
            if (_itemToDisplay.Type == ItemType.CollectableExchange)
            {
                costStr = npcInfo.Costs.Aggregate("", (current, cost) => current + string.Format(Loc.Localize("WillYield", "{0} will yield {1}"), cost.Item2, cost.Item1) + "\n");
            }
            else
            {
                costStr = npcInfo.Costs.Aggregate("", (current, cost) => current + $"{cost.Item2} x{cost.Item1}, ");
                costStr = costStr.Length > 0 ? costStr[..^2] : "";
            }

            DrawTableRow(npcInfo, npcInfo.ShopName, npcInfo.Location, costStr);
        }

        ImGui.EndTable();
        ImGui.EndChild();
    }

    public void SetItemToDisplay(ItemInfo item)
    {
        _itemToDisplay = item;
    }
}