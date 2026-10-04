using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UI_BattlePopup : UI_Popup
{
    #region Enum
    enum Images
    {
        BGImage,
    }

    enum Objects
    {

    }

    #endregion

    // 스킬 이름 (Tools/ui_text_parts/ui.py). BattleSkills.Kind 순서 — 강타·철벽·흡혈.
    public static readonly int[] SkillTextIds = { 191, 192, 193 };
    // 배속·건너뛰기 문구 (Tools/ui_text_parts/battle.py)
    const int HoldHintText = 352, SkipText = 353, PaidText = 354, ForetoldText = 355, SkipToldText = 356;

    // 카드 배율. 카드는 이 창 캔버스의 자식이라 캔버스가 이미 화면 높이(1080 기준)만큼 늘고 준다. 예전에는 여기서
    // 화면 폭·높이를 한 번 더 곱해서(창 모드 8, 전체 화면 4.5) 두 번 커졌다 — 1280x720 창에서는 스킬 막대가 카드
    // 이름을 덮었고, 그보다 큰 창·1440p 이상 전체 화면에서는 카드가 화면을 넘쳐 이름·능력치가 잘렸다. 가로세로를
    // 따로 곱해서 4:3·16:10 에서는 그림이 찌그러졌다. 배율은 하나다 (1080p 전체 화면에서 보던 크기).
    const float CardScale = 4.5f;

    const int HoldSpeed = 8;                    // Space 를 누르고 있는 동안의 배속. Shift 는 안 쓴다 — Shift+Tab(건너뛰기)이 Steam 오버레이다
    // 건너뛰기가 끝나지 않는 싸움(둘 다 상대를 못 깎는다)을 붙잡고 영영 돌지 않게 하는 마개. 예측(BattleForecast)과 같은 600초.
    const float MaxSkipSeconds = 600f;
    const float SummarySeconds = 0.5f;          // 건너뛴 싸움의 값을 보여 주고 창이 닫히기까지
    const float HitStopSeconds = 0.07f;         // 치명타 한 대에 전투 시계를 세우는 실시간
    // 치명타 소리(대역): 평타 소리를 낮게 — 무거운 한 대. 치명타에는 카드가 평타 소리를 내지 않는다(UI_PlayerCard.PresentAttack).
    // 효과음 소스가 하나라 높이는 나중에 튼 소리를 따른다 — 평타에 얹으면 같은 높이로 두 번 날 뿐이라 바꿔 낸다.
    // 이름으로 고를 것이 없었다: MainTitle_Impact 는 0.4초에 가장 커지는 2.2초짜리 부풂이라 히트스톱(0.07초)이 끝난 뒤에 울렸고,
    // HeroReady_SFX 는 레벨 업, Defense_SFX 는 막았다는 소리다. 전용 소리(기획 §8 "crit")가 오면 이 두 줄만 바꾼다(높이 1).
    const string CritSound = "HeroAttack0_SFX";
    const float CritPitch = 0.8f;
    const string SkipToldPref = "SKIP_TOLD";    // 건너뛰기를 알렸다 (처음 한 번만)

    BattleStepper _battle;
    bool _ending;
    int _monsterId;
    bool _boss;     // 우두머리(챕터 보스·킹 슬라임)와의 전투인가
    int _stage;
    UI_PlayerCard playerCard = null;
    UI_MonsterCard monsterCard = null;

    bool _skipAllowed;      // 건너뛸 수 있는 싸움이다 — 보스가 아니고 치명 수업을 봤다. 그때그때의 조건은 CanSkip 이 본다
    bool _skipping;         // 끝까지 돌리는 중(돌린 뒤) — 한 대마다의 연출을 끈다
    bool _struck;           // 플레이어가 한 번이라도 때렸다 (LastBattle.FirstHitCrit)
    float _hitStopUntil;    // 이 실시간까지 전투 시계를 세운다 (치명타)
    GameObject _skipButton;

    /// <summary>승부가 났다. 닫히기까지 0.3초가 남아 있어도 이제 아무것도 바꾸지 못한다 (스킬 포함).</summary>
    public bool BattleOver => _ending || _battle == null || _battle.Finished;

    /// <summary>
    /// 지금 전투 시계가 FixedUpdate 한 번에 몇 걸음 도는가. 걸음 수만 늘리므로 결과는 같다 — 예측(BattleForecast)도 같은 시계다.
    ///   봇이 돌면                   봇의 값 (AutoPlayer 가 GameManager.GameSpeed 에 8·16 을 넣는다) — 녹화·검증은 예전 그대로
    ///   Space 를 누르는 동안        8
    ///   그 밖에는                   설정(GameSettings.BattleSpeed 1·2·4) — 1배를 고르면 1배다(목걸이는 이제 배속을 주지 않는다)
    /// 전투 소리의 높이(SoundManager)도 이것을 따른다.
    /// </summary>
    public static int Speed
    {
        get
        {
            if (GameEvents.IsAutoPlaying)
                return Mathf.Max(1, Managers.Game.GameSpeed);
            if (Input.GetKey(KeyCode.Space))
                return HoldSpeed;
            return GameSettings.BattleSpeed;
        }
    }

    /// <summary>건너뛸 수 있는 싸움인가 — 보스가 아니고 치명 수업을 봤다. 봇(AutoPlayer.ForceSkipAll)이 스킬 대신 건너뛸지 본다.</summary>
    public bool SkipAllowed => _skipAllowed;

    /// <summary>지금 건너뛸 수 있는가. 건너뛸 수 있는 싸움이고, 아직 승부가 안 났고, 두 카드가 섰고,
    /// 이야기(치명 수업·장면)나 메뉴가 전투를 붙들고 있지 않을 때.</summary>
    public bool CanSkip => _skipAllowed && _skipping == false && BattleOver == false && Managers.Game.OnBattle
        && playerCard != null && monsterCard != null && playerCard.Ready && monsterCard.Ready
        && StoryDirector.HoldsBattle == false && StoryDirector.IsPlaying == false && Managers.UI.IsPaused == false;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindObject(typeof(Objects));
        #endregion

        GetImage((int)Images.BGImage).sprite = Managers.Game._screenShot2;

        Managers.Sound.Play(Define.Sound.Effect, "BattleStart_SFX");

        playerCard = Managers.UI.SetBattleCard<UI_PlayerCard>(gameObject.transform, Managers.Game.PlayerData);

        float width = Screen.width;
        float height = Screen.height;
        playerCard.transform.position = new Vector3(width * 0.3f, height * 0.6f, 0);
        playerCard.transform.localScale = new Vector3(CardScale, CardScale, 1);
        //playerCard.Data = Managers.Game.Player.Data;

        for (int i = 0; i < Managers.Game.MonsterData.Count; i++)
        {
            monsterCard = Managers.UI.SetBattleCard<UI_MonsterCard>(gameObject.transform, Managers.Game.MonsterData[i]);

            monsterCard.transform.position = new Vector3(width * 0.7f, height * 0.6f, 0);
            monsterCard.transform.localScale = new Vector3(CardScale, CardScale, 1);
        }

        //monsterCard.Data = Managers.Game.MonsterData;

        Managers.Game.OnBattle = true;
        Managers.Game.OnBattleAction -= BattleEnd;
        Managers.Game.OnBattleAction += BattleEnd;

        // 스킬은 전투마다 셋 다 새로 채운다.
        BattleSkills.ResetForBattle();
        _monsterId = Managers.Game.MonsterData[0].id;
        // 챕터 보스(생성 층의 "Boss" 태그)와 킹 슬라임. 분열 슬라임은 보스 연출은 쓰지만 우두머리가 아니다.
        MonsterController fought = Managers.Game.Monster;
        _boss = fought != null && (fought.CompareTag("Boss") || fought is KingSlimeController);
        _stage = Managers.Game.PlayerData.CurStageid;
        // 건너뛰기는 치명 수업(3~4층)을 본 뒤부터 — 그 전에는 한 번에 끝나는 싸움이 수업을 지나쳐 버린다(StoryDirector.WatchCrit).
        // 연출이 달린 싸움(챕터 보스·킹 슬라임·분열 슬라임)은 언제나 끝까지 본다.
        _skipAllowed = fought != null && _boss == false && fought.GetComponent<BossMonsterController>() == null
                       && StoryDirector.MechanicSeen(StoryMechanic.Crit);
        RecordStart();
        _battle = new BattleStepper(Managers.Game.PlayerData, Managers.Game.MonsterData[0],
            Managers.Game.AttackCount, Managers.Game.DefenceCoolTime);
        _battle.OnStrike = OnStrike;
        _battle.OnGuard = player =>
        {
            if (_skipping) return;
            if (player) playerCard.Defence(); else monsterCard.Defence();
        };

        BuildSkillBar();
        BuildSpeedHint();
        return true;
    }

    // 전투 요약(LastBattle)을 새로 적기 시작한다. 예측은 전투 시계를 세우기 전에 — 관문(FightGate)에서 잰 것과 같은 상태로 잰다.
    void RecordStart()
    {
        GameManager.CurPlayerData p = Managers.Game.PlayerData;
        BattleForecast.Result forecast = BattleForecast.Of(_monsterId, _stage);
        LastBattle.Clear();
        LastBattle.MonsterId = _monsterId;
        LastBattle.Boss = _boss;
        LastBattle.Foretold = forecast.Ok ? forecast.Damage : -1;
        LastBattle.HpBefore = p.CurHP;
        LastBattle.MaxHp = p.MaxHP;
    }

    void OnStrike(bool player, int damage, bool critical, bool guarded)
    {
        if (player && _struck == false)
        {
            _struck = true;
            LastBattle.FirstHitCrit = critical;
        }
        if (_skipping)
            return;     // 끝까지 돌리는 중 — 숫자·이펙트·소리를 한 대마다 내지 않는다
        if (player && critical)
            Crit();
        UI_BaseCard card = player ? (UI_BaseCard)playerCard : monsterCard;
        card.PresentAttack(player ? _battle.Player : _battle.Monster,
            player ? _battle.Monster : _battle.Player, damage, critical, guarded);
    }

    // 치명타에 무게를 싣는다: 전투 시계를 잠깐 세우고(히트스톱 — 걸음을 미룰 뿐이라 결과는 같다. Time.timeScale 은 안 건드린다),
    // 맞은 카드를 흔들고, 평타 대신 무거운 소리를 낸다. 숫자 1.6배는 DamageFont 가 한다.
    // 플레이어의 치명타만이다 — 마법 특성은 모든 공격이 치명이라(MagicTrait) 몬스터 쪽에 걸면 매 대마다 멈추고 흔들린다.
    // 봇이 돌 때는 세우지 않는다 — 녹화·검증 시간이 예전과 같아야 한다.
    void Crit()
    {
        if (GameEvents.IsAutoPlaying == false)
            _hitStopUntil = Time.unscaledTime + HitStopSeconds;
        monsterCard.Shake();
        Managers.Sound.PlayByGameSpeed(Define.Sound.Effect, CritSound, CritPitch);
    }

    #region 스킬 막대
    // 1/2/3 이 무엇인지 화면에 없어서 계약 대사로만 알 수 있었다. 전투창 아래 가운데에 셋을 늘어놓고,
    // 쓴 것(전투마다 한 번)은 흐리게 한다. 프리팹을 고치지 않고 메뉴 단추 그림·HUD 글꼴을 빌린다(CodeUI).
    const float SkillWidth = 230f, SkillHeight = 56f, SkillGap = 16f, SkillBottom = 36f;

    CanvasGroup[] _skillSlots;
    readonly bool[] _skillPainted = new bool[BattleSkills.Count];   // 칠해 둔 "썼다"

    void BuildSkillBar()
    {
        if (BattleSkills.Unlocked == false)
            return;

        TMP_FontAsset font = CodeUI.NumberFont;
        Material outline = CodeUI.Outlined(font, 0.2f);
        Sprite art = SlotArt();
        float width = SkillBarWidth;
        RectTransform bar = CodeUI.Place(CodeUI.NewRect(transform, "SkillBar"),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, SkillBottom), new Vector2(width, SkillHeight));

        _skillSlots = new CanvasGroup[BattleSkills.Count];
        for (int i = 0; i < BattleSkills.Count; i++)
        {
            Image slot = CodeUI.NewImage(bar, $"Skill{i + 1}", art, art != null ? Color.white : new Color(0.1f, 0.12f, 0.18f, 0.9f), true);
            CodeUI.Place(slot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * (SkillWidth + SkillGap), 0f),
                new Vector2(SkillWidth, SkillHeight));

            TextMeshProUGUI label = CodeUI.NewText(slot.transform, "Label", font, 26f, Color.white, TextAlignmentOptions.Center);
            label.fontSharedMaterial = outline;
            CodeUI.Stretch(label.rectTransform).offsetMin = new Vector2(20f, 0f);
            label.rectTransform.offsetMax = new Vector2(-20f, 0f);
            CodeUI.Fit(label, 14f).text = $"<color=#F0D28A>{i + 1}</color>  {Managers.GetString(SkillTextIds[i])}";

            _skillSlots[i] = slot.gameObject.AddComponent<CanvasGroup>();
            _skillPainted[i] = !BattleSkills.IsUsed(i);     // 처음 한 번은 반드시 칠한다
        }
        PaintSkills();
    }

    static float SkillBarWidth => SkillWidth * BattleSkills.Count + SkillGap * (BattleSkills.Count - 1);

    static Sprite SlotArt() => CodeUI.Sliced(CodeUI.PrefabSprite("UI_MenuPopup", "SystemUI_Button"), new Vector4(24f, 0f, 24f, 0f));

    // 누가 썼든(사람 키·봇의 BattleSkills.Use) 여기서 본다. 셋뿐이라 매 프레임 견줘도 싸다.
    void PaintSkills()
    {
        if (_skillSlots == null)
            return;
        for (int i = 0; i < _skillSlots.Length; i++)
        {
            bool used = BattleSkills.IsUsed(i);
            if (used == _skillPainted[i])
                continue;
            _skillPainted[i] = used;
            _skillSlots[i].alpha = used ? 0.35f : 1f;
        }
    }
    #endregion

    #region 배속·건너뛰기 안내
    // 스킬 막대와 같은 줄에 둔다 — 막대 바로 위는 카드 이름 줄이다. 왼쪽에 "Space 길게: 8배속" 을 늘 띄우고, 오른쪽에는
    // 건너뛸 수 있는 동안만 누를 수 있는 "Tab: 건너뛰기" 단추를 세운다(Update 가 CanSkip 으로 켜고 끈다). 스킬 막대와 같은 틀·글꼴.
    const float HintWidth = 360f;
    static readonly Color Soft = new Color32(200, 206, 220, 255);
    static readonly Color Gold = new Color32(240, 210, 138, 255);

    void BuildSpeedHint()
    {
        TMP_FontAsset font = CodeUI.NumberFont;
        Material outline = CodeUI.Outlined(font, 0.2f);

        // 막대가 없으면(계약 전) 그 자리 가운데.
        bool bar = _skillSlots != null;
        TextMeshProUGUI hold = CodeUI.NewText(transform, "SpeedHint", font, 22f, Soft,
            bar ? TextAlignmentOptions.Right : TextAlignmentOptions.Center);
        hold.fontSharedMaterial = outline;
        CodeUI.Place(hold.rectTransform, new Vector2(0.5f, 0f), new Vector2(bar ? 1f : 0.5f, 0f),
            new Vector2(bar ? -(SkillBarWidth / 2f + SkillGap) : 0f, SkillBottom), new Vector2(HintWidth, SkillHeight));
        CodeUI.Fit(hold, 12f).text = Managers.GetString(HoldHintText);

        if (_skipAllowed == false)
            return;

        Sprite art = SlotArt();
        Image button = CodeUI.NewImage(transform, "SkipButton", art, art != null ? Color.white : new Color(0.1f, 0.12f, 0.18f, 0.9f), true);
        button.raycastTarget = true;
        CodeUI.Place(button.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 0f),
            new Vector2(SkillBarWidth / 2f + SkillGap, SkillBottom), new Vector2(SkillWidth, SkillHeight));
        TextMeshProUGUI label = CodeUI.NewText(button.transform, "Label", font, 26f, Color.white, TextAlignmentOptions.Center);
        label.fontSharedMaterial = outline;
        CodeUI.Stretch(label.rectTransform).offsetMin = new Vector2(20f, 0f);
        label.rectTransform.offsetMax = new Vector2(-20f, 0f);
        CodeUI.Fit(label, 14f).text = Managers.GetString(SkipText);
        button.gameObject.BindEvent(Skip);
        _skipButton = button.gameObject;
        _skipButton.SetActive(false);

        // 처음 건너뛸 수 있게 된 싸움에서 한 번, 화면 위쪽 띠로 알린다 — 결과는 예측 그대로다. 막지 않고 5초 뒤 사라진다.
        // 봇은 이 한 번을 쓰지 않는다(사람이 볼 몫). 띠는 이 창의 자식이라 창과 함께 사라진다 — 알렸다고 적는 것은 띠가 4초를
        // 다 채워 옅어지기 시작할 때나 건너뛰기를 직접 써 봤을 때(Skip)다. 띄울 때 적으면 짧은 싸움·곧바로 건너뛴 싸움에서
        // 한 줄뿐인 설명을 거의 못 본 채 영영 잃었다.
        if (GameEvents.IsAutoPlaying || PlayerPrefs.GetInt(SkipToldPref, 0) != 0)
            return;
        Image band = CodeUI.NewImage(transform, "SkipTold", null, new Color(0f, 0f, 0f, 0.6f));
        CodeUI.Place(band.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1200f, 56f));
        TextMeshProUGUI told = CodeUI.NewText(band.transform, "Text", font, 26f, Gold, TextAlignmentOptions.Center);
        told.fontSharedMaterial = outline;
        CodeUI.Stretch(told.rectTransform);
        CodeUI.Fit(told, 14f).text = Managers.GetString(SkipToldText);
        // OnStart 는 지연(4초)이 끝난 뒤에 불린다. 그 전에 창이 닫히면 SetLink 가 트윈을 죽여 다음 싸움에서 다시 띄운다.
        band.gameObject.AddComponent<CanvasGroup>().DOFade(0f, 1f).SetDelay(4f).SetLink(band.gameObject)
            .OnStart(() => PlayerPrefs.SetInt(SkipToldPref, 1));
    }

    // 건너뛴 싸움의 값: 크게 "체력 -31", 밑에 예측. 창이 닫힐 때(SummarySeconds 뒤) 같이 사라진다.
    void ShowSummary()
    {
        GameManager.CurPlayerData p = Managers.Game.PlayerData;
        // 레벨이 오르면 최대 체력과 함께 지금 체력도 는다(GameManager.LevelUp) — 그 몫은 싸움에서 잃은 것이 아니다.
        // 봇의 예측 대조(AutoPlayer.CompareForecast)와 같은 셈이다.
        int paid = Mathf.Max(0, Mathf.RoundToInt(LastBattle.HpBefore - p.CurHP + (p.MaxHP - LastBattle.MaxHp)));
        TMP_FontAsset font = CodeUI.NumberFont;

        RectTransform box = CodeUI.Place(CodeUI.NewRect(transform, "SkipSummary"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(640f, 200f));
        CodeUI.Stretch(CodeUI.NewImage(box, "Band", null, new Color(0f, 0f, 0f, 0.6f)).rectTransform);

        TextMeshProUGUI big = CodeUI.NewText(box, "Paid", font, 96f, Util.HexToColor("FF3E3E"), TextAlignmentOptions.Center);
        big.fontSharedMaterial = CodeUI.Outlined(font, 0.3f);
        CodeUI.Place(big.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(640f, 112f));
        CodeUI.Fit(big, 48f).text = string.Format(Managers.GetString(PaidText), paid);

        if (LastBattle.Foretold >= 0)
        {
            TextMeshProUGUI small = CodeUI.NewText(box, "Foretold", font, 40f, Soft, TextAlignmentOptions.Center);
            small.fontSharedMaterial = CodeUI.Outlined(font, 0.2f);
            CodeUI.Place(small.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(640f, 52f));
            CodeUI.Fit(small, 20f).text = string.Format(Managers.GetString(ForetoldText), LastBattle.Foretold);
        }

        box.localScale = Vector3.one * 0.7f;
        box.DOScale(1f, 0.12f).SetEase(Ease.OutBack).SetLink(box.gameObject);
    }
    #endregion

    /// <summary>전투 중 액티브 스킬 입력. 1 강타 / 2 철벽 / 3 흡혈. Tab 은 건너뛰기.
    ///
    /// 전투는 자동으로 굴러가므로 스킬은 "언제 끼어드느냐" 가 전부다.
    /// 셋 다 전투당 한 번뿐이라, 아낄지 지금 쓸지가 유일한 판단거리가 된다.</summary>
    void Update()
    {
        PaintSkills();
        if (_skipButton != null)
            _skipButton.SetActive(CanSkip);
        // 치명타 수업(StoryDirector)이 전투를 세워 두고 말하는 동안은 키도 받지 않는다 — 시계는 멈췄는데 스킬만 들어가면 안 된다.
        if (StoryDirector.HoldsBattle)
            return;
        if (Input.GetKeyDown(KeyCode.Tab) && CanSkip)
        {
            Skip();
            return;
        }
        if (BattleSkills.Unlocked == false || Managers.Game.OnBattle == false || Managers.UI.IsPaused)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            BattleSkills.Use((int)BattleSkills.Kind.Smash, playerCard, monsterCard);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            BattleSkills.Use((int)BattleSkills.Kind.Guard, playerCard, monsterCard);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            BattleSkills.Use((int)BattleSkills.Kind.Drain, playerCard, monsterCard);
    }

    public void RaisePlayerGuard() => _battle?.Guard(true);

    void FixedUpdate()
    {
        // 치명타 수업(StoryDirector)이 말하는 동안 전투 시계를 세운다. 게이지·치명 횟수가 그 자리에 머문다.
        if (StoryDirector.HoldsBattle) return;
        if (_battle == null || _ending || !Managers.Game.OnBattle) return;
        // 치명타 히트스톱. 그동안 걸음을 미룰 뿐 — 미룬 걸음은 뒤에 그대로 돈다.
        if (Time.unscaledTime < _hitStopUntil) return;
        int steps = Speed;
        for (int i = 0; i < steps && !_battle.Finished && Time.unscaledTime >= _hitStopUntil; i++)
            _battle.Step(Time.fixedDeltaTime);
        CarryState();
    }

    // 치명 횟수·방어 게이지는 전투 사이에 이어진다(예측도 그 값으로 잰다). 걸음을 돌린 뒤마다 옮겨 적고 게이지를 칠한다.
    void CarryState()
    {
        Managers.Game.AttackCount = _battle.PlayerHits;
        Managers.Game.DefenceCoolTime = _battle.PlayerDefenceTime;
        playerCard.ShowCombatGauges(_battle.PlayerAttackTime, _battle.PlayerAttackPeriod,
            _battle.PlayerDefenceTime, _battle.PlayerDefencePeriod);
        monsterCard.ShowCombatGauges(_battle.MonsterAttackTime, _battle.MonsterAttackPeriod,
            _battle.MonsterDefenceTime, _battle.MonsterDefencePeriod);
    }

    /// <summary>
    /// 이 싸움을 지금 끝까지 돌린다 (Tab·단추·봇). 같은 전투 시계(BattleStepper)를 같은 걸음(Time.fixedDeltaTime)으로 한 프레임에
    /// 끝까지 — 한 대마다의 연출만 끄고, 쓰러짐은 평소 길을 그대로 탄다: UI_MonsterCard.Dead(경험치·레벨·MonsterActiveDic·보상·
    /// MarkDead)·UI_PlayerCard.Dead(게임오버)가 OnDeadAction 으로 불리고 BattleEnd → CoBattleEnd 가 창을 닫는다. 그래서 끝난 상태
    /// (HP·경험치·레벨·치명 횟수·방어 게이지·죽은 표시·보상)가 끝까지 지켜본 것과 똑같다. 창은 값을 0.5초 보여 주고 닫힌다.
    /// 건너뛴 뒤에는 스킬도 못 쓴다 — 승부가 났다(BattleOver).
    /// </summary>
    public void Skip()
    {
        if (CanSkip == false)
            return;
        _skipping = true;
        playerCard.Quiet = monsterCard.Quiet = true;
        float dt = Time.fixedDeltaTime;
        int cap = Mathf.CeilToInt(MaxSkipSeconds / dt);
        try
        {
            for (int i = 0; i < cap && _battle.Finished == false; i++)
                _battle.Step(dt);
        }
        finally
        {
            playerCard.Quiet = monsterCard.Quiet = false;
            _skipping = _battle.Finished;   // 못 끝냈으면(마개·예외) 연출을 다시 켜고 평소대로 잇는다
            CarryState();
        }

        if (_skipping == false)
        {
            Debug.LogError($"[Battle] 건너뛰기: 몬스터 {_monsterId} 와의 싸움이 전투 시계 {MaxSkipSeconds}초 안에 끝나지 않았다 — 평소대로 잇는다");
            return;
        }

        LastBattle.Skipped = true;
        // 써 봤으면 알린 것이다 — 값(ShowSummary)이 예측과 나란히 뜬다. 안 적으면 늘 곧바로 건너뛰는 사람에게 띠가 매번 번쩍인다.
        if (GameEvents.IsAutoPlaying == false)
            PlayerPrefs.SetInt(SkipToldPref, 1);
        playerCard.Refresh();
        monsterCard.Refresh();
        ShowSummary();
    }

    void OnDestroy()
    {
        _battle?.Dispose();
        // 메뉴에서 다시 시작하거나 타이틀로 나가면 전투가 끝나지 않은 채 파괴된다 — 남은 구독이 다음 전투를 닫으면 안 된다.
        if (Managers.IsAlive && Managers.Game.OnBattleAction != null)
            Managers.Game.OnBattleAction -= BattleEnd;
    }

    public void BattleEnd()
    {
        if (_ending) return;
        _ending = true;
        // 건너뛰었으면 값(ShowSummary)을 읽을 만큼 더 둔다. Skip 이 끝까지 돌리는 도중에 여기로 온다.
        float closeTime = _skipping ? SummarySeconds : 0.3f;
        StartCoroutine(CoBattleEnd(closeTime));
    }

    IEnumerator CoBattleEnd(float time)
    {
        yield return new WaitForSeconds(time);

        Destroy(playerCard.gameObject);
        Destroy(monsterCard.gameObject);

        Managers.Game.OnBattleAction = null;
        if (Managers.Game.GameScene != null)
        {
            Managers.Game.GameScene.SetPlayerInfo();
            Managers.Game.GameScene.Refresh();
        }

        Managers.Game.OnBattle = false;

        ClosePopupUI();

        // 우두머리를 쓰러뜨렸다는 알림은 승부가 난 여기서 낸다. 쓰러진 순간(UI_MonsterCard.Dead)에 내면
        // 거대의 다섯 번째 대 포효가 같은 교환에서 플레이어를 죽여도 이미 나간 뒤였다 — 같이 죽으면
        // 진 것이고(thesword_balance, BattleForecast.Win) 체크포인트가 보스를 되살린다.
        bool won = Managers.Game.IsPlayerDead == false;
        // 전투 요약을 확정한다 — 두 알림을 듣는 쪽(업적·장부)이 LastBattle 에서 읽는다.
        LastBattle.Won = won;
        LastBattle.HpAfter = Managers.Game.PlayerData.CurHP;
        for (int i = 0; i < BattleSkills.Count; i++)
            LastBattle.SkillsUsed |= BattleSkills.IsUsed(i);
        if (won && _boss)
            GameEvents.RaiseBossDefeated(_monsterId, _stage);
        GameEvents.RaiseBattleEnded(_monsterId, won);

        // Game Over Popup
        if (Managers.Game.IsPlayerDead)
        {
            int moveCount = PlayerPrefs.GetInt("DEATHCOUNT", 0);
            moveCount++;
            PlayerPrefs.SetInt("DEATHCOUNT", moveCount);
            Managers.UI.ShowPopupUI<UI_GameOverPopup>();
        }

        // 최초 포션인지 확인
        if (PlayerPrefs.GetInt("ISFIRSTBATTLE") == 0)
        {
            PlayerPrefs.SetInt("ISFIRSTBATTLE", 1);
            UI_GuidePopup guidePopup = Managers.UI.ShowPopupUI<UI_GuidePopup>();
            guidePopup.SetInfo(Define.GUIDE_BATTLE);
        }
    }
}
