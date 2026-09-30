"""Feel: price changes you can see, rune gains, spilled potions, what-if tooltips (ids 410-439).

C# constants: ForecastUI (Assets/@Scripts/Utils/ForecastUI.cs). Same rules as forecast.py: no newlines, commas
or '^' inside the strings; Korean and English stay inside the glyphs of the two pixel fonts (DNFBitBitv2 SDF,
Silver SDF). Arrows and prices (-31, X) are put together in C#, like the manual's lines.
"""

TEXT = {
    # Stat gains. Floating text over the player when a rune is picked up (ObjectManager.ShowRuneGain), and the
    # "if picked up" part of the item tooltip (UI_CItemInfo). Same words as the manual's 265-269.
    410: ("공격 +{0}", "ATK +{0}", "攻撃 +{0}", "攻击 +{0}"),
    411: ("방어 +{0}", "DEF +{0}", "防御 +{0}", "防御 +{0}"),
    412: ("최대 체력 +{0}", "Max HP +{0}", "最大体力 +{0}", "最大生命 +{0}"),
    # UI_CItemInfo: "ATK +4 → no change" when no monster left on the floor gets cheaper
    413: ("변화 없음", "no change", "変化なし", "没有变化"),
    # UI_MonsterInfo next breakpoint: {0} = 410 / {1}→{2} = price now → with that ATK (-31→-24 or X→-44)
    414: ("{0}이면 {1}→{2}", "With {0}: {1}→{2}", "{0} なら {1}→{2}", "{0} 时 {1}→{2}"),
    # Potion floating text when healing spills over max HP: "40 (80 wasted)". {0} = HP wasted. Same words as the
    # potion tooltip's 284 so the float and the tooltip read alike.
    415: ("(넘침 {0})", "({0} wasted)", "({0} 無駄)", "(溢出 {0})"),
}


if __name__ == "__main__":
    # Self-check (as forecast.py): Korean/English glyphs exist in both pixel fonts, no forbidden characters, and
    # every language uses exactly the placeholders C# passes - a stray {1} makes string.Format throw, and the
    # float is dropped (ObjectManager logs it). Run this after editing a translation: the bake does not run it.
    import re
    from pathlib import Path
    font_dir = Path(__file__).resolve().parents[2] / "Assets/@Resources/Font"

    def glyphs(name):
        text = (font_dir / f"{name}.asset").read_text(encoding="utf-8", errors="replace")
        table = text.split("  m_CharacterTable:", 1)[1].split("\n  m_", 1)[0]
        return {int(u) for u in re.findall(r"m_Unicode: (\d+)", table)}

    args = {410: 1, 411: 1, 412: 1, 413: 0, 414: 3, 415: 1}
    fonts = {name: glyphs(name) for name in ("Silver SDF", "DNFBitBitv2 SDF")}
    for sid, values in TEXT.items():
        assert 410 <= sid < 440 and len(values) == 4, sid
        for value in values:
            assert not set(value) & set(",^\n"), (sid, value)
            assert set(re.findall(r"\{(\d+)\}", value)) == {str(i) for i in range(args[sid])}, (sid, value)
        for value in values[:2]:
            for name, have in fonts.items():
                missing = sorted({c for c in value if ord(c) not in have})
                assert not missing, (sid, name, missing, value)
    print(f"feel.py: {len(TEXT)} strings OK")
