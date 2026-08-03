using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace CardArtReplacer;

/// <summary>
/// 精简版“全图异画”mod 入口。只做一件事：把原版卡牌立绘换成你放进
/// image/cards/ 里的 606x852 整图。无边框、无远古立绘、无鸣谢、无配置。
/// 入口写法遵循官方教程（环境配置一章）。
/// </summary>
[ModInitializer(nameof(Init))]
public class Entry
{
    public const string ModId = "CardArtReplacer";

    // 目标卡面尺寸；你的图做成整张 606x852 全出血。
    public const int CardWidth = 606;
    public const int CardHeight = 852;

    // 卡图根目录：res://CardArtReplacer/image/cards/**（子目录随你整理）
    public static readonly string CardsRoot = $"res://{ModId}/image/cards";

    // 发现模式：把每个遇到的卡牌类名打进日志一次，帮你确定卡图文件该叫什么名字。
    // 图配齐后可改成 false 关掉日志。
    public static bool DiscoverCardNames = true;

    public static void Init()
    {
        LogInfo("init begin");
        CardArtLibrary.Rescan();

        var harmony = new Harmony("sts2." + ModId.ToLowerInvariant());
        harmony.PatchAll();

        // 让 .tscn 能加载本程序集内的自定义脚本（官方模板通用写法；本 mod 无自定义脚本也无害）。
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);

        LogInfo($"{CardArtLibrary.Count} card skins loaded, patched.");
    }

    public static void LogInfo(string msg) => Log.Info($"[{ModId}] {msg}");
}
