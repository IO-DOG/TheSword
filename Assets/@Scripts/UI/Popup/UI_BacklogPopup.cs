using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 지난 대사 (11-28) — 메뉴에서 여는, 이번 실행에서 나눈 대사·바크의 기록(StoryUI.Log, 최근 80줄). 대사를 놓쳤거나(자동 넘김·건너뛰기)
/// 방금 무슨 말을 했는지 다시 보고 싶을 때 쓴다. 읽기만 한다. 쪽은 끝(가장 최근)에서부터 채워 나눈다 — 처음 뜨는 쪽이 늘 가득 차고,
/// 한 줄(화자 + 글)이 두 쪽에 걸쳐 잘리지 않는다.
/// 창의 뼈대는 장부 창(UI_LedgerPopup)과 같다 — 메뉴가 열려 시간이 멎은 동안에도 돌아야 해서 트윈·코루틴을 쓰지 않는다.
/// </summary>
public class UI_BacklogPopup : UI_Popup
{
    public const int Title = 301;       // 지난 대사 (story_ui.py) — 메뉴 줄도 같은 말
    const int Keys = 302;               // W S 넘기기  Enter 닫기 (패드 짝 676)

    const float Width = 1280f, Height = 820f, Pad = 36f;
    const float InputDelay = 0.15f;

    static readonly Color Gold = new Color32(240, 210, 138, 255);
    static readonly Color Soft = new Color32(174, 182, 200, 255);
    static readonly Color Ink = new Color32(236, 236, 242, 255);

    TextMeshProUGUI _body, _page;
    readonly System.Collections.Generic.List<string> _pages = new System.Collections.Generic.List<string>();   // 0 = 가장 최근 쪽
    int _at;
    float _opened;
    bool _closing;

    public static UI_BacklogPopup Show()
    {
        UI_BacklogPopup popup = new GameObject(nameof(UI_BacklogPopup), typeof(RectTransform)).AddComponent<UI_BacklogPopup>();
        Managers.UI.PushPopup(popup);
        popup.Init();
        popup.Build();
        return popup;
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        scaler.enabled = false;
        scaler.enabled = true;
        return true;
    }

    void Build()
    {
        _opened = Time.unscaledTime;
        TMP_FontAsset prose = CodeUI.ProseFont;

        Image dim = CodeUI.NewImage(transform, "Dim", null, new Color(0f, 0f, 0f, 0.55f));
        CodeUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        dim.gameObject.BindEvent(() =>
        {
            if (Time.unscaledTime - _opened >= 0.4f)
                Close();
        });

        Sprite frame = CodeUI.PrefabSprite("UI_InvenPopup", "Inventory_Popup32");
        Image panel = CodeUI.NewImage(transform, "Panel", frame, frame != null ? Color.white : new Color(0.07f, 0.08f, 0.12f, 0.97f), true);
        panel.raycastTarget = true;
        RectTransform root = panel.rectTransform;
        CodeUI.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width, Height));
        root.localScale = Vector3.one * CodeUI.PanelScale(new Vector2(Width, Height));

        TextMeshProUGUI title = CodeUI.NewText(root, "Title", prose, 44f, Gold);
        CodeUI.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Pad), new Vector2(Width - Pad * 2f, 56f));
        title.text = Managers.GetString(Title);
        _page = CodeUI.NewText(root, "Page", CodeUI.NumberFont, 26f, Soft, TextAlignmentOptions.Right);
        CodeUI.Place(_page.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Pad - 10f), new Vector2(Width - Pad * 2f, 40f));

        float bodyWidth = Width - Pad * 2f, bodyHeight = Height - Pad * 2f - 72f - 56f;
        _body = CodeUI.NewText(root, "Lines", prose, 40f, Ink, TextAlignmentOptions.TopLeft);
        CodeUI.Place(_body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Pad - 72f), new Vector2(bodyWidth, bodyHeight));
        _body.textWrappingMode = TextWrappingModes.Normal;
        _body.overflowMode = TextOverflowModes.Truncate;    // 한 줄이 쪽보다 길어도 아래 안내 글을 덮지 않게
        _body.lineSpacing = 6f;

        // 끝에서부터 쪽을 채운다. 줄 하나의 높이는 TMP 에 물어 잰다(글자 크기·줄바꿈 그대로).
        const string Gap = "\n\n";
        var page = new System.Collections.Generic.List<string>();
        float used = 0f;
        for (int k = StoryUI.Log.Count - 1; k >= 0; k--)
        {
            var line = StoryUI.Log[k];
            // 화자는 금빛, 화자 없는 줄(내레이션)은 옅게
            string entry = line.Key.Length > 0 ? $"<color=#F0D28A>{line.Key}</color>  {line.Value}" : $"<color=#AEB6C8>{line.Value}</color>";
            float h = _body.GetPreferredValues(entry + Gap + ".", bodyWidth, 0f).y - _body.GetPreferredValues(".", bodyWidth, 0f).y;
            if (page.Count > 0 && used + h > bodyHeight)
            {
                page.Reverse();
                _pages.Add(string.Join(Gap, page));
                page.Clear();
                used = 0f;
            }
            page.Add(entry);
            used += h;
        }
        page.Reverse();
        _pages.Add(string.Join(Gap, page));
        Turn(0);

        TextMeshProUGUI keys = CodeUI.NewText(root, "Keys", prose, 32f, Soft, TextAlignmentOptions.Center);
        CodeUI.Place(keys.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, Pad - 6f), new Vector2(Width - Pad * 2f, 36f));
        PromptText.Set(keys, Keys);
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_SFX");
    }

    void Turn(int at)
    {
        at = Mathf.Clamp(at, 0, _pages.Count - 1);
        if (at != _at && Time.unscaledTime - _opened > InputDelay)
            Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_SFX");
        _at = at;
        _body.text = _pages[at];
        _page.text = _pages.Count > 1 ? $"{_pages.Count - at}/{_pages.Count}" : "";
    }

    void Update()
    {
        if (_closing || Managers.UI.TopPopup != this || Managers.UI.ClosedThisFrame || Time.unscaledTime - _opened < InputDelay)
            return;
        if (GameKeys.Nav(KeyCode.W) || GameKeys.Nav(KeyCode.UpArrow))
            Turn(_at + 1);      // 위 = 더 예전 쪽
        else if (GameKeys.Nav(KeyCode.S) || GameKeys.Nav(KeyCode.DownArrow))
            Turn(_at - 1);
        else if (GameKeys.Down(KeyCode.Return) || GameKeys.Down(KeyCode.KeypadEnter) || GameKeys.Down(KeyCode.Space))
            Close();
    }

    public override bool OnEscape()
    {
        Close();
        return true;
    }

    void Close()
    {
        if (_closing)
            return;
        _closing = true;
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Back_SFX");
        ClosePopupUI();
    }
}
