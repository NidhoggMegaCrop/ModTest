using HarmonyLib;
using Godot;

// 使用游戏 modding 命名空间中的入口 attribute。
// VERIFY(1): 确认游戏实际的 mod 入口机制与该 attribute 的完整名字。
//            逆向原 DLL 时它引用了 MegaCrit.Sts2.Core.Modding 且带有 ModInitializerAttribute。
// using MegaCrit.Sts2.Core.Modding;

namespace CardArtReplacer
{
    /// <summary>
    /// 精简版“全图异画”mod 的入口。只做一件事：把原版卡牌的立绘换成你自己放进
    /// image/cards/ 里的 606x852 图片。不含边框、远古立绘、画师鸣谢等任何其它功能。
    /// </summary>
    public static class ModEntry
    {
        public const string ModId = "CardArtReplacer";

        // 目标卡面尺寸（与原框架一致）。你的图应做成整张 606x852 全出血。
        public const int CardWidth = 606;
        public const int CardHeight = 852;

        // 卡图根目录：res://CardArtReplacer/image/cards/**（子目录随你怎么分都行）
        public static readonly string CardsRoot = $"res://{ModId}/image/cards";

        private static Harmony _harmony;

        // 游戏加载本 mod 时调用的入口。
        // VERIFY(1): 按游戏 modding API 给这个方法加上正确的入口标记（例如 [ModInitializer]），
        //            或改成游戏要求的入口签名。
        // [ModInitializer]
        public static void Initialize()
        {
            Log("startup begin");
            CardArtLibrary.Rescan();
            _harmony = new Harmony(ModId);
            _harmony.PatchAll(typeof(ModEntry).Assembly);
            Log($"patched, {CardArtLibrary.Count} card skins loaded");
        }

        public static void Log(string msg) => GD.Print($"[{ModId}] {msg}");
    }
}
