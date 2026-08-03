using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ItemVendorLocation.Models;
using Lumina.Excel.Sheets;
using Lumina.Text.Expressions;
using Lumina.Text.ReadOnly;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace ItemVendorLocation;
internal class HighlightMenus : IDisposable
{
    private NpcInfo[] _npcInfo = [];
    private ItemInfo? _itemInfo = null;

    public HighlightMenus()
    {
        Service.Framework.Update += Framework_OnUpdate;
    }

    private unsafe void Framework_OnUpdate(IFramework framework)
    {
        if (!Service.Configuration.HighlightMenuSelections || _npcInfo == null || _npcInfo.Length == 0)
        {
            return;
        }

        HighlightShopAddon();
        HighlightSelectIconStringAddon();
        HighlightSelectStringAddon();
        HighlightInclusionShopAddon();
        HighlightShopExchangeCurrencyAddon();
        HighlightShopExchangeItemAddon();
        HighlightCollectablesShopAddon();
    }

    private unsafe void HighlightShopAddon()
    {
        if (_itemInfo == null)
        {
            return;
        }
        var shopAddonPtr = Service.GameGui.GetAddonByName("Shop");
        if (shopAddonPtr == nint.Zero)
        {
            return;
        }


        var shopAddon = (AtkUnitBase*)shopAddonPtr.Address;

        var itemList = (AtkComponentList*)shopAddon->GetComponentByNodeId(16);

        var bestMatchIndex = uint.MaxValue;

        foreach (uint index in Enumerable.Range(0, itemList->ListLength))
        {
            var listItemRenderer = itemList->ItemRendererList[index].AtkComponentListItemRenderer;

            if (listItemRenderer == null)
            {
                continue;
            }
            var text = (AtkTextNode*)listItemRenderer->GetTextNodeById(3);
            if (text == null)
            {
                continue;
            }
            var itemName = ((ReadOnlySeStringSpan)text->NodeText.AsSpan()).ExtractText();
            // I use a partial matching because I guess item names can be concatenated. I don't think what I came up with
            // is foolproof, but it's good enough for now. I'm trying to figure out if I can use the agent for exact name
            // matches, but what I'm seeing doesn't quite match up with what I see in CS. So until I figure that out, I'm
            // going with this.
            if (string.Equals(_itemInfo.Name, itemName))
            {
                // if we ever find an exact match, that must be it, so highlight it and return.
                text->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
                text->SetText(itemName);
                return;
            }
            // 截斷符號在不同語系客戶端可能是三個半形句點或全形省略號,兩種都認。
            else if (itemName.EndsWith("...") || itemName.EndsWith("…"))
            {
                if (_itemInfo.Name.StartsWith(itemName.TrimEnd('.', '…')))
                {
                    bestMatchIndex = index;
                }
            }
        }

        if (bestMatchIndex != uint.MaxValue)
        {
            var listItemRenderer = itemList->ItemRendererList[bestMatchIndex].AtkComponentListItemRenderer;
            var text = (AtkTextNode*)listItemRenderer->GetTextNodeById(3);
            var itemName = ((ReadOnlySeStringSpan)text->NodeText.AsSpan()).ExtractText();
            text->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
            // strangely, it doesn't seem like the list gets its color updated until we set the text below
            text->SetText(itemName);
        }
    }

    private unsafe void HighlightSelectIconStringAddon()
    {
        var selectIconStringAddonPtr = Service.GameGui.GetAddonByName("SelectIconString");

        if (selectIconStringAddonPtr == nint.Zero)
        {
            return;
        }

        var selectIconStringAddon = (AtkUnitBase*)selectIconStringAddonPtr.Address;

        var componentList = selectIconStringAddon->GetComponentListById(3);

        if (componentList == null)
        {
            return;
        }

        foreach (uint index in Enumerable.Range(0, componentList->ListLength))
        {
            var listItemRenderer = componentList->ItemRendererList[index].AtkComponentListItemRenderer;
            if (listItemRenderer == null)
            {
                continue;
            }
            var text = (AtkTextNode*)listItemRenderer->GetTextNodeById(2);
            if (text == null)
            {
                continue;
            }
            try
            {
                if (_npcInfo.Any(n => n.ShopName == null ? false : n.ShopName.Split("\n").Any(s => string.Equals(s, text->NodeText.ToString()))))
                {
                    text->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
                    return;
                }
            }
            catch (NullReferenceException)
            {
                continue;
            }
        }
    }

    private unsafe void HighlightSelectStringAddon()
    {
        var selectIconStringAddonPtr = Service.GameGui.GetAddonByName("SelectString");

        if (selectIconStringAddonPtr == nint.Zero)
        {
            return;
        }

        var selectIconStringAddon = (AtkUnitBase*)selectIconStringAddonPtr.Address;

        var componentList = selectIconStringAddon->GetComponentListById(3);

        if (componentList == null)
        {
            return;
        }

        foreach (uint index in Enumerable.Range(0, componentList->ListLength))
        {
            var listItemRenderer = componentList->ItemRendererList[index].AtkComponentListItemRenderer;
            if (listItemRenderer == null)
            {
                continue;
            }
            var text = (AtkTextNode*)listItemRenderer->GetTextNodeById(2);
            if (text == null)
            {
                continue;
            }
            try
            {
                if (_npcInfo.Any(n => n.ShopName == null ? false : n.ShopName.Split("\n").Any(s => string.Equals(s, ((ReadOnlySeStringSpan)text->NodeText.AsSpan()).ExtractText()))))
                {
                    text->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
                    return;
                }
            }
            catch (NullReferenceException)
            {
                continue;
            }
        }
    }

    private unsafe void HighlightInclusionShopAddon()
    {
        var inclusionShopAddonPtr = Service.GameGui.GetAddonByName("InclusionShop");

        if (inclusionShopAddonPtr == nint.Zero)
        {
            return;
        }

        var inclusionShopAddon = (AtkUnitBase*)inclusionShopAddonPtr.Address;

        var category = (AtkComponentDropDownList*)inclusionShopAddon->GetComponentByNodeId(7);
        var subcategory = (AtkComponentDropDownList*)inclusionShopAddon->GetComponentByNodeId(9);
        var itemList = (AtkComponentTreeList*)inclusionShopAddon->GetComponentByNodeId(19);

        if (category == null || subcategory == null)
        {
            return;
        }

        foreach (uint index in Enumerable.Range(0, category->List->ListLength))
        {
            var listItemRenderer = category->List->ItemRendererList[index].AtkComponentListItemRenderer;
            if (listItemRenderer == null)
            {
                continue;
            }
            var text = (AtkTextNode*)listItemRenderer->GetTextNodeById(4);
            if (text == null)
            {
                continue;
            }
            var textValue = ((ReadOnlySeStringSpan)text->NodeText.AsSpan()).ExtractText();
            try
            {
                if (_npcInfo.Any(n => n.ShopName == null ? false : n.ShopName.Split("\n").Any(s => string.Equals(s, textValue))))
                {
                    text->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
                    break;
                }
            }
            catch (NullReferenceException)
            {
                continue;
            }
        }
        foreach (uint index in Enumerable.Range(0, subcategory->List->ListLength))
        {
            var listItemRenderer = subcategory->List->ItemRendererList[index].AtkComponentListItemRenderer;
            if (listItemRenderer == null)
            {
                continue;
            }
            var text = (AtkTextNode*)listItemRenderer->GetTextNodeById(4);
            if (text == null)
            {
                continue;
            }
            var textValue = ((ReadOnlySeStringSpan)text->NodeText.AsSpan()).ExtractText();
            try
            {
                if (_npcInfo.Any(n => n.ShopName == null ? false : n.ShopName.Split("\n").Any(s => string.Equals(s, textValue))))
                {
                    text->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
                    break;
                }
            }
            catch (NullReferenceException)
            {
                continue;
            }
        }

        if (itemList == null)
        {
            return;
        }

        foreach (var item in itemList->Items)
        {
            var listItemRenderer = item.Value->Renderer;
            if (listItemRenderer == null)
            {
                continue;
            }
            var text = (AtkTextNode*)listItemRenderer->GetTextNodeById(5);
            if (text == null)
            {
                continue;
            }
            var itemName = ((ReadOnlySeStringSpan)text->NodeText.AsSpan()).ExtractText();
            if (itemName == _itemInfo?.Name)
            {
                text->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
                // strangely, it doesn't seem like the list gets its color updated until we set the text below
                text->SetText(itemName);
                return;
            }
        }
    }

    private unsafe void HighlightShopExchangeCurrencyAddon()
    {
        var shopExchangeCurrencyAddonPtr = Service.GameGui.GetAddonByName("ShopExchangeCurrency");

        if (shopExchangeCurrencyAddonPtr == nint.Zero)
        {
            return;
        }

        var shopExchangeCurrencyAddon = (AtkUnitBase*)shopExchangeCurrencyAddonPtr.Address;


        // highlight tab
        var tabs = (AtkResNode*)shopExchangeCurrencyAddon->GetNodeById(7);

        if (tabs != null)
        {
            AtkResNode* othersTab = tabs->ChildNode;
            AtkResNode* accessoriesTab = othersTab->PrevSiblingNode;
            AtkResNode* armorTab = accessoriesTab->PrevSiblingNode;
            AtkResNode* weaponsTab = armorTab->PrevSiblingNode;
            if (othersTab != null && _itemInfo?.SpecialShopCategory == 4)
            {
                othersTab->GetAsAtkComponentRadioButton()->GetTextNodeById(2)->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
            }
            if (accessoriesTab != null && _itemInfo?.SpecialShopCategory == 3)
            {
                accessoriesTab->GetAsAtkComponentRadioButton()->GetTextNodeById(2)->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
            }
            if (armorTab != null && _itemInfo?.SpecialShopCategory == 2)
            {
                armorTab->GetAsAtkComponentRadioButton()->GetTextNodeById(2)->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
            }
            if (weaponsTab != null && _itemInfo?.SpecialShopCategory == 1)
            {
                weaponsTab->GetAsAtkComponentRadioButton()->GetTextNodeById(2)->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
            }
        }

        // highlight item in list
        var itemList = (AtkComponentTreeList*)shopExchangeCurrencyAddon->GetComponentByNodeId(19);

        if (itemList == null)
        {
            itemList = (AtkComponentTreeList*)shopExchangeCurrencyAddon->GetComponentByNodeId(20);
        }

        if (itemList == null)
        {
            return;
        }

        foreach (var item in itemList->Items)
        {
            var listItemRenderer = item.Value->Renderer;
            if (listItemRenderer == null)
            {
                continue;
            }
            var text = (AtkTextNode*)listItemRenderer->GetTextNodeById(3);
            if (text == null)
            {
                text = (AtkTextNode*)listItemRenderer->GetTextNodeById(8);
            }
            if (text == null)
            {
                continue;
            }
            var itemName = ((ReadOnlySeStringSpan)text->NodeText.AsSpan()).ExtractText();
            if (itemName == _itemInfo?.Name)
            {
                text->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
                // strangely, it doesn't seem like the list gets its color updated until we set the text below
                text->SetText(itemName);
                return;
            }
        }
    }

    private unsafe void HighlightShopExchangeItemAddon()
    {
        var shopExchangeItemAddonPtr = Service.GameGui.GetAddonByName("ShopExchangeItem");

        if (shopExchangeItemAddonPtr == nint.Zero)
        {
            return;
        }

        var shopExchangeItemAddon = (AtkUnitBase*)shopExchangeItemAddonPtr.Address;

        var itemList = (AtkComponentTreeList*)shopExchangeItemAddon->GetComponentByNodeId(20);

        if (itemList == null)
        {
            return;
        }

        foreach (var item in itemList->Items)
        {
            var listItemRenderer = item.Value->Renderer;
            if (listItemRenderer == null)
            {
                continue;
            }
            var text = (AtkTextNode*)listItemRenderer->GetTextNodeById(7);
            if (text == null)
            {
                continue;
            }
            var itemName = ((ReadOnlySeStringSpan)text->NodeText.AsSpan()).ExtractText();
            if (itemName == _itemInfo?.Name)
            {
                text->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
                // strangely, it doesn't seem like the list gets its color updated until we set the text below
                text->SetText(itemName);
                return;
            }
        }
    }

    private unsafe void HighlightCollectablesShopAddon()
    {
        var collectablesShopAddonPtr = Service.GameGui.GetAddonByName("CollectablesShop");

        if (collectablesShopAddonPtr == nint.Zero)
        {
            return;
        }

        var collectablesShopAddon = (AtkUnitBase*)collectablesShopAddonPtr.Address;

        // 原本是 _npcInfo.First(n => n.ShopName.Contains("Oddly Specific Materials Exchange")),
        // 而 ShopName 是以 client 語言即時讀出來的(見 ItemLookup.AddItem.AddCollectablesShop),
        // 台服是繁中 → First() 丟 InvalidOperationException → 被原本的空 catch 吞掉,
        // 兌換商店的職業分類高亮在台服完全不作用且完全靜默。
        // 改成用遊戲自己的 ClassJob 表(語言由 client 決定)解析括號內的職業名,不再比對英文字面。
        NpcInfo? shop = null;
        uint nodeId = 0;
        foreach (var candidate in _npcInfo)
        {
            var resolved = ResolveCollectablesShopNodeId(candidate.ShopName);
            if (resolved == null)
            {
                continue;
            }
            shop = candidate;
            nodeId = resolved.Value;
            break;
        }

        if (shop == null || shop.Costs == null || shop.Costs.Count == 0)
        {
            return;
        }

        // 這個字串是我們自己在 AddCollectablesShop 組出來的英文格式,不是遊戲顯示文字。
        var itemCost = shop.Costs[0].Item2.Split(" min ")[0];

        var radioButton = (AtkComponentRadioButton*)collectablesShopAddon->GetComponentByNodeId(nodeId);
        if (radioButton == null || radioButton->ButtonBGNode == null)
        {
            return;
        }
        radioButton->ButtonBGNode->Color = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);

        var itemList = (AtkComponentTreeList*)collectablesShopAddon->GetComponentByNodeId(28);

        if (itemList == null)
        {
            return;
        }

        foreach (var item in itemList->Items)
        {
            var listItemRenderer = item.Value->Renderer;
            if (listItemRenderer == null)
            {
                continue;
            }
            var text = (AtkTextNode*)listItemRenderer->GetTextNodeById(4);
            if (text == null)
            {
                continue;
            }
            var itemName = ((ReadOnlySeStringSpan)text->NodeText.AsSpan()).ExtractText().Split(" ")[0];
            if (itemName == itemCost)
            {
                text->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
                // strangely, it doesn't seem like the list gets its color updated until we set the text below
                text->SetText(((ReadOnlySeStringSpan)text->NodeText.AsSpan()).ExtractText());
                return;
            }
        }

        //var carpenterButton = (AtkComponentRadioButton*)collectablesShopAddon->GetComponentByNodeId(3);
        //carpenterButton->ButtonBGNode->Color = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
        //var blacksmithButton = (AtkComponentRadioButton*)collectablesShopAddon->GetComponentByNodeId(4);
        //blacksmithButton->ButtonBGNode->Color = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
        //var armorerButton = (AtkComponentRadioButton*)collectablesShopAddon->GetComponentByNodeId(5);
        //armorerButton->ButtonBGNode->Color = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
        //var goldsmithButton = (AtkComponentRadioButton*)collectablesShopAddon->GetComponentByNodeId(6);
        //goldsmithButton->ButtonBGNode->Color = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
        //var leatherworkerButton = (AtkComponentRadioButton*)collectablesShopAddon->GetComponentByNodeId(7);
        //leatherworkerButton->ButtonBGNode->Color = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
        //var weaverButton = (AtkComponentRadioButton*)collectablesShopAddon->GetComponentByNodeId(8);
        //weaverButton->ButtonBGNode->Color = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
        //var alchemistButton = (AtkComponentRadioButton*)collectablesShopAddon->GetComponentByNodeId(9);
        //alchemistButton->ButtonBGNode->Color = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
        //var culinarianButton = (AtkComponentRadioButton*)collectablesShopAddon->GetComponentByNodeId(10);
        //culinarianButton->ButtonBGNode->Color = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
        //var minerButton = (AtkComponentRadioButton*)collectablesShopAddon->GetComponentByNodeId(11);
        //minerButton->ButtonBGNode->Color = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
        //var botanistButton = (AtkComponentRadioButton*)collectablesShopAddon->GetComponentByNodeId(12);
        //botanistButton->ButtonBGNode->Color = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
        //var fisherButton = (AtkComponentRadioButton*)collectablesShopAddon->GetComponentByNodeId(13);
        //fisherButton->ButtonBGNode->Color = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);


        //var itemList = (AtkComponentTreeList*)collectablesShopAddon->GetComponentByNodeId(28);

        //if (itemList == null)
        //{
        //    return;
        //}

        //foreach (var item in itemList->Items)
        //{
        //    var listItemRenderer = item.Value->Renderer;
        //    if (listItemRenderer == null)
        //    {
        //        continue;
        //    }
        //    var text = (AtkTextNode*)listItemRenderer->GetTextNodeById(7);
        //    if (text == null)
        //    {
        //        continue;
        //    }
        //    var itemName = SeString.Parse(text->GetText()).TextValue;
        //    if (itemName == _itemName)
        //    {
        //        text->TextColor = Dalamud.Utility.Numerics.VectorExtensions.ToByteColor(Service.Configuration.ShopHighlightColor);
        //        // strangely, it doesn't seem like the list gets its color updated until we set the text below
        //        text->SetText(SeString.Parse(text->GetText()).TextValue);
        //    }
        //}
    }

    /// <summary>
    ///     CollectablesShop 收藏品交易視窗裡,每個職業的單選按鈕節點 ID。
    ///     鍵是 ClassJob 的 RowId(木工師 8 ~ 漁師 18)。
    /// </summary>
    private static readonly Dictionary<uint, uint> CollectablesShopNodeIdByClassJob = new()
    {
        [8] = 3,   // 木工師 / Carpenter
        [9] = 4,   // 鍛造師 / Blacksmith
        [10] = 5,  // 甲冑師 / Armorer
        [11] = 6,  // 金工師 / Goldsmith
        [12] = 7,  // 皮革師 / Leatherworker
        [13] = 8,  // 裁縫師 / Weaver
        [14] = 9,  // 鍊金術師 / Alchemist
        [15] = 10, // 烹調師 / Culinarian
        [16] = 11, // 採掘師 / Miner
        [17] = 12, // 園藝師 / Botanist
        [18] = 13, // 漁師 / Fisher
    };

    /// <summary>
    ///     從 <see cref="NpcInfo.ShopName" /> 的第二行解析出收藏品交易的職業分頁節點 ID。
    /// </summary>
    /// <remarks>
    ///     ShopName 的格式是「{CollectablesShop.Name}\n{CollectablesShopItemGroup.Name}」,
    ///     兩者都是以 client 語言讀出的遊戲顯示文字。第二行結尾會帶括號職業名,例如
    ///     英文「Oddly Specific Materials Exchange (Carpenter)」、
    ///     台服「交換最終改良用材料（木工師）」。因此只認括號內容,再用遊戲自己的
    ///     ClassJob 表比對——語言由 client 決定,不需要任何寫死的語系字面。
    ///     比不到時才退回上游原本的英文列舉名(Carpenter / Carpentry / ...)。
    /// </remarks>
    private static uint? ResolveCollectablesShopNodeId(string? shopName)
    {
        if (string.IsNullOrEmpty(shopName))
        {
            return null;
        }

        var lines = shopName.Split('\n');
        var group = lines.Length > 1 ? lines[1] : lines[0];

        var open = group.LastIndexOfAny(['(', '（']);
        if (open < 0)
        {
            return null;
        }

        var close = group.IndexOfAny([')', '）'], open + 1);
        var suffix = (close < 0 ? group[(open + 1)..] : group[(open + 1)..close]).Trim();
        if (suffix.Length == 0)
        {
            return null;
        }

        var classJobs = Service.DataManager.GetExcelSheet<ClassJob>();
        if (classJobs != null)
        {
            foreach (var (classJobRowId, nodeId) in CollectablesShopNodeIdByClassJob)
            {
                var row = classJobs.GetRowOrDefault(classJobRowId);
                if (row == null)
                {
                    continue;
                }
                var name = row.Value.Name.ExtractText();
                if (!string.IsNullOrEmpty(name) && string.Equals(name, suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return nodeId;
                }
            }
        }

        // 後備:上游原本的英文列舉名。
        var names = Enum.GetNames<CollectablesShopIconIndex>();
        var index = Array.FindIndex(names, e => string.Equals(e, suffix, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            return (uint)Enum.GetValues<CollectablesShopIconIndex>()[index];
        }

        return null;
    }

    public void SetNpcInfo(NpcInfo[] npcInfos)
    {
        _npcInfo = npcInfos;
    }

    public void SetItemInfo(ItemInfo item)
    {
        _itemInfo = item;
    }

    public void ClearAllInfo()
    {
        _npcInfo = [];
        _itemInfo = null;
    }

    public void Dispose()
    {
        Service.Framework.Update -= Framework_OnUpdate;
    }
}