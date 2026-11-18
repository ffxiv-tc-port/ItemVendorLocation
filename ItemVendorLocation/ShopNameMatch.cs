using System;
using System.Collections.Generic;
using System.Text;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace ItemVendorLocation;

// 本檔只有純函式：不讀遊戲記憶體、不碰 Dalamud（只參照 Lumina），離線探針直接編這一個檔。

/// <summary>
/// 選單列文字 ↔ 商人的商店名（<c>NpcInfo.ShopName</c> 以 "\n" 切開的任一段）整句相等（序數比較）。
/// 商店名在設定商人時切好；每幀只把列文字解到呼叫端的緩衝區再比，不配置。
/// </summary>
internal static class ShopNameMatch
{
    private const char NoBreakSpace = (char)0x00A0;

    /// <summary>所有非 null 商店名以 "\n" 切開的各段（空字串段也算，與逐幀 Split 的結果一致），去重。</summary>
    public static string[] Segments(IEnumerable<string?> shopNames)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var segments = new List<string>();
        foreach (var shopName in shopNames)
        {
            if (shopName == null)
            {
                continue;
            }

            foreach (var segment in shopName.Split("\n"))
            {
                if (seen.Add(segment))
                {
                    segments.Add(segment);
                }
            }
        }

        return segments.ToArray();
    }

    public static bool Contains(string[] segments, ReadOnlySpan<char> text)
    {
        foreach (var segment in segments)
        {
            if (text.SequenceEqual(segment))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 與 Lumina <c>ReadOnlySeStringSpan.ExtractText()</c>（不含軟連字號、巨集佔位為空）逐字相同的結果寫進 <paramref name="dest"/>，
    /// 回字數；放不下回 -1（呼叫端退回 <c>ExtractText()</c>）。
    /// </summary>
    public static int ExtractTextInto(ReadOnlySeStringSpan text, Span<char> dest)
    {
        var n = 0;
        foreach (var payload in text)
        {
            switch (payload.Type)
            {
                case ReadOnlySePayloadType.Text:
                    if (Encoding.UTF8.GetCharCount(payload.Body) > dest.Length - n)
                    {
                        return -1;
                    }

                    n += Encoding.UTF8.GetChars(payload.Body, dest[n..]);
                    break;
                case ReadOnlySePayloadType.Macro:
                    switch (payload.MacroCode)
                    {
                        case MacroCode.NewLine:
                            if (!Append(dest, ref n, Environment.NewLine))
                            {
                                return -1;
                            }

                            break;
                        case MacroCode.NonBreakingSpace:
                            if (!Append(dest, ref n, NoBreakSpace))
                            {
                                return -1;
                            }

                            break;
                        case MacroCode.Hyphen:
                            if (!Append(dest, ref n, '-'))
                            {
                                return -1;
                            }

                            break;
                    }

                    break;
            }
        }

        return n;
    }

    private static bool Append(Span<char> dest, ref int n, string value)
    {
        if (value.Length > dest.Length - n)
        {
            return false;
        }

        value.AsSpan().CopyTo(dest[n..]);
        n += value.Length;
        return true;
    }

    private static bool Append(Span<char> dest, ref int n, char value)
    {
        if (n >= dest.Length)
        {
            return false;
        }

        dest[n++] = value;
        return true;
    }
}
