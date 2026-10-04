"""Input: click-to-move (ids 590-609).

C# reads these with Managers.GetString(id). Same rules as the other parts: no newlines, commas or '^';
Korean and English stay inside the glyphs of both pixel fonts (DNFBitBitv2 SDF, Silver SDF) - no middle dot,
em dash, minus sign or multiplication sign. Parts are separated by three spaces, like the key legends (288, 535).
"""

TEXT = {
    # One-time hint for click-to-move (PlayerController, region 클릭 이동). "双击快速移动" is how Chinese
    # Magic Tower players ask for it, so the Chinese line uses those words.
    590: ("바닥을 누르면 걸어갑니다   두 번 누르면 빠르게",
          "Click the floor to walk   Double-click to hurry",
          "床をクリックで移動   ダブルクリックで早足",
          "点击地面即可移动   双击快速移动"),
}
