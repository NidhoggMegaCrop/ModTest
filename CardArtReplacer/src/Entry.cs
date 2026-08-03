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

    // 调试用：把第一张卡的视觉节点树 dump 到日志一次，用来搞清楚全图异画要动哪些节点。
    // 搞清结构后可设为 false。
    public static bool DumpCardNodes = true;

    public static void Init()
    {
        LogInfo("init begin");

        var harmony = new Harmony("sts2." + ModId.ToLowerInvariant());
        harmony.PatchAll();

        // 让 .tscn 能加载本程序集内的自定义脚本（官方模板通用写法；本 mod 无自定义脚本也无害）。
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);

        // 调试：安装卡牌节点树 dump（安全：找不到目标只记日志，不影响核心换图）。
        try { CardNodeDumpPatch.TryInstall(harmony); }
        catch (System.Exception e) { LogInfo($"dump install failed: {e.Message}"); }

        LogInfo($"patched. card art resolves on demand from res://{ModId}/image/cards/**.");
    }

    public static void LogInfo(string msg) => Log.Info($"[{ModId}] {msg}");
}
