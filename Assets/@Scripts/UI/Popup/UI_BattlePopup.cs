using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        if (Managers.Game.ScreenType == Define.ScreenType.Window)
            playerCard.transform.localScale = new Vector3(width / 1920 * 8f, height / 1080 * 8f, 1);
        else
            playerCard.transform.localScale = new Vector3(width / 1920 * 4.5f, height / 1080 * 4.5f, 1);
        //playerCard.Data = Managers.Game.Player.Data;

        for (int i = 0; i < Managers.Game.MonsterData.Count; i++)
        {
            monsterCard = Managers.UI.SetBattleCard<UI_MonsterCard>(gameObject.transform, Managers.Game.MonsterData[i]);

            monsterCard.transform.position = new Vector3(width * 0.7f, height * 0.6f, 0);
            if (Managers.Game.ScreenType == Define.ScreenType.Window)
                monsterCard.transform.localScale = new Vector3(width / 1920 * 8f, height / 1080 * 8f, 1);
            else
                monsterCard.transform.localScale = new Vector3(width / 1920 * 4.5f, height / 1080 * 4.5f, 1);
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

        return true;
    }

    /// <summary>전투 중 액티브 스킬 입력. 1 강타 / 2 철벽 / 3 흡혈.
    ///
    /// 전투는 자동으로 굴러가므로 스킬은 "언제 끼어드느냐" 가 전부다.
    /// 셋 다 전투당 한 번뿐이라, 아낄지 지금 쓸지가 유일한 판단거리가 된다.</summary>
    void Update()
    {
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
