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

        return true;
    }

    public override void Refresh()
    {
        base.Refresh();
    }

    public override void Attack(CreatureData attacker, CreatureData target)
    {
        base.Attack(attacker, target);
        Managers.Game.AttackCount++;

        if (target.IsDefence)
        {
            target.OnDefenceAction.Invoke();
        }

        GetImage((int)Images.CreatureImage).gameObject.GetComponent<Animator>().Play("UIPlayerAttackAnim");
        GetImage((int)Images.CreatureSwordImage).gameObject.GetComponent<Animator>().Play($"UISword{Managers.Game.PlayerData.CurSword - 9}AttackAnim");
        if (Managers.Game.PlayerData.CurShield != 0)
            GetImage((int)Images.CreatureShieldImage).gameObject.GetComponent<Animator>().Play($"UIShield{Managers.Game.PlayerData.CurShield - 20}AttackAnim");
        GetImage((int)Images.AttackIcon).gameObject.GetComponent<Animator>().Play("UIAttackIcon");
        CreatePlayerAttackParticle();
        CreateMonsterHitParticle();

        if (Managers.Game.AttackCount == Managers.Game.PlayerData.Critical)
        {
            _creature.IsCritical = true;
            Managers.Game.AttackCount = 0;
        }
    }

    public override void Defence()
    {
        base.Defence();
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
            if (_defenceCoolTime >= _maxDefenceCoolTime)
            {
                if (_creature.IsDefence == false)
                {
                    _creature.IsDefence = true;
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

    public override void ClearDefence()
    {
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
        swordanimator.Play($"UISword{Managers.Game.PlayerData.CurSword - 9}IdleAnim");
        if (Managers.Game.PlayerData.CurShield != 0)
            shieldanimator.Play($"UIShield{Managers.Game.PlayerData.CurShield - 20}IdleAnim");
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
        GameObject go = Managers.Resource.Instantiate(attackFX, GetImage((int)Images.CreatureImage).transform);
        go.transform.localPosition += new Vector3(0, -150, 0);
        var uiParticle = go.GetOrAddComponent<UIParticle>();
        uiParticle.scale = 300;
        uiParticle.Play();

        //Destroy(uiParticle, 0.3f);
    }

    void CreateMonsterHitParticle()
    {
        int swordId = Managers.Game.PlayerData.CurSword;
        string hitFX = Managers.Data.EquipDic[swordId].HitFX;
        GameObject monster = GameObject.Find("UI_MonsterCard");
        GameObject go = Managers.Resource.Instantiate(hitFX, monster.transform);
        go.transform.position += new Vector3(0, 70, 0);
        var uiParticle = go.GetOrAddComponent<UIParticle>();

        //var childrenUIParticle = go.GetComponentsInChildren<UIParticle>()[1]; // 이거 좀 위험한 코드임.
        uiParticle.scale = 50;
        //childrenUIParticle.scale = 300;
        //Debug.Log($"childrenUIParticle.gameObject.name : {childrenUIParticle.gameObject.name}");
        uiParticle.Play();
        //childrenUIParticle.Play();
        //Destroy(uiParticle, 0.3f);
    }

    public override void Dead()
    {
        base.Dead();

        // Game Over Popup TODO
        //CreatePlayerDeathParticle();
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
