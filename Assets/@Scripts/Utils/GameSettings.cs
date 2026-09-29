using System;
using UnityEngine;

// 플레이어가 고르는 설정. PlayerPrefs 에 남아 다음 실행에도 그대로다.
// 게임 진행(세이브)과는 따로 산다 — 새 게임을 해도 설정은 지워지지 않는다.
public static class GameSettings
{
    const string KeyLanguage = "SET_LANGUAGE";
    const string KeyShake = "SET_SCREEN_SHAKE";
    const string KeyTextSpeed = "SET_TEXT_SPEED";
    const string KeyForecast = "SET_SHOW_FORECAST";
    const string KeyVSync = "SET_VSYNC";
    // 소리 키는 예전 이름 그대로다 (UI_SettingPopup 이 쓰고, 배경음·효과음 키는 여러 곳이 직접 읽는다).
    const string KeyMasterVolume = "CURSOUND";

    // 무엇이든 바뀌면 한 번. 언어가 바뀌면 글자를 다시 칠하는 쪽이 듣는다.
    public static event Action Changed;

    // GetString 이 글자마다 부른다. Windows 의 PlayerPrefs 는 레지스트리라 한 번만 읽어 둔다.
    static Define.ScriptType? s_language;

    // 처음 켜면 OS 언어를 따른다. 고르면 그 값이 남는다.
    public static Define.ScriptType Language
    {
        get
        {
            if (s_language == null)
            {
                int saved = PlayerPrefs.GetInt(KeyLanguage, 0);
                s_language = saved >= (int)Define.ScriptType.Kr && saved <= (int)Define.ScriptType.Cn
                    ? (Define.ScriptType)saved : FromSystem(Application.systemLanguage);
            }
            return s_language.Value;
        }
        set { s_language = value; PlayerPrefs.SetInt(KeyLanguage, (int)value); Commit(); }
    }

    // 화면 흔들림. 멀미하는 사람을 위해 끌 수 있다.
    public static bool ScreenShake
    {
        get => PlayerPrefs.GetInt(KeyShake, 1) == 1;
        set { PlayerPrefs.SetInt(KeyShake, value ? 1 : 0); Commit(); }
    }

    // 대사 글자가 찍히는 속도. 0 느림, 1 보통, 2 빠름, 3 한 번에.
    public static int TextSpeed
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(KeyTextSpeed, 1), 0, 3);
        set { PlayerPrefs.SetInt(KeyTextSpeed, Mathf.Clamp(value, 0, 3)); Commit(); }
    }

    // 맵 위 몬스터마다 예상 피해를 띄울지.
    public static bool ShowForecast
    {
        get => PlayerPrefs.GetInt(KeyForecast, 1) == 1;
        set { PlayerPrefs.SetInt(KeyForecast, value ? 1 : 0); Commit(); }
    }

    // 수직 동기화. Unity 는 이 값을 기억하지 않아서 켤 때마다 다시 건다(ApplyDisplay).
    public static bool VSync
    {
        get => PlayerPrefs.GetInt(KeyVSync, 1) == 1;
        set { PlayerPrefs.SetInt(KeyVSync, value ? 1 : 0); ApplyDisplay(); Commit(); }
    }

    public static void ApplyDisplay()
    {
        QualitySettings.vSyncCount = VSync ? 1 : 0;
    }

    // 전체 음량은 AudioListener 한 곳에서 건다. 그래야 음량을 스스로 곱하지 않는 소리
    // (이펙트 프리팹의 PlayOnAwake — 독 폭발·연기 등 여덟 개)도 슬라이더를 따른다.
    // ponytail: 소리마다 곱하던 SAVESOUND 는 1 로 묶는다. 곱하는 곳이 열 군데라 키는 두고 값만 1 로 둔다 —
    //           두 곳에서 곱하면 음량이 제곱으로 준다.
    public static void ApplyVolume()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(KeyMasterVolume, 1);
        PlayerPrefs.SetFloat("SAVESOUND", 1);
    }

    // 전투 카드와 커서 크기가 이것을 본다(UI_BattlePopup, CursorManager). 예전에는 버튼으로 고른 값만
    // 들고 있어서 다시 켜면 None 으로 돌아갔다 — 창 상태에서 바로 읽는다. 예전 세 버튼과 같은 뜻이다:
    // 작은 창(960x540) = Window, 모니터만 한 창 = FullWindow, 전체 화면 = Full.
    public static Define.ScreenType ScreenTypeOf(FullScreenMode mode, int height)
    {
        if (mode != FullScreenMode.Windowed)
            return Define.ScreenType.Full;
        return height < 1080 ? Define.ScreenType.Window : Define.ScreenType.FullWindow;
    }

    static void Commit()
    {
        PlayerPrefs.Save();
        if (Changed == null)
            return;
        // 듣는 쪽 하나가 터져도 나머지는 다시 칠한다 (GameEvents 와 같은 이유).
        foreach (Action handler in Changed.GetInvocationList())
        {
            try { handler(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    public static Define.ScriptType FromSystem(SystemLanguage lang)
    {
        switch (lang)
        {
            case SystemLanguage.Korean: return Define.ScriptType.Kr;
            case SystemLanguage.Japanese: return Define.ScriptType.Jp;
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
            case SystemLanguage.ChineseTraditional: return Define.ScriptType.Cn;
            default: return Define.ScriptType.En;
        }
    }
}
