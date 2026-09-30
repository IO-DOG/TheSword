"""Story UI (ids 300-349). Story dialogue itself lives in ScriptData 300000-309999 (Tools/story_gen.py).

No newlines, commas or '^' inside the strings (see ui.py).
"""

TEXT = {
    # UI_ConversationPopup: shown above the ending choice. Nothing is pre-selected, so Enter alone does nothing.
    300: ("방향키로 고른 뒤 Enter", "Pick with the arrow keys then press Enter",
          "方向キーで選んでからEnter", "用方向键选择后按Enter"),
}
