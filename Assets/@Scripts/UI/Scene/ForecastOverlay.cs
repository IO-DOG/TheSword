using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 맵 위 몬스터마다 "다음에 싸우면 잃을 체력" 을 띄운다 — 마탑의 데미지 표시. 마검의 눈이 보는 값이라
/// 계약(PlayerData.IsContractedSword) 뒤에만 보이고, V 로 끄고 켠다(GameSettings.ShowForecast).
/// 지는 싸움은 빨간 X, 쓰러뜨리면 레벨이 오르면 ★(ForecastUI.LevelUpMark). 색은 지금 체력에 대한 몫이다(ForecastUI.Tone).
///
/// 숫자는 그림이 <b>보이는</b> 윗변 바로 위에 선다. 옆 칸 숫자와 겹치면 한 줄씩 엇갈려 올린다(Follow).
///
/// 값은 BattleForecast 그대로다. 치명 횟수·방어 게이지가 전투 사이에 이어져서 한 번 싸울 때마다 나머지
/// 값이 바뀐다 — 전투·아이템·레벨·층 이동을 알림으로 받아 다음 프레임에 한 번만 다시 잰다.
/// 매 프레임 하는 일은 글자를 몬스터 머리 위로 옮기는 것뿐이다(카메라가 따라가고 줌을 한다).
///
/// 값이 바뀌면 옛 값에서 새 값까지 세어 가고, 곁에 ▼(싸졌다)·▲(비싸졌다)가 떴다 옅어지며 숫자가 초록·빨강에서 제
/// 색으로 돌아온다. 레벨 업·룬·장비·이어지는 치명 횟수로 층의 절반 넘게 값이 싸지는데, 예전에는 숫자가 소리 없이
/// 갈아 끼워져 아무도 몰랐다. 층에 들어설 때·되살아날 때·처음 칠할 때는 세지 않는다. 화면 밖에서 바뀐 것은 보일 때 센다.
///
/// HUD(UI_GameScene) 밑에 붙는다. HUD 가 꺼지면(연출) 같이 꺼지고, 그리는 순서는 HUD·툴팁보다 아래다.
/// </summary>
public class ForecastOverlay : MonoBehaviour
{
    const float FontSize = 24f;
    const float Lift = 4f;          // 그림 윗변에서 띄우는 거리 (캔버스 단위)
    const float Gap = 4f;           // 이웃 숫자 사이 (캔버스 단위)
    const float CountTime = 0.6f;   // 값이 바뀌면 옛 값에서 새 값까지 세는 시간 (실시간)
    const float MarkTime = 1.2f;    // ▼▲ 가 옅어져 사라질 때까지 (실시간)
    // 값이 바뀐 순간의 짧은 소리 — 대사 글자 소리(0.1초)를 빌린다. 음높이가 바뀌어도 딸깍이다.
    const string TickSound = "TextOutput_SFX";

    class Label
    {
        public RectTransform Rect;
        public TextMeshProUGUI Text;
        public MonsterController Monster;
        public Vector3 Head;        // 몬스터 위치에서 그림의 보이는 윗변 가운데까지
        public Vector2 Size;        // 글자가 차지하는 너비·높이 (캔버스 단위). 글자를 바꿀 때 잰다
        public Vector3 Want;        // 이번 프레임에 서고 싶은 자리 (화면)
        public Vector3 Last;        // 지금 선 자리 (화면)
        public bool Shown;
        public BattleForecast.Result Result;
        public Color Tone;
        public Price Price;         // 이 몬스터의 값 기억 (Rebuild 가 붙인다)
        public bool Live;           // 세는 중이라 매 프레임 다시 칠한다
        public TextMeshProUGUI Mark;    // 숫자 오른쪽의 ▼▲. 처음 쓸 때 만든다 — 글에 넣으면 사라질 때 숫자가 반 칸 튄다
    }

    // 몬스터마다 마지막으로 칠한 값. 같은 종 셋이 id 를 나눠 쓰니 컨트롤러로 묶는다.
    class Price
    {
        public int Value;               // 지금 값 (Cost)
        public float From;              // 세기 시작하는 값 — 플레이어가 마지막으로 본 것
        public int Dir;                 // -1 싸졌다 ▼, +1 비싸졌다 ▲, 0 바뀐 적 없다
        public float Start = float.NaN; // 세기 시작한 때. NaN 이면 화면 밖이라 보일 때까지 기다린다(Follow)
    }

    const int Unknown = -1;             // ? — 세지 않는다
    const int Fatal = 1000000000;       // X — 어떤 값보다 비싸다 (BattleForecast 의 값은 1e7 아래다)
    static int Cost(BattleForecast.Result r) => r.Ok == false ? Unknown : r.Win == false ? Fatal : r.Damage;
    static bool Numeric(float v) => v >= 0f && v < Fatal;

    readonly Dictionary<MonsterController, Price> _prices = new Dictionary<MonsterController, Price>();
    int _pricedStage = -1;

    readonly List<Label> _labels = new List<Label>();     // 풀. 앞의 _used 개만 쓴다
    readonly List<Label> _placing = new List<Label>();    // Follow 가 왼쪽부터 놓는 순서
    readonly List<MonsterController> _monsters = new List<MonsterController>();
    // 왼쪽부터, 같은 세로줄이면 아래부터. 순서가 프레임마다 뒤집히면 엇갈림이 깜빡인다.
    readonly System.Comparison<Label> _leftFirst = (a, b) =>
    {
        int x = a.Want.x.CompareTo(b.Want.x);
        return x != 0 ? x : a.Want.y.CompareTo(b.Want.y);
    };
    readonly Dictionary<int, BattleForecast.Result> _byId = new Dictionary<int, BattleForecast.Result>();
    int _used;
    Canvas _canvas;
    TMP_FontAsset _font;
    Material _material;
    bool _dirty = true;
    bool _visible;
    bool _setting;                  // GameSettings.ShowForecast. PlayerPrefs 를 매 프레임 읽지 않게 들고 있는다

    public static ForecastOverlay Spawn(Transform hud)
    {
        return CodeUI.Stretch(CodeUI.NewRect(hud, nameof(ForecastOverlay))).gameObject.AddComponent<ForecastOverlay>();
    }

    void Awake()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = -1;
        _canvas.enabled = false;
        _font = CodeUI.NumberFont;
        _material = CodeUI.Outlined(_font, 0.25f);
    }

    void OnEnable()
    {
        _setting = GameSettings.ShowForecast;
        _dirty = true;
        GameEvents.HudRefreshed += MarkDirty;
        GameEvents.Respawned += MarkDirty;
        GameEvents.FloorEntered += OnFloorEntered;
        GameEvents.BattleEnded += OnBattleEnded;
        GameEvents.LevelUp += OnLevelUp;
        GameEvents.ItemPicked += OnItemPicked;
        GameSettings.Changed += OnSettingsChanged;
    }

    void OnDisable()
    {
        GameEvents.HudRefreshed -= MarkDirty;
        GameEvents.Respawned -= MarkDirty;
        GameEvents.FloorEntered -= OnFloorEntered;
        GameEvents.BattleEnded -= OnBattleEnded;
        GameEvents.LevelUp -= OnLevelUp;
        GameEvents.ItemPicked -= OnItemPicked;
        GameSettings.Changed -= OnSettingsChanged;
    }

    void MarkDirty() => _dirty = true;
    void OnFloorEntered(int stage, bool firstVisit) => _dirty = true;
    void OnBattleEnded(int monsterId, bool won) => _dirty = true;
    void OnLevelUp(int level) => _dirty = true;
    void OnItemPicked(int itemId, int heal, int overflow) => _dirty = true;

    void OnSettingsChanged()
    {
        _setting = GameSettings.ShowForecast;
        _dirty = true;
        if (_setting == false)
            _prices.Clear();    // 다시 켜면 처음 칠하는 것이다 — 꺼 둔 사이의 차이를 세지 않는다
    }

    void LateUpdate()
    {
        bool visible = Visible();
        if (visible != _visible)
        {
            _visible = visible;
            _canvas.enabled = visible;
        }
        if (visible == false)
            return;

        if (_dirty)
            Rebuild();
        Follow();

        float now = Time.unscaledTime;
        for (int i = 0; i < _used; i++)
        {
            if (_labels[i].Live)
                Paint(_labels[i], now);
        }
    }

    bool Visible() => _setting && Managers.Game.PlayerData.IsContractedSword && MapInView();

    /// <summary>
    /// 맵 위에 얹는 것(이 숫자·몬스터와 아이템 툴팁)이 서도 되는가. 전투·연출·대화·메뉴 동안, 그리고 창이 떠 있으면
    /// 아니다. 저절로 사라지는 층·보스 이름 위에서는 된다. besideInventory 면 인벤토리 위에서도 된다(툴팁만 쓴다 —
    /// UI_GameScene.CanShowTooltip).
    /// </summary>
    public static bool MapInView(bool besideInventory = false)
    {
        GameManager g = Managers.Game;
        if (g.OnBattle || g.OnDirect || g.OnConversation || g.OnFade || g.IsPlayerDead
            || FightGate.Pending || Managers.UI.IsPaused)
            return false;
        UI_Popup top = Managers.UI.TopPopup;
        return top == null || top is UI_StageNamePopup || top is UI_BossNamePopup
            || (besideInventory && top is UI_InvenPopup);
    }

    void Rebuild()
    {
        _dirty = false;
        _used = 0;
        _byId.Clear();

        GameManager g = Managers.Game;
        int stage = g.PlayerData.CurStageid;
        // 층이 바뀌었으면 처음 칠하는 것이다 — 세지 않는다. 같은 챕터의 층은 맵이 그대로 남아 있어서, 다시 내려온
        // 층의 몬스터가 지난번에 본 값을 쥐고 있다. 되살아나면 씬째 새로 서서 기억이 비어 있다.
        if (stage != _pricedStage)
        {
            _prices.Clear();
            _pricedStage = stage;
        }
        GameObject map;
        if (g.Maps.TryGetValue(stage, out map) && map != null)
        {
            // 컨테이너 이름에 기대지 않는다 — 4층 킹 슬라임은 "BossMonsters" 밑에 있다. 꺼진(잡은) 것은 빠진다.
            map.GetComponentsInChildren(false, _monsters);
            foreach (MonsterController mc in _monsters)
            {
                if (ForecastUI.IsStanding(mc))
                    Show(mc, Forecast(mc.id, stage), g.PlayerData.CurHP);
            }
        }

        for (int i = _used; i < _labels.Count; i++)
        {
            _labels[i].Monster = null;
            SetShown(_labels[i], false);
        }
    }

    BattleForecast.Result Forecast(int id, int stage)
    {
        BattleForecast.Result r;
        if (_byId.TryGetValue(id, out r) == false)
            _byId[id] = r = BattleForecast.Of(id, stage);
        return r;
    }

    void Show(MonsterController mc, BattleForecast.Result r, float hp)
    {
        Label label = _used < _labels.Count ? _labels[_used] : NewLabel();
        _used++;

        label.Monster = mc;
        label.Head = HeadOf(mc) - mc.transform.position;
        label.Last = new Vector3(float.NaN, 0f, 0f);    // 다음 Follow 에서 반드시 옮긴다

        label.Result = r;
        label.Tone = ForecastUI.Tone(r, hp);
        label.Price = PriceOf(mc, r);
        Paint(label, Time.unscaledTime);
    }

    // 이 몬스터의 값 기억을 새 값으로 옮긴다. 처음 보는 몬스터면 세지 않는다.
    Price PriceOf(MonsterController mc, BattleForecast.Result r)
    {
        int cost = Cost(r);
        Price p;
        if (_prices.TryGetValue(mc, out p) == false)
        {
            _prices[mc] = p = new Price { Value = cost };
            return p;
        }
        if (cost == p.Value)
            return p;

        // 세던 중에 또 바뀌면 지금 보이는 숫자에서, 화면 밖에서 기다리던 중이면 마지막으로 본 값에서 다시 센다.
        float seen = p.Dir == 0 ? p.Value : float.IsNaN(p.Start) ? p.From : Counted(p, Time.unscaledTime - p.Start);
        p.Value = cost;
        p.From = seen;
        p.Dir = seen == Unknown || cost == Unknown || Mathf.Approximately(seen, cost) ? 0 : cost > seen ? 1 : -1;
        p.Start = float.NaN;
        return p;
    }

    // 세는 중인 숫자. 둘 다 숫자일 때만 센다 — X·? 에서(로) 가는 것은 바로 바뀐다. 빨리 떨어지다 천천히 멎는다.
    static float Counted(Price p, float age)
    {
        if (age >= CountTime || Numeric(p.From) == false || Numeric(p.Value) == false)
            return p.Value;
        float k = age / CountTime;
        return Mathf.Lerp(p.From, p.Value, 1f - (1f - k) * (1f - k));
    }

    /// <summary>
    /// 숫자와 색을 칠한다. 값이 바뀌었으면 옛 값에서 세어 가고, 색은 초록(싸졌다)·빨강(비싸졌다)에서 제 색으로 돌아오며,
    /// 숫자 오른쪽의 ▼▲ 는 다 센 뒤 옅어진다. 화살표가 있어 색을 못 가려도 방향이 읽힌다. ▼▲ 는 두 픽셀 글꼴에 없어
    /// FontFallback 이 얹은 Silver 원본에서 그린다.
    /// </summary>
    void Paint(Label label, float now)
    {
        Price p = label.Price;
        BattleForecast.Result r = label.Result;
        bool waiting = float.IsNaN(p.Start);
        float age = p.Dir == 0 ? MarkTime : waiting ? 0f : now - p.Start;
        label.Live = age < MarkTime && waiting == false;

        string text = ForecastUI.Price(r);
        if (r.Win && age < CountTime && Numeric(p.From))
        {
            int shown = Mathf.RoundToInt(Counted(p, age));
            text = shown > 0 ? "-" + shown : "0";
        }
        text = r.LevelUp ? text + ForecastUI.LevelUpMark : text;
        // 너비는 글이 바뀔 때마다 다시 잰다 — 세는 동안 자릿수가 바뀐다(-105→-95). 겹침(Follow)과 ▼▲ 자리가 이 너비를
        // 쓰는데, 옛 숫자로 잰 채 두면 화살표가 숫자에서 떠 있거나 끝자리를 덮었다.
        if (label.Text.text != text)
        {
            label.Text.text = text;
            label.Size = label.Text.GetPreferredValues();
            if (label.Mark != null)
                label.Mark.rectTransform.anchoredPosition = MarkAt(label.Size);
        }

        bool marked = age < MarkTime;
        Color pulse = p.Dir < 0 ? ForecastUI.Cheaper : ForecastUI.Dearer;
        label.Text.color = marked ? Color.Lerp(pulse, label.Tone, age / CountTime) : label.Tone;
        if (marked)
        {
            TextMeshProUGUI mark = MarkOf(label);
            mark.text = p.Dir < 0 ? "▼" : "▲";
            pulse.a = 1f - Mathf.Clamp01((age - CountTime) / (MarkTime - CountTime));
            mark.color = pulse;
        }
        if (label.Mark != null && label.Mark.gameObject.activeSelf != marked)
            label.Mark.gameObject.SetActive(marked);
    }

    TextMeshProUGUI MarkOf(Label label)
    {
        if (label.Mark == null)
        {
            label.Mark = CodeUI.NewText(label.Rect, "Change", _font, FontSize * 0.8f, Color.white, TextAlignmentOptions.BottomLeft);
            label.Mark.fontSharedMaterial = _material;
            RectTransform rt = label.Mark.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);    // 숫자의 가운데 아래에서, 숫자 너비의 반만큼 오른쪽
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(40f, 32f);
            rt.anchoredPosition = MarkAt(label.Size);
        }
        return label.Mark;
    }

    static Vector2 MarkAt(Vector2 size) => new Vector2(size.x * 0.5f + 1f, 0f);

    /// <summary>
    /// 그림이 보이는 윗변 가운데 (월드). 스프라이트 사각형은 위쪽 빈자리까지 품어서(Mob_001_Idle_3 은 사각형 0.86,
    /// 그려진 끝 0.30) 사각형 위에 세우면 숫자가 한 칸 반 위에 떠 윗칸 몬스터의 것으로 읽혔다. 몬스터 시트는
    /// 촘촘한 메시(Tight)라 꼭짓점이 그려진 곳을 감싼다 — 그 가장 높은 점을 쓴다. 꼭짓점이 없으면 사각형 윗변.
    /// ponytail: 다시 잴 때 보이는 프레임 하나로 잰다. 대기 동작의 높이 차는 시트마다 0.02~0.03, Mob_002 만 0.11
    /// 이라 그 숫자만 전투 뒤마다 조금 오르내린다. 거슬리면 시트별 최대값 표를 둔다.
    /// </summary>
    static Vector3 HeadOf(MonsterController mc)
    {
        SpriteRenderer sr = mc.GetComponentInChildren<SpriteRenderer>();
        Sprite sprite = sr != null ? sr.sprite : null;
        if (sprite == null)
            return mc.transform.position + Vector3.up * Define.TILE_SIZE;

        float top = sprite.bounds.max.y;
        Vector2[] vertices = sprite.vertices;
        if (vertices.Length > 0)
        {
            top = float.MinValue;
            foreach (Vector2 v in vertices)
                top = Mathf.Max(top, v.y);
        }
        // 높이는 몸집·세로 늘림(MapBuilder.StretchBillboard)이 걸린 트랜스폼으로, 가로는 그림 가운데로.
        Vector3 center = sr.bounds.center;
        return new Vector3(center.x, sr.transform.TransformPoint(new Vector3(0f, top, 0f)).y, center.z);
    }

    Label NewLabel()
    {
        TextMeshProUGUI text = CodeUI.NewText(transform, "Forecast", _font, FontSize, Color.white, TextAlignmentOptions.Bottom);
        text.fontSharedMaterial = _material;
        RectTransform rt = text.rectTransform;
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(160f, 32f);
        rt.gameObject.SetActive(false);

        Label label = new Label { Rect = rt, Text = text };
        _labels.Add(label);
        return label;
    }

    // 몬스터를 따라간다. 잡혀서 부서졌거나 꺼졌거나 카메라 뒤면 숨긴다. 자리가 그대로면 건드리지 않는다
    // (옮기면 캔버스가 다시 묶인다).
    void Follow()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        float scale = _canvas.rootCanvas.scaleFactor;
        bool started = false;
        _placing.Clear();
        for (int i = 0; i < _used; i++)
        {
            Label label = _labels[i];
            MonsterController mc = label.Monster;
            bool on = mc != null && mc.gameObject.activeInHierarchy;
            if (on)
            {
                label.Want = cam.WorldToScreenPoint(mc.transform.position + label.Head);
                // 몸의 가운데가 화면 밖이면 숨긴다. 화면 아래 가장자리에 머리만 걸친 몬스터는 몸이 안 보이는데 숫자만
                // 떠서, HUD 의 "치명까지" 글자 사이로 비쳐 보였다.
                Vector3 body = cam.WorldToScreenPoint(mc.transform.position + label.Head * 0.5f);
                on = label.Want.z > 0f && body.x >= 0f && body.x <= Screen.width && body.y >= 0f && body.y <= Screen.height;
            }
            SetShown(label, on);
            if (on == false)
                continue;

            // 값이 바뀐 것은 보이는 순간부터 센다 — 화면 밖에서 다 세어 버리면 바뀐 줄 모른다.
            if (label.Price.Dir != 0 && float.IsNaN(label.Price.Start))
            {
                label.Price.Start = Time.unscaledTime;
                label.Live = true;
                started = true;
            }

            label.Want.y += Lift * scale;
            label.Want.z = 0f;
            _placing.Add(label);
        }
        if (started)
            Managers.Sound.Play(Define.Sound.Effect, TickSound);

        // 같은 종 셋이 한 줄로 선다(층마다 앞 세 자리). 숫자가 칸보다 넓으면 옆 숫자에 붙어 "-42 -42 -42" 가
        // 한 덩어리로 읽혔다. 왼쪽부터 놓고, 먼저 놓인 것과 겹치면 그 위로 올린다 — 셋이면 아래·위·아래로 엇갈린다.
        // 줌을 바꾸지 않는 한 순서와 겹침이 그대로라 카메라가 따라가도 흔들리지 않는다.
        _placing.Sort(_leftFirst);
        for (int i = 0; i < _placing.Count; i++)
        {
            Label label = _placing[i];
            Vector3 at = label.Want;
            // ponytail: 먼저 놓인 모두와 견준다(n²) — 한 층에 열 남짓이다. 수십이 되면 x 로 정렬돼 있으니 가까운 것만 보면 된다.
            // 올릴 때마다 처음부터 다시 본다. 늘 위로만 가니 끝난다.
            for (int j = 0; j < i; j++)
            {
                Label other = _placing[j];
                if (Overlaps(label, at, other, scale))
                {
                    at.y = other.Last.y + (other.Size.y + Gap) * scale;
                    j = -1;
                }
            }
            // 보스는 덩치 2 라(MapBuilder.MonsterBulk) 화면 위쪽에 서면 머리 위 숫자가 화면 밖으로 나갔다 — 위 가장자리 안에 붙든다.
            at.y = Mathf.Min(at.y, Screen.height - label.Size.y * scale);
            if ((at - label.Last).sqrMagnitude > 0.01f || float.IsNaN(label.Last.x))
            {
                label.Last = at;
                label.Rect.position = at;
            }
        }
    }

    // 둘 다 가운데 아래(피벗 0.5, 0)에 선다. 가로는 글자 너비 + 틈, 세로는 글자 높이로 잰다.
    static bool Overlaps(Label a, Vector3 at, Label b, float scale)
    {
        return Mathf.Abs(at.x - b.Last.x) < ((a.Size.x + b.Size.x) * 0.5f + Gap) * scale
            && at.y < b.Last.y + b.Size.y * scale
            && b.Last.y < at.y + a.Size.y * scale;
    }

    static void SetShown(Label label, bool on)
    {
        if (label.Shown == on)
            return;
        label.Shown = on;
        label.Rect.gameObject.SetActive(on);
    }
}
