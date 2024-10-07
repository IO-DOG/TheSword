using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_KingSlimeCard : UI_Base
{
    #region Enum

    enum Images
    {
        CreatureImage,
        HPHar,
        HPHarGauge,
        AttackDelayGauge,
        DefenceDelayGauge,
        AttackIcon,
        DefenceIcon,
    }

    enum Texts
    {
        CreatureName,
        HPBarText,
        AttackStatusText,
        DefenceStatusText,
    }

    public enum MonsterClass
    {
        None = 0,
        Beast = 1,
        Magic = 2,
        Guard = 3,
        Immort = 4,
        Knight = 5,
        Giant = 6,
        Assassin = 7,
        Armor = 8,
    }

    #endregion

    #region Member
    public bool _isCri = false;
    public int _attackCount = 0;
    public int _totalAttackCount = 0;
    public float _maxDefenceCoolTime = 3f;
    public float _defenceCoolTime = 0f;
    public MonsterClass _monsterClass = MonsterClass.None;
    public bool _forBeast = false;
    public bool _forGuard = false;
    public int _forGiant = 0;
    public float _forArmor = 0;
    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        _monsterClass = (MonsterClass)Managers.Game.MonsterData.Feature;
        GetText((int)Texts.CreatureName).text = Managers.Game.MonsterData.Name;
        GetText((int)Texts.HPBarText).text = Managers.Game.MonsterData.MaxHP.ToString();
        GetText((int)Texts.AttackStatusText).text = Managers.Game.MonsterData.Attack.ToString();
        GetText((int)Texts.DefenceStatusText).text = Managers.Game.MonsterData.Defence.ToString();
        GetImage((int)Images.CreatureImage).gameObject.GetComponent<Animator>().Play($"{Managers.Game.MonsterData.IdleAnimStr}");

        Managers.Game.OnBattleDataRefreshAction -= Refresh;
        Managers.Game.OnBattleDataRefreshAction += Refresh;
        Managers.Game.OnBattleCreatureDefeceAction += ClearDefence;
        Managers.Game.OnBattleCreatureDamagedAction += StartDamagedMat;

        StartCoroutine(CoDelayAttack());
        if (_monsterClass != MonsterClass.Armor)
            StartCoroutine(CoDelayDefence());

        //GetImage((int)Images.CreatureImage).SetNativeSize();

        if (_monsterClass == MonsterClass.Guard)
        {
            Defence();
            Debug.Log("수호 효과 발동");
        }

        if (_monsterClass == MonsterClass.Armor)
        {
            _forArmor = Managers.Game.MonsterData.Defence;
            Debug.Log("갑옷 효과 발동");
        }

        return true;
    }

    public void Refresh()
    {
        StartCoroutine(CoRefresh());
    }

    IEnumerator CoRefresh()
    {
        if (_monsterClass == MonsterClass.Beast && _forBeast == false && Managers.Game.MonsterData.CurHP <= Managers.Game.MonsterData.MaxHP * 0.1)
        {
            _forBeast = true;
            Managers.Game.MonsterData.CurHP += Managers.Game.MonsterData.MaxHP * 0.4f;
            Debug.Log("비스트 효과 발동");
        }

        if (_monsterClass == MonsterClass.Armor)
        {
            if (_forArmor > 0)
            {
                Debug.Log("갑옷 효과 발동 방어막부터 감소");
                float gap = Managers.Game.MonsterData.MaxHP - Managers.Game.MonsterData.CurHP;
                if (_forArmor >= gap)
                {
                    Managers.Game.MonsterData.CurHP = Managers.Game.MonsterData.MaxHP;
                    _forArmor -= gap;
                }
                else
                {
                    Managers.Game.MonsterData.CurHP += (gap - _forArmor);
                    _forArmor = 0;
                }
            }
        }

        //GetImage((int)Images.CreatureImage).SetNativeSize();
        GetText((int)Texts.HPBarText).text = Managers.Game.MonsterData.CurHP.ToString();
        GetImage((int)Images.HPHar).fillAmount = Managers.Game.MonsterData.CurHP / Managers.Game.MonsterData.MaxHP;
        yield return new WaitForSeconds(0.2f);
        GetImage((int)Images.HPHarGauge).fillAmount = Managers.Game.MonsterData.CurHP / Managers.Game.MonsterData.MaxHP;
    }

    public void Attack()
    {
        if (_monsterClass == MonsterClass.Magic)
        {
            _isCri = true;
            Debug.Log("마법 효과 발동");
        }

        GetImage((int)Images.AttackIcon).gameObject.GetComponent<Animator>().Play("UIAttackIcon");

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
                Managers.Game.CurPlayerData.CurHP -= Mathf.Max(0, Managers.Game.MonsterData.Attack * (Managers.Game.MonsterData.CriticalAttack / 100) - Managers.Game.CurPlayerData.Defence) * 0.2f;
                _isCri = false;
            }
        }
        else
        {
            if (_isCri)
            {
                Managers.Game.CurPlayerData.CurHP -= Mathf.Max(0, Managers.Game.MonsterData.Attack * (Managers.Game.MonsterData.CriticalAttack / 100) - Managers.Game.CurPlayerData.Defence);
                Managers.Game.OnBattlePlayerDamagedAction.Invoke();
                _isCri = false;
            }
            else
            {
                Managers.Game.CurPlayerData.CurHP -= Mathf.Max(0, Managers.Game.MonsterData.Attack - Managers.Game.CurPlayerData.Defence);
                Managers.Game.OnBattlePlayerDamagedAction.Invoke();
            }
        }
        Managers.Game.CurPlayerData.CurHP = Mathf.RoundToInt(Managers.Game.CurPlayerData.CurHP);

        if (Managers.Game.CurPlayerData.CurHP <= 0)
        {
            // Game Over Popup TODO
            CreatePlayerDeathParticle();
            Managers.Game.OnBattleAction.Invoke();
            Managers.Game.OnBattle = false;
            return;
        }

        _attackCount++;
        PlayMonsterAttackAnim();
        CreateMonsterAttackParticle();
        CreatePlayerHitParticle();
        Managers.Game.OnBattleDataRefreshAction.Invoke();
    }

    public void Defence()
    {
        _defenceCoolTime = _maxDefenceCoolTime;
        GetImage((int)Images.DefenceDelayGauge).fillAmount = _defenceCoolTime / _maxDefenceCoolTime;

        GetImage((int)Images.DefenceIcon).gameObject.GetComponent<Animator>().Play("UIDefenceIcon");
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
                if (_monsterClass == MonsterClass.Knight)
                {
                    Attack();
                    Debug.Log("검사 효과 발동");
                }
            }
            attackCoolTime += Time.deltaTime * Managers.Game.GameSpeed;

            GetImage((int)Images.AttackDelayGauge).fillAmount = attackCoolTime / maxAttackCoolTime;

            yield return new WaitForFixedUpdate();
        }
    }

    public bool _defenseFlag = false;
    IEnumerator CoDelayDefence()
    {

        _maxDefenceCoolTime = _maxDefenceCoolTime / Managers.Game.MonsterData.DefenceSpeed;

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

    public void Berserk()
    {
        Managers.Game.MonsterData.Attack *= 1.2f;
        Managers.Game.MonsterData.AttackSpeed *= 1.2f;
        Managers.Game.MonsterData.Defence *= 1.2f;
        Managers.Game.MonsterData.DefenceSpeed *= 1.2f;
    }

    public void ClearDefence()
    {
        // TODO play damaged anim
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
        WaitForSeconds delay = new WaitForSeconds(0.1f);
        GameObject go = Managers.Resource.Instantiate("UI_CreatureCardCopyImage", GetImage((int)Images.CreatureImage).transform);
        Image image = go.GetOrAddComponent<Image>();
        image.rectTransform.sizeDelta = GetImage((int)Images.CreatureImage).rectTransform.sizeDelta;
        Animator animator = go.GetOrAddComponent<Animator>();
        animator.runtimeAnimatorController = Managers.Resource.Load<RuntimeAnimatorController>("UIMonsterAnimController");
        animator.Play($"{Managers.Game.MonsterData.IdleAnimStr}");
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
        animator.Play($"{Managers.Game.MonsterData.IdleAnimStr}");
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
        string battleParticleAttack = Managers.Game.MonsterData.BattleParticleAttack;

        GameObject go = Managers.Resource.Instantiate(battleParticleAttack, GetImage((int)Images.CreatureImage).gameObject.transform);
    }

    void CreatePlayerHitParticle()
    {
        string battleParticleHit = Managers.Game.MonsterData.BattleParticleHit;
        GameObject player = GameObject.Find("CreatureImage");
        GameObject go = Managers.Resource.Instantiate(battleParticleHit, player.transform);
    }

    void PlayMonsterAttackAnim()
    {
        string animStr = Managers.Game.MonsterData.AttackAnimStr;
        GetImage((int)Images.CreatureImage).GetComponent<Animator>().Play(animStr);

    }
}