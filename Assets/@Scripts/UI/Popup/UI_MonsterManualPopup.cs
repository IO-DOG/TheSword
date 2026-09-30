using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 몬스터 도감 (M, 또는 HUD 의 마검 단추). 마검의 눈으로 이 층에 남은 몬스터를 읽는다 — 계약 뒤에만 열린다.
///
/// 종마다 한 줄: 그림·이름·남은 수·특성·능력치·경험치, 그리고 "다음에 싸우면" 잃을 체력과 맞는 횟수,
/// 임계(공격을 몇 올려야 한 대 덜 맞는가, 방어 +1 의 값). 싼 싸움부터 늘어놓는다. 줄을 고르면(W/S·마우스)
/// 아래 칸에 특성과 몬스터 설명이 나온다. 셈은 전부 BattleForecast 다 — 전투 시계는 하나뿐이다.
///
/// 값은 연 순간의 것이다. 떠 있는 동안에는 캐릭터가 움직이지 않으니(OnInputLock) 바뀔 일이 없다.
/// 프리팹 없이 세운다. 틀은 인벤토리 칸(Inventory_Popup32), 줄은 메뉴 단추(SystemUI_Button/Choice),
/// 능력치 칸은 HUD 의 것(MainUI_Status_*)을 빌려 와 다른 창과 같은 그림을 쓴다.
/// </summary>
public class UI_MonsterManualPopup : UI_Popup
{
    const float PanelWidth = 1440f;
    const float Pad = 28f;
    const float HeaderHeight = 112f;
    const float RowHeight = 96f;
    const float RowGap = 8f;
    const float DetailHeight = 150f;
    const float FooterHeight = 36f;
    const float ScreenHeight = 1080f;   // 캔버스는 높이를 1080 에 맞춘다(Init)
    const int AttackSearch = 30;        // 임계를 찾는 끝. 한 층에 오르는 공격력(레벨 2~5, 룬 1)의 몇 배다

    static readonly Color Gold = new Color32(240, 210, 138, 255);
    static readonly Color TraitGold = new Color32(232, 193, 112, 255);
    static readonly Color Soft = new Color32(174, 182, 200, 255);
    static readonly Color Ink = new Color32(236, 236, 242, 255);

    // 한 종 = 한 줄. 생성 층은 같은 종 셋에 번호를 따로 준다(1040·1041·1042) — 능력치·그림·이름으로 묶는다.
    class Entry
    {
        public Data.MonsterData Data;
        public int Id;
        public int Alive;
        public BattleForecast.Result Now;
        public BattleForecast.Result DefencePlus;
        public BattleForecast.Result AttackThen;
        public int AttackStep;
    }

    readonly List<Entry> _entries = new List<Entry>();
    readonly List<GameObject> _choices = new List<GameObject>();
    int _cursor;
    bool _lockedInput;

    TMP_FontAsset _number;
    TMP_FontAsset _prose;
    Material _outline;
    Image _detailIcon;
    TextMeshProUGUI _detailTrait;
    TextMeshProUGUI _detailTraitDesc;
    TextMeshProUGUI _detailName;
    TextMeshProUGUI _detailDesc;

    /// <summary>M 과 HUD 의 마검 단추. 떠 있으면 닫고, 아니면 열 수 있을 때만 연다.</summary>
    public static void Toggle()
    {
        UI_MonsterManualPopup open = Managers.UI.FindPopup<UI_MonsterManualPopup>();
        if (open != null)
        {
            open.Close();
            return;
        }
        if (Managers.Game.PlayerData.IsContractedSword == false || UI_GameScene.CanOpenPanel() == false)
            return;

        UI_MonsterManualPopup popup = new GameObject(nameof(UI_MonsterManualPopup), typeof(RectTransform))
            .AddComponent<UI_MonsterManualPopup>();
        Managers.UI.PushPopup(popup);
        popup.Init();
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        // 코드로 세운 창은 스케일러를 새로 받는다. 높이를 1080 에 맞춰 어느 화면비에서도 창이 세로로 들어가게 한다.
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920f, ScreenHeight);
        scaler.matchWidthOrHeight = 1f;
        // 스케일러는 켜질 때 한 번 재고 그 뒤로는 자기 Update 에서 잰다. 다시 켜서 첫 프레임부터 맞게 한다.
        scaler.enabled = false;
        scaler.enabled = true;

        // 떠 있는 동안 캐릭터를 붙잡는다. W/S 가 이동 키이기도 하다. 켠 쪽만 끈다.
        if (Managers.Game.OnInputLock == false)
        {
            Managers.Game.OnInputLock = true;
            _lockedInput = true;
        }

        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_SFX");
        _number = CodeUI.NumberFont;
        _prose = CodeUI.ProseFont;
        _outline = CodeUI.Outlined(_number, 0.2f);

        Collect();
        Build();
        Select(0, false);
        return true;
    }

    #region 셈
    void Collect()
    {
        GameManager g = Managers.Game;
        int stage = g.PlayerData.CurStageid;
        GameObject map;
        if (g.Maps.TryGetValue(stage, out map) == false || map == null)
            return;

        foreach (MonsterController mc in map.GetComponentsInChildren<MonsterController>(false))
        {
            Data.MonsterData md;
            if (ForecastUI.IsStanding(mc) == false || Managers.Data.MonsterDic.TryGetValue(mc.id, out md) == false)
                continue;
            Entry same = _entries.Find(e => SameSpecies(e.Data, md));
            if (same != null)
                same.Alive++;
            else
                _entries.Add(new Entry { Data = md, Id = mc.id, Alive = 1 });
        }

        foreach (Entry e in _entries)
        {
            e.Now = BattleForecast.Of(e.Id, stage);
            e.DefencePlus = BattleForecast.Of(e.Id, stage, 0, 1);
            e.AttackStep = BattleForecast.AttackStep(e.Id, stage, e.Now, AttackSearch, out e.AttackThen);
        }

        // 싼 싸움부터. 지는 싸움, 끝나지 않는 싸움, 잴 수 없는 상대가 뒤로 간다.
        _entries.Sort((a, b) =>
        {
            int rank = Rank(a.Now).CompareTo(Rank(b.Now));
            return rank != 0 ? rank : a.Now.Damage.CompareTo(b.Now.Damage);
        });
    }

    static int Rank(BattleForecast.Result r) => r.Ok == false ? 3 : r.Win ? 0 : r.Kills ? 1 : 2;

    static bool SameSpecies(Data.MonsterData a, Data.MonsterData b)
    {
        if (a.id == b.id)
            return true;
        return a.Ability == b.Ability && a.IdleAnimStr == b.IdleAnimStr
            && a.Attack == b.Attack && a.Defence == b.Defence && a.MaxHP == b.MaxHP
            && a.AttackSpeed == b.AttackSpeed && a.DefenceSpeed == b.DefenceSpeed
            && a.Critical == b.Critical && a.CriticalAttack == b.CriticalAttack && a.RewardExp == b.RewardExp
            && Managers.GetString(a.MonsterNameId) == Managers.GetString(b.MonsterNameId);
    }
    #endregion

    #region 그리기
    void Build()
    {
        Image dim = CodeUI.NewImage(transform, "Dim", null, new Color(0f, 0f, 0f, 0.6f));
        CodeUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        dim.gameObject.BindEvent(Close);     // 창 밖을 누르면 닫는다

        int n = _entries.Count;
        float fixedPart = Pad * 2f + HeaderHeight + FooterHeight + (n > 0 ? DetailHeight + 28f : 0f);
        // 줄이 많아 화면을 넘으면 줄을 낮춘다. 지금 데이터의 한 층은 많아야 다섯 종이다.
        float rowHeight = n > 0 ? Mathf.Min(RowHeight, (ScreenHeight - 40f - fixedPart - (n - 1) * RowGap) / n) : 0f;
        float rowsHeight = n > 0 ? n * rowHeight + (n - 1) * RowGap : 60f;
        float height = fixedPart + rowsHeight;

        Image panel = CodeUI.NewImage(transform, "Panel", Frame(), Frame() != null ? Color.white : new Color(0.07f, 0.08f, 0.12f, 0.97f), true);
        panel.raycastTarget = true;          // 창 안을 눌러도 닫히지 않게 가린다
        CodeUI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, height));
        // 좁은 화면(4:3)에서는 창째로 줄인다. 캔버스 높이는 늘 1080 이다.
        float canvasWidth = ScreenHeight * Screen.width / Mathf.Max(1, Screen.height);
        panel.rectTransform.localScale = Vector3.one * Mathf.Min(1f, (canvasWidth - 40f) / PanelWidth);
        Transform root = panel.transform;

        float y = -Pad;
        BuildHeader(root, y);
        y -= HeaderHeight;

        if (n == 0)
        {
            TextMeshProUGUI empty = CodeUI.NewText(root, "Empty", _prose, 30f, Soft, TextAlignmentOptions.Center);
            CodeUI.Place(empty.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(PanelWidth - Pad * 2f, rowsHeight));
            empty.text = Managers.GetString(ForecastUI.ManualEmpty);
        }
        for (int i = 0; i < n; i++)
            BuildRow(root, i, y - i * (rowHeight + RowGap), rowHeight);
        y -= rowsHeight;

        if (n > 0)
        {
            y -= 16f;
            BuildDetail(root, y);
            y -= DetailHeight + 12f;
        }

        TextMeshProUGUI footer = CodeUI.NewText(root, "Controls", _prose, 22f, Soft, TextAlignmentOptions.Center);
        CodeUI.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, Pad - 6f), new Vector2(PanelWidth - Pad * 2f, FooterHeight));
        CodeUI.Fit(footer, 14f).text = Managers.GetString(ForecastUI.Controls);
    }

    void BuildHeader(Transform root, float top)
    {
        Image eye = CodeUI.NewImage(root, "SwordEye", Managers.Resource.Load<Sprite>("MainUI_Sword_A"), Color.white);
        eye.preserveAspect = true;
        eye.enabled = eye.sprite != null;
        CodeUI.Place(eye.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Pad, top + 6f), new Vector2(84f, 84f));

        TextMeshProUGUI title = CodeUI.NewText(root, "Title", _prose, 44f, Gold);
        CodeUI.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Pad + 100f, top), new Vector2(520f, 52f));
        CodeUI.Fit(title, 24f).text = Managers.GetString(ForecastUI.ManualTitle);

        TextMeshProUGUI legend = CodeUI.NewText(root, "Legend", _prose, 22f, Soft);
        CodeUI.Place(legend.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Pad + 100f, top - 56f), new Vector2(800f, 30f));
        CodeUI.Fit(legend, 14f).text = Managers.GetString(ForecastUI.ManualLegend);

        GameManager g = Managers.Game;
        Data.StageInfoData info;
        TextMeshProUGUI floor = CodeUI.NewText(root, "Floor", _prose, 28f, Ink, TextAlignmentOptions.Right);
        CodeUI.Place(floor.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Pad, top - 4f), new Vector2(460f, 40f));
        CodeUI.Fit(floor, 16f).text = Managers.Data.StageInfoDic.TryGetValue(g.PlayerData.CurStageid, out info)
            ? Managers.GetString(info.DungeonNameScriptID) : "";

        // 값을 정하는 플레이어 쪽: 지금 체력(이기는가)과 이어 온 치명 횟수(언제 치명타가 나는가).
        int crit = ForecastUI.HitsToCrit();
        TextMeshProUGUI state = CodeUI.NewText(root, "State", _number, 24f, Ink, TextAlignmentOptions.Right);
        state.fontSharedMaterial = _outline;
        CodeUI.Place(state.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Pad, top - 50f), new Vector2(460f, 34f));
        CodeUI.Fit(state, 14f).text = $"{Managers.GetString(ForecastUI.Hp)} {Mathf.RoundToInt(g.PlayerData.CurHP)}/{Mathf.RoundToInt(g.PlayerData.MaxHP)}"
            + (crit > 0 ? "   " + string.Format(Managers.GetString(ForecastUI.CritIn), crit) : "");

        Sprite lineSprite = CodeUI.PrefabSprite("UI_SettingPopup", "SystemUI_Setting_ClassLine");
        Image line = CodeUI.NewImage(root, "Line", lineSprite, lineSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.2f));
        CodeUI.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, top - HeaderHeight + 12f), new Vector2(PanelWidth - Pad * 2f, 2f));
    }

    void BuildRow(Transform root, int index, float top, float height)
    {
        Entry e = _entries[index];
        float width = PanelWidth - Pad * 2f;

        Sprite art = RowArt("SystemUI_Button");
        Image bg = CodeUI.NewImage(root, $"Row{index}", art, art != null ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.1f, 0.12f, 0.18f, 0.9f), true);
        bg.raycastTarget = true;
        CodeUI.Place(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, top), new Vector2(width, height));
        Transform row = bg.transform;
        bg.gameObject.BindEvent(() => Select(index, true), type: Define.UIEvent.PointerEnter);
        bg.gameObject.BindEvent(() => Select(index, true));

        Sprite pick = RowArt("SystemUI_Choice");
        Image choice = CodeUI.NewImage(row, "Choice", pick, pick != null ? Color.white : new Color(1f, 1f, 1f, 0.12f), true);
        CodeUI.Stretch(choice.rectTransform);
        _choices.Add(choice.gameObject);

        // 칸 배치: 양 끝(48)은 단추 모서리다. 그림 52~132 · 이름/특성 144~450 · 능력치 462~886 · 값 904~1124 · 임계 1136~.
        // 그림: 맵과 전투창이 쓰는 대기 애니메이션과 색 그대로 (UI_MonsterCard 와 같다).
        Image look = CodeUI.NewImage(row, "Monster", null, MonsterTint.Of(e.Id));
        look.preserveAspect = true;
        CodeUI.Place(look.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(52f, 0f), new Vector2(80f, 80f));
        Animate(look, e.Data.IdleAnimStr);

        string count = e.Alive > 1 ? $" <size=70%><color=#9AA4B8>{string.Format(Managers.GetString(ForecastUI.Count), e.Alive)}</color></size>" : "";
        TextMeshProUGUI name = CodeUI.NewText(row, "Name", _prose, 30f, Ink);
        CodeUI.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(144f, 18f), new Vector2(306f, 38f));
        CodeUI.Fit(name, 16f).text = Managers.GetString(e.Data.MonsterNameId) + count;

        Data.MonsterClassData trait;
        Managers.Data.MonsterClassDic.TryGetValue(e.Data.Ability, out trait);
        Image traitIcon = CodeUI.NewImage(row, "TraitIcon", trait != null ? Managers.Resource.Load<Sprite>(trait.AbilityImage) : null, Color.white);
        traitIcon.enabled = traitIcon.sprite != null;
        CodeUI.Place(traitIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(144f, -20f), new Vector2(34f, 34f));
        TextMeshProUGUI traitName = CodeUI.NewText(row, "Trait", _prose, 24f, TraitGold);
        CodeUI.Place(traitName.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(186f, -20f), new Vector2(264f, 32f));
        CodeUI.Fit(traitName, 14f).text = trait != null ? Managers.GetString(trait.ClassName) : "";

        // 능력치: HUD 와 같은 칸에 같은 값 (공격·방어는 층 배수를 곱한 것 — UI_MonsterInfo 와 같다).
        Data.StageInfoData stage = Managers.Data.StageInfoDic[Managers.Game.PlayerData.CurStageid];
        StatBox(row, "MainUI_Status_ATK", 462f, stage.ATK * e.Data.Attack);
        StatBox(row, "MainUI_Status_DEF", 606f, stage.DEF * e.Data.Defence);
        StatBox(row, "MainUI_Status_HP", 750f, e.Data.MaxHP);

        TextMeshProUGUI exp = CodeUI.NewText(row, "Exp", _prose, 24f, Soft);
        CodeUI.Place(exp.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(462f, -22f), new Vector2(424f, 32f));
        CodeUI.Fit(exp, 14f).text = string.Format(Managers.GetString(ForecastUI.Exp), e.Now.Exp)
            + (e.Now.LevelUp ? "  " + ForecastUI.LevelUpMark : "");

        // 다음에 싸우면: 잃을 체력(색은 지금 체력에 대한 몫)과 맞는 횟수.
        TextMeshProUGUI next = CodeUI.NewText(row, "NextLabel", _prose, 22f, Soft);
        CodeUI.Place(next.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(904f, 24f), new Vector2(220f, 28f));
        CodeUI.Fit(next, 14f).text = Managers.GetString(ForecastUI.NextFight);

        TextMeshProUGUI cost = CodeUI.NewText(row, "Cost", _number, 40f, ForecastUI.Tone(e.Now, Managers.Game.PlayerData.CurHP));
        cost.fontSharedMaterial = _outline;
        CodeUI.Place(cost.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(904f, -14f), new Vector2(220f, 48f));
        CodeUI.Fit(cost, 18f).text = CostText(e.Now);

        // 임계: 공격을 몇 올리면 한 대 덜 맞는가, 방어 +1 은 얼마인가.
        TextMeshProUGUI step = CodeUI.NewText(row, "Breakpoint", _prose, 24f, Ink);
        CodeUI.Place(step.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1136f, 0f), new Vector2(width - 1190f, height - 16f));
        step.alignment = TextAlignmentOptions.Left;
        string attack = AttackHint(e), defence = DefenceHint(e);
        CodeUI.Fit(step, 14f).text = attack.Length > 0 && defence.Length > 0 ? attack + "\n" + defence : attack + defence;
    }

    void StatBox(Transform row, string sprite, float x, float value)
    {
        Image box = CodeUI.NewImage(row, sprite, CodeUI.PrefabSprite("UI_GameScene", sprite), Color.white);
        CodeUI.Place(box.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 20f), new Vector2(136f, 38f));
        if (box.sprite == null)
            box.color = new Color(0f, 0f, 0f, 0.4f);

        // HUD 의 숫자처럼 칸 왼쪽의 그림을 비켜 가운데에 쓴다.
        TextMeshProUGUI text = CodeUI.NewText(box.transform, "Value", _number, 24f, Ink, TextAlignmentOptions.Center);
        CodeUI.Stretch(text.rectTransform).offsetMin = new Vector2(34f, 0f);
        CodeUI.Fit(text, 12f).text = Mathf.RoundToInt(value).ToString();
    }

    void BuildDetail(Transform root, float top)
    {
        Image box = CodeUI.NewImage(root, "Detail", Frame(), Frame() != null ? new Color(0.8f, 0.8f, 0.85f, 1f) : new Color(0f, 0f, 0f, 0.35f), true);
        CodeUI.Place(box.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, top), new Vector2(PanelWidth - Pad * 2f, DetailHeight));
        Transform t = box.transform;
        float half = (PanelWidth - Pad * 2f) / 2f;

        _detailIcon = CodeUI.NewImage(t, "TraitIcon", null, Color.white);
        CodeUI.Place(_detailIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -16f), new Vector2(34f, 34f));
        _detailTrait = CodeUI.NewText(t, "Trait", _prose, 28f, TraitGold);
        CodeUI.Place(_detailTrait.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -14f), new Vector2(half - 90f, 36f));
        CodeUI.Fit(_detailTrait, 16f);
        _detailTraitDesc = Paragraph(t, "TraitDesc", new Vector2(22f, -54f), half - 44f);

        _detailName = CodeUI.NewText(t, "Name", _prose, 28f, Ink);
        CodeUI.Place(_detailName.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(half + 10f, -14f), new Vector2(half - 32f, 36f));
        CodeUI.Fit(_detailName, 16f);
        _detailDesc = Paragraph(t, "Desc", new Vector2(half + 10f, -54f), half - 32f);
    }

    TextMeshProUGUI Paragraph(Transform parent, string name, Vector2 pos, float width)
    {
        TextMeshProUGUI text = CodeUI.NewText(parent, name, _prose, 22f, Soft);
        CodeUI.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), pos, new Vector2(width, DetailHeight - 66f));
        text.textWrappingMode = TextWrappingModes.Normal;
        text.alignment = TextAlignmentOptions.TopLeft;
        return CodeUI.Fit(text, 14f);
    }

    static void Animate(Image look, string state)
    {
        Animator anim = look.gameObject.AddComponent<Animator>();
        anim.runtimeAnimatorController = Managers.Resource.Load<RuntimeAnimatorController>("UIMonsterAnimController");
        anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        if (anim.runtimeAnimatorController != null && string.IsNullOrEmpty(state) == false)
        {
            anim.Play(state);
            anim.Update(0f);    // Play 만으로는 이 프레임에 그림이 들어오지 않는다 (UI_MonsterCard 와 같다)
        }
        look.enabled = look.sprite != null;   // 없는 상태면 흰 네모 대신 빈칸
    }

    static string CostText(BattleForecast.Result r)
    {
        if (r.Ok == false)
            return "?";
        if (r.Win)
            return (r.Damage > 0 ? "-" + r.Damage : "0")
                + $" <size=55%>{string.Format(Managers.GetString(ForecastUI.HitsTaken), r.HitsTaken)}</size>";
        return r.Kills
            ? $"X <size=50%>{Managers.GetString(ForecastUI.Falls)} -{r.Damage}</size>"
            : $"X <size=50%>{Managers.GetString(ForecastUI.CannotWin)}</size>";
    }

    static string AttackHint(Entry e)
    {
        if (e.AttackStep <= 0)
            return "";
        if (e.Now.Win == false)
            return string.Format(Managers.GetString(ForecastUI.AtkWin), e.AttackStep);
        return e.AttackThen.HitsTaken < e.Now.HitsTaken
            ? string.Format(Managers.GetString(ForecastUI.AtkOneHit), e.AttackStep)
            : string.Format(Managers.GetString(ForecastUI.AtkLess), e.AttackStep, e.Now.Damage - e.AttackThen.Damage);
    }

    static string DefenceHint(Entry e)
    {
        if (e.Now.Ok == false || e.Now.Kills == false)
            return "";
        if (e.Now.Win == false && e.DefencePlus.Win)
            return Managers.GetString(ForecastUI.DefWin);
        return string.Format(Managers.GetString(ForecastUI.DefLess), Mathf.Max(0, e.Now.Damage - e.DefencePlus.Damage));
    }

    // 틀: 인벤토리의 능력치 칸 그림(9분할). 두 배 픽셀로 그린다.
    static Sprite Frame() => CodeUI.PrefabSprite("UI_InvenPopup", "Inventory_Popup32");

    // 줄: 메뉴 단추 그림(240x48). 양 끝의 뾰족한 모서리(비스듬한 선이 22px 까지 온다)만 남기고 가운데를 늘린다.
    static Sprite RowArt(string sprite) => CodeUI.Sliced(CodeUI.PrefabSprite("UI_MenuPopup", sprite), new Vector4(24f, 0f, 24f, 0f));
    #endregion

    #region 고르기
    void Select(int index, bool sound)
    {
        if (_entries.Count == 0)
            return;
        index = Mathf.Clamp(index, 0, _entries.Count - 1);
        if (sound && index != _cursor)
            Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_SFX");
        _cursor = index;
        for (int i = 0; i < _choices.Count; i++)
            _choices[i].SetActive(i == index);

        Entry e = _entries[index];
        Data.MonsterClassData trait;
        bool known = Managers.Data.MonsterClassDic.TryGetValue(e.Data.Ability, out trait);
        _detailIcon.sprite = known ? Managers.Resource.Load<Sprite>(trait.AbilityImage) : null;
        _detailIcon.enabled = _detailIcon.sprite != null;
        _detailTrait.text = known ? Managers.GetString(trait.ClassName) : "";
        _detailTraitDesc.text = known ? Managers.GetString(trait.ClassDesc) : "";
        _detailName.text = Managers.GetString(e.Data.MonsterNameId);
        _detailDesc.text = Managers.GetString(e.Data.MonsterDescId);
    }

    void Update()
    {
        if (Managers.UI.TopPopup != this || Managers.UI.ClosedThisFrame)
            return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            Select(_cursor - 1, true);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            Select(_cursor + 1, true);
    }

    // Esc 는 씬 UI 가 여기로 넘겨준다. M 은 UI_GameScene 이 Toggle 로 닫는다.
    public override bool OnEscape()
    {
        Close();
        return true;
    }

    void Close()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Back_SFX");
        ClosePopupUI();
    }

    void OnDestroy()
    {
        if (_lockedInput && Managers.IsAlive)
            Managers.Game.OnInputLock = false;
    }
    #endregion
}
