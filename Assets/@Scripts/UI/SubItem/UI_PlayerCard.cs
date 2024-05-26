using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_PlayerCard : UI_Base
{
    #region Enum

    enum Images
    {
        CreatureImage,
        HPHar,
        HPHarGauge,
        AttackDelayGauge,
        DefenceDelayGauge,
        AttackIcon,
        DefenceIcon,
    }

    enum Texts
    {
        CreatureName,
        HPBarText,
        AttackStatusText,
        DefenceStatusText,
    }

    #endregion

    public bool _isCri = false;
    public int _attackCount = 0;
    public float _maxDefenceCoolTime = 3f;
    public float _defenceCoolTime = 0f;
    public bool _forAssassin = false;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        GetText((int)Texts.CreatureName).text = "Player!";
        GetText((int)Texts.HPBarText).text = Managers.Game.CurPlayerData.CurHP.ToString();
        GetText((int)Texts.AttackStatusText).text = Managers.Game.CurPlayerData.Attack.ToString();
        GetText((int)Texts.DefenceStatusText).text = Managers.Game.CurPlayerData.Defence.ToString();

        Managers.Game.OnBattleDataRefreshAction -= Refresh;
        Managers.Game.OnBattleDataRefreshAction += Refresh;
        Managers.Game.OnBattlePlayerDefeceAction += ClearDefence;

        StartCoroutine(CoDelayAttack());
        StartCoroutine(CoDelayDefence());

        return true;
    }

    public void Refresh()
    {
        StartCoroutine(CoRefresh());
    }

    IEnumerator CoRefresh()
    {
        GetText((int)Texts.HPBarText).text = Managers.Game.CurPlayerData.CurHP.ToString();
        GetImage((int)Images.HPHar).fillAmount = Managers.Game.CurPlayerData.CurHP / Managers.Game.CurPlayerData.MaxHP;
        yield return new WaitForSeconds(0.2f);
        GetImage((int)Images.HPHarGauge).fillAmount = Managers.Game.CurPlayerData.CurHP / Managers.Game.CurPlayerData.MaxHP;
    }

    public void Attack()
    {
        _attackCount++;

        if (Managers.Game.MonsterData.Feature == 6)
        {
            Managers.Game.MonsterData.DamagedCount++;
            if (Managers.Game.MonsterData.DamagedCount == 5)
            {
                Debug.Log("거대 효과 발동");
                Managers.Game.CurPlayerData.CurHP -= Mathf.Max(0, Managers.Game.MonsterData.Attack - Managers.Game.CurPlayerData.Defence) * 0.2f;
                Refresh();
            }
        }
        GetImage((int)Images.AttackIcon).gameObject.GetComponent<Animator>().Play("UIAttackIcon");

        if (_attackCount == Managers.Game.CurPlayerData.Critical)
        {
            _isCri = true;
            _forAssassin= true;
            _attackCount = 0;
        }

        // 몬스터가 암살일 경우
        if (Managers.Game.MonsterData.Feature == 7 && _forAssassin == false)
        {
            Debug.Log("암살 효과 발동");
            return;
        }

        // 몬스터가 방어 상태일 경우
        if (Managers.Game.MonsterData.IsDefence == true)
        {
            Managers.Game.MonsterData.IsDefence = false;
            Managers.Game.OnBattleCreatureDefeceAction.Invoke();

            if (_isCri == true)
            {
                Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack * (Managers.Game.CurPlayerData.CriticalAttack / 100) - Managers.Game.MonsterData.Defence) * 0.2f;
                _isCri = false;
            }
        }
        else // 일반 공격
        {
            if (_isCri)
            {
                // 몬스터가 불사 효과일 경우
                if (Managers.Game.MonsterData.Feature == 4)
                {
                    Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack * (Managers.Game.CurPlayerData.CriticalAttack / 100) - Managers.Game.MonsterData.Defence) * 20;
                    Debug.Log("불사 효과 발동 치명 데미지 200퍼");
                }
                else
                    Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack * (Managers.Game.CurPlayerData.CriticalAttack / 100) - Managers.Game.MonsterData.Defence);
                _isCri = false;
            }
            else
            {
                // 몬스터가 불사 효과일 경우
                if (Managers.Game.MonsterData.Feature == 4)
                {
                    Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack * (Managers.Game.CurPlayerData.CriticalAttack / 100) - Managers.Game.MonsterData.Defence) * 0.2f;
                    Debug.Log("불사 효과 발동 일반 데미지 20퍼");
                }
                else
                    Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack - Managers.Game.MonsterData.Defence);
            }


            if (Managers.Game.MonsterData.CurHP <= 0)
            {
                // add exp
                Managers.Game.CurPlayerData.CurExp += Managers.Game.MonsterData.RewardExp;

                Managers.Data.MonsterActiveDic[Managers.Game.MonsterData.IsActiveIndex] = false;

                Destroy(Managers.Game.Monster.gameObject);
                Managers.Game.OnBattleAction.Invoke();
                Managers.Game.OnBattle = false;
                return;
            }
        }
        Managers.Game.OnBattleDataRefreshAction.Invoke();
    }

    public void Defence()
    {
        GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play("UIDefenceIcon");
        Managers.Game.CurPlayerData.IsDefence = true;
    }

    IEnumerator CoDelayAttack()
    {
        float maxAttackCoolTime = 3f;
        float attackCoolTime = 0f;
        maxAttackCoolTime = maxAttackCoolTime / Managers.Game.CurPlayerData.AttackSpeed;

        while (true)
        {
            if (attackCoolTime >= maxAttackCoolTime)
            {
                attackCoolTime = 0f;
                Attack();
            }
            attackCoolTime += Time.deltaTime * Managers.Game.GameSpeed;

            GetImage((int)Images.AttackDelayGauge).fillAmount = attackCoolTime / maxAttackCoolTime;

            yield return new WaitForFixedUpdate();
        }
    }

    public bool _defenseFlag = false;
    IEnumerator CoDelayDefence()
    {
        _maxDefenceCoolTime = _maxDefenceCoolTime / Managers.Game.CurPlayerData.AttackSpeed;

        while (true)
        {
            if (_defenceCoolTime >= _maxDefenceCoolTime)
            {
                if (_defenseFlag == false)
                {
                    _defenseFlag = true;
                    Defence();
                }
                _defenceCoolTime = _maxDefenceCoolTime;
                //_defenceCoolTime = 0f;
            }
            _defenceCoolTime += Time.deltaTime * Managers.Game.GameSpeed;

            GetImage((int)Images.DefenceDelayGauge).fillAmount = _defenceCoolTime / _maxDefenceCoolTime;

            yield return new WaitForFixedUpdate();
        }
    }

    public void ClearDefence()
    {
        _defenceCoolTime = 0f;
        _defenseFlag = false;
        if (GetImage((int)Images.DefenceIcon) != null)
            GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play("UIIdleDefense");
    }
}
