"""Game mode: the New Game rule picker and the title's best scores (ids 530-559).

C# constants: UI_TitleScene (MODE_* and BEST_SCORE), read by UI_TitleScene and UI_ModePopup.
Same rules as battle.py: no newlines, commas or '^'; Korean and English stay inside the glyphs of
both pixel fonts (DNFBitBitv2 SDF, Silver SDF) - no middle dot, em dash, minus sign or multiplication sign.
Names follow Tools/story/GLOSSARY.md (potion = ポーション / 药水). 533 is the source of the Tower's Law name:
Steam/check_steam.py holds the achievement names to it.
"""

TEXT = {
    # UI_ModePopup: the question, then one row per mode (name + one line), then the keys
    530: ("규칙을 고르세요", "Choose the rules", "ルールを選んでください", "请选择规则"),
    531: ("보통", "Normal", "ノーマル", "普通"),
    532: ("처음이라면 이쪽을 권합니다. 물약에 여유가 있습니다.",
          "Recommended for your first climb. Potions leave some room for error.",
          "初めてならこちらがおすすめです。ポーションに余裕があります。",
          "初次攀登推荐选择此项。药水较为充裕。"),
    533: ("탑의 법", "Tower's Law", "塔の掟", "塔之法则"),
    # 곁길을 거르는 사람이 탑의 법에서 가장 많이 막혔다(재미 검증 v2 R4) — 한 줄로 미리 말한다.
    534: ("더 아픈 몬스터. 곁길까지 다 잡고 물약을 아껴야 버팁니다.",
          "Monsters hurt more. Clear the side paths and spend every potion with care.",
          "敵の一撃がより重い。脇道の敵も倒しポーションを大切に。",
          "怪物下手更狠。岔路怪物也要清掉。药水一瓶也别浪费。"),
    535: ("W/S 고르기   Enter 시작   Esc 뒤로", "W/S Choose   Enter Start   Esc Back",
          "W/S 選択   Enter 開始   Esc 戻る", "W/S 选择   Enter 开始   Esc 返回"),
    # UI_TitleScene, under "Endings n/3": {0} = mode name (531 / 533), {1} = best run score (lower is better)
    536: ("{0} 최고 {1}", "{0} best {1}", "{0} ベスト {1}", "{0} 最佳 {1}"),
    # after the Tower's Law line once a Tower's Law run reached an ending (Records.ModeCleared)
    537: ("완주", "Cleared", "踏破", "通关"),
}


if __name__ == "__main__":
    # Self-check: ids in the block, no forbidden characters, placeholders as C# passes them, KR/EN glyphs in both pixel fonts.
    import re
    from pathlib import Path
    font_dir = Path(__file__).resolve().parents[2] / "Assets/@Resources/Font"

    def glyphs(name):
        text = (font_dir / f"{name}.asset").read_text(encoding="utf-8", errors="replace")
        table = text.split("  m_CharacterTable:", 1)[1].split("\n  m_", 1)[0]
        return {int(u) for u in re.findall(r"m_Unicode: (\d+)", table)}

    args = {536: 2}
    fonts = {name: glyphs(name) for name in ("Silver SDF", "DNFBitBitv2 SDF")}
    for sid, values in TEXT.items():
        assert 530 <= sid < 560 and len(values) == 4, sid
        for value in values:
            assert value and not set(value) & set(",^\n"), (sid, value)
            assert set(re.findall(r"\{(\d+)\}", value)) == {str(i) for i in range(args.get(sid, 0))}, (sid, value)
        for value in values[:2]:
            for name, have in fonts.items():
                missing = sorted({c for c in value if ord(c) not in have})
                assert not missing, (sid, name, missing, value)
    print(f"mode.py: {len(TEXT)} strings OK")
