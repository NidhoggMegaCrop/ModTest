using System.Reflection;
using HarmonyLib;
using Godot;

namespace CardArtReplacer;

/// <summary>
/// 卡图替换补丁——官方「卡图&Spine」教程的标准做法：
/// patch <c>CardModel.PortraitPath</c> 的 getter，用卡牌**类名**匹配你的图，
/// 直接把返回的资源路径改成你的 res:// 图片路径。游戏自己去加载，无需碰节点/贴图。
///
/// 这里用反射定位目标，避免编译期依赖 CardModel 的具体命名空间（本地已知命名空间后，
/// 可改用文件底部注释里的官方强类型写法，更简洁）。
/// </summary>
[HarmonyPatch]
public static class CardPortraitPatch
{
    static MethodBase? TargetMethod()
    {
        var t = AccessTools.TypeByName("CardModel")
                ?? AccessTools.TypeByName("MegaCrit.Sts2.Core.CardModel");
        var getter = AccessTools.PropertyGetter(t, "PortraitPath");
        if (getter == null) Entry.LogInfo("PortraitPath getter not found — 确认 CardModel 类型名");
        return getter;
    }

    // 已在日志里报告过的卡牌类名（发现模式用，每个只打一次）。
    static readonly System.Collections.Generic.HashSet<string> _seen = new();

    // 官方 Postfix：把卡图路径替换成我们的图。__instance 即 CardModel 实例。
    static void Postfix(object __instance, ref string __result)
    {
        string? className = __instance?.GetType().Name;      // 卡牌用类名标识，如 BigBang / AllForOne
        if (string.IsNullOrEmpty(className)) return;

        string? path = CardArtLibrary.ResolvePath(className);

        // 发现模式：把遇到的卡牌类名打进日志（每个只打一次），方便你确定文件该叫什么。
        if (Entry.DiscoverCardNames && _seen.Add(className))
        {
            string hint = path != null
                ? $"matched: {path}"
                : $"no art — put {CardArtLibrary.PreferredFileName(className)} under res://{Entry.ModId}/image/cards/<subfolder>/";
            Entry.LogInfo($"card class: {className}  ->  {hint}");
        }

        if (path == null) return;                            // 没配图：保留原版
        if (!ResourceLoader.Exists(path)) return;
        __result = path;
    }
}

/*
CardModel 位于命名空间 MegaCrit.Sts2.Core.Models.Cards（由卡牌类型全名确认，
如 MegaCrit.Sts2.Core.Models.Cards.AscendersBane）。因此可直接用官方强类型写法：

using MegaCrit.Sts2.Core.Models.Cards;   // CardModel 及各卡牌类所在命名空间

[HarmonyPatch(typeof(CardModel), nameof(CardModel.PortraitPath), MethodType.Getter)]
public static class CardModel_GetPortrait_Patch
{
    static void Postfix(CardModel __instance, ref string __result)
    {
        var path = CardArtLibrary.ResolvePath(__instance?.GetType().Name);
        if (path != null && ResourceLoader.Exists(path)) __result = path;
    }
}
*/
