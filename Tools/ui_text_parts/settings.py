"""Settings popup rows (UI_SettingPopup). Block 230-259. Toggle for the damage forecast reuses 133."""

TEXT = {
    230: ("해상도", "Resolution", "解像度", "分辨率"),
    231: ("수직 동기화", "VSync", "垂直同期", "垂直同步"),
    232: ("글자 속도", "Text speed", "文字速度", "文字速度"),
    # 233-236 follow GameSettings.TextSpeed 0-3 (UI_SettingPopup adds the value to 233).
    233: ("느림", "Slow", "遅い", "慢"),
    234: ("보통", "Normal", "普通", "中"),
    235: ("빠름", "Fast", "速い", "快"),
    236: ("즉시", "Instant", "瞬時", "瞬间"),
    237: ("화면 흔들림", "Screen shake", "画面の揺れ", "屏幕震动"),
    238: ("게임", "Game", "ゲーム", "游戏"),
    # dialogue lines move on by themselves after a reading-time hold (GameSettings.AutoAdvance, StoryUI.AutoRead). Choices never do.
    239: ("대사 자동 넘김", "Auto-advance dialogue", "セリフの自動送り", "对话自动播放"),
    # small mark on the dialogue box while 239 is on (UI_ConversationPopup.ShowAutoMark)
    240: ("자동", "AUTO", "オート", "自动"),
    # hides the hit explosions / white hit flash in battle and the full-screen white flashes of cutscenes (GameSettings.ReduceFlash)
    241: ("번쩍임 줄이기", "Reduce flashes", "点滅を抑える", "减少闪光"),
}
