using Febucci.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 예/아니오를 묻는 창 하나. 타이틀(새 게임이 저장을 지운다)과 게임(메뉴의 종료·타이틀·다시 시작,
/// 전투 직전 관문)이 같이 쓴다. 팝업 스택에 올라가므로 Esc 는 씬 UI 가 OnEscape 로 넘겨준다.
///
/// 그림은 새로 그리지 않고 보스방 확인 창 프리팹을 빌려 쓴다 — 글 상자와 O/X 버튼, 버튼이 반짝이는
/// 애니메이션이 이미 있다. 상자만 마검 계약 창의 것(칼 문장)으로 바꾼다. 보스방 것은 악마 가면이라
/// "게임을 끌까요" 에 맞지 않는다.
///
/// 키보드: A/D 또는 ←/→ 로 고르고 Enter/Space 로 누른다. Esc 는 "아니오"(알림이면 확인).
/// 되돌릴 수 없는 질문은 AskDestructive 로 묻는다 — 처음 골라 둔 쪽이 "아니오" 라 Enter 를 연타해도
/// 아무것도 지워지지 않는다. 나머지(Ask)는 "예" 가 먼저 골라져 있다.
/// 자동 플레이 중이면 창을 띄우지 않고 곧장 "예" 로 간다.
/// </summary>
public class UI_ConfirmPopup : UI_Popup
{
    const string PrefabKey = "UI_BossRoomCheckPopup";
    const string BoxArtKey = "UI_MagicalSwordCheckPopup";

    // 빌려 온 프리팹 안의 이름 그대로다.
    enum Images { BossRoomCheckBox }
    enum Texts { BossRoomCheckText }
    enum Buttons { YesBtn, NoBtn }

    /// <summary>확인 창이 하나라도 떠 있다. 떠 있는 동안 플레이어는 움직이지 않는다.</summary>
    public static bool IsOpen => Managers.UI.FindPopup<UI_ConfirmPopup>() != null;

    public static void Ask(string message, Action onYes, Action onNo = null, string yesLabel = null, string noLabel = null)
        => Open(message, onYes, onNo, yesLabel, noLabel, true, false);

    /// <summary>되돌릴 수 없는 일(새 게임, 타이틀로, 다시 시작, 종료)을 물을 때. 처음엔 "아니오" 가 골라져 있다.</summary>
    public static void AskDestructive(string message, Action onYes, Action onNo = null, string yesLabel = null, string noLabel = null)
        => Open(message, onYes, onNo, yesLabel, noLabel, false, false);

    /// <summary>알림 한 마디. 버튼은 확인 하나뿐이고 Esc 도 확인이다.</summary>
    public static void Tell(string message, Action onClose = null)
        => Open(message, onClose, onClose, null, null, true, true);

    Action _onYes;
    Action _onNo;
    bool _yes;          // 지금 골라 둔 쪽 — Enter 가 누를 것
    bool _single;       // 알림: 확인 버튼 하나
    bool _answered;

    static void Open(string message, Action onYes, Action onNo, string yesLabel, string noLabel, bool defaultYes, bool single)
    {
        if (GameEvents.IsAutoPlaying)
        {
            onYes?.Invoke();
            return;
        }

        if (Managers.Resource.Load<GameObject>(PrefabKey) == null)
        {
            // 그림을 못 올렸으면 물을 수가 없다. Enter 를 바로 누른 것과 같은 답으로 넘어간다 —
            // 되돌릴 수 없는 질문은 "아니오" 쪽이라 아무것도 지워지지 않는다.
            Debug.LogError($"[Confirm] {PrefabKey} 가 없어 묻지 못했다: {message}");
            (defaultYes ? onYes : onNo)?.Invoke();
            return;
        }

        UI_ConfirmPopup popup = Managers.UI.ShowPopupUI<UI_ConfirmPopup>(PrefabKey);
        popup.Setup(message, onYes, onNo, yesLabel, noLabel, defaultYes, single);
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindButton(typeof(Buttons));
        return true;
    }

    void Setup(string message, Action onYes, Action onNo, string yesLabel, string noLabel, bool defaultYes, bool single)
    {
        // 빌려 온 프리팹의 원래 스크립트는 Start 전에 뗀다. 두면 대화 잠금을 켜고, 예를 누르면 보스방 문이 열린다.
        UI_BossRoomCheckPopup borrowed = GetComponent<UI_BossRoomCheckPopup>();
        if (borrowed != null)
        {
            borrowed.enabled = false;
            Destroy(borrowed);
        }

        // 다음 프레임(Start)까지 기다리면 프리팹의 자리 글("무시무시")이 한 프레임 비친다.
        Init();

        _onYes = onYes;
        _onNo = onNo;
        _single = single;
        _yes = single || defaultYes;

        AddDim();

        Image box = GetImage((int)Images.BossRoomCheckBox);
        box.color = Color.white;
        GameObject boxArt = Managers.Resource.Load<GameObject>(BoxArtKey);
        Transform swordBox = boxArt != null ? boxArt.transform.Find("MagicalSwordCheckBox") : null;
        Image swordImage = swordBox != null ? swordBox.GetComponent<Image>() : null;
        if (swordImage != null)
            box.sprite = swordImage.sprite;

        // 글은 한 번에 보인다. 타자기는 멈춘 시간(게임 메뉴)에서는 한 글자도 찍지 않는다.
        // 끄기만 한다 — 타자기가 애니메이터를 요구해서(RequireComponent) 애니메이터는 떼어 낼 수 없다.
        // 애니메이터는 켜질 때 TMP 가 제 글을 그리지 못하게 막아 두므로(DontRender) 다시 그리게 한다.
        TMP_Text text = GetText((int)Texts.BossRoomCheckText);
        TypewriterByCharacter writer = text.GetComponent<TypewriterByCharacter>();
        if (writer != null)
            writer.enabled = false;
        TextAnimator_TMP animator = text.GetComponent<TextAnimator_TMP>();
        if (animator != null)
            animator.enabled = false;
        text.gameObject.SetActive(true);   // 꺼져 있었다면 여기서 애니메이터의 Awake 가 돈다 — 그 뒤에 되돌린다
        text.renderMode = TextRenderFlags.Render;
        text.fontSizeMax = text.fontSize;
        text.fontSizeMin = Mathf.Min(24f, text.fontSize);
        text.enableAutoSizing = true;
        text.text = message;

        Button yes = GetButton((int)Buttons.YesBtn);
        Button no = GetButton((int)Buttons.NoBtn);
        yes.gameObject.SetActive(true);
        no.gameObject.SetActive(single == false);
        if (single)
        {
            RectTransform rt = (RectTransform)yes.transform;
            rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
        }

        // 게임 메뉴 위에서는 시간이 멈춰 있다. 버튼 반짝임은 그래도 돈다.
        foreach (Animator a in GetComponentsInChildren<Animator>(true))
            a.updateMode = AnimatorUpdateMode.UnscaledTime;

        yes.gameObject.BindEvent(() => Select(true), type: Define.UIEvent.PointerEnter);
        no.gameObject.BindEvent(() => Select(false), type: Define.UIEvent.PointerEnter);
        yes.gameObject.BindEvent(() => Answer(true));
        no.gameObject.BindEvent(() => Answer(false));

        AddLabel(yes, yesLabel, text);
        AddLabel(no, noLabel, text);

        Paint();
    }

    // 뒤를 어둡게 덮는다. 상자 밖을 눌러도 밑의 메뉴·타이틀 버튼이 눌리지 않는다.
    void AddDim()
    {
        GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(transform, false);
        dim.transform.SetAsFirstSibling();
        RectTransform rt = (RectTransform)dim.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
    }

    // 버튼은 O/X 그림뿐이다. 부르는 쪽이 이름을 주면 버튼 밑에 적는다.
    static void AddLabel(Button button, string label, TMP_Text style)
    {
        if (string.IsNullOrEmpty(label))
            return;

        GameObject go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(button.transform, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 60f);
        rt.anchoredPosition = new Vector2(0f, -8f);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = style.font;
        text.color = style.color;
        text.fontSize = 40f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = label;
    }

    void Update()
    {
        if (_answered || Managers.UI.TopPopup != this)
            return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            Select(true);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            Select(false);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            Answer(_yes);
    }

    public override bool OnEscape()
    {
        Answer(_single);
        return true;
    }

    void Select(bool yes)
    {
        if (_single || _answered || _yes == yes)
            return;

        _yes = yes;
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Choice_SFX");
        Paint();
    }

    // 골라 둔 버튼만 반짝인다. 마우스를 떼어도 그대로 두어 Enter 가 무엇을 누를지 보이게 한다.
    void Paint()
    {
        if (_single)
        {
            GetButton((int)Buttons.YesBtn).GetComponent<Animator>().Play("OnlyYesMouseOver");
            return;
        }
        GetButton((int)Buttons.YesBtn).GetComponent<Animator>().Play(_yes ? "YesMouseOver" : "YesIdle");
        GetButton((int)Buttons.NoBtn).GetComponent<Animator>().Play(_yes ? "NoIdle" : "NoMouseOver");
    }

    void Answer(bool yes)
    {
        if (_answered)
            return;
        _answered = true;

        Managers.Sound.Play(Define.Sound.Effect, yes ? "ButtonUI_Ok_SFX" : "ButtonUI_No_SFX");
        // 먼저 닫는다. 답이 새 창을 열거나 씬을 바꿔도 이 창이 스택에 남지 않게.
        ClosePopupUI();
        (yes ? _onYes : _onNo)?.Invoke();
    }

    // 답을 받기 전에 사라졌다(연출이 창을 모두 걷었거나 씬이 바뀌었다). 묻던 쪽이 답을 기다리며
    // 굳지 않게 "아니오" 로 끝낸다 — 전투 관문이 그 답으로 전투를 거둔다.
    // 플레이를 끄는 중이면(매니저가 먼저 사라졌으면) 기다리는 쪽도 없다. 답이 매니저를 부르면 @Managers 가
    // 새로 서서 다음 플레이가 깨진다(도메인 리로드가 꺼져 있다).
    void OnDestroy()
    {
        if (_answered || Managers.IsAlive == false)
            return;
        _answered = true;

        try { _onNo?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
    }
}
