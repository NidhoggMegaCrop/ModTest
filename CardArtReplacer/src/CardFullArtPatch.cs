using System;
using System.Reflection;
using HarmonyLib;
using Godot;

namespace CardArtReplacer;

/// <summary>
/// 全图异画：hook NCard.UpdateVisuals（每次卡牌视觉刷新都会调）。
/// 对"有我们图"的卡：把整张卡框 _frame 换成我们的图并铺满 606×852，并清掉 _frame 上的
/// 材质/调制（否则诅咒等卡的去色 shader / 灰 tint 会把我们的彩图染成黑白），
/// 隐藏立绘窗口 _portraitCanvasGroup / 窗口边框 _portraitBorder / 名字横幅 _banner，
/// 文字层（标题/描述/费用/类型）原样保留在上层。
///
/// 对象池安全：NCard 会被复用，所以非本 mod 卡若之前被改过，会精确还原。
/// 手动安装（不走 PatchAll），找不到目标只记日志、不影响核心换图。
/// </summary>
public static class CardFullArtPatch
{
    static readonly Type? T = AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Cards.NCard");
    static readonly FieldInfo? FModel  = T != null ? AccessTools.Field(T, "_model") : null;
    static readonly FieldInfo? FFrame  = T != null ? AccessTools.Field(T, "_frame") : null;
    static readonly FieldInfo? FGroup  = T != null ? AccessTools.Field(T, "_portraitCanvasGroup") : null;
    static readonly FieldInfo? FBorder = T != null ? AccessTools.Field(T, "_portraitBorder") : null;
    static readonly FieldInfo? FBanner = T != null ? AccessTools.Field(T, "_banner") : null;

    // 存到节点 meta 上的原始状态（用于池复用还原）。
    const string MMod = "car_fullart",                     // 是否已被我们改过
                 MExp = "car_fexp",  MStr = "car_fstr",     // frame 的 Expand/Stretch
                 MMat = "car_fmat",  MUpm = "car_fupm",     // frame 的 Material / UseParentMaterial
                 MSlf = "car_fself", MMdl = "car_fmod",     // frame 的 SelfModulate / Modulate
                 MGrp = "car_gvis",  MBrd = "car_bvis", MBan = "car_nvis"; // 隐藏节点的可见性

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
            string? path = CardArtLibrary.ResolvePath(model?.GetType().Name);
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
                // 清掉去色 shader / 灰 tint（诅咒等卡会有），让我们的彩图正常显示。
                frame.Material = null;
                frame.UseParentMaterial = false;
                frame.SelfModulate = Colors.White;
                frame.Modulate = Colors.White;

                if (group  != null) group.Visible  = false;
                if (border != null) border.Visible = false;
                if (banner != null) banner.Visible = false;
                ncard.SetMeta(MMod, true);
            }
            else if (modified) // 池复用到非本 mod 卡：还原（_frame 贴图游戏本帧已重设，无需还原）
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
}
