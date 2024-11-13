using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        Managers.Game.OnBattleDataRefreshAction -= Refresh;
        Managers.Game.OnBattleDataRefreshAction += Refresh;
        Managers.Game.OnBattleCreatureDefeceAction += ClearDefence;
        Managers.Game.OnBattleCreatureDamagedAction += StartDamagedMat;
        Managers.Game.OnHitMonsterAction[0] -= Refresh;
        Managers.Game.OnHitMonsterAction[0] += Refresh;
        Managers.Game.OnDeadMonsterAction[0] -= Dead;
        Managers.Game.OnDeadMonsterAction[0] += Dead;

        StartCoroutine(CoDelayAttack());

        //if (_monsterClass != MonsterClass.Armor)
        //    StartCoroutine(CoDelayDefence());

        //GetImage((int)Images.CreatureImage).SetNativeSize();

        //if (_monsterClass == MonsterClass.Guard)
        //{
        //    Defence();
        //    Debug.Log("수호 효과 발동");
        //}

        //if (_monsterClass == MonsterClass.Armor)
        //{
        //    _forArmor = _creature.Defence;
        //    Debug.Log("갑옷 효과 발동");
        //}

        return true;
    }

    public override void Refresh()
    {
        base.Refresh();
    }

    IEnumerator CoRefresh()
    {
        //GetImage((int)Images.CreatureImage).SetNativeSize();
        GetText((int)Texts.HPBarText).text = _creature.CurHP.ToString();
        GetImage((int)Images.HPHar).fillAmount = _creature.CurHP / _creature.MaxHP;
        yield return new WaitForSeconds(0.2f);
        GetImage((int)Images.HPHarGauge).fillAmount = _creature.CurHP / _creature.MaxHP;
    }

    public override void Attack(CreatureData attacker, CreatureData target)
    {
        base.Attack(attacker, target);

        GetImage((int)Images.AttackIcon).gameObject.GetComponent<Animator>().Play("UIAttackIcon");

        if (_totalAttackCount > 0 && _totalAttackCount % 20 == 0)
        {
            Berserk();
        }

        if (_attackCount == _creature.Critical)
        {
            _creature.ISCritical = true;
            _attackCount = 0;
        }

        _attackCount++;
        PlayMonsterAttackAnim();
        CreateMonsterAttackParticle();
        CreatePlayerHitParticle();
        Managers.Game.OnBattleDataRefreshAction.Invoke();
    }

    public override void Defence()
    {
        base.Defence();
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
                //if (_monsterClass == MonsterClass.Knight)
                {
                    Attack(_creature, Managers.Game.PlayerData);
                    Debug.Log("검사 효과 발동");
                }
            }
            attackCoolTime += Time.deltaTime * Managers.Game.GameSpeed;

            GetImage((int)Images.AttackDelayGauge).fillAmount = attackCoolTime / maxAttackCoolTime;

            yield return new WaitForFixedUpdate();
        }
    }

    //public bool _defenseFlag = false;
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
        //// TODO play damaged anim
        //if (GetImage((int)Images.DefenceIcon) != null)
        //    GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play("UIShieldFX");
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

    public void StartDamagedMat()
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
        string battleParticleHit = _creature.BattleParticleHit;
        GameObject player = GameObject.Find("CreatureImage");
        GameObject go = Managers.Resource.Instantiate(battleParticleHit, player.transform);
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

        int id = Managers.Game.Monster.id;
        Debug.Log($"Monster Id : {id}");
        string name = Managers.Data.MonsterDic[id].Name;
        switch (id)
        {
            case Define.KingSlime:
                BlackSlimeController blackSlimeController = Managers.Game.Monster.gameObject.GetOrAddComponent<BlackSlimeController>();
                blackSlimeController.Dead();
                break;
            default:
                break;
        }

        // for king slime
        if (Managers.Game.Monster.gameObject.name == "KingSlimeSplitMonster")
        {
            Managers.Game.TotalKillSplitSlime++;
            if (Managers.Game.TotalKillSplitSlime == 3)
                Managers.Game.OnKingSlimeDeadAction.Invoke();
        }

        //StartCoroutine(CoMonsterDead());
        Managers.Game.OnBattleAction.Invoke();
        Managers.Game.OnBattle = false;

        // 몬스터 죽는 파티클 생성
        Transform particlePos = Managers.Game.Monster.gameObject.transform;
        GameObject deathSoulPurple = Managers.Resource.Instantiate("DeathSoulPurple");
        deathSoulPurple.transform.position = particlePos.position;
        deathSoulPurple.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        deathSoulPurple.GetComponentsInChildren<ParticleSystem>()[0].startDelay = 0.2f;
        deathSoulPurple.GetComponentsInChildren<ParticleSystem>()[1].startDelay = 0.2f;
        deathSoulPurple.GetComponentsInChildren<ParticleSystem>()[2].startDelay = 0.2f;
        Destroy(deathSoulPurple, 3);
        Destroy(Managers.Game.Monster.gameObject);
        return;

    }

    private void OnDestroy()
    {
        Managers.Game.OnDeadMonsterAction[0] -= Dead;
        Managers.Game.OnHitMonsterAction[0] -= Refresh;
    }
}
