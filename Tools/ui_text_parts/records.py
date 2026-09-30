"""Records across runs: endings seen on the title (ids 440-469).

C# constant: UI_TitleScene.ENDINGS_SEEN. No newlines, commas or '^' inside the strings (see ui.py).
Korean and English stay inside the two pixel fonts' glyphs - no middle dot.
"""

TEXT = {
    # UI_TitleScene: {0} = endings seen, {1} = all endings (Records.EndingCount). Shown once one ending is seen.
    440: ("결말 {0}/{1}", "Endings {0}/{1}", "エンディング {0}/{1}", "结局 {0}/{1}"),
}
