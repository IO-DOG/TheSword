using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 새 게임의 규칙을 고른다 — 보통 / 탑의 법 (기획 L4). 타이틀의 새 게임이 연다(UI_TitleScene.OnClickNewGameButton).
///
/// W/S·↑/↓ 로 고르고 Enter·Space 로 정한다(컨트롤러는 Steam Input 이 십자키·A 를 그 키로 준다). 마우스는 올리면 고르고
/// 누르면 정한다 — 다만 창이 뜬 자리에 있던 커서는 움직이기 전까지 고르지 않고, 뜨자마자(두 번 누르기 간격) 누른 것은
/// 받지 않는다. 탑의 법 줄이 새 게임 단추를 거의 다 덮기 때문이다(MouseDeadZone).
/// Esc(타이틀이 OnEscape 로 넘겨준다)나 창 밖을 누르면 아무것도 정하지 않고 타이틀로 물러난다.
/// 처음 골라 둔 것은 보통이다 — 처음 오르는 사람에게 권하는 쪽이라 Enter 를 연타해도 보통으로 간다.
/// 봇(자동 플레이)에게는 묻지 않고 보통으로 간다 (UI_ConfirmPopup 과 같다).
///
/// 프리팹 없이 세운다. 틀은 인벤토리 칸, 줄은 메뉴 단추 — 몬스터 도감과 같은 그림을 빌린다.
/// </summary>
public class UI_ModePopup : UI_Popup
{
    // ponytail: 창 폭 1100 은 5:4(캔버스 폭 1350)에도 들어간다 — 더 좁은 화면을 받게 되면 도감처럼 창째 줄인다.
    const float PanelWidth = 1100f;
    const float Pad = 32f;
    const float TitleHeight = 84f;
    const float RowHeight = 110f;
    const float RowGap = 14f;
    const float FooterHeight = 40f;

    static readonly Color Gold = new Color32(240, 210, 138, 255);
    static readonly Color Soft = new Color32(174, 182, 200, 255);
    static readonly Color Ink = new Color32(236, 236, 242, 255);

    // 줄 순서가 곧 고르는 순서다. 첫 줄(보통)이 처음 골라 둔 것.
    static readonly (GameMode Mode, int Name, int Desc)[] Rows =
    {
        (GameMode.Normal, UI_TitleScene.MODE_NORMAL, UI_TitleScene.MODE_NORMAL_DESC),
        (GameMode.Tower, UI_TitleScene.MODE_TOWER, UI_TitleScene.MODE_TOWER_DESC),
    };

    readonly List<GameObject> _choices = new List<GameObject>();
    Action<GameMode> _onPick;
    int _cursor;
    bool _answered;

    // 창은 타이틀 한가운데에 뜨고, 탑의 법 줄이 방금 누른 새 게임 단추를 거의 다 덮는다. 가만히 있던 커서도 창이 뜨자마자
    // PointerEnter 를 받아서 탑의 법이 골라진 채 열렸고(다음 Enter 가 탑의 법), 새 게임을 두 번 누르면 두 번째가 그 줄에
    // 떨어져 묻지도 않고 탑의 법으로 시작했다. 그래서 올리기는 마우스가 움직인 뒤부터, 누르기는 두 번 누르기 간격 뒤부터 받는다.
    const float MouseDeadZone = 4f;     // px. Windows 가 두 번 누르기로 쳐 주는 폭과 같다
    const float ClickGrace = 0.5f;      // 초(실시간). Windows 의 두 번 누르기 기본 간격
    Vector3 _openMouse;
    float _openedAt;
    bool _mouseLive;                    // 창이 뜬 뒤 마우스가 움직였다
    int _hover = -1;                    // 커서가 올라가 있는 줄. 움직이기 전에 받은 것도 적어 뒀다가 움직이면 그 줄로 간다

    /// <summary>창을 띄운다. 고르면 창을 닫은 뒤 onPick(규칙), 물러나면 아무것도 부르지 않는다.</summary>
    public static void Ask(Action<GameMode> onPick)
    {
        if (GameEvents.IsAutoPlaying)
        {
            onPick?.Invoke(GameMode.Normal);
            return;
        }

        UI_ModePopup popup = new GameObject(nameof(UI_ModePopup), typeof(RectTransform)).AddComponent<UI_ModePopup>();
        popup._onPick = onPick;
        Managers.UI.PushPopup(popup);
        popup.Init();
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        // 코드로 세운 창은 스케일러를 새로 받는다. 높이를 1080 에 맞추고, 켜질 때 한 번 재는 값을 첫 프레임부터 맞게
        // 다시 켠다 (몬스터 도감과 같다).
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        scaler.enabled = false;
        scaler.enabled = true;

        Build();
        Select(0, false);
        _openMouse = Input.mousePosition;
        _openedAt = Time.unscaledTime;
        return true;
    }

    void Build()
    {
        TMP_FontAsset prose = CodeUI.ProseFont;
        float width = PanelWidth - Pad * 2f;

        Image dim = CodeUI.NewImage(transform, "Dim", null, new Color(0f, 0f, 0f, 0.6f));
        CodeUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        dim.gameObject.BindEvent(Back);      // 창 밖을 누르면 물러난다

        Sprite frame = CodeUI.PrefabSprite("UI_InvenPopup", "Inventory_Popup32");
        float height = Pad * 2f + TitleHeight + Rows.Length * RowHeight + (Rows.Length - 1) * RowGap + 16f + FooterHeight;
        Image panel = CodeUI.NewImage(transform, "Panel", frame, frame != null ? Color.white : new Color(0.07f, 0.08f, 0.12f, 0.97f), true);
        panel.raycastTarget = true;          // 창 안을 눌러도 물러나지 않게 가린다
        CodeUI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, height));
        Transform root = panel.transform;

        TextMeshProUGUI title = CodeUI.NewText(root, "Title", prose, 44f, Gold, TextAlignmentOptions.Center);
        CodeUI.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Pad), new Vector2(width, TitleHeight - 20f));
        CodeUI.Fit(title, 24f).text = Managers.GetString(UI_TitleScene.MODE_TITLE);

        Sprite rowArt = RowArt("SystemUI_Button");
        Sprite pickArt = RowArt("SystemUI_Choice");
        for (int i = 0; i < Rows.Length; i++)
        {
            int index = i;
            Image bg = CodeUI.NewImage(root, $"Row{i}", rowArt, rowArt != null ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.1f, 0.12f, 0.18f, 0.9f), true);
            bg.raycastTarget = true;
            CodeUI.Place(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -Pad - TitleHeight - i * (RowHeight + RowGap)), new Vector2(width, RowHeight));
            bg.gameObject.BindEvent(() => { _hover = index; if (_mouseLive) Select(index, true); }, type: Define.UIEvent.PointerEnter);
            bg.gameObject.BindEvent(() => { if (_hover == index) _hover = -1; }, type: Define.UIEvent.PointerExit);
            bg.gameObject.BindEvent(() => { if (Time.unscaledTime - _openedAt >= ClickGrace) Pick(index); });

            Image choice = CodeUI.NewImage(bg.transform, "Choice", pickArt, pickArt != null ? Color.white : new Color(1f, 1f, 1f, 0.12f), true);
            CodeUI.Stretch(choice.rectTransform);
            _choices.Add(choice.gameObject);

            // 양 끝(48)은 단추 모서리다. 이름 한 줄, 그 밑에 한 줄 설명.
            TextMeshProUGUI name = CodeUI.NewText(bg.transform, "Name", prose, 36f, Ink);
            CodeUI.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -12f), new Vector2(width - 96f, 44f));
            CodeUI.Fit(name, 20f).text = Managers.GetString(Rows[i].Name);

            TextMeshProUGUI desc = CodeUI.NewText(bg.transform, "Desc", prose, 25f, Soft);
            CodeUI.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -58f), new Vector2(width - 96f, 40f));
            CodeUI.Fit(desc, 16f).text = Managers.GetString(Rows[i].Desc);
        }

        TextMeshProUGUI footer = CodeUI.NewText(root, "Controls", prose, 24f, Soft, TextAlignmentOptions.Center);
        CodeUI.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, Pad - 6f), new Vector2(width, FooterHeight));
        CodeUI.Fit(footer, 14f).text = Managers.GetString(UI_TitleScene.MODE_CONTROLS);
    }

    // 줄: 메뉴 단추 그림(240x48). 양 끝의 뾰족한 모서리만 남기고 가운데를 늘린다 (몬스터 도감과 같다).
    static Sprite RowArt(string sprite) => CodeUI.Sliced(CodeUI.PrefabSprite("UI_MenuPopup", sprite), new Vector4(24f, 0f, 24f, 0f));

    void Select(int index, bool sound)
    {
        index = Mathf.Clamp(index, 0, Rows.Length - 1);
        if (sound && index != _cursor)
            Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UImove");
        _cursor = index;
        for (int i = 0; i < _choices.Count; i++)
            _choices[i].SetActive(i == index);
    }

    void Update()
    {
        // 창을 닫은 Enter 가 같은 프레임에 여기서 한 번 더 먹히지 않게 (UIManager.ClosedThisFrame)
        if (_answered || Managers.UI.TopPopup != this || Managers.UI.ClosedThisFrame)
            return;

        // 창이 뜬 뒤 처음 움직였다. 그 전부터 커서 밑에 있던 줄로 간다 — 그 줄의 PointerEnter 는 이미 지나갔다.
        if (_mouseLive == false && (Input.mousePosition - _openMouse).sqrMagnitude > MouseDeadZone * MouseDeadZone)
        {
            _mouseLive = true;
            if (_hover >= 0)
                Select(_hover, true);
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            Select(_cursor - 1, true);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            Select(_cursor + 1, true);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            Pick(_cursor);
    }

    public override bool OnEscape()
    {
        Back();
        return true;
    }

    // 고른 소리는 부르는 쪽이 낸다(다음 확인 창·암전이 "MainTitle_UIselect" 를 낸다) — 같은 소리가 겹치지 않게.
    void Pick(int index)
    {
        if (_answered)
            return;
        _answered = true;
        // 먼저 닫는다. 답이 확인 창을 열어도 이 창이 스택에 남지 않게 (UI_ConfirmPopup 과 같다).
        ClosePopupUI();
        _onPick?.Invoke(Rows[index].Mode);
    }

    void Back()
    {
        if (_answered)
            return;
        _answered = true;
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Back_SFX");
        ClosePopupUI();
    }
}
