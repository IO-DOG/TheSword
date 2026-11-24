"""Battle forecast: map overlay, monster manual, fatal-fight question, warp (ids 260-299).

C# constants: ForecastUI (Assets/@Scripts/Utils/ForecastUI.cs).
No newlines, commas or '^' inside the strings (see ui.py). Korean and English must stay inside the
glyphs of the two pixel fonts (DNFBitBitv2 SDF, Silver SDF) - no middle dot, em dash or multiplication
sign; Japanese and Chinese punctuation is drawn by the runtime fallback (FontFallback).
"""

TEXT = {
    # UI_MonsterManualPopup
    260: ("몬스터 도감", "Monster Manual", "モンスター図鑑", "怪物图鉴"),
    # ★ = ForecastUI.LevelUpMark (one glyph: "LV+" ran into the next number on the map)
    261: ("숫자 = 다음에 싸우면 잃는 체력 (스킬 제외)   X = 진다   ★ = 레벨 업",
          "Number = HP lost if you fight it next (no skills)   X = you lose   ★ = level up",
          "数字 = 次に戦うと失う体力 (スキル抜き)   X = 負ける   ★ = レベルアップ",
          "数字 = 下一战损失的生命 (不计技能)   X = 会输   ★ = 升级"),
    262: ("{0}마리", "x{0}", "{0}体", "{0}只"),                        # {0} = alive on this floor
    263: ("다음에 싸우면", "If fought next", "次に戦うと", "下一战"),
    264: ("{0}대 맞음", "hits taken: {0}", "被弾 {0}回", "挨打 {0}次"),
    # 임계: {0} = ATK increase / {1} = HP saved
    265: ("공격 +{0} → 한 대 덜 맞음", "ATK +{0} → one hit fewer", "攻撃 +{0} → 被弾 1回減", "攻击 +{0} → 少挨一次"),
    266: ("공격 +{0} → -{1}", "ATK +{0} → -{1}", "攻撃 +{0} → -{1}", "攻击 +{0} → -{1}"),
    267: ("공격 +{0} → 이긴다", "ATK +{0} → you win", "攻撃 +{0} → 勝てる", "攻击 +{0} → 能赢"),
    268: ("방어 +1 → -{0}", "DEF +1 → -{0}", "防御 +1 → -{0}", "防御 +1 → -{0}"),
    269: ("방어 +1 → 이긴다", "DEF +1 → you win", "防御 +1 → 勝てる", "防御 +1 → 能赢"),
    270: ("치명까지 {0}타", "Crit in {0}", "会心まで {0}回", "距暴击 {0}次"),   # also on the HUD
    271: ("첫 타 치명", "First hit crits", "初撃が会心", "首击暴击"),
    272: ("경험치 {0}", "EXP {0}", "経験値 {0}", "经验 {0}"),
    273: ("이 층에는 남은 몬스터가 없다.", "No monsters left on this floor.",
          "この階にモンスターは残っていない。", "本层没有剩下的怪物。"),
    274: ("WASD 이동   마우스 올리기 정보   M 도감   V 예측 표시   I 가방   Tab 워프   1/2/3 스킬   Esc 메뉴",
          "WASD Move   Hover Info   M Manual   V Forecast   I Bag   Tab Warp   1/2/3 Skills   Esc Menu",
          "WASD 移動   マウスオーバー 情報   M 図鑑   V 予測表示   I 持ち物   Tab ワープ   1/2/3 スキル   Esc メニュー",
          "WASD 移动   鼠标悬停 信息   M 图鉴   V 预测显示   I 背包   Tab 传送   1/2/3 技能   Esc 菜单"),
    # UI_GameScene
    275: ("M: 몬스터 도감   V: 예측 표시", "M: Monster Manual   V: Forecast",
          "M: モンスター図鑑   V: 予測表示", "M: 怪物图鉴   V: 预测显示"),
    276: ("전투 예측 표시 켬", "Battle forecast on", "戦闘予測 表示オン", "已显示战斗预测"),
    277: ("전투 예측 표시 끔", "Battle forecast off", "戦闘予測 表示オフ", "已隐藏战斗预测"),
    278: ("방패 준비", "Guard ready", "ガード準備完了", "护盾就绪"),
    # FatalFightGuard: {0} = expected damage / {1} = current HP
    279: ("이 싸움은 진다. 예상 피해 {0} / 체력 {1}. 그래도 싸울까요?",
          "You will lose this fight. Expected damage {0} / HP {1}. Fight anyway?",
          "この戦いは負ける。予想ダメージ {0} / 体力 {1}。それでも戦いますか？",
          "这一战会输。预计伤害 {0} / 生命 {1}。仍要战斗吗？"),
    280: ("지금은 쓰러뜨릴 수 없는 상대다. 그래도 싸울까요?", "You cannot defeat this foe yet. Fight anyway?",
          "今は倒せない相手だ。それでも戦いますか？", "现在还打不倒这个对手。仍要战斗吗？"),
    281: ("결과를 읽을 수 없는 상대다. 싸울까요?", "This fight cannot be foreseen. Fight?",
          "結果が読めない相手だ。戦いますか？", "无法预测这一战的结果。要战斗吗？"),
    282: ("싸운다", "Fight", "戦う", "战斗"),
    283: ("물러선다", "Back off", "退く", "后退"),
    # UI_CItemInfo: {0} = HP healed / {1} = HP wasted over the maximum
    284: ("회복 +{0} (넘침 {1})", "Heal +{0} ({1} wasted)", "回復 +{0} (溢れ {1})", "恢复 +{0} (溢出 {1})"),
    # WarpUI
    285: ("워프", "Warp", "ワープ", "传送"),
    286: ("{0}층", "{0}F", "{0}階", "{0}层"),
    287: ("금고", "Vault", "金庫", "金库"),
    288: ("갈 층을 누르세요   Tab / Esc 닫기", "Click a floor   Tab / Esc to close",
          "行く階をクリック   Tab / Esc 閉じる", "点击要去的楼层   Tab / Esc 关闭"),
    # 도감 아래 "이 층" 줄 — 챕터 기믹(11-24). 첫 만남 수업(mechanic_first)은 한 번뿐이라 다시 볼 자리
    289: ("이 층", "This floor", "この階", "本层"),
    290: ("물살: 화살표 쪽으로만 들어가고 거꾸로는 못 나온다. 안쪽 레버를 당기면 멎는다.",
          "Current: enter only along the arrows. You cannot go back against it. Pull the lever inside to stop it.",
          "水流: 矢印の向きにしか入れず 逆には戻れない。奥のレバーを引けば止まる。",
          "水流：只能顺着箭头进入 不能逆流返回。拉下里面的拉杆就会停止。"),
    291: ("화로: 켜진 동안 그 방 몬스터(발밑 불빛)의 공격이 세다. 밸브를 잠그면 꺼진다.",
          "Furnace: while lit the monsters in its room (glow at their feet) hit harder. Close the valve to put it out.",
          "炉: 燃えている間 その部屋の敵(足元の光)は攻撃が強い。バルブを閉めれば消える。",
          "火炉：燃着时 该房间的怪物(脚下发光)攻击更强。关上风门就会熄灭。"),
    292: ("얼음: 디디면 막힐 때까지 미끄러진다. 몬스터 앞에서 서고 물건은 주우며 선다.",
          "Ice: you slide until something stops you. You stop before monsters and on items.",
          "氷: 踏めば止まるまで滑る。敵の手前で止まり 物は拾って止まる。",
          "冰面：踩上就一直滑到被挡住。在怪物前停下 踩到物品会拾起并停下。"),
    293: ("균열: 한쪽을 밟으면 다른 쪽에 선다. 사이의 몬스터를 건너뛰면 체력은 아끼고 경험치는 못 받는다.",
          "Rift: step into one and you stand at the other. Skipping the monster between saves HP but gives no EXP.",
          "裂け目: 片方を踏めばもう片方に立つ。間の敵を飛ばせば体力は残るが経験値は入らない。",
          "裂缝：踩进一边就站到另一边。跳过中间的怪物能省体力 但得不到经验。"),
    294: ("(멈춤)", "(stopped)", "(停止)", "(已停止)"),
}


if __name__ == "__main__":
    # Self-check: Korean/English glyphs exist in both pixel fonts, no forbidden characters, placeholders parse.
    import re
    from pathlib import Path
    font_dir = Path(__file__).resolve().parents[2] / "Assets/@Resources/Font"

    def glyphs(name):
        text = (font_dir / f"{name}.asset").read_text(encoding="utf-8", errors="replace")
        table = text.split("  m_CharacterTable:", 1)[1].split("\n  m_", 1)[0]
        return {int(u) for u in re.findall(r"m_Unicode: (\d+)", table)}

    fonts = {name: glyphs(name) for name in ("Silver SDF", "DNFBitBitv2 SDF")}
    for sid, values in TEXT.items():
        assert 260 <= sid < 300 and len(values) == 4, sid
        for value in values:
            assert not set(value) & set(",^\n"), (sid, value)
            value.format(*range(3))
        for value in values[:2]:
            for name, have in fonts.items():
                missing = sorted({c for c in value if ord(c) not in have})
                assert not missing, (sid, name, missing, value)
    print(f"forecast.py: {len(TEXT)} strings OK")
