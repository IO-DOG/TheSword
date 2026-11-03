"""Gamepad twins of the key legends (ids 660-689).

While the pad is the active input (GameKeys.UsingPad - the last thing pressed or moved), C# shows the twin
instead of the keyboard line: GameKeys.PromptId / PromptText. Steam Deck Verified: "Mouse and keyboard glyphs
should not be shown if they are not the active input". Button names are the Xbox ones (Valve accepts Xbox
glyphs; the Deck's Gamepad template is an Xbox pad) and stay the same in every language.
Same rules as battle.py: no newlines, commas or '^'; Korean and English stay inside the glyphs of both pixel
fonts - so no up/down arrows (the stick picks rows) and no menu/view symbols.
"""

# keyboard id -> pad id. ui_text bakes this into GeneratedUiText.PadTwin.
TWIN = {
    143: 660, 144: 661, 145: 662, 274: 663, 275: 664, 288: 665, 300: 666, 352: 667,
    353: 668, 356: 669, 381: 670, 382: 671, 480: 672, 481: 673, 535: 674,
}

TEXT = {
    660: ("아무 버튼이나 누르세요", "Press any button", "何かボタンを押してください", "请按任意按钮"),
    661: ("불러오지 못했습니다. A: 재시도 / B: 종료", "Loading failed. A: retry / B: quit",
          "読み込めませんでした。A：再試行 / B：終了", "加载失败。A：重试 / B：退出"),
    662: ("저장을 복구하지 못했습니다. 원본은 보존됩니다. 아무 버튼이나 눌러 돌아가세요.",
          "Could not restore your save. The original is preserved. Press any button to return.",
          "セーブを復元できませんでした。元のデータは保持されます。何かボタンを押すと戻ります。",
          "无法恢复存档。原始存档已保留。请按任意按钮返回。"),
    # 274 without "Hover Info" - tooltips are a mouse thing. Y opens the bag on the map but is skill 2 in a fight,
    # and the keyboard line has no bag key either.
    663: ("스틱 이동   View 도감   LS 예측 표시   LB 워프   X/Y/RB 스킬   Menu 메뉴",
          "Stick Move   View Manual   LS Forecast   LB Warp   X/Y/RB Skills   Menu Pause",
          "スティック 移動   View 図鑑   LS 予測表示   LB ワープ   X/Y/RB スキル   Menu メニュー",
          "摇杆 移动   View 图鉴   LS 预测显示   LB 传送   X/Y/RB 技能   Menu 菜单"),
    664: ("View: 몬스터 도감   LS: 예측 표시", "View: Monster Manual   LS: Forecast",
          "View: モンスター図鑑   LS: 予測表示", "View: 怪物图鉴   LS: 预测显示"),
    665: ("스틱으로 층 고르기   A 이동   LB / B 닫기", "Stick Choose a floor   A Go   LB / B Close",
          "スティック 階を選ぶ   A 移動   LB / B 閉じる", "摇杆 选择楼层   A 前往   LB / B 关闭"),
    666: ("스틱으로 고른 뒤 A", "Pick with the stick then press A", "スティックで選んでからA", "用摇杆选择后按A"),
    667: ("RT 길게: 8배속", "Hold RT: 8x speed", "RT 長押し: 8倍速", "按住 RT: 8倍速"),
    668: ("LB: 건너뛰기", "LB: Skip", "LB: スキップ", "LB: 跳过"),
    669: ("이제 LB 로 전투를 건너뛸 수 있습니다. 결과는 예측 그대로입니다.",
          "You can now skip fights with LB. The result is exactly as forecast.",
          "LBで戦闘をスキップできます。結果は予測どおりです。",
          "现在可以按 LB 跳过战斗。结果与预测完全一致。"),
    670: ("LB 길게: 건너뛰기", "Hold LB: skip", "LB長押し: スキップ", "长按LB: 跳过"),
    671: ("LB: 건너뛰기 (본 장면)", "LB: skip (seen)", "LB: スキップ (既読)", "LB: 跳过 (已读)"),
    672: ("←→ 고르기  A 받기  B 지나간다", "←→ choose  A receive  B pass",
          "←→ 選ぶ  A 受け取る  B 通り過ぎる", "←→ 选择  A 领取  B 离开"),
    673: ("A 계속", "A continue", "A 続ける", "A 继续"),
    674: ("스틱 고르기   A 시작   B 뒤로", "Stick Choose   A Start   B Back",
          "スティック 選択   A 開始   B 戻る", "摇杆 选择   A 开始   B 返回"),
}


if __name__ == "__main__":
    # Self-check: ids in the block, one twin per keyboard line, no forbidden characters, no keyboard words,
    # KR/EN glyphs in both pixel fonts.
    import re
    from pathlib import Path
    font_dir = Path(__file__).resolve().parents[2] / "Assets/@Resources/Font"

    def glyphs(name):
        text = (font_dir / f"{name}.asset").read_text(encoding="utf-8", errors="replace")
        table = text.split("  m_CharacterTable:", 1)[1].split("\n  m_", 1)[0]
        return {int(u) for u in re.findall(r"m_Unicode: (\d+)", table)}

    assert sorted(TWIN.values()) == sorted(TEXT), "every pad line is the twin of exactly one keyboard line"
    fonts = {name: glyphs(name) for name in ("Silver SDF", "DNFBitBitv2 SDF")}
    for sid, values in TEXT.items():
        assert 660 <= sid < 690 and len(values) == 4, sid
        for value in values:
            assert value and not set(value) & set(",^\n"), (sid, value)
            assert not re.search(r"\b(Enter|Esc|Tab|Space|WASD|W/S|Click)\b|키|クリック|鼠标|点击", value), (sid, value)
        for value in values[:2]:
            for name, have in fonts.items():
                missing = sorted({c for c in value if ord(c) not in have})
                assert not missing, (sid, name, missing, value)
    print(f"pad.py: {len(TEXT)} strings OK")
