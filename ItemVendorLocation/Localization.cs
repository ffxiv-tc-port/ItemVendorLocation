using CheapLoc;
using Dalamud.Game;

namespace ItemVendorLocation;

internal class Localization
{
    public static void SetupLocalization(ClientLanguage language)
    {
        var localizationJson = language switch
                               {
                                   ClientLanguage.Japanese => /*lang=json,strict*/ """
                                                                                   {
                                                                                       "ContextMenuItem": {
                                                                                           "message": "ベンダーロケーション"
                                                                                       }
                                                                                   }
                                                                                   """,
                                   ClientLanguage.German => /*lang=json,strict*/ """
                                                                                 {
                                                                                     "ContextMenuItem": {
                                                                                         "message": "Standort des Anbieters"
                                                                                     }
                                                                                 }
                                                                                 """,
                                   ClientLanguage.French => /*lang=json,strict*/ """
                                                                                 {
                                                                                     "ContextMenuItem": {
                                                                                         "message": "Emplacement du Vendeur"
                                                                                     }
                                                                                 }
                                                                                 """,
                                   (ClientLanguage)4 => ChineseTraditionalJson,
                                   ClientLanguage.English or _ => /*lang=json,strict*/ """
                                                                                       {
                                                                                           "ContextMenuItem": {
                                                                                               "message": "Vendor Location"
                                                                                           }
                                                                                       }
                                                                                       """
                               };

        Loc.Setup(localizationJson);
    }

    // zh-TW (FFXIV TC client reports ClientLanguage 4)
    private const string ChineseTraditionalJson = /*lang=json,strict*/ """
        {
            "ContextMenuItem": { "message": "商人位置" },
            "GlamourSuffix": { "message": "(投影)" },
            "CommandHelpMessage": { "message": "顯示 Item Vendor Location 設定視窗" },
            "NoItemsFound": { "message": " 找不到符合「{0}」的物品" },
            "RefineSearch": { "message": "建議縮小搜尋範圍" },
            "DisplayedMatches": { "message": "已顯示 {0}/{1} 筆符合結果。" },
            "BeMoreSpecific": { "message": "建議使用更精確的關鍵字。" },
            "NoVendorsFound": { "message": "找不到「{0}」的販售商人" },
            "NoNpcLocationFound": { "message": "找不到有地點資訊的販售NPC：" },
            "PurchasedFrom": { "message": " 可向 " },
            "PurchasedAt": { "message": " 購買，地點：" },
            "SpecialVendor": { "message": "特殊商人" },
            "FcCredits": { "message": "公會戰績" },
            "CollectablesExchangeReward": { "message": "收藏品兌換獎勵" },
            "SettingsWindowTitle": { "message": "物品購買地點設定" },
            "FilterDuplicates": { "message": "篩選重複項目" },
            "FilterDuplicatesHelp": { "message": "勾選後，將依地點篩選重複的商人" },
            "FilterGCResults": { "message": "篩選軍隊結果" },
            "FilterGCResultsHelp": { "message": "勾選後，只顯示您所屬軍隊的商人" },
            "FilterNoLocation": { "message": "篩選無地點的結果" },
            "FilterNoLocationHelp": { "message": "勾選後，只顯示有地點資訊的NPC" },
            "ShowShopInfo": { "message": "顯示商店資訊" },
            "ShowShopInfoHelp": { "message": "勾選後，將顯示商店名稱資訊，例如「購買魔法師裝備 - 購買裝備 (Lv. 20-29)」" },
            "HighlightSelectedNpc": { "message": "醒目提示所選NPC" },
            "HighlightSelectedNpcHelp": { "message": "勾選後，將在畫面上出現販售上次搜尋物品的NPC時進行醒目提示" },
            "HighlightColor": { "message": "醒目提示顏色" },
            "HighlightMenuSelections": { "message": "醒目提示選單項目" },
            "HighlightMenuSelectionsHelp": { "message": "勾選後，將醒目提示選單項目以便更容易找到物品。\n\n注意：如果您搜尋另一個由已開啟選單的商人所販售的物品，\n這將導致先前的物品與新的物品同時被醒目提示。這是可以修正的，但唯一的方法\n是每次都以原始顏色重新繪製所有未醒目提示的項目。醒目提示是每一\n影格都會執行的，而我不願意為這種我認為很蠢的情況多加一個每影格迴圈。" },
            "MaxSearchResults": { "message": "最大搜尋結果數" },
            "MaxSearchResultsHelp": { "message": "使用文字指令時的最大搜尋結果數量，以避免聊天欄洗版。\n\n最大允許值為50。" },
            "ResultsViewType": { "message": "結果顯示方式" },
            "ResultsViewTypeHelp": { "message": "插件如何顯示商人地點的搜尋結果。\n\n「Single」將選取第一個結果並印在您的聊天欄中。\n\n「Multiple」將以彈出視窗顯示結果。若保持此設定，插件將維持先前的運作方式不變。" },
            "NPCNameTextColor": { "message": "NPC名稱文字顏色" },
            "NPCNameTextColorHelp": { "message": "使用 /pvendor 搜尋時，NPC名稱的聊天文字顏色。" },
            "ResultsViewTypeModifier": { "message": "結果顯示方式修飾鍵" },
            "ResultsViewTypeModifierHelp": { "message": "按住時將變更結果顯示方式。" },
            "ItemSearchWindowTitle": { "message": "物品商人搜尋" },
            "SearchLabel": { "message": "搜尋：" },
            "VendorResultsWindowTitle": { "message": "物品購買地點" },
            "PlayerHousing": { "message": "玩家房屋" },
            "CopiedToClipboard": { "message": "已複製商人資訊到剪貼簿" },
            "NoLocation": { "message": "無地點資訊" },
            "VendorListLabel": { "message": "商人列表：" },
            "RightClickCopyHelp": { "message": "您可以右鍵點擊按鈕以複製商人資訊到剪貼簿" },
            "ClipboardFormat": { "message": "{0} -> {1}@{2}，花費 {3}" },
            "WillYield": { "message": "{0} 可兌換 {1} 個" },
            "ColumnNpcName": { "message": "NPC名稱" },
            "ColumnShopName": { "message": "商店名稱" },
            "ColumnLocation": { "message": "地點" },
            "ColumnExchangeRate": { "message": "兌換比例" },
            "ColumnCost": { "message": "花費" },
            "ColumnObtainRequirement": { "message": "取得條件" }
        }
        """;
}