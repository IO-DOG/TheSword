using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 띠 결산 (기획 L2·L6). 띠(다섯 층)의 마지막 층 위 계단을 처음 오를 때 SwordLedger.CoCloseBand 가 띄우고, 계단은 이 창이
/// 닫힐 때까지 기다린다. 그 띠의 별(★ 통과 · ★★ 기준 이하 · ★★★ 기준의 70% 이하, 기준이 없으면 별도 없다), 예언·치름·기준,
/// 다음 다섯 층에 서는 특성(처음 나오는 것은 금빛). 그 층에 제단이 있으면(StageInfoData.AltarPrice > 0) 최대 체력의 몇 %를
/// 바치고 공격이나 방어를 하나 받는다 — 값과 거절은 SwordLedger.Altar 가 generate_content 와 같은 식으로 잰다.
///
/// 처음 골라 둔 것은 "지나간다" 라서 Enter·Space·Esc·Tab·창 밖 클릭은 전부 그냥 넘어간다. ←→(A/D)로 고르고 Enter 로 받는다.
/// 마우스는 창이 뜨고 ClickDelay 뒤부터 받고, 골라 둔 단추를 눌러야 받는다(다른 단추를 누르면 고르기만) — 계단을 두 번 눌러
/// 빨리 걷던 두 번째 누름(PlayerController.DoubleClickTime)이 창에 떨어져 체력을 바치거나 제단을 지나치지 않게.
/// 고를 것이 없으면(제단이 없는 층, 체력이 모자라 못 사는 때, 받은 뒤) 몇 초 뒤 스스로 닫힌다. 봇은 늘 지나간다.
/// 프리팹 없이 세운다 — 틀·단추 그림과 글꼴은 도감(UI_MonsterManualPopup)처럼 이미 있는 프리팹에서 빌린다.
/// </summary>
public class UI_TallyPopup : UI_Popup
{
    // 문구 (Tools/ui_text_parts/ledger.py)
    public const int TitleText = 470;       // {0}~{1}층 결산
    public const int TotalsText = 471;      // 예언 {0}  치름 {1}  기준 {2}
    const int StarsLegend = 472;
    const int NextText = 473;               // 다음 다섯 층: {0}
    const int AltarTitle = 474;
    const int AltarDesc = 475;              // 최대 체력의 {0}%를 바치고 하나를 받는다
    const int PriceText = 476;              // 체력 -{0}
    const int PassText = 477;
    const int RefuseText = 478;             // 체력이 모자라다: 바친 뒤에도 {0} 이상 남아야 한다
    const int BoughtText = 479;             // 받았다: {0}
    const int ChooseKeys = 480;
    const int ContinueKey = 481;

    const float Width = 1000f, Pad = 32f, ButtonHeight = 96f, ButtonGap = 20f;
    const float AutoClose = 2.5f;           // 고를 것이 없으면 이만큼(실시간 초) 뒤 스스로 닫힌다
    const float BotClose = 0.3f;
    const float InputDelay = 0.15f;         // 계단을 밟은 그 순간의 입력은 받지 않는다
    const float ClickDelay = 0.4f;          // 마우스는 더 — 두 번 누름(0.35초)의 두 번째가 지나가도록
    const int Pass = 2;                     // 단추: 0 공격 · 1 방어 · 2 지나간다

    static readonly Color Gold = new Color32(240, 210, 138, 255);
    static readonly Color Soft = new Color32(174, 182, 200, 255);
    static readonly Color Ink = new Color32(236, 236, 242, 255);
    static readonly Color Warn = new Color32(255, 150, 60, 255);

    int _stage;
    bool _choosing;                         // 제단 앞에서 고르는 중 (아직 받지도 지나가지도 않았다)
    int _cursor = Pass;
    readonly bool[] _enabled = new bool[3];
    readonly GameObject[] _picked = new GameObject[3];
    readonly CanvasGroup[] _buttons = new CanvasGroup[3];
    readonly string[] _gains = new string[3];
    TextMeshProUGUI _altarDesc, _footer;
    float _opened, _closeAt = float.MaxValue;
    Vector2 _openMouse;                     // 창이 뜰 때의 마우스 — 가만히 있던 커서가 단추를 고르지 않게
    bool _closing;

    bool ClickReady => Time.unscaledTime - _opened >= ClickDelay;

    /// <summary>창을 띄운다. 닫히면(파괴되면) 부른 쪽의 기다림이 끝난다.</summary>
    public static UI_TallyPopup Show(int stageId, int stars, int foretold, int paid, int par)
    {
        UI_TallyPopup popup = new GameObject(nameof(UI_TallyPopup), typeof(RectTransform)).AddComponent<UI_TallyPopup>();
        Managers.UI.PushPopup(popup);
        popup.Init();
        popup.Build(stageId, stars, foretold, paid, par);
        return popup;
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;
        // 도감과 같다 — 높이를 1080 에 맞춰 어느 화면비에서도 창이 세로로 들어가게 하고, 첫 프레임부터 맞게 다시 켠다.
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        scaler.enabled = false;
        scaler.enabled = true;
        return true;
    }

    /// <summary>★★☆ — 받은 별은 금빛, 못 받은 자리는 어둡게. 0(기준이 없던 띠)은 "-". 판 카드도 쓴다.</summary>
    public static string StarText(int stars)
    {
        if (stars <= 0)
            return "-";
        stars = Mathf.Min(stars, 3);
        return $"<color=#F0D28A>{new string('★', stars)}</color><color=#5A6272>{new string('☆', 3 - stars)}</color>";
    }

    #region 그리기
    void Build(int stageId, int stars, int foretold, int paid, int par)
    {
        _stage = stageId;
        _opened = Time.unscaledTime;
        _openMouse = Input.mousePosition;
        int band = stageId / SwordLedger.BandFloors;
        TMP_FontAsset prose = CodeUI.ProseFont, number = CodeUI.NumberFont;
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_SFX");

        Image dim = CodeUI.NewImage(transform, "Dim", null, new Color(0f, 0f, 0f, 0.55f));
        CodeUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        dim.gameObject.BindEvent(() =>      // 창 밖을 누르면 지나간다
        {
            if (ClickReady)
                Close();
        });

        Sprite frame = CodeUI.PrefabSprite("UI_InvenPopup", "Inventory_Popup32");
        Image panel = CodeUI.NewImage(transform, "Panel", frame, frame != null ? Color.white : new Color(0.07f, 0.08f, 0.12f, 0.97f), true);
        panel.raycastTarget = true;         // 창 안을 눌러도 지나가지 않게 가린다
        RectTransform root = panel.rectTransform;
        float y = -Pad;

        TextMeshProUGUI title = Line(root, "Title", prose, 40f, Gold, ref y, 56f);
        title.text = string.Format(Managers.GetString(TitleText), band * SwordLedger.BandFloors + 1, band * SwordLedger.BandFloors + SwordLedger.BandFloors);
        if (stars > 0)
        {
            TextMeshProUGUI starText = CodeUI.NewText(root, "Stars", number, 52f, Gold, TextAlignmentOptions.Right);
            CodeUI.Place(starText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Pad, -Pad), new Vector2(300f, 56f));
            starText.text = StarText(stars);
        }

        // 띠 합은 기준이 있는 싸움만 센다 — 기준이 없으면(자료가 0) 셀 것도 별도 없다.
        if (par > 0)
        {
            TextMeshProUGUI totals = Line(root, "Totals", number, 30f, Ink, ref y, 44f);
            totals.fontSharedMaterial = CodeUI.Outlined(number, 0.2f);
            totals.text = string.Format(Managers.GetString(TotalsText), foretold, paid, par);
            // 숫자가 든 줄은 숫자 글꼴로 — 이야기 글꼴(Silver)의 숫자는 한글의 반 높이라 1280x800 에서 "70%" 가 5~6 픽셀, "78%" 로 읽혔다.
            Line(root, "Legend", number, 20f, Soft, ref y, 32f).text = Managers.GetString(StarsLegend);
        }

        string next = NextTraits(band);
        if (next.Length > 0)
        {
            y -= 8f;
            Line(root, "Next", prose, 28f, Ink, ref y, 38f).text = string.Format(Managers.GetString(NextText), next);
        }

        if (SwordLedger.Altar(stageId, out SwordLedger.AltarOffer offer))
            y = BuildAltar(root, offer, y);
        if (_choosing == false)             // 제단이 없거나 못 사면 고를 것이 없다 — 거절한 까닭까지 읽을 만큼 두고 닫힌다
            _closeAt = _opened + AutoClose;

        y -= 10f;
        _footer = Line(root, "Keys", prose, 26f, Soft, ref y, 34f, TextAlignmentOptions.Center);   // 22 는 1280x800 에서 9 픽셀
        _footer.text = Managers.GetString(_choosing ? ChooseKeys : ContinueKey);
        y -= Pad;

        CodeUI.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width, -y));
        root.localScale = Vector3.one * 0.92f;
        root.DOScale(1f, 0.12f).SetEase(Ease.OutBack).SetLink(gameObject);
        Paint();
    }

    float BuildAltar(RectTransform root, SwordLedger.AltarOffer offer, float y)
    {
        _choosing = offer.Sells;            // 못 사면 단추는 보이되(값을 알게) 고르지는 않는다
        y -= 14f;
        Sprite lineSprite = CodeUI.PrefabSprite("UI_SettingPopup", "SystemUI_Setting_ClassLine");
        Image line = CodeUI.NewImage(root, "Line", lineSprite, lineSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.2f));
        CodeUI.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(Width - Pad * 2f, 2f));
        y -= 14f;

        Line(root, "AltarTitle", CodeUI.ProseFont, 34f, Gold, ref y, 44f).text = Managers.GetString(AltarTitle);
        _altarDesc = Line(root, "AltarDesc", CodeUI.NumberFont, 22f, Soft, ref y, 34f);    // "10%" — 숫자 글꼴(Legend 와 같은 까닭)
        _altarDesc.text = string.Format(Managers.GetString(AltarDesc), offer.Percent.ToString("0.#"));
        y -= 10f;

        _enabled[0] = offer.Sells && offer.Atk > 0f;
        _enabled[1] = offer.Sells && offer.Def > 0f;
        _enabled[Pass] = true;
        _gains[0] = string.Format(Managers.GetString(ForecastUI.AtkUp), Mathf.RoundToInt(offer.Atk));
        _gains[1] = string.Format(Managers.GetString(ForecastUI.DefUp), Mathf.RoundToInt(offer.Def));
        _gains[Pass] = Managers.GetString(PassText);
        string price = string.Format(Managers.GetString(PriceText), offer.Price);
        float width = (Width - Pad * 2f - ButtonGap * 2f) / 3f;
        for (int i = 0; i < 3; i++)
            Button(root, i, (i - 1) * (width + ButtonGap), y, width, _gains[i], i == Pass ? "" : price);
        y -= ButtonHeight + 8f;

        if (offer.Sells == false)
            Line(root, "Refused", CodeUI.NumberFont, 22f, Warn, ref y, 34f).text = string.Format(Managers.GetString(RefuseText), offer.Keep);
        return y;
    }

    void Button(Transform root, int index, float x, float top, float width, string label, string sub)
    {
        Sprite art = RowArt("SystemUI_Button");
        Image bg = CodeUI.NewImage(root, $"Choice{index}", art, art != null ? Color.white : new Color(0.1f, 0.12f, 0.18f, 0.9f), true);
        bg.raycastTarget = true;
        CodeUI.Place(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, top), new Vector2(width, ButtonHeight));
        _buttons[index] = bg.gameObject.AddComponent<CanvasGroup>();
        _buttons[index].alpha = _enabled[index] ? 1f : 0.35f;

        Sprite pick = RowArt("SystemUI_Choice");
        Image picked = CodeUI.NewImage(bg.transform, "Picked", pick, pick != null ? Color.white : new Color(1f, 1f, 1f, 0.15f), true);
        CodeUI.Stretch(picked.rectTransform);
        _picked[index] = picked.gameObject;

        bool twoLines = sub.Length > 0;
        TextMeshProUGUI main = CodeUI.NewText(bg.transform, "Label", CodeUI.ProseFont, 32f, Ink, TextAlignmentOptions.Center);
        CodeUI.Place(main.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, twoLines ? -10f : -(ButtonHeight - 44f) / 2f),
            new Vector2(width - 56f, 44f));
        CodeUI.Fit(main, 16f).text = label;
        if (twoLines)
        {
            TextMeshProUGUI price = CodeUI.NewText(bg.transform, "Price", CodeUI.NumberFont, 22f, ForecastUI.Danger, TextAlignmentOptions.Center);
            price.fontSharedMaterial = CodeUI.Outlined(price.font, 0.2f);
            CodeUI.Place(price.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(width - 56f, 30f));
            CodeUI.Fit(price, 12f).text = sub;
        }

        // 마우스를 움직여 올렸을 때만 고른다. 창이 뜬 자리에 커서가 있던 단추가 골라지면 Enter 가 체력을 바쳤다.
        bg.gameObject.BindEvent(() =>
        {
            if ((Vector2)Input.mousePosition != _openMouse)
                Select(index);
        }, type: Define.UIEvent.PointerEnter);
        // 골라 둔 단추가 아니면 고르기만 한다 — 커서가 놓여 있던 단추를 한 번 눌러 체력을 바치지 않게. 못 고르는 단추는 Pick 이 "안 된다" 소리를 낸다.
        bg.gameObject.BindEvent(() =>
        {
            if (ClickReady == false)
                return;
            if (index != _cursor && _enabled[index])
                Select(index);
            else
                Pick(index);
        });
    }

    TextMeshProUGUI Line(Transform root, string name, TMP_FontAsset font, float size, Color color, ref float y, float height,
                         TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        TextMeshProUGUI text = CodeUI.NewText(root, name, font, size, color, align);
        CodeUI.Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(Width - Pad * 2f, height));
        y -= height;
        return CodeUI.Fit(text, size * 0.5f);
    }

    // 도감의 줄과 같은 메뉴 단추 그림 — 양 끝 모서리만 남기고 가운데를 늘린다.
    static Sprite RowArt(string sprite) => CodeUI.Sliced(CodeUI.PrefabSprite("UI_MenuPopup", sprite), new Vector4(24f, 0f, 24f, 0f));

    /// <summary>
    /// 다음 띠(다섯 층)에 서는 특성 이름. 몬스터 id 는 1000 + 층·8 + 자리, 보스는 900 + 챕터(generate_content 의 약속 —
    /// MonsterTint 도 같은 셈으로 층을 되짚는다). 이 띠까지 한 번도 안 나온 특성은 금빛 — 새 규칙이 온다. 없으면 "".
    /// </summary>
    static string NextTraits(int band)
    {
        int first = (band + 1) * SwordLedger.BandFloors + 1;
        List<int> seen = Traits(1, first - 1), next = Traits(first, first + SwordLedger.BandFloors - 1);
        if (next.Count > 1)
            next.Remove(0);                 // "없음" 은 다른 특성이 있으면 빼고 센다
        List<string> names = new List<string>(next.Count);
        foreach (int ability in next)
        {
            if (Managers.Data.MonsterClassDic.TryGetValue(ability, out Data.MonsterClassData trait) == false)
                continue;
            string name = Managers.GetString(trait.ClassName);
            names.Add(seen.Contains(ability) ? name : $"<color=#E8C170>{name}</color>");
        }
        return string.Join(" / ", names);
    }

    static List<int> Traits(int fromFloor, int toFloor)
    {
        List<int> found = new List<int>();
        for (int floor = fromFloor; floor <= toFloor; floor++)
        {
            for (int k = 0; k <= 8; k++)
            {
                int id = k < 8 ? 1000 + floor * 8 + k : floor % 20 == 0 ? 900 + floor / 20 - 1 : -1;
                if (Managers.Data.MonsterDic.TryGetValue(id, out Data.MonsterData md) && found.Contains(md.Ability) == false)
                    found.Add(md.Ability);
            }
        }
        return found;
    }
    #endregion

    #region 고르기
    void Update()
    {
        if (_closing)
            return;
        float t = Time.unscaledTime - _opened;
        if (GameEvents.IsAutoPlaying)       // 봇은 늘 지나간다 — 녹화에 잠깐 비친다
        {
            if (t >= BotClose)
                Close();
            return;
        }
        if (Managers.UI.TopPopup != this || t < InputDelay)
            return;

        bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
        if (_choosing == false)
        {
            if (enter || Input.GetKeyDown(KeyCode.Tab) || (Input.GetMouseButtonDown(0) && ClickReady) || Time.unscaledTime >= _closeAt)
                Close();
            return;
        }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            Move(-1);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            Move(1);
        else if (enter)
            Pick(_cursor);
        else if (Input.GetKeyDown(KeyCode.Tab))
            Close();
    }

    // Esc 는 씬 UI 가 여기로 넘겨준다 — 언제나 지나간다.
    public override bool OnEscape()
    {
        Close();
        return true;
    }

    void Move(int dir)
    {
        for (int i = _cursor + dir; i >= 0 && i < 3; i += dir)
        {
            if (_enabled[i])
            {
                Select(i);
                return;
            }
        }
    }

    void Select(int index)
    {
        if (_choosing == false || _enabled[index] == false || index == _cursor)
            return;
        _cursor = index;
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Choice_SFX");
        Paint();
    }

    void Pick(int index)
    {
        if (_choosing == false || _closing)
            return;
        if (index == Pass)
        {
            Close();
            return;
        }
        if (_enabled[index] == false || SwordLedger.BuyAltar(_stage, index == 0) == false)
        {
            Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_No_SFX");
            return;
        }

        // 받았다. 한 번 들를 때 하나 — 단추를 거두고 받은 것을 보인 뒤, 누르거나 몇 초 뒤 닫힌다.
        _choosing = false;
        for (int i = 0; i < 3; i++)
        {
            _enabled[i] = false;
            _buttons[i].alpha = i == index ? 1f : 0.35f;
        }
        _altarDesc.text = string.Format(Managers.GetString(BoughtText), _gains[index]);
        _altarDesc.color = Gold;
        _footer.text = Managers.GetString(ContinueKey);
        _closeAt = Time.unscaledTime + AutoClose;
        Paint();
    }

    // 골라 둔 단추만 반짝인다. 고르는 중이 아니면 아무것도.
    void Paint()
    {
        for (int i = 0; i < 3; i++)
        {
            if (_picked[i] != null)
                _picked[i].SetActive(_choosing && i == _cursor);
        }
    }

    void Close()
    {
        if (_closing)
            return;
        _closing = true;
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Back_SFX");
        ClosePopupUI();
    }
    #endregion
}
