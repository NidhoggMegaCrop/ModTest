using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Godot;

namespace CardArtReplacer;

/// <summary>
/// 全图异画：hook NCard.UpdateVisuals（每次卡牌视觉刷新都会调）。
/// 对"有我们图"的卡：把整张卡框 _frame 换成我们的图并铺满 606×852，清掉去色材质/灰 tint，
/// 隐藏立绘窗口/窗口边框/名字横幅/卡专属覆盖层，并（可选）把类型文框移到卡片底部。
///
/// 跨 mod 安全：只在"我们自己的卡"上改动；还原放在节点回收进对象池时（OnReturnedFromPool），
/// 绝不碰别的 mod 正在显示的卡——否则会把别人（如 SignatureLib）藏起来的边框还原出来。
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
            harmony.Patch(upd, postfix: new HarmonyMethod(AccessTools.Method(typeof(CardFullArtPatch), nameof(Postfix))));
            Entry.LogInfo("[fullart] hooked NCard.UpdateVisuals");
        }
        else Entry.LogInfo("[fullart] UpdateVisuals 未找到，跳过");

        // 回池时还原我们的改动，保证下一张卡（原版/别的 mod）拿到干净节点。
        var ret = AccessTools.Method(T, "OnReturnedFromPool");
        if (ret != null)
            harmony.Patch(ret, postfix: new HarmonyMethod(AccessTools.Method(typeof(CardFullArtPatch), nameof(PostReturn))));
        else Entry.LogInfo("[fullart] OnReturnedFromPool 未找到（池还原退化，但不影响他卡）");
    }

    public static void Postfix(object __instance)
    {
        try
        {
            if (!Entry.FullArt || __instance is not Node ncard) return;
            if (FFrame!.GetValue(ncard) is not TextureRect frame) return;

            var model = FModel?.GetValue(ncard);
            string? className = model?.GetType().Name;

            // 诊断：按卡名 dump 节点树（定位某张卡的多余边框），每个类名只打一次。
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

            bool modified = ncard.HasMeta(MMod) && ncard.GetMeta(MMod).AsBool();
            if (!modified) // 首次改这张节点：记录原始状态，供回池时还原
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

            // 类型文框移到卡片底部居中（游戏每帧会重设它的位置，我们在其后覆盖）。
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

    // 节点回收进池时：把我们的改动还原成原始状态，让下一张卡从干净状态开始。
    public static void PostReturn(object __instance)
    {
        try
        {
            if (__instance is not Node ncard) return;
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
        catch (Exception e) { Entry.LogInfo($"[fullart] restore error: {e.Message}"); }
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
