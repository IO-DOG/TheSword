using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SettingPopup : UI_Popup
{
    // 새 줄의 이름 (Tools/ui_text_parts/settings.py). 예상 피해 표시는 전투 예측 줄의 이름(133)을 그대로 쓴다.
    const int RESOLUTION = 230, VSYNC = 231, TEXT_SPEED = 232, TEXT_SPEED_SLOW = 233, SCREEN_SHAKE = 237, GAME = 238;
    const int FORECAST = 133;
    // 전투 속도 줄 (Tools/ui_text_parts/battle.py): 이름, "{0}배속". 고를 수 있는 값은 GameSettings.BattleSpeed 와 같다.
    const int BATTLE_SPEED = 350, BATTLE_SPEED_VALUE = 351;
    static readonly int[] BattleSpeeds = { 1, 2, 4 };

    #region Enums
    enum GameObjects
    {
        TotalSoundSlider,
        BGMSoundSlider,
        EffectSoundSlider,
        //SoundToggle,
    }

    enum Texts
    {
        SoundClassText,
        TotalSoundText,
        BGMSoundText,
        EffectSoundText,
        TotalSoundClassText,
        BGMSoundClassText,
        EffectSoundClassText,
        ScreenClassText,
        FullScreenText,
        WindowScreenText,
        FullWindowScreenText,
    }

    enum Images
    {
        BackgroundImage,
        FullScreenCheckBox,
        FullScreenChoice,
        FullScreenPick,
        WindowScreenCheckBox,
        WindowScreenChoice,
        WindowScreenPick,
        FullWindowScreenCheckBox,
        FullWindowScreenChoice,
        FullWindowScreenPick,
    }
    #endregion

    public UI_MenuPopup menuPopup;

    List<Vector2Int> _resolutions;
    Vector2Int _native;   // 모니터 원래 크기
    Slider _resolution;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindObject(typeof(GameObjects));
        BindText(typeof(Texts));
        BindImage(typeof(Images));
        #endregion

        GetText((int)Texts.SoundClassText).text = Managers.GetString(Define.SOUND);
        GetText((int)Texts.TotalSoundClassText).text = Managers.GetString(Define.TOTAL_SOUND);
        GetText((int)Texts.BGMSoundClassText).text = Managers.GetString(Define.BGM);
        GetText((int)Texts.EffectSoundClassText).text = Managers.GetString(Define.EFFECT);
        GetText((int)Texts.ScreenClassText).text = Managers.GetString(Define.SCREEN);
        GetText((int)Texts.FullScreenText).text = Managers.GetString(Define.FULL_SCREEN);
        GetText((int)Texts.WindowScreenText).text = Managers.GetString(Define.WINDOW_SCREEN);
        GetText((int)Texts.FullWindowScreenText).text = Managers.GetString(Define.FULL_WINDOW_SCREEN);

        //GetImage((int)Images.BackgroundImage).gameObject.transform.localScale = new Vector3(0, 0, 0);
        float dist = Screen.width * 0.645f;

        GetImage((int)Images.BackgroundImage).gameObject.transform.DOMoveX(dist, 0.2f).SetLink(gameObject);   // 곧장 닫혀도 경고 없이
        //GetImage((int)Images.BackgroundImage).gameObject.transform.DOScale(2, 0.2f);

        // 복제 원본에 이벤트를 걸기 전에 복제한다.
        AddRows();

        GetImage((int)Images.FullScreenCheckBox).gameObject.BindEvent(OnClickFullScreenCheckBox);
        GetImage((int)Images.WindowScreenCheckBox).gameObject.BindEvent(OnClickWindowScreenCheckBox);
        GetImage((int)Images.FullWindowScreenCheckBox).gameObject.BindEvent(OnClickFullWindowScreenCheckBox);

        // 전체 음량은 AudioListener 가, 배경음·효과음은 각 소리가 맡는다 (GameSettings.ApplyVolume).
        BindVolume(GameObjects.TotalSoundSlider, Texts.TotalSoundText, "CURSOUND", v => AudioListener.volume = v);
        BindVolume(GameObjects.BGMSoundSlider, Texts.BGMSoundText, "CURBGMSOUND", Managers.Sound.SetBGMVolume);
        BindVolume(GameObjects.EffectSoundSlider, Texts.EffectSoundText, "CUREFFECTSOUND", Managers.Sound.SetEffectVolume);

        OnEnterExitImage();

        Refresh();
        return true;
    }

    // Esc 는 한 칸 뒤로 — 메뉴로 돌아간다. 비켜 있던 메뉴 버튼은 메뉴가 제자리로 돌린다.
    public override bool OnEscape()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Back_SFX");
        ClosePopupUI();
        return true;
    }

    // 끄는 동안에는 저장을 미룬다. 닫힐 때 한 번.
    void OnDestroy()
    {
        PlayerPrefs.Save();
    }

    // 값이 바뀔 때만 한 번. 예전에는 창이 떠 있는 동안 매 프레임 GetComponent 를 열여섯 번,
    // PlayerPrefs 를 세 번 불렀다.
    void BindVolume(GameObjects slider, Texts percent, string key, Action<float> apply)
    {
        Slider s = GetObject((int)slider).GetComponent<Slider>();
        TMP_Text text = GetText((int)percent);
        // "100%" 는 칸(너비 15)을 꽉 채워 오른쪽 끝에 선 손잡이(너비 8)에 붙어 있었다. 조금 띄운다.
        ((RectTransform)text.transform).anchoredPosition += new Vector2(6f, 0f);
        s.SetValueWithoutNotify(PlayerPrefs.GetFloat(key, 1));
        text.text = Percent(s.value);
        s.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetFloat(key, v);
            apply(v);
            text.text = Percent(v);
        });
    }

    static string Percent(float value) => $"{(int)(value * 100)}%";

    void Refresh()
    {
        GetImage((int)Images.FullScreenChoice).gameObject.SetActive(false);
        GetImage((int)Images.FullScreenPick).gameObject.SetActive(false);
        GetImage((int)Images.WindowScreenChoice).gameObject.SetActive(false);
        GetImage((int)Images.WindowScreenPick).gameObject.SetActive(false);
        GetImage((int)Images.FullWindowScreenChoice).gameObject.SetActive(false);
        GetImage((int)Images.FullWindowScreenPick).gameObject.SetActive(false);

        TurnOnCurScreenModeImage();
    }

    void OnEnterExitImage()
    {
        GetImage((int)Images.FullScreenCheckBox).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.FullScreenChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.WindowScreenCheckBox).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.WindowScreenChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.FullWindowScreenCheckBox).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.FullWindowScreenChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);

        GetImage((int)Images.FullScreenCheckBox).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.FullScreenChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.WindowScreenCheckBox).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.WindowScreenChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.FullWindowScreenCheckBox).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.FullWindowScreenChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
    }

    // 세 칸이 곧 창 상태다: 전체 화면 = 독점 전체 화면, 창 모드 = 창, 전체 창 모드 = 테두리 없는 창.
    // 표시는 Screen.fullScreenMode 에서 읽는다 — Unity 가 창 상태를 기억하므로 다시 켜도 맞다.
    // 예전에는 고른 값을 GameManager 에만 들고 있어서 다시 켜면 아무 칸에도 표시가 없었다.
    void TurnOnCurScreenModeImage()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen:
                GetImage((int)Images.FullScreenPick).gameObject.SetActive(true);
                break;
            case FullScreenMode.Windowed:
                GetImage((int)Images.WindowScreenPick).gameObject.SetActive(true);
                break;
            default:   // FullScreenWindow, MaximizedWindow (윈도에서는 같은 것)
                GetImage((int)Images.FullWindowScreenPick).gameObject.SetActive(true);
                break;
        }
    }

    void OnClickFullScreenCheckBox() => SetScreenMode(FullScreenMode.ExclusiveFullScreen);
    void OnClickWindowScreenCheckBox() => SetScreenMode(FullScreenMode.Windowed);
    void OnClickFullWindowScreenCheckBox() => SetScreenMode(FullScreenMode.FullScreenWindow);

    // 전체 화면 둘은 모니터 원래 크기로 편다. 해상도 줄은 지금 창 크기에서 시작해서, 그 값을 쓰면
    // 1280x720 창에서 넘어온 전체 화면이 흐리게 늘어났다. 다른 크기의 전체 화면은 그 뒤에 해상도 줄로 고른다.
    // (예전에는 1920x1080 과 960x540 이 박혀 있어서 1440p·4K 모니터에서 전체 화면이 흐렸다.)
    void SetScreenMode(FullScreenMode mode)
    {
        ApplyScreen(mode == FullScreenMode.Windowed ? Picked : _native, mode);
    }

    // 창 상태는 그대로 두고 크기만 바꾼다.
    void OnReleaseResolution()
    {
        if (Picked.x != Screen.width || Picked.y != Screen.height)
            ApplyScreen(Picked, Screen.fullScreenMode);
    }

    Vector2Int Picked => _resolutions[(int)_resolution.value];

    void ApplyScreen(Vector2Int size, FullScreenMode mode)
    {
        // 창은 모니터보다 작아야 제목 줄과 아래 줄이 화면 안에 든다 (창 가장자리를 끌어 줄일 수도 없다).
        // 넘치면 들어가는 것 중 가장 큰 것으로. 들어가는 것이 없는 720 급 모니터면 그대로 둔다.
        if (mode == FullScreenMode.Windowed && (size.x >= _native.x || size.y >= _native.y))
        {
            Vector2Int fit = _resolutions.LastOrDefault(s => s.x < _native.x && s.y < _native.y);
            if (fit.x > 0)
                size = fit;
        }
        Screen.SetResolution(size.x, size.y, mode);
        // 커서·전투 카드 크기가 본다. 창 상태는 프레임 끝에야 바뀌니 고른 값으로 정한다.
        Managers.Game.ScreenType = GameSettings.ScreenTypeOf(mode, size.y);
        // 메뉴를 닫는다 (세 칸은 예전부터 그랬다). 메뉴는 비켜 둔 버튼의 제자리를 화면 픽셀로 재 두어서,
        // 창 크기가 바뀐 채 설정 창만 닫으면 버튼이 엉뚱한 곳으로 돌아간다.
        menuPopup.OnClickContinueGameButton();
    }

    // 모니터가 내놓는 크기 중 16:9 이고 720 이상인 것 + 모니터 원래 크기 + 지금 크기.
    // 화면 구성(캔버스 기준·카드 위치)이 16:9 로 짜여 있어 다른 비율은 모니터 원래 크기만 둔다.
    // 모니터 원래 크기 = 모니터가 내놓는 가장 큰 크기 (목록이 비면 데스크톱 크기).
    void ListResolutions()
    {
        List<Vector2Int> modes = Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).ToList();
        Resolution desktop = Screen.currentResolution;
        _native = modes.Count > 0 ? modes.OrderBy(s => s.x * s.y).Last() : new Vector2Int(desktop.width, desktop.height);
        List<Vector2Int> list = modes.Where(s => s.y >= 720 && Mathf.Abs((float)s.x / s.y - 16f / 9f) < 0.02f).ToList();
        list.Add(_native);
        list.Add(new Vector2Int(Screen.width, Screen.height));
        _resolutions = list.Distinct().OrderBy(s => s.x * s.y).ToList();
    }

    #region 새 줄
    // 해상도·수직 동기화·글자 속도·전투 속도·화면 흔들림·예상 피해. 모양은 프리팹의 체크박스·슬라이더를 복제해
    // 그대로 쓰고, 판을 아래로 늘려 담는다. 자리는 프리팹 단위이고 판의 가운데가 0 이다:
    //   화면  제목 80.9 · 줄 70.9 · 창 상태 55/45/35 · 해상도 10 · 수직 동기화 -5
    //   소리  통째로 40 내린다 (제목 -30 · 슬라이더 -70/-95/-120)
    //   게임  제목 -147 · 줄 -157 · 글자 속도 -187 · 전투 속도 -212 · 흔들림 -227 · 예상 피해 -237
    //   판    위 110(프리팹 그대로) ~ 아래 -262
    void AddRows()
    {
        Image board = GetImage((int)Images.BackgroundImage);
        GameObject check = GetImage((int)Images.FullScreenCheckBox).gameObject;
        GameObject slider = GetObject((int)GameObjects.TotalSoundSlider);
        Transform screen = check.transform.parent;
        Transform sound = slider.transform.parent;
        Transform game = new GameObject("Game", typeof(RectTransform)).transform;
        game.SetParent(board.transform, false);

        ListResolutions();
        _resolution = AddSlider(slider, screen, 10f, _resolutions.Count,
            _resolutions.IndexOf(new Vector2Int(Screen.width, Screen.height)),
            i => $"{Managers.GetString(RESOLUTION)}  {_resolutions[i].x} x {_resolutions[i].y}", null);
        // 손을 뗄 때 한 번만 바꾼다. 끄는 동안 창 크기가 바뀌면 손잡이가 커서 밑에서 달아난다.
        _resolution.gameObject.BindEvent(OnReleaseResolution, null, Define.UIEvent.PointerUp);
        // 그래서 마우스로만 고른다 — 키보드(A/D·←/→)로 움직이면 이름만 바뀌고 창은 그대로였다. None 이면
        // 눌러도 선택되지 않고(Selectable.OnPointerDown) 옆 줄에서 방향키로 건너오지도 못한다(FindSelectable).
        _resolution.navigation = new Navigation { mode = Navigation.Mode.None };
        AddToggle(check, screen, -5f, VSYNC, () => GameSettings.VSync, on => GameSettings.VSync = on);

        Shift(sound, -40f);

        TMP_Text title = Instantiate(GetText((int)Texts.ScreenClassText), game);
        title.text = Managers.GetString(GAME);
        PlaceY(title.transform, -147f);
        PlaceY(Instantiate(Util.FindChild(screen.gameObject, "ClassLineImage"), game).transform, -157f);
        AddSlider(slider, game, -187f, 4, GameSettings.TextSpeed,
            i => $"{Managers.GetString(TEXT_SPEED)}  {Managers.GetString(TEXT_SPEED_SLOW + i)}",
            i => GameSettings.TextSpeed = i);
        // 전투 시계를 FixedUpdate 마다 몇 걸음 돌리나 — 결과는 같고 지켜보는 시간만 준다 (UI_BattlePopup.Speed).
        AddSlider(slider, game, -212f, BattleSpeeds.Length, Mathf.Max(0, Array.IndexOf(BattleSpeeds, GameSettings.BattleSpeed)),
            i => $"{Managers.GetString(BATTLE_SPEED)}  {string.Format(Managers.GetString(BATTLE_SPEED_VALUE), BattleSpeeds[i])}",
            i => GameSettings.BattleSpeed = BattleSpeeds[i]);
        AddToggle(check, game, -227f, SCREEN_SHAKE, () => GameSettings.ScreenShake, on => GameSettings.ScreenShake = on);
        AddToggle(check, game, -237f, FORECAST, () => GameSettings.ShowForecast, on => GameSettings.ShowForecast = on);

        // 판을 늘리고 셋을 가운데로 모은다. 9분할로 그려 위쪽 테두리 무늬는 늘어나지 않게 한다
        // (스프라이트 테두리 값은 임포트 설정에 있다).
        const float top = 110f, bottom = -262f;
        board.type = Image.Type.Sliced;
        board.rectTransform.sizeDelta = new Vector2(board.rectTransform.sizeDelta.x, top - bottom);
        foreach (Transform block in new[] { screen, sound, game })
            Shift(block, -(top + bottom) / 2f);

        // 줄 글자(13·15)가 판 배율 2 에서도 옆 메뉴 단추 글자(72)의 3분의 1 남짓이라 960x540 창에서는 깨알 같았다.
        // 판째로 1.3배 키운다(늘린 판이 16:9 화면 높이의 90%). 다만 옆으로 비킨 메뉴 단추(폭 480, 화면 폭 36.5%)와 판 가운데
        // (64.5%) 사이에 드는 만큼만 — 4:3 에서는 덜 키워야 판이 단추 끝을 덮지 않는다.
        // 높이도 넘지 않게 한다(위아래 20 씩 남긴다). 캔버스 높이는 21:9(2560x1080·3440x1440)에서 935 남짓이라 1.3배 판(967)의
        // 위아래 테두리가 잘렸다. 32:9(5120x1440)는 764 라 1배 판(744)도 여백이 모자라 1배보다 조금 작게 줄인다.
        float scaleFactor = GetComponent<Canvas>().scaleFactor;
        float canvasWidth = Screen.width / scaleFactor;
        float room = ((0.645f - 0.365f) * canvasWidth - 240f - 16f) / (board.rectTransform.sizeDelta.x * board.transform.localScale.x / 2f);
        float fitHeight = (Screen.height / scaleFactor - 40f) / (board.rectTransform.sizeDelta.y * board.transform.localScale.y);
        board.transform.localScale *= Mathf.Min(Mathf.Clamp(room, 1f, 1.3f), fitHeight);
    }

    // 값은 이름 옆에 붙인다 — 소리 줄의 오른쪽 칸(너비 15)에는 "1920 x 1080" 이 안 들어간다.
    Slider AddSlider(GameObject template, Transform parent, float y, int steps, int value,
        Func<int, string> label, Action<int> changed)
    {
        GameObject go = Instantiate(template, parent);
        PlaceY(go.transform, y);
        TMP_Text name = Util.FindChild<TMP_Text>(go, nameof(Texts.TotalSoundClassText));
        Util.FindChild(go, nameof(Texts.TotalSoundText)).SetActive(false);

        Slider s = go.GetComponent<Slider>();
        s.wholeNumbers = true;
        s.minValue = 0;
        s.maxValue = Mathf.Max(0, steps - 1);
        s.SetValueWithoutNotify(value);
        name.text = label(value);
        s.onValueChanged.AddListener(v =>
        {
            name.text = label((int)v);
            changed?.Invoke((int)v);
        });
        return s;
    }

    void AddToggle(GameObject template, Transform parent, float y, int labelId, Func<bool> get, Action<bool> set)
    {
        GameObject box = Instantiate(template, parent);
        PlaceY(box.transform, y);
        GameObject choice = Util.FindChild(box, nameof(Images.FullScreenChoice));
        GameObject pick = Util.FindChild(box, nameof(Images.FullScreenPick));
        Util.FindChild<TMP_Text>(box, nameof(Texts.FullScreenText)).text = Managers.GetString(labelId);
        choice.SetActive(false);
        pick.SetActive(get());

        box.BindEvent(() =>
        {
            set(!get());
            pick.SetActive(get());
        });
        box.BindEvent(() => choice.SetActive(true), null, Define.UIEvent.PointerEnter);
        box.BindEvent(() => choice.SetActive(false), null, Define.UIEvent.PointerExit);
    }

    static void PlaceY(Transform t, float y)
    {
        RectTransform rt = (RectTransform)t;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
    }

    static void Shift(Transform t, float dy)
    {
        ((RectTransform)t).anchoredPosition += new Vector2(0, dy);
    }
    #endregion
}
