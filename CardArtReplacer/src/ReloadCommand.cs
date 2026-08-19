using Godot;

// 【可选文件】控制台命令，改完图后即时重扫，无需重启游戏。
// 若游戏 DevConsole API 与此不一致导致编译不过，直接删掉本文件即可，核心换图功能不受影响。
//
// 逆向自原 DLL：命令基类 AbstractConsoleCmd（MegaCrit.Sts2.Core.DevConsole），
// 原 mod 注册了 reload_ncards / reload_rtrsmoeifymod 两个命令。
// VERIFY(5): 确认基类全名、需要重写的成员（CmdName / Process 等）与注册方式。
//
// using MegaCrit.Sts2.Core.DevConsole;
//
// namespace CardArtReplacer
// {
//     public sealed class ReloadArtCmd : AbstractConsoleCmd
//     {
//         public override string CmdName => "reload_cardart";
//
//         public override void Process(string[] args)
//         {
//             CardArtLibrary.Rescan();
//             GD.Print($"[{ModEntry.ModId}] card art reloaded ({CardArtLibrary.Count} files).");
//             // 提示：重扫后，重新打开卡牌视图/牌库即可看到新图；
//             //       如需强制刷新已显示的卡，按游戏 API 重新触发一次卡面刷新。
//         }
//     }
// }
