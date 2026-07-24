using CheapLoc;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using ItemVendorLocation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ItemVendorLocation.GUI;
public class ItemSearchWindow : Window
{
    private string searchName = "";
    private int selectedItem;

    // Cache of the filtered item list, only recomputed when the search string actually
    // changes instead of re-running the LINQ filter over the whole item dictionary every frame.
    private string cachedSearchName;
    private ItemInfo[] cachedFilteredItems = Array.Empty<ItemInfo>();
    private string[] cachedFilteredItemNames = Array.Empty<string>();

    public ItemSearchWindow() : base(Loc.Localize("ItemSearchWindowTitle", "Item Vendor Search"))
    {
        RespectCloseHotkey = true;

        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(740, 400);
    }

    public override void Draw()
    {
        if (cachedSearchName != searchName)
        {
            cachedSearchName = searchName;
            cachedFilteredItems = Service.Plugin.ItemLookup.GetItems()
                .Where(i => i.Value.Name.Contains(searchName, StringComparison.CurrentCultureIgnoreCase))
                .Select(i => i.Value)
                .ToArray();
            cachedFilteredItemNames = cachedFilteredItems.Select(i => i.Name).ToArray();
        }

        ImGui.Text(Loc.Localize("SearchLabel", "Search:"));
        ImGui.SameLine();
        _ = ImGui.InputText("##ItemNameSearchFilter", ref searchName, 60);
        if (ImGui.ListBox("##ItemSearchList", ref selectedItem, cachedFilteredItemNames, cachedFilteredItemNames.Length))
        {
            var item = cachedFilteredItems[selectedItem];
            Service.VendorResultsUi.SetItemToDisplay(item);
            Service.VendorResultsUi.IsOpen = true;
            Service.VendorResultsUi.Collapsed = false;
            Service.VendorResultsUi.CollapsedCondition = ImGuiCond.Once;
        }
    }
}
