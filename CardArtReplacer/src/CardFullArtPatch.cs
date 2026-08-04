using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Godot;

namespace CardArtReplacer;

/// <summary>
/// 全图异画：hook NCard.UpdateVisuals（每次卡牌视觉刷新都会调）。
/// 对"有我们图"的卡：把整张卡框 _frame 换成我们的图并铺满 606×852，清掉 _frame 上的
/// 材质/调制（否则诅咒等卡的去色 shader 会把彩图染成黑白），隐藏立绘窗口/窗口边框/名字横幅，
/// 并（可选）把类型文框移到卡片底部。文字层原样保留在上层。
///
/// 对象池安全：NCard 会被复用，非本 mod 卡若之前被改过会精确还原。
/// </summary>
public static class CardFullArtPatch
{
    static readonly Type? T = AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Cards.NCard");
    static readonly FieldInfo? FModel  = T != null ? AccessTools.Field(T, "_model") : null;
    static readonly FieldInfo? FFrame  = T != null ? AccessTools.Field(T, "_frame") : null;
    static readonly FieldInfo? FGroup  = T != null ? AccessTools.Field(T, "_portraitCanvasGroup") : null;
    static readonly FieldInfo? FBorder = T != null ? AccessTools.Field(T, "_portraitBorder") : null;
    static readonly FieldInfo? FBanner = T != null ? AccessTools.Field(T, "_banner") : null;
    static readonly FieldInfo? FTypePlaque = T != null ? AccessTools.Field(T, "_typePlaque") : null;

    static readonly HashSet<string> _dumped = new();

    const string MMod = "car_fullart",
                 MExp = "car_fexp",  MStr = "car_fstr",
                 MMat = "car_fmat",  MUpm = "car_fupm",
                 MSlf = "car_fself", MMdl = "car_fmod",
                 MGrp = "car_gvis",  MBrd = "car_bvis", MBan = "car_nvis";

    public static void Install(Harmony harmony)
    {
        if (T == null || FFrame == null) { Entry.LogInfo("[fullart] NCard/_frame 未找到，跳过"); return; }
        var target = AccessTools.Method(T, "UpdateVisuals");
        if (target == null) { Entry.LogInfo("[fullart] UpdateVisuals 未找到，跳过"); return; }
        harmony.Patch(target, postfix: new HarmonyMethod(AccessTools.Method(typeof(CardFullArtPatch), nameof(Postfix))));
        Entry.LogInfo("[fullart] hooked NCard.UpdateVisuals");
    }

    public static void Postfix(object __instance)
    {
        try
        {
            if (!Entry.FullArt || __instance is not Node ncard) return;
            if (FFrame!.GetValue(ncard) is not TextureRect frame) return;

            var group  = FGroup?.GetValue(ncard) as CanvasItem;
            var border = FBorder?.GetValue(ncard) as CanvasItem;
            var banner = FBanner?.GetValue(ncard) as CanvasItem;

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
            bool ours = path != null && ResourceLoader.Exists(path);
            bool modified = ncard.HasMeta(MMod) && ncard.GetMeta(MMod).AsBool();

            if (ours)
            {
                if (!modified) // 首次改这张节点：记录原始状态
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
                }

                frame.Texture = GD.Load<Texture2D>(path);
                frame.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                frame.StretchMode = TextureRect.StretchModeEnum.Scale;
                frame.Material = null;
                frame.UseParentMaterial = false;
                frame.SelfModulate = Colors.White;
                frame.Modulate = Colors.White;

                if (group  != null) group.Visible  = false;
                if (border != null) border.Visible = false;
                if (banner != null) banner.Visible = false;

                // 类型文框移到卡片底部居中（游戏每帧会重设它的位置，我们在其后覆盖）。
                if (Entry.MoveTypePlaqueToBottom && FTypePlaque?.GetValue(ncard) is Control plaque)
                {
                    var fp = frame.Position; var fs = frame.Size; var ps = plaque.Size;
                    plaque.Position = new Vector2(
                        fp.X + (fs.X - ps.X) * 0.5f,
                        fp.Y + fs.Y - ps.Y - Entry.TypePlaqueBottomMargin);
                }

                ncard.SetMeta(MMod, true);
            }
            else if (modified) // 池复用到非本 mod 卡：还原（贴图/类型文框位置游戏本帧已重设，无需还原）
            {
                frame.ExpandMode  = (TextureRect.ExpandModeEnum)ncard.GetMeta(MExp, (int)frame.ExpandMode).AsInt32();
                frame.StretchMode = (TextureRect.StretchModeEnum)ncard.GetMeta(MStr, (int)frame.StretchMode).AsInt32();
                frame.Material = ncard.GetMeta(MMat).As<Material>();
                frame.UseParentMaterial = ncard.GetMeta(MUpm, false).AsBool();
                frame.SelfModulate = ncard.GetMeta(MSlf, Colors.White).AsColor();
                frame.Modulate = ncard.GetMeta(MMdl, Colors.White).AsColor();
                if (group  != null) group.Visible  = ncard.GetMeta(MGrp, true).AsBool();
                if (border != null) border.Visible = ncard.GetMeta(MBrd, true).AsBool();
                if (banner != null) banner.Visible = ncard.GetMeta(MBan, true).AsBool();
                ncard.SetMeta(MMod, false);
            }
        }
        catch (Exception e) { Entry.LogInfo($"[fullart] error: {e.Message}"); }
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
