"""The Sword's Ledger: band tally and altar, fullness gauge, run card (ids 470-529).

C# constants: UI_TallyPopup (470-481), UI_GameScene (490-492), UI_EndingScene (500-504).
Same rules as battle.py: no newlines, commas or '^'; Korean and English stay inside the glyphs of
both pixel fonts (DNFBitBitv2 SDF, Silver SDF) - no middle dot, em dash, minus sign or multiplication sign.
"""

TEXT = {
    # UI_TallyPopup title: {0}~{1} = first and last floor of the band (1~5, 6~10 ...)
    470: ("{0}~{1}층 결산", "Floors {0}-{1}: tally", "{0}〜{1}階の決算", "第{0}~{1}层 结算"),
    # foretold (sum of the eye's numbers) / paid (HP actually lost) / par (the designed price). Also the run card.
    471: ("예언 {0}  치름 {1}  기준 {2}", "Foretold {0}  Paid {1}  Par {2}",
          "予言 {0}  支払い {1}  基準 {2}", "预言 {0}  付出 {1}  基准 {2}"),
    # what the stars mean (only when the band has a par)
    472: ("★ 통과  ★★ 기준 이하  ★★★ 기준의 70% 이하", "★ cleared  ★★ at or under par  ★★★ 70% of par or less",
          "★ 突破  ★★ 基準以下  ★★★ 基準の70%以下", "★ 通过  ★★ 不超过基准  ★★★ 基准的70%以下"),
    # {0} = trait names of the next five floors (joined in C#)
    473: ("다음 다섯 층: {0}", "Next five floors: {0}", "この先5階分: {0}", "接下来五层: {0}"),
    # altar at the stairs
    474: ("제단", "Altar", "祭壇", "祭坛"),
    # {0} = price in % of max HP (10 + n(n-1))
    475: ("최대 체력의 {0}%를 바치고 하나를 받는다", "Offer {0}% of max HP to receive one",
          "最大体力の{0}%を捧げて一つ受け取る", "献上最大生命的{0}%换取一项"),
    # {0} = HP paid
    476: ("체력 -{0}", "HP -{0}", "体力 -{0}", "生命 -{0}"),
    477: ("지나간다", "Pass", "通り過ぎる", "离开"),
    # refused: {0} = HP that must remain after paying (AltarReserve / AltarReserveTower)
    478: ("체력이 모자라다: 바친 뒤에도 {0} 이상 남아야 한다", "Not enough HP: at least {0} must remain after the offering",
          "体力が足りない: 捧げた後も{0}以上残す必要がある", "生命不足: 献上后须剩余{0}以上"),
    # after buying: {0} = "공격 +3" (feel.py 410/411)
    479: ("받았다: {0}", "Received: {0}", "受け取った: {0}", "获得: {0}"),
    480: ("←→ 고르기  Enter 받기  Esc 지나간다", "←→ choose  Enter receive  Esc pass",
          "←→ 選ぶ  Enter 受け取る  Esc 通り過ぎる", "←→ 选择  Enter 领取  Esc 离开"),
    481: ("Enter 계속", "Enter continue", "Enter 続ける", "Enter 继续"),

    # UI_GameScene fullness gauge: {0} level now, {1} this floor's par level (ParLevelIn), {2} difference (+2 / -1)
    490: ("Lv {0}  기준 {1} ({2})", "Lv {0}  Par {1} ({2})", "Lv {0}  基準 {1} ({2})", "Lv {0}  基准 {1} ({2})"),
    # from floor 90: the level the sword wants at the throne (StoryDirector.DawnLevel, the dawn ending's gate)
    491: ("왕좌에서 Lv {0}", "At the throne: Lv {0}", "玉座で Lv {0}", "抵达王座时 Lv {0}"),
    # badge next to the floor name / on the run card. Same words as mode.py 533 (the New Game picker).
    492: ("탑의 법", "Tower's Law", "塔の掟", "塔之法则"),

    # UI_EndingScene run card
    500: ("마검의 장부", "The Sword's Ledger", "魔剣の帳簿", "魔剑的账簿"),
    # {0} HP offered at altars (inside Paid) / {1} healing spilled over max HP (not in the score)
    501: ("제단 {0}  넘친 회복 {1}", "Altar {0}  Healing spilled {1}", "祭壇 {0}  溢れた回復 {1}", "祭坛 {0}  溢出治疗 {1}"),
    # {0} = round(1000 * paid / par)
    502: ("점수 {0}", "Score {0}", "スコア {0}", "分数 {0}"),
    503: ("낮을수록 좋다", "lower is better", "低いほど良い", "越低越好"),
    # {0} = chapter number, followed by its four bands' stars
    504: ("{0}장", "Ch. {0}", "第{0}章", "第{0}章"),
}


if __name__ == "__main__":
    # Self-check (as battle.py): ids in the block, no forbidden characters, the placeholders C# passes and no
    # others (a stray {1} makes string.Format throw), KR/EN glyphs exist in both pixel fonts.
    import re
    from pathlib import Path
    font_dir = Path(__file__).resolve().parents[2] / "Assets/@Resources/Font"

    def glyphs(name):
        text = (font_dir / f"{name}.asset").read_text(encoding="utf-8", errors="replace")
        table = text.split("  m_CharacterTable:", 1)[1].split("\n  m_", 1)[0]
        return {int(u) for u in re.findall(r"m_Unicode: (\d+)", table)}

    args = {470: 2, 471: 3, 472: 0, 473: 1, 474: 0, 475: 1, 476: 1, 477: 0, 478: 1, 479: 1, 480: 0, 481: 0,
            490: 3, 491: 1, 492: 0, 500: 0, 501: 2, 502: 1, 503: 0, 504: 1}
    assert set(args) == set(TEXT), set(args) ^ set(TEXT)
    fonts = {name: glyphs(name) for name in ("Silver SDF", "DNFBitBitv2 SDF")}
    for sid, values in TEXT.items():
        assert 470 <= sid < 530 and len(values) == 4, sid
        for value in values:
            assert value and not set(value) & set(",^\n"), (sid, value)
            assert set(re.findall(r"\{(\d+)\}", value)) == {str(i) for i in range(args[sid])}, (sid, value)
        for value in values[:2]:
            for name, have in fonts.items():
                missing = sorted({c for c in value if ord(c) not in have})
                assert not missing, (sid, name, missing, value)
    print(f"ledger.py: {len(TEXT)} strings OK")
