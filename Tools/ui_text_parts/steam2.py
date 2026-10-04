"""Steam, second pass: the demo's end card (ids 560-589).

C# constants: UI_DemoEndPopup (TITLE, BODY, LEDGER, FORETOLD, PAID, WISHLIST). "Back to title" reuses 182 (ui.py),
the third figure "Altar" reuses 474 (ledger.py).
Same rules as mode.py: no newlines, commas or '^'; Korean and English stay inside the glyphs of both pixel fonts
(DNFBitBitv2 SDF, Silver SDF) - no middle dot or em dash. "Foretold" follows the achievements (Cheaper Than Foretold);
the wishlist button uses each language's Steam store wording.
"""

TEXT = {
    # UI_DemoEndPopup: the card's title and the line under it. The demo writes the 21F checkpoint to the folder the
    # full game reads, but the full game only accepts it while its MapData hash equals the demo's (ValidateCheckpoint)
    # - so 561 states where the save is and promises nothing. Bring back "carries over to the full game" once K1
    # (old MapData shipped by hash) lands or the demo's content is frozen for the full game.
    560: ("체험판은 여기까지", "The demo ends here", "体験版はここまで", "试玩版到此为止"),
    561: ("탑은 100층까지 이어집니다. 저장은 21층 입구에 남아 있습니다.",
          "The tower goes on to floor 100. Your save waits at the entrance to floor 21.",
          "塔は100階まで続きます。セーブは21階の入口に残っています。",
          "塔一直延伸到第100层。存档保留在第21层入口。"),
    # the run's ledger (LedgerState): a caption, then numbers under these labels - foretold, paid in fights
    # (Paid - AltarPaid) and, if any, 474 "Altar". Same words as the tally and the run card (ledger.py 471, 500)
    # - keep them in step.
    562: ("마검의 장부", "The Sword's Ledger", "魔剣の帳簿", "魔剑的账簿"),
    563: ("예언", "Foretold", "予言", "预言"),
    564: ("치름", "Paid", "支払い", "付出"),
    # button: the full game's store page (Steam overlay, or the browser without Steam)
    565: ("찜 목록에 추가", "Add to Wishlist", "ウィッシュリストに追加", "加入愿望单"),
}


if __name__ == "__main__":
    # Self-check (as mode.py): ids in the block, no forbidden characters, no placeholders (C# passes none),
    # KR/EN glyphs in both pixel fonts.
    import re
    from pathlib import Path
    font_dir = Path(__file__).resolve().parents[2] / "Assets/@Resources/Font"

    def glyphs(name):
        text = (font_dir / f"{name}.asset").read_text(encoding="utf-8", errors="replace")
        table = text.split("  m_CharacterTable:", 1)[1].split("\n  m_", 1)[0]
        return {int(u) for u in re.findall(r"m_Unicode: (\d+)", table)}

    fonts = {name: glyphs(name) for name in ("Silver SDF", "DNFBitBitv2 SDF")}
    for sid, values in TEXT.items():
        assert 560 <= sid < 590 and len(values) == 4, sid
        for value in values:
            assert value and not set(value) & set(",^\n"), (sid, value)
            assert not re.findall(r"\{\d+\}", value), (sid, value)
        for value in values[:2]:
            for name, have in fonts.items():
                missing = sorted({c for c in value if ord(c) not in have})
                assert not missing, (sid, name, missing, value)
    print(f"steam2.py: {len(TEXT)} strings OK")
