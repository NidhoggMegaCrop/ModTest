卡图命名规则（最重要，看这一条就够）
=====================================

文件名 = 卡牌的“类名”，大小写和下划线都不敏感（程序会统一小写并去掉下划线来匹配）。
放进哪个子目录都行（子目录只是给你自己整理用，程序不看目录，只看文件名）。

原理：官方做法用卡牌类名标识（__instance.GetType().Name），
原版卡类名基本就是卡名去空格的 PascalCase。

例子（卡名 -> 类名 -> 文件名，任一写法都行）：
  All For One -> AllForOne -> allforone.png / AllForOne.png / all_for_one.png
  Big Bang    -> BigBang   -> bigbang.png   / big_bang.png
  Creative AI -> CreativeAI-> creativeai.png

规格：
  - 606 x 852 像素，PNG（推荐）。整张全出血——你的图应包含插画+边框+标题底，
    因为本 mod 不额外画边框，直接用你的整图作为卡面立绘。
  - 游戏仍会在图上层渲染动态文字（费用/名称/描述/关键字），所以别把这些做进图里。

建议的分目录（可选）：
  defect/     Defect 等原版角色卡
  regent/     Regent 等新角色卡
  colorless/  无色卡
  curse/      诅咒卡

拿不准某张卡的类名？反编译本机 sts2.dll 看 CardModel 的子类名，
或用教程侧栏「工具 / ID 生成器」。
