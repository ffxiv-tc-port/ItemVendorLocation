# ItemVendorLocation

[ItemVendorLocation](https://github.com/electr0sheep/ItemVendorLocation) 的台服移植版，查詢道具的商人購買地點。現由 [Nuko](https://github.com/NukoOoOoOoO) 持續維護。

## 功能

- **道具視窗販售方式標示**：一般金幣商店價格、軍票兌換價格、特殊商人（非金幣/軍票），或標示「無」代表無商人販售
- **右鍵選單**：道具新增「商人位置」選項，可彈出視窗列出所有商人，或直接在聊天欄印出第一個商人的座標連結
- **前往商人**：商人結果每列有「前往」按鈕，交給 Lifestream 帶你過去（需要 Lifestream 與 vnavmesh；可設定最後一段用飛行坐騎）
- **指令**：`/pvendor <關鍵字>` 依名稱搜尋道具的販售商人（最多顯示 50 筆結果）；`/pvendor` 不帶參數開啟設定視窗
- **篩選與顯示設定**：篩選重複地點、只顯示所屬大國防聯軍的商人、篩選無地點資訊的結果、顯示商店資訊、醒目提示所選 NPC 與選單項目、自訂顏色、最大搜尋結果數、結果顯示方式（彈出視窗/聊天欄）
- **提供給其他插件的 IPC**：`ItemVendorLocation.GetItemVendors`、`GetVendorLocation`、`OpenVendorResults`，以及我方新增、給世界座標與代價的 `GetItemVendorsWorld`

## 台服版的修改

- 新增「前往」按鈕（透過 Lifestream）與 `GetItemVendorsWorld` IPC。
- 修正無人島特殊商店名稱恆為空白、商店標色在店名帶特殊格式時標不出來、跨多張地圖的區域座標偏移。
- 特殊商店出現未收錄的貨幣時顯示「未知」，不再讓整個插件載入失敗。
- 多處降低記憶體用量。
- 介面繁體中文化。

## 需要搭配的插件

- 選用：Lifestream＋vnavmesh（「前往」按鈕）

原作者：[electr0sheep](https://github.com/electr0sheep/ItemVendorLocation)
