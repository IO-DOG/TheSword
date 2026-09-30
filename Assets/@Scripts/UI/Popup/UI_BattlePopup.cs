using System;
using System.Collections;
using System.Collections.Generic;
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

    // 카드 배율. 카드는 이 창 캔버스의 자식이라 캔버스가 이미 화면 높이(1080 기준)만큼 늘고 준다. 예전에는 여기서
    // 화면 폭·높이를 한 번 더 곱해서(창 모드 8, 전체 화면 4.5) 두 번 커졌다 — 1280x720 창에서는 스킬 막대가 카드
    // 이름을 덮었고, 그보다 큰 창·1440p 이상 전체 화면에서는 카드가 화면을 넘쳐 이름·능력치가 잘렸다. 가로세로를
    // 따로 곱해서 4:3·16:10 에서는 그림이 찌그러졌다. 배율은 하나다 (1080p 전체 화면에서 보던 크기).
    const float CardScale = 4.5f;

    BattleStepper _battle;
    bool _ending;
    int _monsterId;
    bool _boss;     // 우두머리(챕터 보스·킹 슬라임)와의 전투인가
    int _stage;
    UI_PlayerCard playerCard = null;
    UI_MonsterCard monsterCard = null;

    /// <summary>승부가 났다. 닫히기까지 0.3초가 남아 있어도 이제 아무것도 바꾸지 못한다 (스킬 포함).</summary>
    public bool BattleOver => _ending || _battle == null || _battle.Finished;

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
        _battle = new BattleStepper(Managers.Game.PlayerData, Managers.Game.MonsterData[0],
            Managers.Game.AttackCount, Managers.Game.DefenceCoolTime);
        _battle.OnStrike = (player, damage, critical, guarded) => {
            UI_BaseCard card = player ? (UI_BaseCard)playerCard : monsterCard;
            card.PresentAttack(player ? _battle.Player : _battle.Monster,
                player ? _battle.Monster : _battle.Player, damage, critical, guarded);
        };
        _battle.OnGuard = player => { if (player) playerCard.Defence(); else monsterCard.Defence(); };

        BuildSkillBar();
        return true;
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
        Sprite art = CodeUI.Sliced(CodeUI.PrefabSprite("UI_MenuPopup", "SystemUI_Button"), new Vector4(24f, 0f, 24f, 0f));
        float width = SkillWidth * BattleSkills.Count + SkillGap * (BattleSkills.Count - 1);
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

    /// <summary>전투 중 액티브 스킬 입력. 1 강타 / 2 철벽 / 3 흡혈.
    ///
    /// 전투는 자동으로 굴러가므로 스킬은 "언제 끼어드느냐" 가 전부다.
    /// 셋 다 전투당 한 번뿐이라, 아낄지 지금 쓸지가 유일한 판단거리가 된다.</summary>
    void Update()
    {
        PaintSkills();
        // 치명타 수업(StoryDirector)이 전투를 세워 두고 말하는 동안은 키도 받지 않는다 — 시계는 멈췄는데 스킬만 들어가면 안 된다.
        if (StoryDirector.HoldsBattle)
            return;
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
        for (int i = 0; i < Mathf.Max(1, Managers.Game.GameSpeed) && !_battle.Finished; i++)
            _battle.Step(Time.fixedDeltaTime);
        Managers.Game.AttackCount = _battle.PlayerHits;
        Managers.Game.DefenceCoolTime = _battle.PlayerDefenceTime;
        playerCard.ShowCombatGauges(_battle.PlayerAttackTime, _battle.PlayerAttackPeriod,
            _battle.PlayerDefenceTime, _battle.PlayerDefencePeriod);
        monsterCard.ShowCombatGauges(_battle.MonsterAttackTime, _battle.MonsterAttackPeriod,
            _battle.MonsterDefenceTime, _battle.MonsterDefencePeriod);
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
        float closeTime = 0.3f;
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
