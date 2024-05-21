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
        GetText((int)Texts.AttackStatusText).text = Managers.Game.CurPlayerData.AttackSpeed.ToString();
        GetText((int)Texts.DefenceStatusText).text = Managers.Game.CurPlayerData.DefenceSpeed.ToString();

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
        GetImage((int)Images.AttackIcon).gameObject.GetComponent<Animator>().Play("UIAttackIcon");

        if (_attackCount == Managers.Game.CurPlayerData.Critical)
        {
            _isCri = true;
            _attackCount = 0;
        }

        if (Managers.Game.MonsterData.IsDefence == true)
        {
            Managers.Game.MonsterData.IsDefence = false;
            Managers.Game.OnBattleCreatureDefeceAction.Invoke();

            if (_isCri == true)
            {
                Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack * Managers.Game.CurPlayerData.CriticalAttack / 100 - Managers.Game.MonsterData.Defence) * 0.2f;
                _isCri = false;
            }
        }
        else
        {
            if (_isCri)
                Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack * Managers.Game.CurPlayerData.CriticalAttack / 100 - Managers.Game.MonsterData.Defence);
            else
                Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack - Managers.Game.MonsterData.Defence);


            if (Managers.Game.MonsterData.CurHP <= 0)
            {
                Managers.Data.MonsterActiveDic[Managers.Game.MonsterData.IsActiveIndex] = false;

                Destroy(Managers.Game.Monster.gameObject);
                Managers.Game.OnBattleAction.Invoke();
                Managers.Game.OnBattle = false;
                return;
            }
        }
        _attackCount++;
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

    IEnumerator CoDelayDefence()
    {
        _maxDefenceCoolTime = _maxDefenceCoolTime / Managers.Game.CurPlayerData.AttackSpeed;

        while (true)
        {
            if (_defenceCoolTime >= _maxDefenceCoolTime)
            {
                Defence();
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
    }
}
