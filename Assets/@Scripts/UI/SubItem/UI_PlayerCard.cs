using Coffee.UIExtensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerCard : UI_Base
{
    #region Enum

    enum Images
    {
        PlayerImage,
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

    #endregion

    public bool _isCri = false;
    public float _maxDefenceCoolTime = 3f;
    public float _defenceCoolTime = 0f;
    public bool _forAssassin = false;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        // todo change to nickname
        GetText((int)Texts.CreatureName).text = "Player!";
        GetText((int)Texts.HPBarText).text = Managers.Game.CurPlayerData.CurHP.ToString();
        GetText((int)Texts.AttackStatusText).text = Managers.Game.CurPlayerData.Attack.ToString();
        GetText((int)Texts.DefenceStatusText).text = Managers.Game.CurPlayerData.Defence.ToString();

        Managers.Game.OnBattleDataRefreshAction -= Refresh;
        Managers.Game.OnBattleDataRefreshAction += Refresh;
        Managers.Game.OnBattlePlayerDefeceAction += ClearDefence;
        Managers.Game.OnBattlePlayerDamagedAction += StartDamagedMat;

        StartCoroutine(CoDelayAttack());
        StartCoroutine(CoDelayDefence());

        if (Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Shield].Count == 0)
        {
            GetImage((int)Images.CreatureShieldImage).gameObject.SetActive(false);
        }

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
        Managers.Game.AttackCount++;

        if (Managers.Game.MonsterData.Feature == 6)
        {
            Managers.Game.MonsterData.DamagedCount++;
            if (Managers.Game.MonsterData.DamagedCount == 5)
            {
                Debug.Log("거대 효과 발동");
                Managers.Game.CurPlayerData.CurHP -= Mathf.Max(0, Managers.Game.MonsterData.Attack - Managers.Game.CurPlayerData.Defence) * 0.2f;
                Refresh();
            }
        }
        GetImage((int)Images.PlayerImage).gameObject.GetComponent<Animator>().Play("UIPlayerAttackAnim");
        GetImage((int)Images.CreatureSwordImage).gameObject.GetComponent<Animator>().Play($"UISword{Managers.Game.CurPlayerData.CurSword - 9}AttackAnim");
        if (Managers.Game.CurPlayerData.CurShield != 0)
            GetImage((int)Images.CreatureShieldImage).gameObject.GetComponent<Animator>().Play($"UIShield{Managers.Game.CurPlayerData.CurShield - 20}AttackAnim");
        GetImage((int)Images.AttackIcon).gameObject.GetComponent<Animator>().Play("UIAttackIcon");

        if (Managers.Game.AttackCount == Managers.Game.CurPlayerData.Critical)
        {
            _isCri = true;
            _forAssassin = true;
            Managers.Game.AttackCount = 0;
        }

        // 몬스터가 암살일 경우
        if (Managers.Game.MonsterData.Feature == 7 && _forAssassin == false)
        {
            Debug.Log("암살 효과 발동");
            return;
        }

        // 몬스터가 방어 상태일 경우
        if (Managers.Game.MonsterData.IsDefence == true)
        {
            Managers.Game.MonsterData.IsDefence = false;
            Managers.Game.OnBattleCreatureDefeceAction.Invoke();

            if (_isCri == true)
            {
                Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack * (Managers.Game.CurPlayerData.CriticalAttack / 100) - Managers.Game.MonsterData.Defence) * 0.2f;
                _isCri = false;
            }
        }
        else // 일반 공격
        {
            if (_isCri)
            {
                // 몬스터가 불사 효과일 경우
                if (Managers.Game.MonsterData.Feature == 4)
                {
                    Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack * (Managers.Game.CurPlayerData.CriticalAttack / 100) - Managers.Game.MonsterData.Defence) * 20;
                    Managers.Game.OnBattleCreatureDamagedAction.Invoke();
                    Debug.Log("불사 효과 발동 치명 데미지 200퍼");
                }
                else
                {
                    Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack * (Managers.Game.CurPlayerData.CriticalAttack / 100) - Managers.Game.MonsterData.Defence);
                    Managers.Game.OnBattleCreatureDamagedAction.Invoke();
                }
                _isCri = false;
            }
            else
            {
                // 몬스터가 불사 효과일 경우
                if (Managers.Game.MonsterData.Feature == 4)
                {
                    Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack * (Managers.Game.CurPlayerData.CriticalAttack / 100) - Managers.Game.MonsterData.Defence) * 0.2f;
                    Managers.Game.OnBattleCreatureDamagedAction.Invoke();
                    Debug.Log("불사 효과 발동 일반 데미지 20퍼");
                }
                else
                {
                    Managers.Game.MonsterData.CurHP -= Mathf.Max(0, Managers.Game.CurPlayerData.Attack - Managers.Game.MonsterData.Defence);
                    Managers.Game.OnBattleCreatureDamagedAction.Invoke();
                }
            }
        }
        Managers.Game.MonsterData.CurHP = Mathf.RoundToInt(Managers.Game.MonsterData.CurHP);

        if (Managers.Game.MonsterData.CurHP <= 0)
        {
            // add exp
            Managers.Game.CurPlayerData.CurExp += Managers.Game.MonsterData.RewardExp;

            Managers.Data.MonsterActiveDic[Managers.Game.MonsterData.IsActiveIndex] = false;

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

        CreatePlayerAttackParticle();
        CreateMonsterHitParticle();
        Managers.Game.OnBattleDataRefreshAction.Invoke();
    }

    //IEnumerator CoMonsterDead()
    //{
    //    // 몬스터 죽는 파티클 생성

    //    yield return new WaitForSeconds(2f);

    //    Transform particlePos = Managers.Game.Monster.gameObject.transform;
    //    GameObject deathSoulPurple = Managers.Resource.Instantiate("DeathSoulPurple");
    //    deathSoulPurple.transform.position = particlePos.position;
    //    deathSoulPurple.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
    //    Destroy(deathSoulPurple, 3);
    //    Destroy(Managers.Game.Monster.gameObject);
    //}

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

    public bool _defenseFlag = false;
    IEnumerator CoDelayDefence()
    {
        _maxDefenceCoolTime = _maxDefenceCoolTime / Managers.Game.CurPlayerData.AttackSpeed;

        while (true)
        {
            if (_defenceCoolTime >= _maxDefenceCoolTime)
            {
                if (_defenseFlag == false)
                {
                    _defenseFlag = true;
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

    public void ClearDefence()
    {
        //// TODO play damaged anim
        //if (GetImage((int)Images.DefenceIcon) != null)
        //    GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play("UIShieldFX");
        StartCoroutine(CoStartShieldFX());
        StartCoroutine(CoDefenceMat());
        _defenceCoolTime = 0f;
        _defenseFlag = false;
        if (GetImage((int)Images.DefenceIcon) != null)
            GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play("UIIdleDefense");
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
        GameObject go = Managers.Resource.Instantiate("UI_PlayerCardCopyImage", GetImage((int)Images.PlayerImage).transform);
        go.transform.position = GetImage((int)Images.PlayerImage).transform.position;
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
        swordanimator.Play($"UISword{Managers.Game.CurPlayerData.CurSword - 9}IdleAnim");
        if (Managers.Game.CurPlayerData.CurShield != 0)
            shieldanimator.Play($"UIShield{Managers.Game.CurPlayerData.CurShield - 20}IdleAnim");
        image.sprite = GetImage((int)Images.PlayerImage).sprite;
        swordImage.sprite = GetImage((int)Images.PlayerImage).sprite;
        shieldImage.sprite = GetImage((int)Images.PlayerImage).sprite;
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

    public void StartDamagedMat()
    {
        StartCoroutine(CoDamagedMat());
    }

    IEnumerator CoDamagedMat()
    {
        int width = 660;
        int height = 660;

        WaitForSeconds delay = new WaitForSeconds(0.1f);
        GameObject go = Managers.Resource.Instantiate("UI_PlayerCardCopyImage", GetImage((int)Images.PlayerImage).transform);
        Image image = go.GetOrAddComponent<Image>();
        image.rectTransform.sizeDelta = GetImage((int)Images.PlayerImage).rectTransform.sizeDelta;
        Animator animator = go.GetOrAddComponent<Animator>();
        animator.runtimeAnimatorController = Managers.Resource.Load<RuntimeAnimatorController>("UIPlayerAnimController");
        animator.Play($"UIPlayerIdleAnim");
        image.sprite = GetImage((int)Images.PlayerImage).sprite;
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
        int swordId = Managers.Game.CurPlayerData.CurSword;
        string attackFX = Managers.Data.EquipDic[swordId].AttackFX;
        GameObject player = GameObject.Find("PlayerImage");
        GameObject go = Managers.Resource.Instantiate(attackFX, GetImage((int)Images.PlayerImage).transform);
        go.transform.localPosition += new Vector3(0, -150, 0);
        var uiParticle = go.GetOrAddComponent<UIParticle>();
        uiParticle.scale = 300;
        uiParticle.Play();

        //Destroy(uiParticle, 0.3f);
    }

    void CreateMonsterHitParticle()
    {
        int swordId = Managers.Game.CurPlayerData.CurSword;
        string hitFX = Managers.Data.EquipDic[swordId].HitFX;
        GameObject monster = GameObject.Find("CreatureImage");
        GameObject go = Managers.Resource.Instantiate(hitFX, monster.transform);
        go.transform.position += new Vector3(0, -0, 0);
        var uiParticle = go.GetOrAddComponent<UIParticle>();

        //var childrenUIParticle = go.GetComponentsInChildren<UIParticle>()[1]; // 이거 좀 위험한 코드임.
        uiParticle.scale = 30;
        //childrenUIParticle.scale = 300;
        //Debug.Log($"childrenUIParticle.gameObject.name : {childrenUIParticle.gameObject.name}");
        uiParticle.Play();
        //childrenUIParticle.Play();
        //Destroy(uiParticle, 0.3f);
    }
}
