using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 맵 위 몬스터마다 "다음에 싸우면 잃을 체력" 을 띄운다 — 마탑의 데미지 표시. 마검의 눈이 보는 값이라
/// 계약(PlayerData.IsContractedSword) 뒤에만 보이고, V 로 끄고 켠다(GameSettings.ShowForecast).
/// 지는 싸움은 빨간 X, 쓰러뜨리면 레벨이 오르면 LV+. 색은 지금 체력에 대한 몫이다(ForecastUI.Tone).
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

    class Label
    {
        public RectTransform Rect;
        public TextMeshProUGUI Text;
        public MonsterController Monster;
        public Vector3 Head;        // 몬스터 위치에서 그림 윗변 가운데까지
        public Vector3 Last;
        public bool Shown;
    }

    readonly List<Label> _labels = new List<Label>();     // 풀. 앞의 _used 개만 쓴다
    readonly List<MonsterController> _monsters = new List<MonsterController>();
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
        SpriteRenderer sr = mc.GetComponentInChildren<SpriteRenderer>();
        Vector3 head = sr != null
            ? new Vector3(sr.bounds.center.x, sr.bounds.max.y, sr.bounds.center.z)
            : mc.transform.position + Vector3.up * Define.TILE_SIZE;
        label.Head = head - mc.transform.position;
        label.Last = new Vector3(float.NaN, 0f, 0f);    // 다음 Follow 에서 반드시 옮긴다

        label.Text.color = ForecastUI.Tone(r, hp);
        label.Text.text = r.Ok == false ? "?"
            : r.Win == false ? "X"
            : (r.Damage > 0 ? "-" + r.Damage : "0") + (r.LevelUp ? " " + ForecastUI.LevelUpMark : "");
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

        float lift = Lift * _canvas.rootCanvas.scaleFactor;
        for (int i = 0; i < _used; i++)
        {
            Label label = _labels[i];
            MonsterController mc = label.Monster;
            bool on = mc != null && mc.gameObject.activeInHierarchy;
            Vector3 screen = Vector3.zero;
            if (on)
            {
                screen = cam.WorldToScreenPoint(mc.transform.position + label.Head);
                on = screen.z > 0f;
            }
            SetShown(label, on);
            if (on == false)
                continue;

            screen.y += lift;
            screen.z = 0f;
            if ((screen - label.Last).sqrMagnitude > 0.01f || float.IsNaN(label.Last.x))
            {
                label.Last = screen;
                label.Rect.position = screen;
            }
        }
    }

    static void SetShown(Label label, bool on)
    {
        if (label.Shown == on)
            return;
        label.Shown = on;
        label.Rect.gameObject.SetActive(on);
    }
}
