using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 체험판의 끝 카드 (MASTER_PLAN D1). DemoGate 가 21층에서 띄운다 — 20층 보스와 렌의 반지까지가 체험판이다.
///
/// 이번 판의 장부(예언한 값과 싸움에서 치른 값, 제단에 바쳤으면 제단), 찜하기(본편 스토어 — SteamManager.OpenStorePage), 타이틀로.
/// 본편 AppID 가 아직 없으면 찜하기를 뺀다. 여기서 쓸 저장은 없다 — 계단으로 오면 이 카드는 EnterFloor 바로 앞의 HUD 갱신에서
/// 뜨고, 21층 입구 체크포인트는 같은 호출 안에서 그 뒤에 적힌다(단추는 InputDelay 뒤라 누를 때는 이미 있다).
/// 떠 있는 동안 캐릭터를 붙잡고(OnInputLock) Esc 는 삼킨다 — 체험판은 여기서 더 가지 않는다.
///
/// 프리팹 없이 세운다. 틀(Inventory_Popup32)·단추(SystemUI_Button/Choice)·글꼴은 도감처럼 다른 창에서 빌린다(CodeUI).
/// A/D·←/→ 로 고르고 Enter·Space 로 누른다. 찜하기가 먼저 골라져 있다 — 되돌릴 수 없는 단추가 없다.
/// </summary>
public class UI_DemoEndPopup : UI_Popup
{
    // Tools/ui_text_parts/steam2.py. "타이틀로" 는 메뉴의 것(UI_MenuPopup.MENU_TO_TITLE)을 쓴다.
    public const int TITLE = 560;
    public const int BODY = 561;
    public const int LEDGER = 562;
    public const int FORETOLD = 563;
    public const int PAID = 564;
    public const int WISHLIST = 565;
    const int ALTAR = 474;              // "제단" — 결산 창의 제단 제목(ledger.py, UI_TallyPopup.AltarTitle)

    const float Width = 1000f;
    const float Pad = 36f;
    const float ButtonWidth = 380f;
    const float ButtonHeight = 84f;
    const float InputDelay = 0.6f;      // 계단을 오르던 손(Enter·Space)이 그대로 단추를 누르지 않게 (실시간 초)

    static readonly Color Gold = new Color32(240, 210, 138, 255);
    static readonly Color TraitGold = new Color32(232, 193, 112, 255);
    static readonly Color Soft = new Color32(174, 182, 200, 255);
    static readonly Color Ink = new Color32(236, 236, 242, 255);

    readonly List<GameObject> _choices = new List<GameObject>();
    readonly List<Action> _actions = new List<Action>();
    int _cursor;
    float _openedAt;
    bool _lockedInput;      // 입력 잠금을 이 창이 켰다. 켠 쪽만 끈다
    bool _leaving;

    /// <summary>끝 카드를 띄운다. 이미 떠 있으면 아무 일도 없다(DemoGate 가 HUD 를 그릴 때마다 부른다).</summary>
    public static void Show()
    {
        if (Managers.UI.FindPopup<UI_DemoEndPopup>() != null)
            return;
        UI_DemoEndPopup popup = new GameObject(nameof(UI_DemoEndPopup), typeof(RectTransform)).AddComponent<UI_DemoEndPopup>();
        Managers.UI.PushPopup(popup);
        popup.Init();
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        // 높이를 1080 에 맞춘다(도감과 같다). 스케일러는 켜질 때 한 번 재므로 다시 켜서 첫 프레임부터 맞춘다.
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        scaler.enabled = false;
        scaler.enabled = true;

        if (Managers.Game.OnInputLock == false)
        {
            Managers.Game.OnInputLock = true;
            _lockedInput = true;
        }
        _openedAt = Time.unscaledTime;
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_SFX");
        Build();
        StartCoroutine(CoQuietStory());
        return true;
    }

    // 21층 입구에는 챕터 1 을 여는 이야기가 줄을 선다(같은 FloorEntered). 두면 잠금이 풀리기를 4초 기다리다 화면의 창을
    // 모두 걷고(이 카드도) 튼다. 한 프레임 뒤라야 누가 먼저 들었든 줄이 서 있다. 틀기 전에 거두니 본 것으로 적히지 않는다 —
    // 본편에서 이 저장을 이으면 21층에서 그대로 튼다(StoryDirector.OnSceneLoaded).
    IEnumerator CoQuietStory()
    {
        yield return null;
        StoryDirector.AbortAll();
    }

    #region 그리기
    void Build()
    {
        Image dim = CodeUI.NewImage(transform, "Dim", null, new Color(0f, 0f, 0f, 0.72f));
        CodeUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;           // 뒤의 HUD 를 누르지 않게

        Sprite frame = CodeUI.PrefabSprite("UI_InvenPopup", "Inventory_Popup32");
        Image panel = CodeUI.NewImage(transform, "Panel", frame, frame != null ? Color.white : new Color(0.07f, 0.08f, 0.12f, 0.97f), true);
        panel.raycastTarget = true;
        Transform root = panel.transform;
        float inner = Width - Pad * 2f;
        float y = -Pad;

        TextMeshProUGUI title = CodeUI.NewText(root, "Title", CodeUI.ProseFont, 58f, Gold, TextAlignmentOptions.Center);
        CodeUI.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(inner, 70f));
        CodeUI.Fit(title, 28f).text = Managers.GetString(TITLE);
        y -= 84f;

        TextMeshProUGUI body = CodeUI.NewText(root, "Body", CodeUI.ProseFont, 30f, Soft, TextAlignmentOptions.Top);
        CodeUI.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(inner, 84f));
        body.textWrappingMode = TextWrappingModes.Normal;
        CodeUI.Fit(body, 18f).text = Managers.GetString(BODY);
        y -= 100f;

        // 장부가 빈 판(장부가 들어오기 전의 저장)은 0 과 0 을 보이지 않는다.
        LedgerState ledger = Managers.Game.PlayerData.Ledger;
        if (ledger != null && (ledger.Foretold > 0 || ledger.Paid > 0))
            y = BuildLedger(root, y, inner, ledger);

        List<(string label, Action press)> buttons = new List<(string label, Action press)>();
        if (SteamManager.HasStorePage)
            buttons.Add((Managers.GetString(WISHLIST), Wishlist));
        buttons.Add((Managers.GetString(UI_MenuPopup.MENU_TO_TITLE), ToTitle));
        const float Gap = 40f;
        float x = -((buttons.Count - 1) * (ButtonWidth + Gap)) / 2f;
        y -= 12f;
        for (int i = 0; i < buttons.Count; i++, x += ButtonWidth + Gap)
            AddButton(root, i, buttons[i].label, buttons[i].press, new Vector2(x, y));
        y -= ButtonHeight + Pad;

        CodeUI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width, -y));
        // 좁은 화면(4:3)에서는 창째로 줄인다. 캔버스 높이는 늘 1080 이다.
        float canvasWidth = 1080f * Screen.width / Mathf.Max(1, Screen.height);
        panel.rectTransform.localScale = Vector3.one * Mathf.Min(1f, (canvasWidth - 40f) / Width);
        Select(0, false);
    }

    // 마검의 장부: 예언한 값과 싸움에서 치른 값(LedgerState). 스킬 없이 싸우면 둘이 같고, 스킬로 아낀 만큼 치른 값이 적다.
    // 제단에 바친 HP 는 장부의 Paid 에 들지만 예언과는 상관없다 — 빼서 셋째 숫자로 따로 둔다(바친 적이 있을 때만).
    // 섞으면 제단을 쓴 사람에게 예언이 틀린 것처럼 읽힌다. 5·10·15·20층 계단마다 제단이 있다.
    float BuildLedger(Transform root, float y, float inner, LedgerState ledger)
    {
        Sprite lineSprite = CodeUI.PrefabSprite("UI_SettingPopup", "SystemUI_Setting_ClassLine");
        Image line = CodeUI.NewImage(root, "Line", lineSprite, lineSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.2f));
        CodeUI.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(inner, 2f));
        y -= 18f;

        TextMeshProUGUI caption = CodeUI.NewText(root, "Ledger", CodeUI.ProseFont, 30f, TraitGold, TextAlignmentOptions.Center);
        CodeUI.Place(caption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(inner, 40f));
        CodeUI.Fit(caption, 18f).text = Managers.GetString(LEDGER);
        y -= 48f;

        List<(int label, int value)> figures = new List<(int label, int value)> {
            (FORETOLD, ledger.Foretold), (PAID, ledger.Paid - ledger.AltarPaid) };
        if (ledger.AltarPaid > 0)
            figures.Add((ALTAR, ledger.AltarPaid));
        float width = Mathf.Min(360f, inner / figures.Count);      // 칸을 고르게 나눈다: 둘이면 ±inner/4, 셋이면 -inner/3·0·+inner/3
        for (int i = 0; i < figures.Count; i++)
            Figure(root, inner * ((i + 0.5f) / figures.Count - 0.5f), y, width, figures[i].label, figures[i].value);
        return y - 112f;
    }

    // 이름 한 줄 밑에 큰 숫자. 숫자는 HUD 의 글꼴에 테두리를 두른다(도감의 값과 같다).
    static void Figure(Transform root, float x, float y, float width, int labelId, int value)
    {
        TextMeshProUGUI label = CodeUI.NewText(root, "Label", CodeUI.ProseFont, 26f, Soft, TextAlignmentOptions.Center);
        CodeUI.Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(width, 34f));
        CodeUI.Fit(label, 16f).text = Managers.GetString(labelId);

        TextMeshProUGUI number = CodeUI.NewText(root, "Value", CodeUI.NumberFont, 56f, Ink, TextAlignmentOptions.Center);
        number.fontSharedMaterial = CodeUI.Outlined(CodeUI.NumberFont, 0.2f);
        CodeUI.Place(number.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y - 38f), new Vector2(width, 64f));
        number.text = value.ToString();
    }

    // 단추: 메뉴 단추 그림(9분할로 늘린다)과 골랐을 때의 테두리(SystemUI_Choice). 마우스를 올리면 고르고, 누르면 그것을 한다.
    void AddButton(Transform root, int index, string label, Action press, Vector2 pos)
    {
        Sprite art = RowArt("SystemUI_Button");
        Image bg = CodeUI.NewImage(root, "Button" + index, art, art != null ? Color.white : new Color(0.1f, 0.12f, 0.18f, 0.9f), true);
        bg.raycastTarget = true;
        CodeUI.Place(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(ButtonWidth, ButtonHeight));
        bg.gameObject.BindEvent(() => Select(index, true), type: Define.UIEvent.PointerEnter);
        bg.gameObject.BindEvent(() => Press(index));

        Sprite pick = RowArt("SystemUI_Choice");
        Image choice = CodeUI.NewImage(bg.transform, "Choice", pick, pick != null ? Color.white : new Color(1f, 1f, 1f, 0.15f), true);
        CodeUI.Stretch(choice.rectTransform);
        _choices.Add(choice.gameObject);
        _actions.Add(press);

        TextMeshProUGUI text = CodeUI.NewText(bg.transform, "Label", CodeUI.ProseFont, 34f, Ink, TextAlignmentOptions.Center);
        CodeUI.Stretch(text.rectTransform).offsetMin = new Vector2(30f, 0f);
        text.rectTransform.offsetMax = new Vector2(-30f, 0f);
        CodeUI.Fit(text, 18f).text = label;
    }

    // 메뉴 단추 그림(240x48)은 테두리가 없다 — 양 끝의 모서리만 남기고 가운데를 늘린다(도감의 줄과 같다).
    static Sprite RowArt(string sprite) => CodeUI.Sliced(CodeUI.PrefabSprite("UI_MenuPopup", sprite), new Vector4(24f, 0f, 24f, 0f));
    #endregion

    #region 고르기
    void Select(int index, bool sound)
    {
        index = Mathf.Clamp(index, 0, _choices.Count - 1);
        if (sound && index != _cursor)
            Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_SFX");
        _cursor = index;
        for (int i = 0; i < _choices.Count; i++)
            _choices[i].SetActive(i == index);
    }

    void Press(int index)
    {
        if (_leaving || Time.unscaledTime < _openedAt + InputDelay)
            return;
        Select(index, false);
        _actions[index]();
    }

    void Update()
    {
        if (Managers.UI.TopPopup != this || Managers.UI.ClosedThisFrame)
            return;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            Select(_cursor - 1, true);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            Select(_cursor + 1, true);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            Press(_cursor);
    }

    void Wishlist()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Click_SFX");
        SteamManager.OpenStorePage();
    }

    // 메뉴의 "타이틀로" 와 같은 길 — 이야기를 걷고 씬을 바꾼다. 잃을 것이 없어 묻지 않는다(저장은 21층 입구에 있다).
    void ToTitle()
    {
        _leaving = true;
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Click_SFX");
        StoryDirector.AbortAll();
        Managers.Scene.LoadScene(Define.Scene.TitleScene);
    }

    // Esc 는 삼킨다. 메뉴(이 층 다시·체크포인트)도 열리지 않는다 — 입력 잠금이 막는다(UI_GameScene.TryOpenMenu).
    public override bool OnEscape() => true;

    // 씬이 바뀌며 걷혔다. 플레이를 끄는 중이면(매니저가 먼저 사라졌으면) 건드리지 않는다.
    void OnDestroy()
    {
        if (_lockedInput && Managers.IsAlive)
            Managers.Game.OnInputLock = false;
    }
    #endregion
}
