卡图命名规则（最重要，看这一条就够）
=====================================

文件名 = 卡牌的“类名”，小写最稳（也认原样大小写），但【不要加下划线】。
以游戏里“发现模式”日志给出的名字为准，例如：
  [CardArtReplacer] card class: AscendersBane -> no art — put ascendersbane.png ...
就说明该文件叫 ascendersbane.png。

原理：官方做法用卡牌类名标识（__instance.GetType().Name），
原版卡类名基本就是卡名去空格的 PascalCase（进阶之灾 = AscendersBane）。

放哪：根目录，或下面这几个子目录之一（程序在这些位置按“类名.png”查找）：
  defect/     Defect 等原版角色卡
  regent/     Regent 等新角色卡
  colorless/  无色卡
  curse/      诅咒卡
想用别的子目录名，就到 src/CardArtLibrary.cs 的 SubFolders 里加上。

规格：
  - 606 x 852 像素，PNG（推荐）。整张全出血——含插画+边框+标题底。
  - 游戏仍会在图上层渲染动态文字（费用/名称/描述/关键字），别做进图里。

⚠️ 这些图必须最终落在 res://CardArtReplacer/image/cards/ 下（大小写、拼写与
   modid 完全一致），否则游戏在 pck 里找不到它们。
