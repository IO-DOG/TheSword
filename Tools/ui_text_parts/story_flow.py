"""Story flow: the instinct warning before the contract and the scene skip hint (ids 380-409).

C# constants: StoryUI (Assets/@Scripts/UI/Popup/StoryUI.cs).
No newlines, commas or '^' inside the strings (see ui.py). Korean and English stay inside the glyphs of the
two pixel fonts - ASCII colon and parentheses only; Japanese and Chinese punctuation is drawn by FontFallback.
"""

TEXT = {
    # FatalFightGuard, before the sword contract: no numbers (the sword's eye is not open yet) - Damian's gut.
    380: ("등골이 서늘하다. 지금은 이길 수 없을 것 같다. 그래도 싸울까?",
          "A chill runs down my spine. I don't think I can win this yet. Fight anyway?",
          "背筋が冷たくなる。今はまだ勝てそうにない。それでも戦うか?",
          "脊背一阵发凉。现在恐怕还赢不了。还要战斗吗?"),
    # StoryUI.SkipHint: hold Tab to fast-forward the scene / a scene seen in an earlier run skips on one press
    381: ("Tab 길게: 건너뛰기", "Hold Tab: skip", "Tab長押し: スキップ", "长按Tab: 跳过"),
    382: ("Tab: 건너뛰기 (본 장면)", "Tab: skip (seen)", "Tab: スキップ (既読)", "Tab: 跳过 (已读)"),
}
