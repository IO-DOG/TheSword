using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_CreatureCard : UI_Base
{
    #region Enum

    enum Images
    {
        CreatureImage,
        HPHar,
        HPHarGauge,
        AttackDelayGauge,
        DefenceDelayGauge,
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
    public int _totalAttackCount = 0;
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

        GetText((int)Texts.CreatureName).text = Managers.Game.MonsterData.Name;
        GetText((int)Texts.HPBarText).text = Managers.Game.MonsterData.MaxHP.ToString();
        GetText((int)Texts.AttackStatusText).text = Managers.Game.MonsterData.AttackSpeed.ToString();
        GetText((int)Texts.DefenceStatusText).text = Managers.Game.MonsterData.DefenceSpeed.ToString();
        GetImage((int)Images.CreatureImage).sprite = Managers.Resource.Load<Sprite>($"{Managers.Game.MonsterData.IdleAnimStr}_0");
        GetImage((int)Images.CreatureImage).SetNativeSize();
        //GetImage((int)Images.CreatureImage).gameObject.GetComponent<Animator>().Play($"{Managers.Game.MonsterData.IdleAnimStr}");

        Managers.Game.OnBattleDataRefreshAction -= Refresh;
        Managers.Game.OnBattleDataRefreshAction += Refresh;
        Managers.Game.OnBattleCreatureDefeceAction += ClearDefence;

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
        GetText((int)Texts.HPBarText).text = Managers.Game.MonsterData.CurHP.ToString();
        GetImage((int)Images.HPHar).fillAmount = Managers.Game.MonsterData.CurHP / Managers.Game.MonsterData.MaxHP;
        yield return new WaitForSeconds(0.2f);
        GetImage((int)Images.HPHarGauge).fillAmount = Managers.Game.MonsterData.CurHP / Managers.Game.MonsterData.MaxHP;
    }

    public void Attack()
    {
        if (_totalAttackCount > 0 && _totalAttackCount % 20 == 0)
        {
            Berserk();
        }

        if (_attackCount == Managers.Game.MonsterData.Critical)
        {
            _isCri = true;
            _attackCount = 0;
        }

        if (Managers.Game.CurPlayerData.IsDefence == true)
        {
            Managers.Game.CurPlayerData.IsDefence = false;
            Managers.Game.OnBattlePlayerDefeceAction.Invoke();

            if (_isCri == true)
            {
                Managers.Game.CurPlayerData.CurHP -= Mathf.Max(0, Managers.Game.MonsterData.Attack * Managers.Game.MonsterData.CriticalAttack / 100 - Managers.Game.CurPlayerData.Defence) * 0.2f;
                _isCri = false;
            }
        }
        else
        {
            if (_isCri)
                Managers.Game.CurPlayerData.CurHP -= Mathf.Max(0, Managers.Game.MonsterData.Attack * Managers.Game.MonsterData.CriticalAttack / 100 - Managers.Game.CurPlayerData.Defence);
            else
                Managers.Game.CurPlayerData.CurHP -= Mathf.Max(0, Managers.Game.MonsterData.Attack - Managers.Game.CurPlayerData.Defence);

            if (Managers.Game.CurPlayerData.CurHP <= 0)
            {
                // Game Over Popup TODO
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
        Managers.Game.MonsterData.IsDefence = true;
    }

    IEnumerator CoDelayAttack()
    {
        float maxAttackCoolTime = 3f;
        float attackCoolTime = 0f;
        maxAttackCoolTime = maxAttackCoolTime / Managers.Game.MonsterData.AttackSpeed;

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

        _maxDefenceCoolTime = _maxDefenceCoolTime / Managers.Game.MonsterData.AttackSpeed;

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

    public void Berserk()
    {
        Managers.Game.MonsterData.Attack *= 1.2f;
        Managers.Game.MonsterData.AttackSpeed *= 1.2f;
        Managers.Game.MonsterData.Defence *= 1.2f;
        Managers.Game.MonsterData.DefenceSpeed *= 1.2f;
    }

    public void ClearDefence()
    {
        _defenceCoolTime = 0f;
    }
}
