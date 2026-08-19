using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Godot;

namespace CardArtReplacer;

/// <summary>
/// 全图异画：hook NCard.UpdateVisuals。
/// - Prefix（刷新前）：若该节点上一轮被我们改过，先还原成干净状态，再让游戏设置这张卡。
///   这样详情/图鉴等复用同一 NCard 的地方不会残留我们的隐藏改动而挡住别的卡/别的 mod 的图；
///   且只还原"我们自己"的改动、跑在别的 mod 修改之前，不与它们冲突。
/// - Postfix（刷新后）：只对"有我们图"的卡，把整卡框 _frame 换成我们的图并铺满、清去色材质、
///   隐藏立绘窗口/窗口边框/横幅/覆盖层、类型文框移底部。
/// </summary>
public static class CardFullArtPatch
{
    static readonly Type? T = AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Cards.NCard");
    static readonly FieldInfo? FModel   = T != null ? AccessTools.Field(T, "_model") : null;
    static readonly FieldInfo? FFrame   = T != null ? AccessTools.Field(T, "_frame") : null;
    static readonly FieldInfo? FGroup   = T != null ? AccessTools.Field(T, "_portraitCanvasGroup") : null;
    static readonly FieldInfo? FBorder  = T != null ? AccessTools.Field(T, "_portraitBorder") : null;
    static readonly FieldInfo? FBanner  = T != null ? AccessTools.Field(T, "_banner") : null;
    static readonly FieldInfo? FOverlay = T != null ? AccessTools.Field(T, "_overlayContainer") : null;
    static readonly FieldInfo? FTypePlaque = T != null ? AccessTools.Field(T, "_typePlaque") : null;

    static readonly HashSet<string> _dumped = new();

    const string MMod = "car_fullart",
                 MExp = "car_fexp",  MStr = "car_fstr",
                 MMat = "car_fmat",  MUpm = "car_fupm",
                 MSlf = "car_fself", MMdl = "car_fmod",
                 MGrp = "car_gvis",  MBrd = "car_bvis", MBan = "car_nvis", MOvl = "car_ovis";

    public static void Install(Harmony harmony)
    {
        if (T == null || FFrame == null) { Entry.LogInfo("[fullart] NCard/_frame 未找到，跳过"); return; }

        var upd = AccessTools.Method(T, "UpdateVisuals");
        if (upd != null)
        {
            harmony.Patch(upd,
                prefix:  new HarmonyMethod(AccessTools.Method(typeof(CardFullArtPatch), nameof(Prefix))),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(CardFullArtPatch), nameof(Postfix))));
            Entry.LogInfo("[fullart] hooked NCard.UpdateVisuals (prefix+postfix)");
        }
        else Entry.LogInfo("[fullart] UpdateVisuals 未找到，跳过");

        // 节点回收进池时也还原一次（双保险）。
        var ret = AccessTools.Method(T, "OnReturnedFromPool");
        if (ret != null)
            harmony.Patch(ret, postfix: new HarmonyMethod(AccessTools.Method(typeof(CardFullArtPatch), nameof(PostReturn))));
    }

    // 刷新前：先把我们上一轮的改动还原干净，让复用的节点回到原始状态。
    public static void Prefix(object __instance)
    {
        try { if (__instance is Node n) Restore(n); }
        catch (Exception e) { Entry.LogInfo($"[fullart] prefix error: {e.Message}"); }
    }

    public static void Postfix(object __instance)
    {
        try
        {
            if (!Entry.FullArt || __instance is not Node ncard) return;
            if (FFrame!.GetValue(ncard) is not TextureRect frame) return;

            var model = FModel?.GetValue(ncard);
            string? className = model?.GetType().Name;

            // 诊断：按卡名 dump 节点树，每个类名只打一次。
            if (!string.IsNullOrEmpty(Entry.DumpCardName) && className != null
                && className.Contains(Entry.DumpCardName, StringComparison.OrdinalIgnoreCase)
                && _dumped.Add(className))
            {
                Entry.LogInfo($"[dump] ==== tree for {className} ====");
                DumpTree(ncard, 0);
                Entry.LogInfo("[dump] ==== end ====");
            }

            string? path = CardArtLibrary.ResolvePath(className);
            if (path == null || !ResourceLoader.Exists(path)) return; // 不是我们的卡：绝不碰

            var group   = FGroup?.GetValue(ncard) as CanvasItem;
            var border  = FBorder?.GetValue(ncard) as CanvasItem;
            var banner  = FBanner?.GetValue(ncard) as CanvasItem;
            var overlay = FOverlay?.GetValue(ncard) as CanvasItem;

            // Prefix 已把 MMod 清掉，这里记录游戏刚设置好的干净状态，供下一轮/回池还原。
            if (!(ncard.HasMeta(MMod) && ncard.GetMeta(MMod).AsBool()))
            {
                ncard.SetMeta(MExp, (int)frame.ExpandMode);
                ncard.SetMeta(MStr, (int)frame.StretchMode);
                ncard.SetMeta(MMat, frame.Material);
                ncard.SetMeta(MUpm, frame.UseParentMaterial);
                ncard.SetMeta(MSlf, frame.SelfModulate);
                ncard.SetMeta(MMdl, frame.Modulate);
                ncard.SetMeta(MGrp, group?.Visible ?? true);
                ncard.SetMeta(MBrd, border?.Visible ?? true);
                ncard.SetMeta(MBan, banner?.Visible ?? true);
                ncard.SetMeta(MOvl, overlay?.Visible ?? true);
                ncard.SetMeta(MMod, true);
            }

            frame.Texture = GD.Load<Texture2D>(path);
            frame.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            frame.StretchMode = TextureRect.StretchModeEnum.Scale;
            frame.Material = null;
            frame.UseParentMaterial = false;
            frame.SelfModulate = Colors.White;
            frame.Modulate = Colors.White;

            if (group   != null) group.Visible   = false;
            if (border  != null) border.Visible  = false;
            if (banner  != null) banner.Visible  = false;
            if (overlay != null) overlay.Visible = false;

            if (Entry.MoveTypePlaqueToBottom && FTypePlaque?.GetValue(ncard) is Control plaque)
            {
                var fp = frame.Position; var fs = frame.Size; var ps = plaque.Size;
                plaque.Position = new Vector2(
                    fp.X + (fs.X - ps.X) * 0.5f,
                    fp.Y + fs.Y - ps.Y - Entry.TypePlaqueBottomMargin);
            }
        }
        catch (Exception e) { Entry.LogInfo($"[fullart] error: {e.Message}"); }
    }

    public static void PostReturn(object __instance)
    {
        try { if (__instance is Node n) Restore(n); }
        catch (Exception e) { Entry.LogInfo($"[fullart] restore error: {e.Message}"); }
    }

    // 把我们的改动还原成记录的原始状态（frame 贴图由游戏每帧重设，无需还原）。
    static void Restore(Node ncard)
    {
        if (!(ncard.HasMeta(MMod) && ncard.GetMeta(MMod).AsBool())) return;

        if (FFrame!.GetValue(ncard) is TextureRect frame)
        {
            frame.ExpandMode  = (TextureRect.ExpandModeEnum)ncard.GetMeta(MExp, (int)frame.ExpandMode).AsInt32();
            frame.StretchMode = (TextureRect.StretchModeEnum)ncard.GetMeta(MStr, (int)frame.StretchMode).AsInt32();
            frame.Material = ncard.GetMeta(MMat).As<Material>();
            frame.UseParentMaterial = ncard.GetMeta(MUpm, false).AsBool();
            frame.SelfModulate = ncard.GetMeta(MSlf, Colors.White).AsColor();
            frame.Modulate = ncard.GetMeta(MMdl, Colors.White).AsColor();
        }
        if (FGroup?.GetValue(ncard)   is CanvasItem g) g.Visible = ncard.GetMeta(MGrp, true).AsBool();
        if (FBorder?.GetValue(ncard)  is CanvasItem b) b.Visible = ncard.GetMeta(MBrd, true).AsBool();
        if (FBanner?.GetValue(ncard)  is CanvasItem n) n.Visible = ncard.GetMeta(MBan, true).AsBool();
        if (FOverlay?.GetValue(ncard) is CanvasItem o) o.Visible = ncard.GetMeta(MOvl, true).AsBool();

        ncard.SetMeta(MMod, false);
    }

    static void DumpTree(Node n, int depth)
    {
        string extra = n switch
        {
            Control c => $" [size={c.Size} vis={c.Visible} mat={(c.Material != null)}]",
            Node2D n2 => $" [pos={n2.Position} vis={n2.Visible}]",
            _ => ""
        };
        Entry.LogInfo($"[dump] {new string(' ', depth * 2)}{n.Name} : {n.GetType().Name}{extra}");
        foreach (var child in n.GetChildren())
            DumpTree(child, depth + 1);
    }
}
