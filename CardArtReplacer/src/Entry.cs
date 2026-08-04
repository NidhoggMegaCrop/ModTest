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

    // 全图异画：把整卡换成你的图（隐藏卡框/立绘窗口/横幅）。设 false 则退回“只换立绘窗口”。
    public static bool FullArt = true;

    // 类型文框（状态/诅咒/攻击…）移到卡片底部居中（仅对全图卡）。
    public static bool MoveTypePlaqueToBottom = true;
    public static float TypePlaqueBottomMargin = 12f;

    // 诊断：把“类名包含此串”的卡的节点树 dump 到日志，用于定位某张卡的多余边框。空=关。
    public static string DumpCardName = "";

    // 调试：反射 dump NCard 成员。已搞清结构，默认关闭；需要再看时设 true。
    public static bool DumpCardNodes = false;

    public static void Init()
    {
        LogInfo("init begin");

        var harmony = new Harmony("sts2." + ModId.ToLowerInvariant());
        harmony.PatchAll();

        // 让 .tscn 能加载本程序集内的自定义脚本（官方模板通用写法；本 mod 无自定义脚本也无害）。
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);

        // 全图异画：hook NCard.UpdateVisuals（安全：找不到目标只记日志，不影响核心换图）。
        try { CardFullArtPatch.Install(harmony); }
        catch (System.Exception e) { LogInfo($"fullart install failed: {e.Message}"); }

        // 调试：安装卡牌节点 dump（默认 DumpCardNodes=false，不生效）。
        try { CardNodeDumpPatch.TryInstall(harmony); }
        catch (System.Exception e) { LogInfo($"dump install failed: {e.Message}"); }

        LogInfo($"patched. card art resolves on demand from res://{ModId}/image/cards/**.");
    }

    public static void LogInfo(string msg) => Log.Info($"[{ModId}] {msg}");
}
