using System.Collections.Generic;
using System.Text;
using Godot;

namespace CardArtReplacer
{
    /// <summary>
    /// 扫描 image/cards/** 下所有图片，建立 “归一化卡牌id -> 贴图” 的映射并缓存。
    /// 子目录（defect / regent / colorless / curse / 任意名字）仅供你自己整理，
    /// 解析时与目录无关，只看文件名。
    /// </summary>
    public static class CardArtLibrary
    {
        // 归一化 key -> 资源路径（res://...）
        private static readonly Dictionary<string, string> _paths = new();
        // 归一化 key -> 已加载贴图
        private static readonly Dictionary<string, Texture2D> _cache = new();

        public static int Count => _paths.Count;

        /// <summary>
        /// 归一化：全小写、去掉下划线/连字符/空格。
        /// 这样 "big_bang.png" 与 "bigbang.png" 都能匹配到卡牌 id "big_bang"。
        /// </summary>
        public static string Normalize(string id)
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

        /// <summary>重新扫描目录并清空缓存（改完图后调用即可热更新）。</summary>
        public static void Rescan()
        {
            _paths.Clear();
            _cache.Clear();
            ScanDir(ModEntry.CardsRoot);
            ModEntry.Log($"scanned card art dir, {_paths.Count} files");
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
                        // 若不同目录出现同名，后扫到的覆盖先扫到的（会打一条日志提醒）
                        if (_paths.ContainsKey(key))
                            ModEntry.Log($"duplicate card id '{key}', using {full}");
                        _paths[key] = full;
                    }
                }
            }
            da.ListDirEnd();
        }

        /// <summary>按卡牌 id 取贴图；没有对应图片时返回 null（表示保留原版画）。</summary>
        public static Texture2D Resolve(string cardId)
        {
            string key = Normalize(cardId);
            if (key.Length == 0) return null;
            if (_cache.TryGetValue(key, out var cached)) return cached;
            if (!_paths.TryGetValue(key, out var path)) return null;

            // 首选：走 Godot 导入管线（存在同名 .import 即可，无损清晰）。
            Texture2D tex = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

            // 回退：编辑器/未导入场景下，直接从原始图片文件读取。
            if (tex == null)
            {
                var img = Image.LoadFromFile(path);
                if (img != null) tex = ImageTexture.CreateFromImage(img);
            }

            _cache[key] = tex;
            return tex;
        }
    }
}
