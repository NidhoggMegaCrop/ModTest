卡图命名规则（最重要，看这一条就够）
=====================================

文件名 = 卡牌的 id，全小写；下划线可有可无。
放进哪个子目录都行（子目录只是给你自己整理用，程序不看目录，只看文件名）。

例子：
  big_bang        -> big_bang.png      或  bigbang.png
  all_for_one     -> all_for_one.png   或  allforone.png
  creative_ai     -> creative_ai.png   或  creativeai.png

规格：
  - 606 x 852 像素，PNG（推荐）。整张全出血——你的图应包含插画+边框+标题底，
    因为本 mod 不额外画边框，直接用你的整图铺满卡面。
  - 游戏仍会在图上层渲染动态文字（费用/名称/描述/关键字），所以别把这些做进图里。

建议的分目录（可选）：
  defect/     Defect 等原版角色卡
  regent/     Regent 等新角色卡
  colorless/  无色卡
  curse/      诅咒卡

不知道某张卡的 id？参考同框架 RTRsMoeifyMod/data/art_credits.yaml 里的键名，
或查游戏本体的卡牌定义。
