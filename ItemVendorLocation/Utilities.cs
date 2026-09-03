using CheapLoc;
using System.Collections.Generic;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI;
using ItemInfo = ItemVendorLocation.Models.ItemInfo;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ItemVendorLocation.Models;
using ItemVendorLocation.XIVCommon.Functions.Tooltips;
using System.Linq;
using Lumina.Excel.Sheets;
using Lumina.Excel;

namespace ItemVendorLocation;

internal class Utilities
{
    private static readonly HashSet<string> GameAddonWhitelist = new()
    {
        "CharacterInspect",
        "ChatLog",
        "ColorantColoring",
        "ContentsInfoDetail",
        "DailyQuestSupply",
        "FreeCompanyCreditShop",
        "GrandCompanyExchange",
        "HousingCatalogPreview",
        "HousingGoods",
        "InclusionShop",
        "ItemSearch",
        "Journal",
        "MateriaAttach",
        "MiragePrismPrismBoxCrystallize",
        "RecipeMaterialList",
        "RecipeNote",
        "RecipeTree",
        "ShopExchangeItem",
        "ShopExchangeItemDialog",
        "ShopExchangeCurrency",
        "SubmarinePartsMenu",
        "Tryon",
        "Shop",
    };

    internal static void OutputChatLine(SeString message)
    {
        SeStringBuilder sb = new();
        _ = sb.AddUiForeground("[IVL] ", 45);
        _ = sb.Append(message);
        Service.ChatGui.Print(sb.BuiltString);
    }

    internal static uint CorrectItemId(uint itemId)
    {
        return itemId switch
               {
                   > 1000000 => itemId - 1000000, // hq
                   > 500000 and < 1000000 => itemId - 500000, // collectible, doesnt seem to work
                   _ => itemId,
               };
    }

    internal static unsafe List<(ItemInfo, bool)> GetItemInfoFromContextMenu(IMenuOpenedArgs args)
    {
        var results = new List<(ItemInfo, bool)>();

        if (args.MenuType == ContextMenuType.Inventory)
        {
            var inventoryTarget = (MenuTargetInventory)args.Target;
            if (!inventoryTarget.TargetItem.HasValue)
            {
                return results;
            }

            var itemInfo = Service.Plugin.ItemLookup.GetItemInfo(CorrectItemId(inventoryTarget.TargetItem.Value.ItemId));
            if (itemInfo != null)
                results.Add((itemInfo, false));

            itemInfo = Service.Plugin.ItemLookup.GetItemInfo(CorrectItemId(inventoryTarget.TargetItem.Value.GlamourId));
            if (itemInfo != null)
                results.Add((itemInfo, true));

            return results;
        }

        var addonName = args.AddonName;

        if (string.IsNullOrEmpty(addonName))
        {
            return results;
        }

        if (!GameAddonWhitelist.Contains(addonName))
        {
            return results;
        }

        var defaultTarget = (MenuTargetDefault)args.Target;

        if (defaultTarget.TargetContentId != 0)
        {
            return results;
        }

        uint itemId = 0;
        uint glamorItemId = 0;

        switch (addonName)
        {
            case "RecipeNote":
            {
                // Was: *(uint*)(agent + 0x398). Replaced by the named CS field, which declares
                // the exact same [FieldOffset(0x398)] -- equivalent, but now version-tracked by CS.
                var agent = (AgentRecipeNote*)Service.GameGui.FindAgentInterface(addonName).Address;
                if (agent == null)
                {
                    return results;
                }

                itemId = agent->ContextMenuResultItemId;
                break;
            }
            case "RecipeTree" or "RecipeMaterialList":
            {
                // Was: *(uint*)((nint)agent + 0x28). AgentRecipeItemContext.ResultItemId is
                // declared at [FieldOffset(0x28)] -- equivalent.
                var uiModule = (UIModule*)Service.GameGui.GetUIModule().Address;
                if (uiModule == null)
                {
                    return results;
                }

                var agents = uiModule->GetAgentModule();
                if (agents == null)
                {
                    return results;
                }

                var agent = (AgentRecipeItemContext*)agents->GetAgentByInternalId(AgentId.RecipeItemContext);
                if (agent == null)
                {
                    return results;
                }

                itemId = agent->ResultItemId;
                break;
            }
            case "ColorantColoring":
            {
                // NO NAMED REPLACEMENT: FFXIVClientStructs' AgentColorant declares nothing at
                // 0x3C (its first documented member is CharaView at 0x158), so this stays a raw
                // hardcoded offset with no version guard. If the game shifts this field, we read
                // a neighbouring dword and SILENTLY show the wrong vendor -- there is no error to
                // observe. Re-verify against AgentColorant on every major patch.
                //
                // 離線鑑識(台服 7.20 執行檔)已證實 +0x3C 就是染色視窗當前這件裝備的 item id:
                //   0x140D9DA75: mov esi,[rbx+0x3c]  ->  call 0x1407F40B0 (Item 表取列函式,
                //   內含 HQ -1000000 / 收藏品 -500000 正規化與上界 0xC031)。
                // 讀取終點 0x40 遠小於 agent 槽的最小配置 0x80,所以不會越界;真正的風險是
                // 「靜默顯示錯誤的商人」。
                //
                // ⚠️ 這裡**刻意沒有**版面守衛。看起來可用的不變量「+0x3C 會等於 +0x48 或 +0x4C」
                //    其實會被 0x44 -> 0x3C 的提升路徑打破(0x140D9CD1F 不同步更新 0x48/0x4C),
                //    拿來當守衛只會製造假警報。所以這裡只留診斷,不擋。
                var colorantColoringAgent = Service.GameGui.FindAgentInterface(addonName).Address;
                if (colorantColoringAgent == 0)
                {
                    return results;
                }

                // 遊戲存進 +0x3C 的值可以帶著 HQ / 收藏品偏移(取列函式自己會剝),所以這裡
                // 必須先正規化,否則 HQ / 收藏品的情況會靜默查不到商人。
                var rawColorantItemId = *(uint*)(colorantColoringAgent + 0x3C);
                itemId = CorrectItemId(rawColorantItemId);

                // 非 0 卻連 Item 表都查不到 = 我們八成讀到了鄰居的 dword(偏移位移)。
                // 一併印出鄰居方便下一輪重新定位:+0x44 是「待生效的 item id」,
                // +0x48 / +0x4C 是分色槽的副本。Information 級 —— 使用者跑 LogLevel 1。
                if (rawColorantItemId != 0 && !Service.DataManager.GetExcelSheet<Item>().HasRow(itemId))
                {
                    Service.PluginLog.Information(
                        $"ColorantColoring: +0x3C 讀到 {rawColorantItemId} (正規化後 {itemId}),但 Item 表沒有這一列 —— " +
                        $"寫死偏移可能已位移。鄰居 dword: " +
                        $"+0x38={*(uint*)(colorantColoringAgent + 0x38)} " +
                        $"+0x40={*(uint*)(colorantColoringAgent + 0x40)} " +
                        $"+0x44={*(uint*)(colorantColoringAgent + 0x44)} " +
                        $"+0x48={*(uint*)(colorantColoringAgent + 0x48)} " +
                        $"+0x4C={*(uint*)(colorantColoringAgent + 0x4C)}");
                }

                break;
            }
            case "GrandCompanyExchange":
            case "ShopExchangeItem":
            {
                // NO NAMED REPLACEMENT: there is no AgentGrandCompanyExchange struct in
                // FFXIVClientStructs at all, and AgentShop declares nothing at 0x54. Raw offset.
                // base sig:
                //     dt benchmark: 48 8D 4F ? C6 44 24 ? ? 41 83 CF
                //     6.58: 48 8D 4E ? 44 0F B6 4D
                // offset sig: 89 73 ?? 44 88 63 (offset is still the same in dt benchmark)
                //
                // 離線鑑識(台服 7.20 執行檔)已證實 +0x54 就是 item 右鍵選單當前這一項的
                // item id(等於內嵌 helper 的 +0x2C);寫入端 0x140AB48D9,消費端把它 % 500000
                // 之後餵給 GetAgentByInternalId(RecipeNote / GatheringNote)。
                // 讀取終點 0x58 遠小於 agent 槽的最小配置 0x80,所以不會越界;真正的風險是
                // 「靜默顯示錯誤的商人」。
                //
                // 版面守衛:同一段尾巴的同批寫入(0x140AB48C8 / 0x140AB48D9)裡,helper 會把
                // agent->AddonId 抄進 +0x50(ushort),關閉選單時清 0。AgentInterface.AddonId 是
                // FFXIVClientStructs 追蹤中的具名欄位([FieldOffset(0x20)]),所以這是
                // 「寫死偏移 vs CS 追蹤的欄位」對撞 —— 不是兩個寫死偏移互相背書。
                // 對不上就 fail-closed:寧可什麼都不顯示,也不要顯示錯的商人。
                var agentAddress = Service.GameGui.FindAgentInterface(addonName).Address;
                if (agentAddress == 0)
                {
                    return results;
                }

                var agent = (AgentInterface*)agentAddress;
                var trackedAddonId = *(ushort*)(agentAddress + 0x50);
                var expectedAddonId = (ushort)agent->AddonId;
                if (trackedAddonId != expectedAddonId)
                {
                    // Information 級 —— 使用者跑 LogLevel 1,盲區只有 Verbose,Debug 收得到但單檔數十萬行會淹沒。
                    Service.PluginLog.Information(
                        $"{addonName}: 版面守衛不符,略過本次查詢(寧可不顯示也不要顯示錯的商人)。" +
                        $"+0x50={trackedAddonId} 但 AgentInterface.AddonId={expectedAddonId};" +
                        $"當下 +0x54={*(uint*)(agentAddress + 0x54)}");
                    return results;
                }

                // 遊戲存進 +0x54 的值可以帶著 HQ / 收藏品偏移(消費端自己會剝),所以這裡
                // 必須先正規化,否則 HQ / 收藏品的情況會靜默查不到商人。
                itemId = CorrectItemId(*(uint*)(agentAddress + 0x54));
                break;
            }
            case "ChatLog":
            {
                var agent = (AgentChatLog*)Service.GameGui.FindAgentInterface(addonName).Address;
                if (agent == null)
                {
                    return results;
                }

                itemId = agent->ContextItemId;
                break;
            }
            case "ContentsInfoDetail":
            {
                // Was: *(uint*)(agent + 0x17CC). AgentContentsTimer.ContextMenuItemId is declared
                // at [FieldOffset(0x17CC)] -- equivalent. (The "ContentsInfo" addon is driven by
                // Client::UI::Agent::AgentContentsTimer.)
                var agent = (AgentContentsTimer*)Service.GameGui.FindAgentInterface("ContentsInfo").Address;
                if (agent == null)
                {
                    return results;
                }

                itemId = agent->ContextMenuItemId;
                break;
            }
            case "ItemSearch":
            {
                var agent = AgentContext.Instance();
                if (agent == null)
                {
                    return results;
                }

                itemId = CorrectItemId((uint)agent->UpdateCheckerParam);
                break;
            }
            case "CharacterInspect":
            {
                // BUGFIX: this used to read *(int*)(agent + 0x44C). AgentInspect grew in patch 7.3
                // (0x808 -> 0x940) when _glamourItems was inserted, and SelectedItemSlot moved
                // 0x44C -> 0x584. Verified against the live TC 7.20 binary: the client writes the
                // slot to [agent+0x584] and then indexes _items (0x2A8) with a 0x1C stride, e.g.
                //   mov [rdi+0x584], eax | cdqe | imul rcx, rax, 0x1c | movzx r12d, [rcx+rdi+0x2c0]
                // 0x44C now lands inside _glamourItems, so the old code fed an item id (~30000+)
                // to GetInventorySlot as if it were a slot index.
                var agent = (AgentInspect*)Service.GameGui.FindAgentInterface(addonName).Address;
                if (agent == null)
                {
                    return results;
                }

                var selectedSlot = agent->SelectedItemSlot;

                var inventoryManager = InventoryManager.Instance();
                if (inventoryManager == null)
                {
                    return results;
                }

                var container = inventoryManager->GetInventoryContainer(InventoryType.Examine);
                if (container == null || container->Items == null || !container->IsLoaded)
                {
                    return results;
                }

                // Both axes: a negative index passes a bare "Size > index" check.
                if (selectedSlot < 0 || selectedSlot >= container->Size)
                {
                    return results;
                }

                var item = container->GetInventorySlot(selectedSlot);
                if (item == null)
                {
                    return results;
                }

                itemId = CorrectItemId(item->GetItemId());
                glamorItemId = CorrectItemId(item->GlamourId);
                break;
            }
            case "MiragePrismPrismBoxCrystallize":
            {
                var uiModule = (UIModule*)Service.GameGui.GetUIModule().Address;
                if (uiModule == null)
                {
                    return results;
                }

                var agents = uiModule->GetAgentModule();
                if (agents == null)
                {
                    return results;
                }

                var agent = (AgentMiragePrismPrismBox*)agents->GetAgentByInternalId(AgentId.MiragePrismPrismBox);
                if (agent == null || agent->Data == null)
                {
                    return results;
                }

                itemId = CorrectItemId(agent->Data->TempContextItem.ItemId);
                break;
            }
            // TODO: Find itemId offset in AgentInterface, HoveredItem is inaccurate sometimes (maybe?)
            default:
            {
                itemId = CorrectItemId((uint)Service.GameGui.HoveredItem);
                break;
            }
        }

        var info = Service.Plugin.ItemLookup.GetItemInfo(itemId);
        if (info != null)
        {
            results.Add((info, false));
        }

        info = Service.Plugin.ItemLookup.GetItemInfo(glamorItemId);
        if (info != null)
        {
            results.Add((info, true));
        }

        return results;
    }
    
    /// <summary>
    ///     幻影櫃(MiragePrismPrismItemDetail)附加文字用的「商店販售價格」標籤。
    /// </summary>
    /// <remarks>
    ///     這個字串同時是附加內容的開頭,也是「本外掛已經附加過了嗎」的等冪守衛標記
    ///     (見 EntryPoint.OnMiragePrismPrismItemDetailPreDraw,那是每影格 PreDraw)。
    ///     兩邊必須共用同一個字串,否則翻譯之後每一影格都會再附加一次。
    ///     繁中用語取自遊戲自己的 Addon 表第 1693 列「商店販售價格」。
    /// </remarks>
    internal static string ShopSellingPriceLabel =>
        Loc.Localize("ShopSellingPrice", "Shop Selling Price");

    internal static unsafe SeString GetToolTipString(uint itemId)
    {
        var itemInfo = Service.Plugin.ItemLookup.GetItemInfo(Utilities.CorrectItemId(itemId));

        if (itemInfo == null)
        {
            return $"{ShopSellingPriceLabel}: {Loc.Localize("NoVendor", "None")}";
        }

        switch (itemInfo.Type)
        {
            case ItemType.GilShop:
                var costStr = itemInfo.NpcInfos[0].Costs[0].Item1.ToString();
                return $"{ShopSellingPriceLabel}: {costStr}";
            case ItemType.GcShop:
                var npcInfos = itemInfo.NpcInfos;
                var playerGC = UIState.Instance()->PlayerState.GrandCompany;
                var otherGcVendorIds = Dictionaries.GcVendorIdMap.Values.Where(i => i != Dictionaries.GcVendorIdMap[playerGC]);
                // Only remove items if doing so doesn't remove all the results
                if (npcInfos.Any(i => !otherGcVendorIds.Contains(i.Id)))
                {
                    _ = npcInfos.RemoveAll(i => otherGcVendorIds.Contains(i.Id));
                }

                var info = npcInfos.First();

                costStr = $"{info.Costs[0].Item2} x{info.Costs[0].Item1}";

                return $"{ShopSellingPriceLabel}: {costStr}";
            case ItemType.SpecialShop:
                return $"{ShopSellingPriceLabel}: {Loc.Localize("SpecialVendor", "Special Vendor")}";
            case ItemType.FcShop:
                info = itemInfo.NpcInfos.First();
                costStr = $"{Loc.Localize("FcCredits", "FC Credits")} x{info.Costs[0].Item1}";
                return $"{ShopSellingPriceLabel}: {costStr}";
            case ItemType.CollectableExchange:
                return $"{ShopSellingPriceLabel}: {Loc.Localize("CollectablesExchangeReward", "Collectables Exchange Reward")}";
            default:
                return $"{ShopSellingPriceLabel}: {Loc.Localize("NoVendor", "None")}";
        }

    }

    internal static Item ConvertCurrency(uint itemId, SpecialShop specialShop)
    {
        var tomestonesItemSheet = Service.DataManager.GetExcelSheet<TomestonesItem>();
        var itemSheet = Service.DataManager.GetExcelSheet<Item>();
        var useCurrencyType = specialShop.UseCurrencyType;

        // hack for Quinnana's special shops (ex. Select Ironwood Lumber)
        if (specialShop.RowId is 1770637 or 1770638)
        {
            useCurrencyType = 16;
        }

        return itemId is >= 8 or 0
            ? itemSheet.GetRow(itemId)
            : useCurrencyType switch
            {
                16 => itemSheet.GetRow((uint)Dictionaries.Currencies[itemId]),
                8 => itemSheet.GetRow(1),
                4 => itemSheet.GetRow(tomestonesItemSheet.First(i => i.Tomestones.Value.RowId == itemId).Item.RowId),
                _ => itemSheet.GetRow(itemId),
            };
    }
}
