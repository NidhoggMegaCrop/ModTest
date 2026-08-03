# CardArtReplacer —— 精简版「只换卡面」mod

一个按 `RTRsMoeifyMod` 蓝图重建、**只保留卡面替换**的杀戮尖塔2 mod。
去掉了原框架的自定义边框、远古立绘、画师鸣谢、配置开关等一切其它功能。

你要做的只有两件事：**把 606×852 的图按卡牌 id 命名，丢进 `image/cards/` 里**。

---

## 1. 它做什么 / 不做什么

- ✅ 把原版角色卡、无色卡、诅咒卡的**立绘换成你的整图**，铺满整张 606×852 卡面。
- ✅ 你的图自带边框/标题底（全图异画），游戏仍在其上渲染动态文字（费用/名称/描述/关键字）。
- ❌ 不画自定义边框、不换远古立绘、不显示画师鸣谢、没有任何配置项。
- ❌ 不新增卡牌/不改数值 —— 这是**换画**框架，不是加卡框架。

## 2. 目录结构

```
CardArtReplacer/
├─ CardArtReplacer.csproj        # 编译工程（本地 build 用）
├─ src/
│  ├─ ModEntry.cs                # 入口：扫描目录 + 应用 Harmony 补丁
│  ├─ CardArtLibrary.cs          # 扫描 image/cards/**，建立 id→贴图 映射
│  ├─ CardPortraitPatch.cs       # 核心补丁：把 _portrait 换成你的图并铺满卡面
│  └─ ReloadCommand.cs           # 可选：控制台热重载命令（默认注释掉）
└─ image/cards/                  # ★ 你只需要动这里
   ├─ defect/  regent/  colorless/  curse/
   └─ colorless/_sample_full_face.png   # 606×852 占位样例（可删）
```

## 3. 加/换一张卡的图（无需懂编程）

1. 做一张 **606×852 PNG**，整张全出血（含边框+标题底，别把费用/描述文字做进去）。
2. 命名为该卡的 **id**（全小写，下划线可省）：`big_bang.png` 或 `bigbang.png` 都行。
3. 丢进 `image/cards/` 下任意子目录（目录只为你自己整理，程序只认文件名）。
4. 用 Godot 打开工程让它生成 `.import`（或让游戏在加载时导入），进游戏即可看到。

> 命名细节见 `image/cards/README.txt`。占位图 `_sample_full_face.png` 四角有描边，
> 是用来肉眼确认「铺满整张卡面」效果的，验证完可删。

## 4. 工作原理（对应原 DLL 蓝图）

- 入口 `ModEntry.Initialize()` 扫描 `image/cards/**`，把每个文件名归一化（小写、去下划线）
  成 key，建立 `key → 贴图路径` 字典。
- Harmony 补丁 `CardPortraitPatch` 挂在游戏刷新卡面的方法上（逆向为
  `NCardComponent.UpdateCardDisplay`）。每次卡面刷新后：取该卡 `id` → 查字典 → 找到卡内名为
  `_portrait` 的节点 → 换贴图并缩放到 606×852 铺满。查不到图就保持原版。
- 命名映射与原框架完全一致：卡牌 id `big_bang` ↔ 文件 `bigbang.png`（额外允许带下划线写法）。

## 5. 编译成 DLL（本地进行）

> ⚠️ 本仓库所在的云端环境**没有 dotnet、也没有游戏程序集**，无法在此编译或运行验证。
> 下面是本地步骤。首次编译若报「找不到类型/方法」，见第 6 节的 VERIFY 清单。

前置：
- .NET SDK 8（与 Godot 4.5 一致）
- 本地一份游戏安装（用于引用 `GodotSharp.dll` 和游戏核心程序集）

步骤：
```bash
# 把工程里游戏路径指向你本机安装目录
dotnet build CardArtReplacer.csproj -c Release -p:GameDir="D:\Steam\...\SlayTheSpire2"
```
产物是 `bin/Release/net8.0/CardArtReplacer.dll`。把 **DLL + `image/` 目录**一起按游戏的
mod 加载方式放进 mods 目录（参考同机其它可用 mod 的目录形态，或 config-manager 一类的加载器）。

## 6. 需要你核对的 API 点（VERIFY）

这些是我**从原 DLL 的字符串逆向出来**的名字，大概率正确，但没有游戏 SDK 无法编译验证。
若首次 build 报错，按序核对（源码里对应位置都标了 `VERIFY(n)`）：

| 编号 | 位置 | 需确认的东西 | 逆向得到的值 |
|------|------|-------------|-------------|
| 1 | `ModEntry.cs` | mod 入口标记/机制 | `MegaCrit.Sts2.Core.Modding` + `ModInitializer` |
| 2 | `CardPortraitPatch.TargetMethod` | 补丁目标类型.方法 | `NCardComponent.UpdateCardDisplay` |
| 3 | `CardPortraitPatch.ReadCardId` | 从组件取卡牌 id 的路径 | 组件→`Card/_card`(CardModel)→`Id/ModelId` |
| 4 | `CardPortraitPatch.CoverCardFace` | 立绘节点名与类型/定位 | 节点名 `_portrait`（TextureRect/Sprite2D） |
| 5 | `ReloadCommand.cs`（可选） | 控制台命令基类 | `AbstractConsoleCmd`（DevConsole） |

补丁用的是运行期反射（`AccessTools.TypeByName/Method`），所以**改名只需改字符串**，
不必大动结构。

## 7. 与 RTRsMoeifyMod 的关系

同一套换图思路与命名约定，但本 mod 是**从零写的精简子集**：只留卡面替换，
删掉了边框（`CardCustomBorderPatch`）、远古立绘（`AncientCardImageReplacementPatch`）、
鸣谢浮层（`InspectCardCreditsUpdatePatch`）和全部配置。想扩展时可回头参考原框架。

## 8. 免责

源码基于对已编译 DLL 的逆向重建，未在游戏内编译/运行验证。请在本地按第 5、6 节核对后使用。
卡图版权归各自作者，请勿未经授权二次分发他人作品。
