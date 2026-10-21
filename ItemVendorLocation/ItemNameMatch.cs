using System;

namespace ItemVendorLocation;

// 本檔只有純函式：不讀遊戲記憶體、不碰 Dalamud，離線探針直接編這一個檔。

/// <summary>
/// 商店清單上顯示的道具名 ↔ 道具表名稱。
/// 台服道具表有些列 <c>Singular</c> 與 <c>Name</c> 寫法不同（用字、全形／半形空白），兩欄都當候選、Singular 優先。
/// 比的是正規化後整句相等，不是子字串。
/// </summary>
internal static class ItemNameMatch
{
    private const char PuaFirst = (char)0xE000;
    private const char PuaLast = (char)0xF8FF;
    private const char FullWidthSpace = (char)0x3000;
    private const char NoBreakSpace = (char)0x00A0;
    private const string TruncationChars = ".…";

    /// <summary>
    /// 全形空白與不換行空白當成半形空白、去掉私用區圖示字元、去掉前後空白；其餘逐字保留。
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var chars = new char[text.Length];
        var n = 0;
        foreach (var c in text)
        {
            if (IsIcon(c))
                continue;
            chars[n++] = Map(c);
        }

        return new string(chars, 0, n).Trim();
    }

    /// <summary>正規化後不重複、非空的候選名，<c>Singular</c> 排第一；兩欄都空時回空陣列。</summary>
    public static string[] Candidates(string? name, string? singular)
    {
        var s = Normalize(singular);
        var n = Normalize(name);
        if (s.Length == 0)
            return n.Length == 0 ? [] : [n];
        if (n.Length == 0 || string.Equals(s, n, StringComparison.Ordinal))
            return [s];
        return [s, n];
    }

    /// <summary>顯示名正規化後與某個候選整句相等：回該候選的位置（0＝Singular），都不相等回 -1。不配置。</summary>
    public static int MatchRank(string[] candidates, ReadOnlySpan<char> shown)
    {
        for (var i = 0; i < candidates.Length; i++)
        {
            if (MatchNormalized(shown, candidates[i], whole: true) >= 0)
                return i;
        }

        return -1;
    }

    /// <summary>顯示名以「...」或「…」結尾＝被截斷，<paramref name="head"/> 是去掉截斷符號後的前段。</summary>
    public static bool TryGetTruncatedHead(ReadOnlySpan<char> shown, out ReadOnlySpan<char> head)
    {
        head = default;
        if (!shown.EndsWith("...", StringComparison.Ordinal) && !shown.EndsWith("…", StringComparison.Ordinal))
            return false;
        head = shown.TrimEnd(TruncationChars);
        return true;
    }

    /// <summary><paramref name="head"/> 正規化後是某個候選的開頭：回最長的那個前段長度；都不是（或前段為空）回 -1。</summary>
    public static int LongestPrefix(string[] candidates, ReadOnlySpan<char> head)
    {
        var best = -1;
        foreach (var candidate in candidates)
        {
            var length = MatchNormalized(head, candidate, whole: false);
            if (length > 0 && length > best)
                best = length;
        }

        return best;
    }

    /// <summary>
    /// 一個清單裡挑要高亮的那一列：Singular 整句相等（立即採用）＞ Name 整句相等（取第一列）＞ 截斷列裡最長的前綴。
    /// 最長前綴不只一列時不挑：分不出是哪一個，寧可不亮也不要亮錯。
    /// </summary>
    internal struct ListPick
    {
        private int fallbackRow; // 列號 +1；0＝還沒有
        private bool fallbackIsExact;
        private int prefixLength;
        private bool prefixTied;

        /// <summary>這一列的處理結果。<see cref="Exact"/>＝直接採用並停止；<see cref="Fallback"/>＝成為目前的後備列。</summary>
        public enum Verdict
        {
            None,
            Exact,
            Fallback,
        }

        /// <summary>掃完後的後備列號；沒有或分不出來時回 -1。</summary>
        public readonly int Row => fallbackRow == 0 || (!fallbackIsExact && prefixTied) ? -1 : fallbackRow - 1;

        public Verdict Consider(string[] candidates, int row, ReadOnlySpan<char> shown, bool allowTruncated)
        {
            var rank = MatchRank(candidates, shown);
            if (rank == 0)
                return Verdict.Exact;
            if (rank > 0)
            {
                if (fallbackIsExact)
                    return Verdict.None;
                fallbackRow = row + 1;
                fallbackIsExact = true;
                return Verdict.Fallback;
            }

            if (fallbackIsExact || !allowTruncated || !TryGetTruncatedHead(shown, out var head))
                return Verdict.None;

            var length = LongestPrefix(candidates, head);
            if (length <= 0)
                return Verdict.None;
            if (length == prefixLength)
            {
                prefixTied = true;
                return Verdict.None;
            }

            if (length < prefixLength)
                return Verdict.None;
            prefixLength = length;
            prefixTied = false;
            fallbackRow = row + 1;
            return Verdict.Fallback;
        }
    }

    // 與 Normalize(shown) 再比的結果相同，但不配置：回傳正規化後的長度（是 candidate 的前綴）；不是前綴回 -1。
    private static int MatchNormalized(ReadOnlySpan<char> shown, string candidate, bool whole)
    {
        int start = 0, end = shown.Length - 1;
        while (start <= end && IsTrimmed(shown[start]))
            start++;
        while (end >= start && IsTrimmed(shown[end]))
            end--;

        var j = 0;
        for (var i = start; i <= end; i++)
        {
            var c = shown[i];
            if (IsIcon(c))
                continue;
            if (j >= candidate.Length || Map(c) != candidate[j])
                return -1;
            j++;
        }

        return whole && j != candidate.Length ? -1 : j;
    }

    private static bool IsIcon(char c) => c >= PuaFirst && c <= PuaLast;

    private static char Map(char c) => c is FullWidthSpace or NoBreakSpace ? ' ' : c;

    private static bool IsTrimmed(char c) => IsIcon(c) || char.IsWhiteSpace(Map(c));
}
