using System.Collections.Generic;
using System.Text;
using Godot;

namespace CardArtReplacer;

/// <summary>
/// 扫描 image/cards/** 下所有图片，建立 “归一化卡牌类名 -> 资源路径” 映射。
/// 子目录（defect / regent / colorless / curse / 任意名）仅供你自己整理，
/// 解析时与目录无关，只看文件名。
/// 补丁按卡牌**类名**（如 BigBang）来查，文件名按同样规则归一化后匹配。
/// </summary>
public static class CardArtLibrary
{
    private static readonly Dictionary<string, string> _paths = new();       // key -> res://...

    public static int Count => _paths.Count;

    /// <summary>
    /// 归一化：全小写、去掉下划线/连字符/空格。
    /// 这样类名 "BigBang" 与文件 "bigbang.png"、"big_bang.png" 都能对上。
    /// </summary>
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

    /// <summary>重新扫描目录（改完图后调用即可热更新映射）。</summary>
    public static void Rescan()
    {
        _paths.Clear();
        ScanDir(Entry.CardsRoot);
        Entry.LogInfo($"scanned card art dir, {_paths.Count} files");
    }

    private static void ScanDir(string dir)
    {
        using var da = DirAccess.Open(dir);
        if (da == null) return;

        da.ListDirBegin();
        for (string name = da.GetNext(); name != string.Empty; name = da.GetNext())
        {
            if (name.StartsWith(".")) continue;
            string full = dir.TrimEnd('/') + "/" + name;

            if (da.CurrentIsDir())
            {
                ScanDir(full); // 递归子目录
            }
            else
            {
                string lower = name.ToLowerInvariant();
                if (lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg"))
                {
                    string key = Normalize(System.IO.Path.GetFileNameWithoutExtension(name));
                    if (_paths.ContainsKey(key))
                        Entry.LogInfo($"duplicate card id '{key}', using {full}");
                    _paths[key] = full;
                }
            }
        }
        da.ListDirEnd();
    }

    /// <summary>按卡牌类名取 res:// 图片路径；没有对应图片时返回 null（保留原版画）。</summary>
    public static string? ResolvePath(string? cardClassName)
    {
        string key = Normalize(cardClassName);
        if (key.Length == 0) return null;
        return _paths.TryGetValue(key, out var path) ? path : null;
    }
}
