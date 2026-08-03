using System.Reflection;
using HarmonyLib;
using Godot;

namespace CardArtReplacer;

/// <summary>
/// 核心补丁：游戏刷新一张卡的视觉后，把卡内名为 "_portrait" 的贴图节点换成我们的图，
/// 并铺满整张 606x852 卡面（全图异画）。
///
/// 环境相关的东西（SDK / 入口 / 引用 sts2.dll+0Harmony.dll）已按官方教程坐实。
/// 但下面三处“卡牌内部结构”官方环境配置页没给，是逆向自原 RTRsMoeifyMod.dll 的字符串，
/// 需你对照本地 sts2.dll 或官方“视觉/09 Patch”章节核对一下（名字大概率正确）：
///   VERIFY(2) 补丁目标类型/方法：NCardComponent.UpdateCardDisplay
///   VERIFY(3) 从组件取卡牌 id：component -> Card/_card(CardModel) -> Id/ModelId
///   VERIFY(4) 立绘节点名 "_portrait" 及其类型（TextureRect / Sprite2D）
/// 补丁全程用反射解析，改名只需改这里的字符串，不必动结构。
/// </summary>
[HarmonyPatch]
public static class CardPortraitPatch
{
    static MethodBase? TargetMethod()
    {
        var t = AccessTools.TypeByName("NCardComponent")
                ?? AccessTools.TypeByName("MegaCrit.Sts2.Core.NCardComponent"); // VERIFY(2)
        var m = AccessTools.Method(t, "UpdateCardDisplay");                       // VERIFY(2)
        if (m == null) Entry.LogInfo("TargetMethod not found — check VERIFY(2)");
        return m;
    }

    // Postfix：原始刷新跑完后再覆盖立绘，避免被游戏重置。
    static void Postfix(object __instance)
    {
        try
        {
            if (__instance is not Node node) return;

            string? cardId = ReadCardId(__instance);
            if (string.IsNullOrEmpty(cardId)) return;

            Texture2D? tex = CardArtLibrary.Resolve(cardId);
            if (tex == null) return; // 没配图：保持原样

            var portrait = node.FindChild("_portrait", recursive: true, owned: false); // VERIFY(4)
            if (portrait == null) return;

            CoverCardFace(portrait, tex);
        }
        catch (System.Exception e)
        {
            Entry.LogInfo($"patch error: {e.Message}");
        }
    }

    // 取卡牌 id。原 DLL 路径：组件 -> CardModel -> Id(=ModelId)。
    static string? ReadCardId(object component)
    {
        object? model =
            AccessTools.Field(component.GetType(), "_card")?.GetValue(component)
            ?? AccessTools.Property(component.GetType(), "Card")?.GetValue(component); // VERIFY(3)
        if (model == null) return null;

        object? id =
            AccessTools.Property(model.GetType(), "Id")?.GetValue(model)
            ?? AccessTools.Property(model.GetType(), "ModelId")?.GetValue(model);       // VERIFY(3)
        return id?.ToString();
    }

    // 让贴图铺满整张 606x852 卡面。你的图自带边框/标题底，所以不需要游戏的 border。
    static void CoverCardFace(Node portrait, Texture2D tex)
    {
        switch (portrait)
        {
            case TextureRect tr:
                tr.Texture = tex;
                tr.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                tr.StretchMode = TextureRect.StretchModeEnum.Scale; // 直接拉伸铺满
                tr.CustomMinimumSize = new Vector2(Entry.CardWidth, Entry.CardHeight);
                tr.Size = new Vector2(Entry.CardWidth, Entry.CardHeight);
                break;

            case Sprite2D sp:
                sp.Texture = tex;
                sp.Centered = false;
                var s = tex.GetSize();
                if (s.X > 0 && s.Y > 0)
                    sp.Scale = new Vector2(Entry.CardWidth / s.X, Entry.CardHeight / s.Y);
                break;

            default:
                AccessTools.Property(portrait.GetType(), "Texture")?.SetValue(portrait, tex);
                break;
        }
        // VERIFY(4): 若图有偏移/被裁切，说明该节点靠锚点或父容器定位，
        //            按卡牌场景调整这里的 Position/Anchor 即可。
    }
}
