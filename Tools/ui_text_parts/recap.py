"""Death recap on the game-over screen (ids 690-709).

C# constants: UI_GameOverPopup. The lines are facts only - who, where, the price the sword's eye showed against the HP you
had, and what was left unused on that floor. Same rules as feel.py: no newlines, commas or '^'; numbers are put in by C#.
Drawn with the prose font (Silver SDF), so Korean and English must be inside its glyphs.
"""

TEXT = {
    # {0} = floor number, {1} = monster name
    690: ("{0}층에서 {1}에게 쓰러졌다", "Fell to {1} on floor {0}", "{0}階で{1}に倒された", "在第{0}层倒在{1}面前"),
    # second line, three parts joined with spaces by C#: price of the fight, HP at its start, the shortfall (shown in red)
    691: ("싸움의 값 {0}", "Fight cost {0}", "戦いの値 {0}", "这一战的代价 {0}"),
    692: ("가진 체력 {0}", "Your HP {0}", "体力 {0}", "生命 {0}"),
    693: ("{0} 모자랐다", "Short by {0}", "{0} 足りなかった", "还差 {0}"),
    # potions that could be walked to without a fight or a door when the fight was started. {0} = count
    694: ("싸우지 않고 닿는 물약 {0}개를 남겼다", "Potions left within reach: {0}", "戦わずに届く薬を {0} 個残した", "不用战斗就能拿到的药水还剩 {0} 瓶"),
    # followed by the skill names (191-193) joined with spaces
    695: ("쓰지 않은 스킬", "Unused skills", "未使用のスキル", "未使用的技能"),
}


if __name__ == "__main__":
    # Self-check (as feel.py): glyphs of the prose font for Korean/English, no forbidden characters, and the same
    # placeholders in every language. Run after editing a translation: the bake does not run it.
    import re
    from pathlib import Path
    font_dir = Path(__file__).resolve().parents[2] / "Assets/@Resources/Font"
    text = (font_dir / "Silver SDF.asset").read_text(encoding="utf-8", errors="replace")
    table = text.split("  m_CharacterTable:", 1)[1].split("\n  m_", 1)[0]
    have = {int(u) for u in re.findall(r"m_Unicode: (\d+)", table)}
    args = {690: 2, 691: 1, 692: 1, 693: 1, 694: 1, 695: 0}
    for sid, values in TEXT.items():
        assert 690 <= sid < 710 and len(values) == 4, sid
        for value in values:
            assert not set(value) & set(",^\n"), (sid, value)
            assert set(re.findall(r"\{(\d+)\}", value)) == {str(i) for i in range(args[sid])}, (sid, value)
        for value in values[:2]:
            missing = sorted({c for c in value if ord(c) not in have})
            assert not missing, (sid, missing, value)
    print(f"recap.py: {len(TEXT)} strings OK")
