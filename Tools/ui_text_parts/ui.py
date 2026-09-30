"""Menus, confirm popups and the checkpoint list (ids 180-229).

No newlines, commas or '^' inside the strings: the GeneratedUiText fallback returns them
raw (no ScriptData escape handling), and the sheet converter splits on commas.
"""

TEXT = {
    # UI_MenuPopup: rows that exist only in the in-game menu
    180: ("이 층 다시 시작", "Restart floor", "この階をやり直す", "重新开始本层"),
    181: ("체크포인트", "Checkpoints", "チェックポイント", "检查点"),
    182: ("타이틀로", "Back to title", "タイトルへ", "返回标题"),
    # UI_MenuPopup: confirm questions
    183: ("이 층에 들어섰을 때로 돌아갈까요? 그 뒤의 진행은 사라집니다.",
          "Return to when you entered this floor? Progress since then will be lost.",
          "この階に入った時点に戻りますか？それ以降の進行は失われます。",
          "要回到进入本层时的状态吗？之后的进度将会丢失。"),
    184: ("타이틀로 돌아갈까요? 마지막으로 층에 들어섰을 때부터 이어집니다.",
          "Return to the title? You will resume from the last floor you entered.",
          "タイトルに戻りますか？最後に階に入った時点から再開します。",
          "要返回标题画面吗？将从最后进入的楼层继续。"),
    185: ("게임을 끌까요? 마지막으로 층에 들어섰을 때부터 이어집니다.",
          "Quit the game? You will resume from the last floor you entered.",
          "ゲームを終了しますか？最後に階に入った時点から再開します。",
          "要退出游戏吗？将从最后进入的楼层继续。"),
    # {0} = floor number
    186: ("{0}층에 들어섰을 때로 돌아갈까요? 지금까지의 진행은 사라집니다.",
          "Return to when you entered floor {0}? Current progress will be lost.",
          "{0}階に入った時点に戻りますか？現在の進行は失われます。",
          "要回到进入第{0}层时的状态吗？当前进度将会丢失。"),
    # checkpoint list row: {0} floor, {1} level, {2}/{3} HP (the save time goes on a second line)
    187: ("{0}층  Lv {1}  HP {2}/{3}", "Floor {0}  Lv {1}  HP {2}/{3}",
          "{0}階  Lv {1}  HP {2}/{3}", "第{0}层  Lv {1}  HP {2}/{3}"),
    188: ("아직 돌아갈 체크포인트가 없습니다.", "No checkpoints yet.",
          "まだチェックポイントがありません。", "还没有可以返回的检查点。"),
    189: ("체크포인트를 불러오지 못했습니다. 지금 진행은 그대로입니다.",
          "Could not load the checkpoint. Your current progress is unchanged.",
          "チェックポイントを読み込めませんでした。現在の進行はそのままです。",
          "无法读取检查点。当前进度保持不变。"),
    # UI_TitleScene: New game while a save exists
    190: ("새 게임을 시작하면 지금 저장이 지워집니다. 시작할까요?",
          "Starting a new game erases your current save. Start anyway?",
          "ニューゲームを始めると現在のセーブが消えます。始めますか？",
          "开始新游戏将删除当前存档。确定开始吗？"),
    # UI_BattlePopup skill bar (keys 1/2/3), BattleSkills.Kind order. Names follow Tools/story/GLOSSARY.md.
    191: ("강타", "Smash", "強打", "强击"),
    192: ("철벽", "Bulwark", "鉄壁", "铁壁"),
    193: ("흡혈", "Drain", "吸血", "吸血"),
    # UI_InvenPopup short stat labels beside the numbers (the long names 100-108 are the hover info).
    # The column is narrow: a space is a line break, and only the two-line slots (CRI ATK, the three
    # speeds) may contain one - each line at most two CJK glyphs.
    194: ("공격", "ATK", "攻撃", "攻击"),
    195: ("방어", "DEF", "防御", "防御"),
    196: ("체력", "HP", "体力", "生命"),
    197: ("치명", "CRI", "会心", "暴击"),
    198: ("치명 공격", "CRI ATK", "会心 威力", "暴击 伤害"),
    199: ("레벨", "LV", "Lv", "等级"),
    200: ("공격 속도", "ATK SPEED", "攻撃 速度", "攻击 速度"),
    201: ("방어 속도", "DEF SPEED", "防御 速度", "防御 速度"),
    202: ("이동 속도", "MOVE SPEED", "移動 速度", "移动 速度"),
}
