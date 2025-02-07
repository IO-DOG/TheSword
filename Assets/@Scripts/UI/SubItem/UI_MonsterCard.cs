using Coffee.UIExtensions;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using static GameManager;

public class UI_MonsterCard : UI_BaseCard
{
    #region Member
    public int _attackCount = 0;
    public int _totalAttackCount = 0;
    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        GetImage((int)Images.CreatureImage).gameObject.GetComponent<Animator>().Play($"{_creature.IdleAnimStr}");

        Refresh();
        _creature.OnDefenceAction += ClearDefence;
        _creature.OnHitAction += Refresh;
        _creature.OnHitAction += StartDamagedMat;
        _creature.OnDeadAction += Dead;
        _creature.OnDataRefreshAction += Refresh;

        //SpriteAtlas spriteAtlas = Managers.Resource.Load<SpriteAtlas>("BattleUI_Weppon2");

        //GetImage((int)Images.AttackIcon).sprite = spriteAtlas.GetSprite("BattleUI_Weppon2_0");

        StartCoroutine(CoDelayAttack());
        StartCoroutine(CoDelayDefence());

        return true;
    }

    public override void Refresh()
    {
        base.Refresh();
    }

    public override void Attack(CreatureData attacker, CreatureData target)
    {
        _attackCount++;
        if (_attackCount == _creature.Critical)
        {
            _creature.IsCritical = true;
            _attackCount = 0;
        }

        base.Attack(attacker, target);

        Vector3 pos = GameObject.Find("UI_PlayerCard").GetComponent<UI_PlayerCard>().GetImage((int)Images.CreatureImage).gameObject.transform.position;
        pos = new Vector3(pos.x, pos.y - 100, pos.z);

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

        GetImage((int)Images.AttackIcon).gameObject.GetComponent<Animator>().Play(Managers.Data.MonsterClassDic[_creature.Ability].Weapon);

        Managers.Sound.Play(Define.Sound.Effect, "MonsterAttack0_SFX");

        if (_totalAttackCount > 0 && _totalAttackCount % 20 == 0)
        {
            Berserk();
        }

        PlayMonsterAttackAnim();
        CreateMonsterAttackParticle();
        CreatePlayerHitParticle();
        //Managers.Game.OnBattleDataRefreshAction.Invoke();
    }

    public override void Defence()
    {
        base.Defence();
        GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play(Managers.Data.MonsterClassDic[_creature.Ability].Shield);

    }

    IEnumerator CoDelayAttack()
    {
        float maxAttackCoolTime = 3f;
        float attackCoolTime = 0f;
        maxAttackCoolTime = maxAttackCoolTime / _creature.AttackSpeed;

        while (true)
        {
            if (attackCoolTime >= maxAttackCoolTime)
            {
                attackCoolTime = 0f;
                Attack(_creature, Managers.Game.PlayerData);
            }
            attackCoolTime += Time.deltaTime * Managers.Game.GameSpeed;

            GetImage((int)Images.AttackDelayGauge).fillAmount = attackCoolTime / maxAttackCoolTime;

            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator CoDelayDefence()
    {
        _maxDefenceCoolTime = _maxDefenceCoolTime / _creature.DefenceSpeed;

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

    public void Berserk()
    {
        _creature.Attack *= 1.2f;
        _creature.AttackSpeed *= 1.2f;
        _creature.Defence *= 1.2f;
        _creature.DefenceSpeed *= 1.2f;
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
        WaitForSeconds delay = new WaitForSeconds(0.1f);
        GameObject go = Managers.Resource.Instantiate("UI_CreatureCardCopyImage", GetImage((int)Images.CreatureImage).transform);
        Image image = go.GetOrAddComponent<Image>();
        image.rectTransform.sizeDelta = GetImage((int)Images.CreatureImage).rectTransform.sizeDelta;
        Animator animator = go.GetOrAddComponent<Animator>();
        animator.runtimeAnimatorController = Managers.Resource.Load<RuntimeAnimatorController>("UIMonsterAnimController");
        animator.Play($"{_creature.IdleAnimStr}");
        image.sprite = GetImage((int)Images.CreatureImage).sprite;
        image.material = Managers.Resource.Load<Material>("PaintWhiteMat");
        image.color = Util.DefenceColor();
        float i = 0;
        while (i < 20)
        {
            //image.SetNativeSize();
            i += 1;
            image.color += new Color(0, 0, 0, -0.05f);
            yield return new WaitForSeconds(0.01f);
        }
        yield return delay;
        Destroy(go);
    }

    public override void StartDamagedMat()
    {
        StartCoroutine(CoDamagedMat());
    }

    IEnumerator CoDamagedMat()
    {
        WaitForSeconds delay = new WaitForSeconds(0.1f);
        GameObject go = Managers.Resource.Instantiate("UI_CreatureCardCopyImage", GetImage((int)Images.CreatureImage).transform);
        Image image = go.GetOrAddComponent<Image>();
        image.rectTransform.sizeDelta = GetImage((int)Images.CreatureImage).rectTransform.sizeDelta;
        Animator animator = go.GetOrAddComponent<Animator>();
        animator.runtimeAnimatorController = Managers.Resource.Load<RuntimeAnimatorController>("UIMonsterAnimController");
        animator.Play($"{_creature.IdleAnimStr}");
        image.sprite = GetImage((int)Images.CreatureImage).sprite;
        image.material = Managers.Resource.Load<Material>("PaintWhiteMat");
        image.color = Util.DamagedColor();
        float i = 0;
        while (i < 10)
        {
            //image.SetNativeSize();
            i += 1;
            image.color += new Color(0, 0, 0, -0.1f);
            yield return new WaitForSeconds(0.005f);
        }
        yield return delay;
        Destroy(go);

        //WaitForSeconds delay = new WaitForSeconds(0.1f);
        //GetImage((int)Images.CreatureImage).material = Managers.Resource.Load<Material>("PaintWhiteMat");
        //GetImage((int)Images.CreatureImage).color = Util.DamagedColor();
        //yield return delay;
        //GetImage((int)Images.CreatureImage).color = Color.white;
        //yield return delay;
        //GetImage((int)Images.CreatureImage).material = null;
        //GetImage((int)Images.CreatureImage).color = Color.white;
    }

    void CreatePlayerDeathParticle()
    {
        Transform particlePos = Managers.Game.Player.gameObject.transform;
        GameObject deathSoulPurple = Managers.Resource.Instantiate("BoneHeadBloodExplosion");
        deathSoulPurple.transform.position = particlePos.position;
        Destroy(deathSoulPurple, 10);
    }

    void CreateMonsterAttackParticle()
    {
        string battleParticleAttack = _creature.BattleParticleAttack;

        GameObject go = Managers.Resource.Instantiate(battleParticleAttack, GetImage((int)Images.CreatureImage).gameObject.transform);
    }

    void CreatePlayerHitParticle()
    {
        string hitFX = _creature.BattleParticleHit;
        GameObject player = GameObject.Find("UI_PlayerCard");
        GameObject go = Managers.Resource.Instantiate(hitFX, player.transform);
        var uiParticle = go.GetOrAddComponent<UIParticle>();

        uiParticle.scale = 50;
        uiParticle.Play();
    }

    void PlayMonsterAttackAnim()
    {
        string animStr = _creature.AttackAnimStr;
        GetImage((int)Images.CreatureImage).GetComponent<Animator>().Play(animStr);
    }

    public override void Dead()
    {
        base.Dead();

        // add exp
        Managers.Game.PlayerData.CurExp += Managers.Game.MonsterData[0].RewardExp;

        Managers.Data.MonsterActiveDic[Managers.Game.MonsterData[0].IsActiveIndex] = false;

        Managers.Game.OnBattleAction.Invoke();


        //if (Managers.Data.MonsterDic[Managers.Game.Monster.id].RewardItem != -1)
        //{
        //    GameObject item = Managers.Resource.Instantiate("EquipItem", Managers.Game.DropItems.transform);
        //    item.transform.position = Managers.Game.Monster.transform.localPosition + Vector3.back * 0.1f;
        //    item.GetComponent<Equip>()._id = Managers.Data.MonsterDic[Managers.Game.Monster.id].RewardItem;
        //}

        if(Managers.Game.Monster.GetComponent<BossMonsterController>()!= null)
        {
            //Managers.Game.OnBattle = false;
            // Call specific Boss monster dead event
            Managers.Game.Monster.GetComponent<BossMonsterController>().OnDeadEvent();
        }
        else
        {
            // 몬스터 죽는 파티클 생성
            StartCoroutine(CoDead());
        }

        return;
    }

    IEnumerator CoDead()
    {
        yield return new WaitForSeconds(0.3f);
        Managers.Sound.Play(Define.Sound.Effect, "MonsterDeath_SFX");
        Transform particlePos = Managers.Game.Monster.gameObject.transform;
        GameObject deathSoulPurple = Managers.Resource.Instantiate("DeathSoulPurple");
        deathSoulPurple.transform.position = particlePos.position;
        deathSoulPurple.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Destroy(deathSoulPurple, 3);
        Destroy(Managers.Game.Monster.gameObject.GetComponent<BoxCollider>());
        Managers.Game.Monster.gameObject.GetComponent<Animator>().Play("Stop");
        SpriteRenderer sr = Managers.Game.Monster.gameObject.GetOrAddComponent<SpriteRenderer>();
        sr.material = Managers.Resource.Load<Material>("PaintWhiteMat");
        sr.color = Util.DamagedColor();
        GameObject go = Instantiate(Managers.Game.Monster.gameObject);
        go.transform.position = Managers.Game.Monster.gameObject.transform.position;
        go.GetComponent<Animator>().Play("Stop");
        Destroy(go.GetComponent<BoxCollider>());
        Destroy(Managers.Game.Monster.gameObject);
        Destroy(go, 1);
        Destroy(sr, 1f);
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
