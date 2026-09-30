"""Battle speed and skip: settings row, battle-window hints, skip summary (ids 350-379).

C# constants: UI_SettingPopup (350-351) and UI_BattlePopup (352-356).
Same rules as forecast.py: no newlines, commas or '^'; Korean and English stay inside the glyphs of
both pixel fonts (DNFBitBitv2 SDF, Silver SDF) - no middle dot, em dash, minus sign or multiplication sign.
"""

TEXT = {
    # UI_SettingPopup: "전투 속도  2배속" ({0} = 1 / 2 / 4)
    350: ("전투 속도", "Battle speed", "戦闘速度", "战斗速度"),
    351: ("{0}배속", "{0}x", "{0}倍速", "{0}倍速"),
    # UI_BattlePopup: above the skill bar (always) / the skip button (only when the fight can be skipped)
    352: ("Space 길게: 8배속", "Hold Space: 8x speed", "Space 長押し: 8倍速", "按住 Space: 8倍速"),
    353: ("Tab: 건너뛰기", "Tab: Skip", "Tab: スキップ", "Tab: 跳过"),
    # skip summary: {0} = HP paid in this fight / HP the forecast said
    354: ("체력 -{0}", "HP -{0}", "体力 -{0}", "生命 -{0}"),
    355: ("예측 -{0}", "Forecast -{0}", "予測 -{0}", "预测 -{0}"),
    # once, the first fight that can be skipped
    356: ("이제 Tab 으로 전투를 건너뛸 수 있습니다. 결과는 예측 그대로입니다.",
          "You can now skip fights with Tab. The result is exactly the forecast.",
          "Tabで戦闘をスキップできます。結果は予測どおりです。",
          "现在可以按 Tab 跳过战斗。结果与预测完全一致。"),
}


if __name__ == "__main__":
    # Self-check: ids in the block, no forbidden characters, placeholders parse, KR/EN glyphs exist in both pixel fonts.
    import re
    from pathlib import Path
    font_dir = Path(__file__).resolve().parents[2] / "Assets/@Resources/Font"

    def glyphs(name):
        text = (font_dir / f"{name}.asset").read_text(encoding="utf-8", errors="replace")
        table = text.split("  m_CharacterTable:", 1)[1].split("\n  m_", 1)[0]
        return {int(u) for u in re.findall(r"m_Unicode: (\d+)", table)}

    fonts = {name: glyphs(name) for name in ("Silver SDF", "DNFBitBitv2 SDF")}
    for sid, values in TEXT.items():
        assert 350 <= sid < 380 and len(values) == 4, sid
        for value in values:
            assert value and not set(value) & set(",^\n"), (sid, value)
            value.format(*range(3))
        for value in values[:2]:
            for name, have in fonts.items():
                missing = sorted({c for c in value if ord(c) not in have})
                assert not missing, (sid, name, missing, value)
    print(f"battle.py: {len(TEXT)} strings OK")
