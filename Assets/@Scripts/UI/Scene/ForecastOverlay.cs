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
/// HUD(UI_GameScene) 밑에 붙는다. HUD 가 꺼지면(연출) 같이 꺼지고, 그리는 순서는 HUD·툴팁보다 아래다.
/// </summary>
public class ForecastOverlay : MonoBehaviour
{
    const float FontSize = 24f;
    const float Lift = 4f;          // 그림 윗변에서 띄우는 거리 (캔버스 단위)
    const float Gap = 4f;           // 이웃 숫자 사이 (캔버스 단위)

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
    }

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
    }

    // 전투·연출·대화·메뉴 동안, 그리고 창이 떠 있으면 숨긴다. 저절로 사라지는 층·보스 이름 위에서는 보인다.
    bool Visible()
    {
        GameManager g = Managers.Game;
        if (_setting == false || g.PlayerData.IsContractedSword == false)
            return false;
        if (g.OnBattle || g.OnDirect || g.OnConversation || g.OnFade || g.IsPlayerDead
            || FightGate.Pending || Managers.UI.IsPaused)
            return false;
        UI_Popup top = Managers.UI.TopPopup;
        return top == null || top is UI_StageNamePopup || top is UI_BossNamePopup;
    }

    void Rebuild()
    {
        _dirty = false;
        _used = 0;
        _byId.Clear();

        GameManager g = Managers.Game;
        int stage = g.PlayerData.CurStageid;
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

        label.Text.color = ForecastUI.Tone(r, hp);
        label.Text.text = r.Ok == false ? "?"
            : r.Win == false ? "X"
            : (r.Damage > 0 ? "-" + r.Damage : "0") + (r.LevelUp ? ForecastUI.LevelUpMark : "");
        label.Size = label.Text.GetPreferredValues();
    }

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
        _placing.Clear();
        for (int i = 0; i < _used; i++)
        {
            Label label = _labels[i];
            MonsterController mc = label.Monster;
            bool on = mc != null && mc.gameObject.activeInHierarchy;
            if (on)
            {
                label.Want = cam.WorldToScreenPoint(mc.transform.position + label.Head);
                on = label.Want.z > 0f;
            }
            SetShown(label, on);
            if (on == false)
                continue;

            label.Want.y += Lift * scale;
            label.Want.z = 0f;
            _placing.Add(label);
        }

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
