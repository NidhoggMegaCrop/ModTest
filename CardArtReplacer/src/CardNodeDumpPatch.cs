using System;
using System.Reflection;
using HarmonyLib;

namespace CardArtReplacer;

/// <summary>
/// 【调试用，搞清结构后可删】在启动时用反射把 NCard 类的字段/属性/方法列进日志。
/// 用来确定"全图异画"要 hook 哪个普通方法、以及 _portrait/_frame 等节点怎么访问。
/// 不 hook 生命周期方法（Harmony 拦不住 Godot 的 _Ready）；纯反射，不显示卡牌也能跑。
/// </summary>
public static class CardNodeDumpPatch
{
    public static void TryInstall(Harmony harmony)
    {
        if (!Entry.DumpCardNodes) return;

        // 类型名由日志确认：MegaCrit.Sts2.Core.Nodes.Cards.NCard
        var t = AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Cards.NCard")
                ?? AccessTools.TypeByName("NCard");
        if (t == null) { Entry.LogInfo("[dump] NCard 类型未找到"); return; }

        DumpOne(t);
        // 顺带把父类（若也是游戏类）列一层，视觉刷新方法可能在父类上。
        var b = t.BaseType;
        if (b?.FullName != null && b.FullName.StartsWith("MegaCrit"))
            DumpOne(b);
    }

    static void DumpOne(Type t)
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.Static
                             | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        Entry.LogInfo($"[dump] ==== {t.FullName} (base {t.BaseType?.Name}) ====");

        foreach (var f in t.GetFields(F))
        {
            if (f.Name.Contains('<')) continue; // 跳过编译器生成的 backing field
            Entry.LogInfo($"[dump] field  {Short(f.FieldType)} {f.Name}");
        }
        foreach (var p in t.GetProperties(F))
            Entry.LogInfo($"[dump] prop   {Short(p.PropertyType)} {p.Name}");
        foreach (var m in t.GetMethods(F))
        {
            if (m.IsSpecialName || m.Name.Contains('<')) continue; // 跳过 get_/set_ 与 lambda
            var ps = string.Join(", ", Array.ConvertAll(m.GetParameters(), x => $"{Short(x.ParameterType)} {x.Name}"));
            Entry.LogInfo($"[dump] method {Short(m.ReturnType)} {m.Name}({ps})");
        }
        Entry.LogInfo("[dump] ==== end ====");
    }

    static string Short(Type t) => t.Name;
}
