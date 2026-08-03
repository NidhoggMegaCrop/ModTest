using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Godot;

namespace CardArtReplacer;

/// <summary>
/// 【调试用，搞清结构后可删】把第一张卡的视觉节点树 dump 到日志，
/// 用来确定"全图异画"要撑大/隐藏哪些节点（如 _portrait / _frame / _banner）。
///
/// 手动安装（不走 PatchAll）：找不到目标只记一条日志，绝不影响核心换图补丁。
/// 只 dump 第一张卡，避免刷屏。
/// </summary>
public static class CardNodeDumpPatch
{
    static bool _dumped;

    public static void TryInstall(Harmony harmony)
    {
        if (!Entry.DumpCardNodes) return;

        var postfix = new HarmonyMethod(AccessTools.Method(typeof(CardNodeDumpPatch), nameof(Postfix)));
        int hooked = 0;
        foreach (var mi in FindTargets())
        {
            try
            {
                harmony.Patch(mi, postfix: postfix);
                hooked++;
                Entry.LogInfo($"[dump] hooked {mi.DeclaringType?.FullName}.{mi.Name}");
            }
            catch (System.Exception e)
            {
                Entry.LogInfo($"[dump] hook failed {mi.Name}: {e.Message}");
            }
        }
        if (hooked == 0)
            Entry.LogInfo("[dump] 未找到卡牌视觉方法——把这条发我，我换类名/方法名再试。");
    }

    // 只取“在该类型上直接声明”的方法（DeclaredMethod），避免误挂到基类 Node._Ready 上导致对所有节点生效。
    static IEnumerable<MethodBase> FindTargets()
    {
        string[] types =
        {
            "MegaCrit.Sts2.Core.Nodes.NCardComponent",
            "MegaCrit.Sts2.Core.Nodes.NCard",
            "NCardComponent",
            "NCard",
        };
        string[] methods = { "UpdateCardDisplay", "RefreshCard", "UpdateCard", "_Ready" };

        var seen = new HashSet<MethodBase>();
        foreach (var tn in types)
        {
            var t = AccessTools.TypeByName(tn);
            if (t == null) continue;
            foreach (var mn in methods)
            {
                var mi = AccessTools.DeclaredMethod(t, mn);
                if (mi != null && seen.Add(mi)) yield return mi;
            }
        }
    }

    public static void Postfix(object __instance)
    {
        if (_dumped || !Entry.DumpCardNodes) return;
        if (__instance is not Node node) return;
        _dumped = true;

        Entry.LogInfo($"[dump] ==== card node tree from {node.GetType().FullName} ====");
        Dump(node, 0);
        Entry.LogInfo("[dump] ==== end ====");
    }

    static void Dump(Node n, int depth)
    {
        string extra = n switch
        {
            Control c => $" [Control size={c.Size} vis={c.Visible}]",
            Node2D n2 => $" [Node2D pos={n2.Position} vis={n2.Visible}]",
            _ => ""
        };
        Entry.LogInfo($"[dump] {new string(' ', depth * 2)}{n.Name} : {n.GetType().Name}{extra}");
        foreach (var child in n.GetChildren())
            Dump(child, depth + 1);
    }
}
