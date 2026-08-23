using Data;
using System.Collections;
using TMPro;
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
        SetName(GetText((int)Texts.CreatureName), _creature.Name);
        GetText((int)Texts.HPBarText).text = _creature.CurHP.ToString();
        GetText((int)Texts.AttackStatusText).text = _creature.Attack.ToString();
        GetText((int)Texts.DefenceStatusText).text = _creature.Defence.ToString();

        int abilityIndex = _creature.Ability;
        string battleBGImage = Managers.Data.MonsterClassDic[abilityIndex].BattleBGImage;
        string abilityImage = Managers.Data.MonsterClassDic[abilityIndex].AbilityImage;
        GetImage((int)Images.BattleBGImage).sprite = Managers.Resource.Load<Sprite>(battleBGImage);
        GetImage((int)Images.AbilityImage).sprite = Managers.Resource.Load<Sprite>(abilityImage);
    }

    /// <summary>
    /// 이름을 한 줄로 넣는다. 넘치면 글자를 줄인다.
    ///
    /// 이름표는 줄바꿈이 켜져 있고 넘침을 허용해서, 생성 층의 긴 이름
    /// ("이끼 낀 지하 묘소의 잿빛 파수꾼 우두머리")이 두 줄로 접히면 두 번째 줄이
    /// 위로 넘쳐 공격력·방어력 숫자를 덮었다 — 다섯 보스 전부 그랬다.
    /// 칸을 키우면 카드 그림이 밀리므로, 한 줄로 두고 폭에 맞춰 줄인다.
    /// </summary>
    static void SetName(TMP_Text label, string name)
    {
        if (label == null)
            return;

        float designed = label.fontSize;      // 자동 축소를 켜면 이 값이 바뀐다. 먼저 잡아 둔다.

        label.text = name;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.fontSizeMin = 6f;
        label.fontSizeMax = designed;
        label.enableAutoSizing = true;
    }

    /// <summary>
    /// 전투창 그림칸에 스프라이트를 통째로 넣는다.
    ///
    /// 칸은 크기가 고정인데 스프라이트는 86px 짜리 몹부터 288px 짜리 보스까지 섞여
    /// 있다. 그대로 넣으면 칸 밖으로 넘쳐 아래쪽이 잘린다.
    /// RectTransform 의 크기를 읽어 비교하는 것으로는 안 된다 — 앵커가 늘어나 있으면
    /// sizeDelta 는 실제 크기가 아니라 여백이라, 큰 그림에서도 판정이 걸리지 않는다.
    /// 그래서 스프라이트 비율에서 직접 칸에 맞는 크기를 구하고, 앵커와 피벗을
    /// 가운데로 못박아 어떤 프리팹이 와도 잘리지 않게 한다.
    /// </summary>
    protected void FitCreatureImage(Image img)
    {
        if (img == null || img.sprite == null)
            return;

        RectTransform rt = img.rectTransform;
        RectTransform box = rt.parent as RectTransform;
        if (box == null)
            return;

        Vector2 limit = box.rect.size;
        Rect sp = img.sprite.rect;
        if (limit.x <= 0f || limit.y <= 0f || sp.width <= 0f || sp.height <= 0f)
            return;

        float aspect = sp.width / sp.height;
        float w = limit.x;
        float h = w / aspect;
        if (h > limit.y)
        {
            h = limit.y;
            w = h * aspect;
        }

        img.preserveAspect = true;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(w, h);
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
