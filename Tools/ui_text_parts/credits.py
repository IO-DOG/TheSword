"""Credits: the third-party block rolled before the last greeting (ids 630-659).

C# constants: UI_StoryCreditsPopup.ThirdParty (one row of ids per group: heading first, then the lines).
Same rules as battle.py: no newlines, commas or '^'; Korean and English stay inside the glyphs of
both pixel fonts (DNFBitBitv2 SDF, Silver SDF) - no middle dot, em dash, minus sign or multiplication sign.
Product and person names stay untranslated. 631 is the mandatory credit: Silver is CC BY 4.0 (Poppy Works).
Everything else is a courtesy or a pointer - the licenses themselves are Steam/THIRD_PARTY_NOTICES.txt,
which GameBuild copies next to TheSword.exe. Keep the two in step.
"""

TEXT = {
    # group 1: fonts
    630: ("글꼴", "Fonts", "フォント", "字体"),
    631: ("Silver - Poppy Works (CC BY 4.0)", "Silver by Poppy Works (CC BY 4.0)",
          "Silver - Poppy Works (CC BY 4.0)", "Silver - Poppy Works (CC BY 4.0)"),
    632: ("DNF Bit Bit v2 - Neople", "DNF Bit Bit v2 by Neople",
          "DNF Bit Bit v2 - Neople", "DNF Bit Bit v2 - Neople"),
    # group 2: Asset Store packages (no attribution required - listed with thanks, like the old credits)
    633: ("고마운 에셋", "Assets", "アセット", "素材"),
    634: ("Retro Arsenal / Febucci Text Animator / DOTween",) * 4,
    635: ("polygonTrees / Simple Nature Pack",) * 4,
    # group 3: open-source libraries (MIT) and where the license texts are
    636: ("오픈소스", "Open source", "オープンソース", "开源软件"),
    637: ("Steamworks.NET / UIEffect / UIParticle / Newtonsoft.Json (MIT)",) * 4,
    638: ("전체 라이선스는 게임 폴더의 THIRD_PARTY_NOTICES.txt 에 있습니다",
          "Full license texts: THIRD_PARTY_NOTICES.txt in the game folder",
          "ライセンス全文はゲームフォルダの THIRD_PARTY_NOTICES.txt にあります",
          "完整许可证文本见游戏文件夹中的 THIRD_PARTY_NOTICES.txt"),
}


if __name__ == "__main__":
    # Self-check (as mode.py): ids in the block, no forbidden characters, KR/EN glyphs exist in both pixel fonts,
    # and the required credit is still there. Run this after editing a translation: the bake does not run it.
    import re
    from pathlib import Path
    font_dir = Path(__file__).resolve().parents[2] / "Assets/@Resources/Font"

    def glyphs(name):
        text = (font_dir / f"{name}.asset").read_text(encoding="utf-8", errors="replace")
        table = text.split("  m_CharacterTable:", 1)[1].split("\n  m_", 1)[0]
        return {int(u) for u in re.findall(r"m_Unicode: (\d+)", table)}

    fonts = {name: glyphs(name) for name in ("Silver SDF", "DNFBitBitv2 SDF")}
    for sid, values in TEXT.items():
        assert 630 <= sid < 660 and len(values) == 4, sid
        for value in values:
            assert value and not set(value) & set(",^\n"), (sid, value)
            assert "{" not in value, (sid, value)
        for value in values[:2]:
            for name, have in fonts.items():
                missing = sorted({c for c in value if ord(c) not in have})
                assert not missing, (sid, name, missing, value)
    for value in TEXT[631]:
        assert "Silver" in value and "Poppy Works" in value and "CC BY 4.0" in value, value
    # The rows rolled in C# are exactly these ids, and the notices file still carries what the credits point to.
    root = Path(__file__).resolve().parents[2]
    cs = (root / "Assets/@Scripts/UI/Popup/UI_StoryCreditsPopup.cs").read_text(encoding="utf-8")
    rows = {int(n) for row in re.findall(r"new\[\] \{ ([0-9, ]+) \}", cs) for n in row.split(",")}
    assert rows == set(TEXT), sorted(rows ^ set(TEXT))
    notices = (root / "Steam/THIRD_PARTY_NOTICES.txt").read_text(encoding="ascii")
    for needle in ("Silver", "Poppy Works", "CC BY 4.0", "SIL OPEN FONT LICENSE", "Steamworks.NET", "UIEffect", "UIParticle", "Newtonsoft"):
        assert needle in notices, needle
    print(f"credits.py: {len(TEXT)} strings OK")
