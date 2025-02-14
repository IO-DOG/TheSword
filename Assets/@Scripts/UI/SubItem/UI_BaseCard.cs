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
        BattleBGImage,
        AbilityImage,
        BattleUI_CharacterBG,
    }

    protected enum Texts
    {
        CreatureName,
        HPBarText,
        AttackStatusText,
        DefenceStatusText,
    }

    protected enum GameObjects
    {
        AttackFX,
    }

    //public CreatureClass.IEffect effect;
    public CreatureData _creature;
    public float _defenceCoolTime = 0f;
    public float _maxDefenceCoolTime = 3f;
    public int _hitDamage = 0;
    public bool _isCriHit = false;
    public bool _isHeal = false;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        _creature.Trait = EffectFactory.GetTrait(_creature, this);

        return true;
    }

    public void SetData(CreatureData creature)
    {
        _creature = creature;
        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindObject(typeof(GameObjects));
        #endregion
        SetUI();
    }

    protected void SetUI()
    {
        GetText((int)Texts.CreatureName).text = _creature.Name;
        GetText((int)Texts.HPBarText).text = _creature.CurHP.ToString();
        GetText((int)Texts.AttackStatusText).text = _creature.Attack.ToString();
        GetText((int)Texts.DefenceStatusText).text = _creature.Defence.ToString();

        int abilityIndex = _creature.Ability;
        string battleBGImage = Managers.Data.MonsterClassDic[abilityIndex].BattleBGImage;
        string abilityImage = Managers.Data.MonsterClassDic[abilityIndex].AbilityImage;
        GetImage((int)Images.BattleBGImage).sprite = Managers.Resource.Load<Sprite>(battleBGImage);
        GetImage((int)Images.AbilityImage).sprite = Managers.Resource.Load<Sprite>(abilityImage);
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
        Managers.Game.DefenceCoolTime = 0f;
        _creature.IsDefence = false;
        if (GetImage((int)Images.DefenceIcon) != null)
            GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play("UIIdleDefense");
    }

    public virtual void StartDamagedMat()
    {

    }

    public virtual void Attack(CreatureData attacker, CreatureData target)
    {
        int damage = attacker.Trait.ExecuteAttack(attacker, target);
        target.Trait.ExcuteOnHit(attacker, target, damage);
        _hitDamage = damage;
        _isCriHit = attacker.IsCritical;
    }

    public virtual void Defence()
    {
        _defenceCoolTime = _maxDefenceCoolTime;
        GetImage((int)Images.DefenceDelayGauge).fillAmount = _defenceCoolTime / _maxDefenceCoolTime;

        //GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play(Managers.Data.MonsterClassDic[_creature.Ability].Shield);
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
