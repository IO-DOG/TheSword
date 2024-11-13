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
    protected enum Images
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

    protected enum Texts
    {
        CreatureName,
        HPBarText,
        AttackStatusText,
        DefenceStatusText,
    }

    //public CreatureClass.IEffect effect;
    public CreatureData _creature;
    public float _defenceCoolTime = 0f;
    public float _maxDefenceCoolTime = 3f;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        //#region Bind
        //BindImage(typeof(Images));
        //BindText(typeof(Texts));
        //#endregion

        Managers.Game.OnBattleDataRefreshAction -= Refresh;
        Managers.Game.OnBattleDataRefreshAction += Refresh;
        Managers.Game.OnBattlePlayerDefeceAction += ClearDefence;

        _creature.effect = EffectFactory.GetTrait(_creature, this);
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
        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

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
        _creature.IsDefence = false;
        if (GetImage((int)Images.DefenceIcon) != null)
            GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play("UIIdleDefense");
    }

    public virtual void Attack(CreatureData attacker, CreatureData target)
    {
        target.effect.ExcuteOnHit(attacker, target, _creature.effect.ExecuteAttack(attacker, target));
    }

    public virtual void Defence()
    {
        _defenceCoolTime = _maxDefenceCoolTime;
        GetImage((int)Images.DefenceDelayGauge).fillAmount = _defenceCoolTime / _maxDefenceCoolTime;

        GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play("UIDefenceIcon");
        _creature.IsDefence = true;

    }

    public void FillDefenceGague()
    {
        _defenceCoolTime = _maxDefenceCoolTime;
        _creature.IsDefence = true;
        GetImage((int)Images.DefenceDelayGauge).fillAmount = 1f;
    }

    public virtual void Dead()
    {

    }

}
