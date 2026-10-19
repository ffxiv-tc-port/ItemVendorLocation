using System.Linq;
using CheapLoc;
using Dalamud.Interface.Windowing;
using ItemVendorLocation.Models;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Bindings.ImGui;
using ItemVendorLocation.IPC;

namespace ItemVendorLocation.GUI;

public class VendorResultsWindow : Window
{
    private ItemInfo _itemToDisplay;

    /// <summary>
    /// 這一畫格 Lifestream 的狀態。
    /// </summary>
    /// <remarks>
    /// 🔑 每一畫格在 <see cref="Draw"/> 開頭問一次、整張表共用——問狀態是一次跨外掛呼叫，
    /// 逐列去問等於「列數 × 幀率」次 IPC。
    /// </remarks>
    private LifestreamStatus _lifestreamStatus = LifestreamStatus.NotInstalled;

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

                ImGui.SameLine();
                DrawTravelButton(npcInfo, location);
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

    /// <summary>
    /// 「前往」按鈕：交給 Lifestream 把角色帶到這個商人身邊。
    /// </summary>
    /// <remarks>
    /// 🔴 純手動：只有使用者親手按下去才會呼叫，沒有任何自動或事件驅動的觸發。
    /// 🔑 四種狀態刻意分開畫，「問不到」不會被摺成「可以按」——列上看得見「不知道」。
    /// </remarks>
    private void DrawTravelButton(NpcInfo npcInfo, NpcLocation location)
    {
        switch (_lifestreamStatus)
        {
            case LifestreamStatus.NotInstalled:
                ImGui.TextDisabled(Loc.Localize("TravelNeedsLifestream", "Needs Lifestream"));
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Loc.Localize("TravelNeedsLifestreamHelp",
                                                  "Install and enable Lifestream (and the vnavmesh it needs) to turn this into a travel button."));
                }

                return;

            case LifestreamStatus.Unknown:
                ImGui.TextDisabled(Loc.Localize("TravelLifestreamUnknown", "Needs newer Lifestream"));
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Loc.Localize("TravelLifestreamUnknownHelp",
                                                  "Lifestream is installed but did not answer - most likely it is too old to have the endpoint this needs."));
                }

                return;
        }

        var busy = _lifestreamStatus == LifestreamStatus.Busy;

        if (busy)
        {
            ImGui.BeginDisabled();
        }

        // 與地點按鈕同樣的理由：同一列可能有兩個商人站在同一點，label 必須帶 npc id 才不會撞 ImGui id。
        if (ImGui.Button($"{Loc.Localize("TravelButton", "Travel")}###travel{npcInfo.Id}"))
        {
            Service.HighlightObject.SetNpcInfo([npcInfo]);

            // 🔴 location.Y 存的是世界座標 Z（NpcLocation 建構時傳的是 level.X / level.Z），
            //    Lifestream 的第三個參數要的正是世界 Z，不要換成 MapY。
            var accepted = Service.LifestreamIpc.TryGoToMapPoint(location.TerritoryType, location.X, location.Y,
                                                                 Service.Configuration.TravelUseFlying);

            if (!accepted)
            {
                // 回 false ＝ Lifestream 一件事都沒排。不出聲的話使用者會以為它正在跑。
                Service.NotificationManager.AddNotification(new()
                {
                    Content = Loc.Localize("TravelFailed", "Lifestream did not accept the request - nothing was started."),
                    Title = "ItemVendorLocation",
                    Type = NotificationType.Warning,
                });
            }
        }

        if (busy)
        {
            ImGui.EndDisabled();
        }

        // ⚠️ 停用中的項目預設不算 hovered，要帶 AllowWhenDisabled 才問得到，
        //    否則「為什麼按不下去」這條說明剛好在最需要的時候不會出現。
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(busy
                                 ? Loc.Localize("TravelLifestreamBusy", "Lifestream is busy with another trip - wait for it to finish.")
                                 : Loc.Localize("TravelButtonHelp",
                                                "Let Lifestream take you there: it teleports to the nearest aetheryte if needed, then walks (or flies) to the vendor."));
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
        // 整張表共用這一次查詢的結果（見 _lifestreamStatus 的說明）。
        _lifestreamStatus = Service.LifestreamIpc.QueryStatus();

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