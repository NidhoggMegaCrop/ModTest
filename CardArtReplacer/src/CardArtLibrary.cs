using System.Collections.Generic;
using System.Text;
using Godot;

namespace CardArtReplacer;

/// <summary>
/// 按卡牌**类名**解析出对应的 res:// 图片路径。
/// 不遍历目录（导出成 pck 后目录遍历不可靠），而是拼出候选路径用 ResourceLoader.Exists 逐个查——
/// 这在 pck 里是可靠的。文件名 = 卡牌类名（小写，或原样大小写皆可）。
/// </summary>
public static class CardArtLibrary
{
    // 类名 -> 解析结果缓存（res 路径，或 null 表示没配图）。
    private static readonly Dictionary<string, string?> _cache = new();

    // 在这些子目录（外加根目录 ""）下按“类名.png”查找。想用别的子目录名就往这里加。
    public static string[] SubFolders = { "", "defect", "regent", "colorless", "curse" };
    private static readonly string[] Exts = { ".png", ".jpg", ".jpeg" };

    public static int Count => _cache.Count;

    /// <summary>全小写、去掉下划线/连字符/空格。</summary>
    public static string Normalize(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        var sb = new StringBuilder(id.Length);
        foreach (char c in id)
        {
            if (c == '_' || c == '-' || c == ' ') continue;
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>清空缓存（改完图/改子目录列表后调用即可重新解析）。</summary>
    public static void Rescan() => _cache.Clear();

    /// <summary>按卡牌类名取 res:// 图片路径；没有则返回 null（保留原版画）。</summary>
    public static string? ResolvePath(string? cardClassName)
    {
        if (string.IsNullOrEmpty(cardClassName)) return null;
        if (_cache.TryGetValue(cardClassName, out var cached)) return cached;

        string? found = Search(cardClassName);
        _cache[cardClassName] = found;
        return found;
    }

    /// <summary>本 mod 期望的主命名（用于日志提示）：小写类名。</summary>
    public static string PreferredFileName(string cardClassName) => Normalize(cardClassName) + ".png";

    static string? Search(string cardClassName)
    {
        string lower = Normalize(cardClassName);
        foreach (var sub in SubFolders)
        {
            string dir = sub.Length == 0
                ? $"res://{Entry.ModId}/image/cards/"
                : $"res://{Entry.ModId}/image/cards/{sub}/";
            foreach (var ext in Exts)
            {
                // 先试小写类名（如 ascendersbane.png），再试原样类名（如 AscendersBane.png）。
                string p1 = dir + lower + ext;
                if (ResourceLoader.Exists(p1)) return p1;
                if (cardClassName != lower)
                {
                    string p2 = dir + cardClassName + ext;
                    if (ResourceLoader.Exists(p2)) return p2;
                }
            }
        }
        return null;
    }
}
