using Coffee.UIExtensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GameManager;

public class UI_PlayerCard : UI_BaseCard
{
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        //Managers.Game.OnBattlePlayerDamagedAction += StartDamagedMat;

        _creature.Name = Managers.GetString(Define.USER_NAME_INDEX);
        Refresh();
        _creature.OnDefenceAction += ClearDefence;
        _creature.OnHitAction += Refresh;
        _creature.OnHitAction += StartDamagedMat;
        _creature.OnDeadAction += Dead;
        _creature.OnDataRefreshAction += Refresh;

        StartCoroutine(CoDelayAttack());
        StartCoroutine(CoDelayDefence());

        if (Managers.Game.PlayerData.Inventory[(int)Define.Types.Shield].Count == 0)
        {
            GetImage((int)Images.CreatureShieldImage).gameObject.SetActive(false);
        }

        //GetImage((int)Images.AttackIcon).sprite = Managers.Resource.Load<Sprite>(Managers.Data.MonsterClassDic[_creature.Ability].Weapon + "[" + Managers.Data.MonsterClassDic[_creature.Ability].Weapon + "_0]");

        GetText((int)Texts.HPBarText).text = _creature.CurHP.ToString();
        GetImage((int)Images.HPHar).fillAmount = _creature.CurHP / _creature.MaxHP;
        GetImage((int)Images.HPHarGauge).fillAmount = _creature.CurHP / _creature.MaxHP;
        GetImage((int)Images.CreatureSwordImage).gameObject.GetComponent<Animator>().Play($"UISword{Managers.Game.PlayerData.CurSword - Define.EQUIP_SOWRD_FIRST}IdleAnim");
        SetUI();
        return true;
    }

    public override void Refresh()
    {
        base.Refresh();
        SetUI();
    }

    public override void Attack(CreatureData attacker, CreatureData target)
    {
        Managers.Game.AttackCount++;
        if (Managers.Game.AttackCount == Managers.Game.PlayerData.Critical)
        {
            _creature.IsCritical = true;
            Managers.Game.AttackCount = 0;
        }

        base.Attack(attacker, target);

        Vector3 pos = GameObject.Find("UI_MonsterCard").GetComponent<UI_MonsterCard>().GetImage((int)Images.CreatureImage).gameObject.transform.position;
        pos = new Vector3(pos.x, pos.y + 200, pos.z);

        GameObject go = GameObject.Find("UI_BattlePopup");
        if (go != null)
        {
            Managers.Object.ShowDamageFont(pos, _hitDamage, 0, go.transform, attacker.IsCritical, target.IsDefence);
            if (attacker.IsCritical) attacker.IsCritical = false;
        }

        if (target.IsDefence)
        {
            target.OnDefenceAction.Invoke();
        }

        //Debug.Log(Managers.Game.PlayerData.CurSword);
        GetImage((int)Images.CreatureImage).gameObject.GetComponent<Animator>().Play("UIPlayerAttackAnim");
        GetImage((int)Images.CreatureSwordImage).gameObject.GetComponent<Animator>().Play($"UISword{Managers.Game.PlayerData.CurSword - Define.EQUIP_SOWRD_FIRST}AttackAnim");
        if (Managers.Game.PlayerData.CurShield != Define.NOT_EQUIP)
            GetImage((int)Images.CreatureShieldImage).gameObject.GetComponent<Animator>().Play($"UIShield{Managers.Game.PlayerData.CurShield - Define.EQUIP_SHIELD_FIRST}AttackAnim");
        GetImage((int)Images.AttackIcon).gameObject.GetComponent<Animator>().Play(Managers.Data.MonsterClassDic[_creature.Ability].Weapon);
        CreatePlayerAttackParticle();
        CreateMonsterHitParticle();

        Managers.Sound.Play(Define.Sound.Effect, "HeroAttack0_SFX");
    }

    public override void Defence()
    {
        base.Defence();
        GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play(Managers.Data.MonsterClassDic[_creature.Ability].Shield);
        Debug.Log(Managers.Data.MonsterClassDic[_creature.Ability].Shield);
    }

    IEnumerator CoDelayAttack()
    {
        float maxAttackCoolTime = 3f;
        float attackCoolTime = 0f;
        maxAttackCoolTime = maxAttackCoolTime / Managers.Game.PlayerData.AttackSpeed;

        while (true)
        {
            if (attackCoolTime >= maxAttackCoolTime)
            {
                attackCoolTime = 0f;
                Attack(_creature, Managers.Game.MonsterData[0]);
            }
            attackCoolTime += Time.deltaTime * Managers.Game.GameSpeed;

            GetImage((int)Images.AttackDelayGauge).fillAmount = attackCoolTime / maxAttackCoolTime;

            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator CoDelayDefence()
    {
        _maxDefenceCoolTime = _maxDefenceCoolTime / Managers.Game.PlayerData.DefenceSpeed;

        while (true)
        {
            if (Managers.Game.DefenceCoolTime >= _maxDefenceCoolTime)
            {
                if (_creature.IsDefence == false)
                {
                    _creature.IsDefence = true;
                    Defence();
                }
                Managers.Game.DefenceCoolTime = _maxDefenceCoolTime;
                //_defenceCoolTime = 0f;
            }
            Managers.Game.DefenceCoolTime += Time.deltaTime * Managers.Game.GameSpeed;

            GetImage((int)Images.DefenceDelayGauge).fillAmount = Managers.Game.DefenceCoolTime / _maxDefenceCoolTime;

            yield return new WaitForFixedUpdate();
        }
    }

    public override void ClearDefence()
    {
        Managers.Sound.Play(Define.Sound.Effect, "Defense_SFX");

        StartCoroutine(CoStartShieldFX());
        StartCoroutine(CoDefenceMat());
        base.ClearDefence();
    }

    IEnumerator CoStartShieldFX()
    {
        int width = 75;
        int height = 75;

        GameObject go = Managers.Resource.Instantiate("UI_PlayerCardCopyImage", this.transform);
        Image image = go.GetOrAddComponent<Image>();
        Animator animator = go.GetOrAddComponent<Animator>();
        animator.runtimeAnimatorController = Managers.Resource.Load<RuntimeAnimatorController>("UIFXAnimation");
        animator.Play($"UIShieldFX");
        image.rectTransform.sizeDelta = new Vector2(width, height);
        float delay = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(delay);
        Destroy(go);
    }

    IEnumerator CoDefenceMat()
    {
        int width = 660;
        int height = 660;
        WaitForSeconds delay = new WaitForSeconds(0.1f);
        GameObject go = Managers.Resource.Instantiate("UI_PlayerCardCopyImage", GetImage((int)Images.CreatureImage).transform);
        go.transform.position = GetImage((int)Images.CreatureImage).transform.position;
        GameObject sword = Managers.Resource.Instantiate("UI_PlayerCardCopyImage", GetImage((int)Images.CreatureSwordImage).transform);
        sword.transform.position = GetImage((int)Images.CreatureSwordImage).transform.position;
        GameObject shield = Managers.Resource.Instantiate("UI_PlayerCardCopyImage", GetImage((int)Images.CreatureShieldImage).transform);
        shield.transform.position = GetImage((int)Images.CreatureShieldImage).transform.position;
        Image image = go.GetOrAddComponent<Image>();
        image.rectTransform.sizeDelta = new Vector2(width, height);
        Image swordImage = sword.GetOrAddComponent<Image>();
        swordImage.rectTransform.sizeDelta = new Vector2(width, height);
        Image shieldImage = shield.GetOrAddComponent<Image>();
        shieldImage.rectTransform.sizeDelta = new Vector2(width, height);
        Animator animator = go.GetOrAddComponent<Animator>();
        Animator swordanimator = sword.GetOrAddComponent<Animator>();
        Animator shieldanimator = shield.GetOrAddComponent<Animator>();
        animator.runtimeAnimatorController = Managers.Resource.Load<RuntimeAnimatorController>("UIPlayerAnimController");
        swordanimator.runtimeAnimatorController = Managers.Resource.Load<RuntimeAnimatorController>("CreatureSwordImage");
        shieldanimator.runtimeAnimatorController = Managers.Resource.Load<RuntimeAnimatorController>("CreatureShieldImage");
        animator.Play($"UIPlayerIdleAnim");
        swordanimator.Play($"UISword{Managers.Game.PlayerData.CurSword - Define.EQUIP_SOWRD_FIRST}IdleAnim");
        if (Managers.Game.PlayerData.CurShield != Define.NOT_EQUIP)
            shieldanimator.Play($"UIShield{Managers.Game.PlayerData.CurShield - Define.EQUIP_SHIELD_FIRST}IdleAnim");
        image.sprite = GetImage((int)Images.CreatureImage).sprite;
        swordImage.sprite = GetImage((int)Images.CreatureImage).sprite;
        shieldImage.sprite = GetImage((int)Images.CreatureImage).sprite;
        image.material = Managers.Resource.Load<Material>("PaintWhiteMat");
        swordImage.material = Managers.Resource.Load<Material>("PaintWhiteMat");
        shieldImage.material = Managers.Resource.Load<Material>("PaintWhiteMat");
        image.color = Util.DefenceColor();
        swordImage.color = Util.DefenceColor();
        shieldImage.color = Util.DefenceColor();
        float i = 0;
        while (i < 20)
        {
            //image.SetNativeSize();
            //swordImage.SetNativeSize();
            //shieldImage.SetNativeSize();
            i += 1;
            image.color += new Color(0, 0, 0, -0.05f);
            swordImage.color += new Color(0, 0, 0, -0.05f);
            shieldImage.color += new Color(0, 0, 0, -0.05f);
            yield return new WaitForSeconds(0.01f);
        }
        yield return delay;
        Destroy(go);
        Destroy(sword);
        Destroy(shield);
    }

    public override void StartDamagedMat()
    {
        StartCoroutine(CoDamagedMat());
    }

    IEnumerator CoDamagedMat()
    {
        int width = 660;
        int height = 660;

        WaitForSeconds delay = new WaitForSeconds(0.1f);
        GameObject go = Managers.Resource.Instantiate("UI_PlayerCardCopyImage", GetImage((int)Images.CreatureImage).transform);
        Image image = go.GetOrAddComponent<Image>();
        image.rectTransform.sizeDelta = GetImage((int)Images.CreatureImage).rectTransform.sizeDelta;
        Animator animator = go.GetOrAddComponent<Animator>();
        animator.runtimeAnimatorController = Managers.Resource.Load<RuntimeAnimatorController>("UIPlayerAnimController");
        animator.Play($"UIPlayerIdleAnim");
        image.sprite = GetImage((int)Images.CreatureImage).sprite;
        image.material = Managers.Resource.Load<Material>("PaintWhiteMat");
        image.color = Util.DamagedColor();
        image.rectTransform.sizeDelta = new Vector2(width, height);
        float i = 0;
        while (i < 10)
        {
            i += 1;
            image.color += new Color(0, 0, 0, -0.1f);
            yield return new WaitForSeconds(0.005f);
        }
        yield return delay;

        Destroy(go);

        //WaitForSeconds delay = new WaitForSeconds(0.1f);
        //GetImage((int)Images.CreatureImage).material = Managers.Resource.Load<Material>("PaintWhiteMat");
        //GetImage((int)Images.CreatureSwordImage).material = Managers.Resource.Load<Material>("PaintWhiteMat");
        //GetImage((int)Images.CreatureShieldImage).material = Managers.Resource.Load<Material>("PaintWhiteMat");
        //GetImage((int)Images.CreatureImage).color = Util.DamagedColor();
        //GetImage((int)Images.CreatureSwordImage).color = Util.DamagedColor();
        //GetImage((int)Images.CreatureShieldImage).color = Util.DamagedColor();
        //yield return delay;
        //GetImage((int)Images.CreatureImage).color = Color.white;
        //GetImage((int)Images.CreatureSwordImage).color = Color.white;
        //GetImage((int)Images.CreatureShieldImage).color = Color.white;
        //yield return delay;
        //GetImage((int)Images.CreatureImage).material = null;
        //GetImage((int)Images.CreatureSwordImage).material = null;
        //GetImage((int)Images.CreatureShieldImage).material = null;
        //GetImage((int)Images.CreatureImage).color = Color.white;
        //GetImage((int)Images.CreatureSwordImage).color = Color.white;
        //GetImage((int)Images.CreatureShieldImage).color = Color.white;
    }

    void CreatePlayerAttackParticle()
    {
        int swordId = Managers.Game.PlayerData.CurSword;
        string attackFX = Managers.Data.EquipDic[swordId].AttackFX;
        GameObject player = GameObject.Find("CreatureImage");
        GameObject go = Managers.Resource.Instantiate(attackFX, GetImage((int)GameObjects.AttackFX).transform);
        go.transform.localPosition += new Vector3(-50, -50, 0);
        var uiParticle = go.GetOrAddComponent<UIParticle>();
        uiParticle.scale = 270;
        uiParticle.Play();

        //Destroy(uiParticle, 0.3f);
    }

    void CreateMonsterHitParticle()
    {
        int swordId = Managers.Game.PlayerData.CurSword;
        string hitFX = Managers.Data.EquipDic[swordId].HitFX;
        GameObject monster = GameObject.Find("UI_MonsterCard");
        GameObject go = Managers.Resource.Instantiate(hitFX, monster.transform);
        var uiParticle = go.GetOrAddComponent<UIParticle>();

        uiParticle.scale = 50;
        uiParticle.Play();
    }

    public override void Dead()
    {
        Managers.Sound.Play(Define.Sound.Effect, "GameOver_Event");

        base.Dead();
        Managers.Game.OnInputLock = true;
        Managers.Game.IsPlayerDead = true;
        Managers.Game.OnBattleAction.Invoke();
        Managers.Game.OnBattle = false;
        return;
    }

    private void OnDestroy()
    {
        _creature.OnDefenceAction -= ClearDefence;
        _creature.OnHitAction -= Refresh;
        _creature.OnHitAction -= StartDamagedMat;
        _creature.OnDeadAction -= Dead;
        _creature.OnDataRefreshAction -= Refresh;
    }
}
