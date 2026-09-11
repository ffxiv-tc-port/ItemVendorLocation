using Lumina.Excel.Sheets;
using System;

namespace ItemVendorLocation.Models
{
    public class NpcLocation
    {
        /// <summary>
        /// 建表時另外解出來的「這個 NPC 實際站在哪一張圖上」;沒解出來就是
        /// <see langword="null"/>,那時一律退回區域的預設地圖。
        /// </summary>
        private readonly Map? _map;

        public NpcLocation(float x, float y, TerritoryType territoryType, Map? map = null)
        {
            X = x;
            Y = y;
            TerritoryExcel = territoryType;
            _map = map;
            MapId = map?.RowId ?? territoryType.Map.RowId;
        }

        public TerritoryType TerritoryExcel { get; set; }

        /// <summary>
        /// 換算地圖座標時該用的那張圖。
        /// </summary>
        /// <remarks>
        /// 🔴 這裡以前寫死 <c>TerritoryExcel.Map.Value</c>,而 <see cref="MapId"/> 卻可能是
        /// <c>ItemLookup.ParseLgbFile</c> 另外用 <c>ENpcResident.Map</c> ＋ <c>Map.MapIndex</c>
        /// 解出來的<b>別張圖</b>(一個區域好幾張圖時才會不同)。
        /// 兩者不一致時,座標是拿 A 圖的 SizeFactor/Offset 去換 B 圖上的點
        /// —— <b>算得出來、不報錯、就是偏掉</b>。
        ///
        /// 📌 <c>_map</c> 為 <see langword="null"/>(絕大多數 NPC)時,
        /// <see cref="MapId"/> 本來就等於 <c>TerritoryExcel.Map.RowId</c>,
        /// 所以這個改動對它們算出來的數字<b>逐位元相同</b>,只有先前算錯的那些會變。
        ///
        /// ⚠️ 解不開時刻意讓 <c>.Value</c> 照舊擲例外,不要改成回 null 或 0:
        /// <c>ItemVendorLocationIpc.GetItemVendorsWorld</c> 靠那個例外把
        /// <c>MapCoordinatesKnown</c> 標成 false,吞掉它會讓消費端收到一組假的 (0, 0)。
        /// </remarks>
        private Map ResolvedMap => _map ?? TerritoryExcel.Map.Value;

        public float MapX
        {
            get
            {
                var map = ResolvedMap;
                return ToMapCoordinate(X, map.SizeFactor, map.OffsetX);
            }
        }

        public float MapY
        {
            get
            {
                var map = ResolvedMap;
                return ToMapCoordinate(Y, map.SizeFactor, map.OffsetY);
            }
        }
        public float X { get; }
        public float Y { get; }
        public uint TerritoryType => TerritoryExcel.RowId;
        public uint MapId { get; }

        // TODO: This needs to be removed. This is an exact duplicate of Dalamud/Game/Text/SeStringHandling/Payloads/MapLinkPayload#ConvertRawPositionToMapCoordinate.cs
        private static float ToMapCoordinate(float val, float scale, short offset)
        {
            var c = scale / 100.0f;

            val = (val + offset) * c;

            return (41.0f / c * ((val + 1024.0f) / 2048.0f)) + 1;
        }
    }
}