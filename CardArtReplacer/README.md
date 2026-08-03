# CardArtReplacer —— 精简版「只换卡面」mod

一个按 `RTRsMoeifyMod` 蓝图重建、**只保留卡面替换**的杀戮尖塔2 mod。
去掉了原框架的自定义边框、远古立绘、画师鸣谢、配置开关等一切其它功能。
工程按**官方教程（环境配置一章）**校准，本地 `dotnet build` 即可。

你日常要做的只有一件事：**把 606×852 的图按卡牌 id 命名，丢进 `image/cards/` 里。**

---

## 1. 它做什么 / 不做什么

- ✅ 把原版角色卡、无色卡、诅咒卡的**立绘换成你的整图**，铺满整张 606×852 卡面。
- ✅ 你的图自带边框/标题底（全图异画）；游戏仍在其上渲染动态文字（费用/名称/描述/关键字）。
- ❌ 不画自定义边框、不换远古立绘、不显示鸣谢、无任何配置项。
- ❌ 不新增卡牌/不改数值——这是**换画**框架，不是加卡框架。

## 2. 目录结构

```
CardArtReplacer/
├─ CardArtReplacer.csproj      编译工程（官方 Godot.NET.Sdk 模板）
├─ CardArtReplacer.json        mod 清单（必须，随 dll 一起放进 mods 目录）
├─ src/
│  ├─ Entry.cs                 入口 [ModInitializer]：扫描目录 + Harmony.PatchAll
│  ├─ CardArtLibrary.cs        扫 image/cards/**，建「卡id→贴图」映射
│  ├─ CardPortraitPatch.cs     核心补丁：patch CardModel.PortraitPath 返回你的图路径
│  └─ ReloadCommand.cs         可选：控制台热重载命令（默认注释）
└─ image/cards/                ★ 你只需要动这里
   ├─ defect/  regent/  colorless/  curse/
   └─ colorless/_sample_full_face.png   606×852 占位样例（可删）
```

## 3. 加/换一张卡的图（不用懂编程）

1. 做一张 **606×852 PNG**，整张全出血（含边框+标题底；**别**把费用/描述文字做进去）。
2. 命名为该卡的 **类名**（大小写、下划线都不敏感）：`BigBang` 这张卡命名成
   `bigbang.png`、`BigBang.png` 或 `big_bang.png` 都能匹配。
3. 丢进 `image/cards/` 下任意子目录（目录只为你自己整理，程序只认文件名）。
4. 重新导出 pck（见第 6 节）进游戏即可。命名细节另见 `image/cards/README.txt`。

> 卡牌用**类名**标识（官方做法：`__instance.GetType().Name`），不是 snake_case 的 id。
> 原版卡类名基本就是卡名去空格的 PascalCase（`All For One` → `AllForOne`）。
> 拿不准就反编译本地 `sts2.dll`，或用教程侧栏的「ID 生成器」查。

## 4. 开发环境（官方教程确认）

- **Godot 4.5.1 Mono**（.NET 版），新建项目渲染器选 **Mobile/移动**（与游戏一致）。
- **.NET SDK 9**（或更高）。
- 编辑器：Rider（新手推荐）/ VS Code（装 C# Dev Kit）/ VS 均可。
- 游戏自带依赖，位于 `<游戏目录>\data_sts2_windows_x86_64\`：
  - `sts2.dll` —— 游戏核心程序集（命名空间 `MegaCrit.Sts2.Core.*`）
  - `0Harmony.dll` —— Harmony（打补丁用），**不用装 NuGet**

## 5. 一个 mod 的三个组成部分

放进 `<游戏目录>\mods\CardArtReplacer\` 下：

| 文件 | 作用 | 本 mod |
|------|------|--------|
| `CardArtReplacer.dll` | 代码（换图补丁） | 必须 |
| `CardArtReplacer.pck` | 素材资源（你的卡图） | 必须 |
| `CardArtReplacer.json` | 清单（id/版本/依赖…） | 必须 |

> 清单里本 mod 已设 `"affects_gameplay": false`（纯装饰，不影响多人内容）。

## 6. 编译 + 打包（本地进行）

> ⚠️ 本仓库所在云端环境**没有 dotnet、也没有游戏程序集**，无法在此编译/验证。
> 下面是本地步骤。首次编译报「找不到类型/方法」，见第 7 节 VERIFY 清单。

**准备**：把 `CardArtReplacer.csproj` 顶部的 `<Sts2Dir>` 改成你本机安装目录。

**编译 dll**（会自动复制 dll+json 到 `mods\CardArtReplacer\`）：
```bash
dotnet build -c Release
```

**导出 pck**（把 image/ 资源打包）：回 Godot 编辑器 →「项目→导出」→ 添加一个
Windows 预设 →「导出 pck/zip」→ 文件名 `CardArtReplacer.pck`，存到
`mods\CardArtReplacer\`。（一定是 **pck**。）
> 也可用命令行 `dotnet build -t:ExportPck` 一步导出——取消 `.csproj` 里 `ExportPck`
> 那段注释并填 `<GodotExe>` 路径即可。Mac 需把 `export_presets.cfg` 里
> `binary_format/architecture="x86_64"` 改成 `"msil"`。

**运行验证**：启动游戏，首次问是否开启 mod 选「是」（会自动关闭），再打开一次；
右下角显示「已加载模组」即成功。

> ⚠️ 前提：你的 Godot 项目里，卡图资源要落在 `res://CardArtReplacer/image/cards/**`
> （即项目根下有个与 modid 同名的文件夹）。代码写死读这个路径。

## 7. 补丁做法（官方教程「卡图&Spine」）

替换卡图就是 patch `CardModel.PortraitPath` 这个属性的 **getter**，在 `Postfix` 里按卡牌
类名把返回的资源路径改成你的图：

```csharp
[HarmonyPatch(typeof(CardModel), nameof(CardModel.PortraitPath), MethodType.Getter)]
static void Postfix(CardModel __instance, ref string __result)
{
    var path = CardArtLibrary.ResolvePath(__instance?.GetType().Name);
    if (path != null && ResourceLoader.Exists(path)) __result = path;
}
```

本工程的 `CardPortraitPatch.cs` 用**反射**版实现同样的逻辑（`AccessTools.PropertyGetter`），
好处是编译期不依赖 `CardModel` 的具体命名空间，**开箱即能 build**。

唯一可选的一步：若你想换成上面的**强类型写法**（更简洁），需要在文件顶部
`using` 上 `CardModel` 所在的命名空间（反编译 `sts2.dll` 一看便知）。不换就保持反射版即可，
功能完全一样。

> 说明：官方明确此法「只能替换原版卡图」——正好覆盖你要的原版角色/无色/诅咒卡。

## 8. 与 RTRsMoeifyMod 的关系

同一套换图思路与命名约定，本 mod 是**从零写的精简子集**：只留卡面替换，删掉了
边框（`CardCustomBorderPatch`）、远古立绘（`AncientCardImageReplacementPatch`）、
鸣谢浮层（`InspectCardCreditsUpdatePatch`）和全部配置。想扩展时可回头参考原框架。

## 9. 免责

补丁做法来自官方教程，但本云端环境无法编译/运行验证；`CardModel` 命名空间（仅强类型写法需要）
请以本地 `sts2.dll` 为准。卡图版权归各自作者，勿未经授权二次分发他人作品。
