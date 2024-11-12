using Data;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using static GameManager;

public class UI_BaseCard : UI_Base
{
    enum Images
    {
        CreatureImage,
        HPHar,
        HPHarGauge,
        AttackDelayGauge,
        DefenceDelayGauge,
        AttackIcon,
        DefenceIcon,
        CreatureSwordImage,
        CreatureShieldImage,
    }

    enum Texts
    {
        CreatureName,
        HPBarText,
        AttackStatusText,
        DefenceStatusText,
    }

    protected CreatureData _creature;
    protected float _defenceCoolTime = 0f;
    protected bool _defenseFlag = false;
    protected float _maxDefenceCoolTime = 3f;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion



        Managers.Game.OnBattleDataRefreshAction -= Refresh;
        Managers.Game.OnBattleDataRefreshAction += Refresh;
        Managers.Game.OnBattlePlayerDefeceAction += ClearDefence;
        //Managers.Game.OnBattlePlayerDamagedAction += StartDamagedMat;


        return true;
    }

    public void SetData(CreatureData creature)
    {
        _creature = creature;
        SetUI();
    }

    void SetUI()
    {
        GetText((int)Texts.CreatureName).text = "Player!";
        GetText((int)Texts.HPBarText).text = _creature.CurHP.ToString();
        GetText((int)Texts.AttackStatusText).text = _creature.Attack.ToString();
        GetText((int)Texts.DefenceStatusText).text = _creature.Defence.ToString();

    }

    public virtual void Refresh()
    {
        StartCoroutine(CoRefresh());
    }

    IEnumerator CoRefresh()
    {
        GetText((int)Texts.HPBarText).text = _creature.CurHP.ToString();
        GetImage((int)Images.HPHar).fillAmount = _creature.CurHP / _creature.MaxHP;
        yield return new WaitForSeconds(0.2f);
        GetImage((int)Images.HPHarGauge).fillAmount = _creature.CurHP / _creature.MaxHP;
    }

    public virtual void ClearDefence()
    {
        _defenceCoolTime = 0f;
        _defenseFlag = false;
        if (GetImage((int)Images.DefenceIcon) != null)
            GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play("UIIdleDefense");
    }

    public virtual int Attack(CreatureData attacker, CreatureData target)
    {
        int damage = (int)Mathf.Max(0, attacker.Attack);
        if (attacker.ISCritical) damage *= (int)(attacker.CriticalAttack / 100);
        damage -= (int)target.Defence;
        if (target.IsDefence && attacker.ISCritical) damage = (int)(damage * 0.25f);
        else if (target.IsDefence) damage = 0;

        return damage;
    }

    //public virtual int SAttack(CreatureData target, ref bool isCri)
    //{
    //    int damage = (int)Mathf.Max(0, _creature.Attack);
    //    if (isCri) damage *= (int)(_creature.CriticalAttack / 100);

    //    target.STakeDamage(damage);
    //}

    //public virtual void STakeDamage(CreatureData attacker)
    //{
    //    damage -= (int)_creature.Defence;
    //    if (_creture.isDefence && attacker.isCri) damage = (int)(damage * 0.25f);
    //    else if (_creture.isDefence) damage = 0;

    //    _creature.HP -= damege;
    //    if(_creature.HP < 0)
    //        _creature.Dead();
    //}

    public virtual void Defence()
    {
        _defenceCoolTime = _maxDefenceCoolTime;
        GetImage((int)Images.DefenceDelayGauge).fillAmount = _defenceCoolTime / _maxDefenceCoolTime;

        GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play("UIDefenceIcon");
        _creature.IsDefence = true;

    }

    public virtual void Dead()
    {

    }

}
