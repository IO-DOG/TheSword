"""Story UI (ids 300-349). Story dialogue itself lives in ScriptData 300000-309999 (Tools/story_gen.py).

No newlines, commas or '^' inside the strings (see ui.py).
"""

TEXT = {
    # UI_ConversationPopup: shown above the ending choice. Nothing is pre-selected, so Enter alone does nothing.
    300: ("방향키로 고른 뒤 Enter", "Pick with the arrow keys then press Enter",
          "方向キーで選んでからEnter", "用方向键选择后按Enter"),
    # UI_BacklogPopup: the menu row and the window title - the lines spoken in this session (StoryUI.Log)
    301: ("지난 대사", "Dialogue log", "セリフ履歴", "对话记录"),
    # its key legend (pad twin 676)
    302: ("W S 넘기기  Enter 닫기", "W S page  Enter close", "W S ページ送り  Enter 閉じる", "W S 翻页  Enter 关闭"),
}
