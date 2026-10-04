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
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        Animator anim = GetImage((int)Images.CreatureImage).gameObject.GetComponent<Animator>();
        anim.Play($"{_creature.IdleAnimStr}");
        // Play 만으로는 이 프레임에 스프라이트가 바뀌지 않는다. 그대로 크기를 재면
        // 프리팹에 박혀 있던 기본 스프라이트를 재게 되고, 보스는 계속 잘린다.
        anim.Update(0f);
        FitCreatureImage(GetImage((int)Images.CreatureImage));
        // 맵에서 본 그 색 그대로 전투창에도 올린다. 둘이 다르면 같은 몬스터로 안 보인다.
        GameManager.CurMonsterData mdata = _creature as GameManager.CurMonsterData;
        if (mdata != null)
            GetImage((int)Images.CreatureImage).color = MonsterTint.Of(mdata.id);

        Refresh();
        _creature.OnDefenceAction += ClearDefence;
        _creature.OnHitAction += Refresh;
        _creature.OnHitAction += StartDamagedMat;
        _creature.OnHitAction += CheckEnrage;
        _creature.OnDeadAction += Dead;
        _creature.OnDataRefreshAction += Refresh;

        //SpriteAtlas spriteAtlas = Managers.Resource.Load<SpriteAtlas>("BattleUI_Weppon2");

        //GetImage((int)Images.AttackIcon).sprite = spriteAtlas.GetSprite("BattleUI_Weppon2_0");

        // 챕터 보스·킹 슬라임 (UI_BattlePopup._boss 와 같은 셈). 분열 슬라임은 보스 연출은 쓰지만 우두머리가 아니다.
        MonsterController fought = Managers.Game.Monster;
        _boss = fought != null && (fought.CompareTag("Boss") || fought is KingSlimeController);
        if (_boss)
            PushIn();

        return true;
    }

    #region 우두머리 연출 (L8)
    // 카드의 크기·색·흔들림·소리뿐이다 — 전투 시계(BattleStepper)는 모른다. 예측은 그대로 전투와 같다.
    // 메뉴가 열려 시간이 멈추면(Time.timeScale 0) 트윈도 같이 멈춘다. 봇이 돌 때도 그대로 튼다 — 걸음을 늦추지 않는다.
    const float PushInSeconds = 0.5f;      // 싸움이 열릴 때 카드가 다가온다
    const float PushInFrom = 0.8f;
    const float EnrageAt = 0.5f;           // 체력이 이 아래로 처음 내려간 한 대에 한 번 성낸다
    const float PulseSeconds = 0.25f;      // 붉게 두 번 (한 번 오가는 데 0.5초 — 초당 세 번 넘게 번쩍이지 않는다)
    // ponytail: 이미 있는 부풂 소리(2.2초)를 낮게 튼 대역이다 — 보스 울음(기획 §8 "5 boss roars")이 오면 이 키만 바꾼다.
    const string EnrageSound = "MainTitle_Impact";
    const float EnragePitch = 0.8f;        // 전투 배속과 상관없이 늘 이 높이다 (PlayEnrageSound)
    // 그림 색(MonsterTint)을 이쪽으로 당긴다. 곱하면 푸른 챕터의 보스가 붉어지지 않고 어두워지기만 한다.
    static readonly Color Blood = new Color(1f, 0.15f, 0.1f);

    bool _boss;
    bool _enraged;

    void PushIn()
    {
        Vector3 to = transform.localScale;     // 전투창이 정한 카드 배율
        transform.localScale = to * PushInFrom;
        transform.DOScale(to, PushInSeconds).SetEase(Ease.OutCubic).SetLink(gameObject);
    }

    // 맞을 때마다(OnHitAction). 전투 시계의 한 걸음(BattleStepper.Strike) 안에서 불린다 — 여기서 예외가 나면 그 걸음이
    // 도중에 끊겨 예측과 어긋난다. 연출은 무엇이든 삼킨다. 건너뛰는 중(Quiet)에는 틀지 않는다(보스는 못 건너뛴다).
    // 야수가 광폭해 다시 절반 위로 올라와도 한 번뿐이다.
    void CheckEnrage()
    {
        if (_boss == false || _enraged || Quiet)
            return;
        if (_creature.CurHP <= 0 || _creature.CurHP > _creature.MaxHP * EnrageAt)
            return;
        _enraged = true;
        try
        {
            Image img = GetImage((int)Images.CreatureImage);
            if (img != null)
            {
                Color calm = img.color;
                img.DOColor(Color.Lerp(calm, Blood, 0.7f), PulseSeconds).SetLoops(4, LoopType.Yoyo).SetLink(img.gameObject)
                    .OnComplete(() => img.color = Color.Lerp(calm, Blood, 0.25f));   // 성난 기색이 남는다
            }
            Shake();    // 화면 흔들림을 끈 사람에게는 흔들지 않는다 (UI_BaseCard.Shake)
            PlayEnrageSound();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // 효과음 소스는 하나라 그 높이는 나중에 튼 소리가 정하고, 이미 울리는 소리까지 따라 바뀐다. 이 소리는 맞는 걸음 안에서
    // 나고 같은 걸음 뒤에 플레이어 카드가 타격 소리를 배속 높이(SoundManager.BattlePitch, 기본 2배속에서 1.3)로 내서,
    // 낮게 튼 울음이 늘 새되게 났다. 그래서 카드에 제 소스를 단다 — 크기는 효과음 설정을 따르고, 카드와 함께 사라진다.
    void PlayEnrageSound()
    {
        AudioClip clip = Managers.Resource.Load<AudioClip>(EnrageSound);
        if (clip == null)
            return;
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.pitch = EnragePitch;
        source.volume = Managers.Sound.EffectVolume;
        source.PlayOneShot(clip);
    }
    #endregion

    public override void Refresh()
    {
        base.Refresh();
    }

    public override void PresentAttack(CreatureData attacker, CreatureData target,
        int damage, bool critical, bool guarded)
    {
        base.PresentAttack(attacker, target, damage, critical, guarded);

        Vector3 pos = GameObject.Find("UI_PlayerCard").GetComponent<UI_PlayerCard>().GetImage((int)Images.CreatureImage).gameObject.transform.position;
        pos = new Vector3(pos.x, pos.y - 100f * transform.parent.lossyScale.y, pos.z);   // 캔버스 100 칸 (UI_PlayerCard 와 같다)

        GameObject go = GameObject.Find("UI_BattlePopup");
        if (go != null)
        {
            Managers.Object.ShowDamageFont(pos, _hitDamage, 0, go.transform, critical, guarded);
        }

        GetImage((int)Images.AttackIcon).gameObject.GetComponent<Animator>().Play(Managers.Data.MonsterClassDic[_creature.Ability].Weapon);

        Managers.Sound.PlayByGameSpeed(Define.Sound.Effect, "MonsterAttack0_SFX");

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

    public override void ClearDefence()
    {
        if (Quiet == false)
        {
            Managers.Sound.PlayByGameSpeed(Define.Sound.Effect, "Defense_SFX");

            StartCoroutine(CoStartShieldFX());
            StartCoroutine(CoDefenceMat());
        }
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
        if (Quiet)
            return;
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
        // 이 키는 어드레서블에 없다 — Instantiate 가 null 을 준다.
        // 그대로 .transform 을 만지면 여기서 널참조가 나고, <b>플레이어가 죽는
        // 연출이 통째로 끊긴다.</b> 이펙트가 없어도 게임은 굴러가야 한다.
        GameObject deathSoulPurple = Managers.Resource.Instantiate("BoneHeadBloodExplosion");
        if (deathSoulPurple == null)
            return;

        deathSoulPurple.transform.position = Managers.Game.Player.gameObject.transform.position;
        Destroy(deathSoulPurple, 10);
    }

    // 몬스터의 베기. 색이 특성이다(generate_content.TRAIT_FX). 예전에는 UIParticle 없이 띄워서 오버레이 캔버스에
    // 그려지지 않았다 — 칼 소리만 났다. 플레이어의 베기(UI_PlayerCard.CreatePlayerAttackParticle)와 같은 화면 크기를
    // 좌우만 바꾼 자리에 띄운다. UIParticle 은 부모의 배율을 곱한다 — 플레이어 그림은 0.2 배, 몬스터 그림은 1 배라서
    // 270 x 0.2 = 54, 자리도 (-50,-50) x 0.2 를 뒤집은 (10,-10) 이다.
    // 자리는 더하지 않고 못박는다. 프리팹마다 뿌리 자리가 제각각이라(00 은 (0,-200), 05 는 (3.65,0)) 더하면
    // 회색 베기만 그림 아래 900 픽셀, 화면 밖에 떴다.
    // ponytail: 베기 그림은 좌우로 뒤집지 않는다 — 빗금 한 줄이라 방향이 읽히지 않는다. 거슬리면 ParticleSystemRenderer.flip.
    const float SlashScale = 54f;
    static readonly Vector3 SlashAt = new Vector3(10f, -10f, 0f);

    void CreateMonsterAttackParticle()
    {
        Image img = GetImage((int)Images.CreatureImage);
        GameObject go = SpawnFX(_creature.BattleParticleAttack, img != null ? img.transform : null);
        if (go == null)
            return;
        go.transform.localPosition = SlashAt;
        var uiParticle = go.GetOrAddComponent<UIParticle>();
        uiParticle.scale = SlashScale;
        uiParticle.Play();
    }

    void CreatePlayerHitParticle()
    {
        GameObject player = GameObject.Find("UI_PlayerCard");
        if (player == null)
            return;
        // 이펙트가 없어도(데이터의 "-") 전투는 굴러가야 한다 — 여기서 터지면 전투 시계의 그 걸음이 도중에 끊긴다.
        GameObject go = SpawnFX(_creature.BattleParticleHit, player.transform);
        if (go == null)
            return;
        go.transform.localPosition = Vector3.zero;   // 카드 가운데. 02·05 는 프리팹 뿌리가 옆으로(02 는 z -12.9) 비껴 있다
        var uiParticle = go.GetOrAddComponent<UIParticle>();

        uiParticle.scale = HitScaleOf();
        uiParticle.Play();
    }

    // 맞는 이펙트의 크기가 서열이다 — 모양과 색은 특성(generate_content.TRAIT_FX)이 정하고, 정예·우두머리는 같은 것을
    // 크게 친다. 맵에서 몸집(MapBuilder.MonsterBulk)으로 가르듯 전투창에서도 가른다. 졸개 50 은 예전 그대로다.
    const float MobHitScale = 50f, EliteHitScale = 70f, BossHitScale = 75f;

    float HitScaleOf()
    {
        if (_boss)
            return BossHitScale;
        CurMonsterData md = _creature as CurMonsterData;
        return md != null && MapBuilder.IsElite(md.id) ? EliteHitScale : MobHitScale;
    }

    // 데이터의 이펙트 키로 만든다. 빈 키("-"·null)는 "없음" 이다 — null 키는 Instantiate 의 사전 조회가 예외를 던지고,
    // 여기는 전투 시계의 한 걸음 안(OnStrike)이라 그 걸음이 끊긴다.
    static GameObject SpawnFX(string key, Transform parent)
    {
        if (string.IsNullOrEmpty(key) || key == "-" || parent == null)
            return null;
        return Managers.Resource.Instantiate(key, parent);
    }

    void PlayMonsterAttackAnim()
    {
        string animStr = _creature.AttackAnimStr;
        GetImage((int)Images.CreatureImage).GetComponent<Animator>().Play(animStr);
    }

    bool _deadHandled = false;

    public override void Dead()
    {
        // OnDeadAction 이 두 번 들어온다. 그대로 두면 경험치가 두 배로 들어가고
        // (1층만 돌아도 Lv4 가 됐다 — 층당 1레벨이라는 설계가 깨진다)
        // 보상도 두 개씩 떨어져 한 칸에 쌓인다.
        if (_deadHandled)
            return;
        _deadHandled = true;

        base.Dead();

        // add exp
        Managers.Game.PlayerData.CurExp += Managers.Game.MonsterData[0].RewardExp;

        Managers.Data.MonsterActiveDic[Managers.Game.MonsterData[0].IsActiveIndex] = false;

        // 방금 잡은 게 보스라면 그 층의 위층 계단이 열린다.
        Managers.Game.RefreshBossGates();

        Managers.Game.OnBattleAction.Invoke();


        DropReward();

        // 방금 잡은 놈을 표시해 둔다. 보스는 연출이 끝날 때까지 켜져 있어서
        // 표시가 없으면 그 사이에 다시 부딪혀 두 번 싸운다.
        // 파괴된 오브젝트일 수도 있어서 (연출이 이미 끝난 경우) 반드시 검사한다.
        MonsterController fought = Managers.Game.Monster;
        if (fought != null)
            fought.MarkDead();

        // 우두머리를 쓰러뜨렸다는 알림(GameEvents.BossDefeated)은 여기서 내지 않는다 — UI_BattlePopup.CoBattleEnd.
        BossMonsterController boss = (fought != null) ? fought.GetComponent<BossMonsterController>() : null;
        if (boss != null)
        {
            //Managers.Game.OnBattle = false;
            // Call specific Boss monster dead event
            boss.OnDeadEvent();
        }
        else
        {
            // 몬스터 죽는 파티클 생성
            StartCoroutine(CoDead());
        }

        return;
    }

    /// <summary>
    /// 잡은 몬스터가 보상 아이템을 떨군다.
    ///
    /// 기획서 34·107쪽 — 장비는 맵 바닥에 떨어진 상태로 남고, 지나가면 줍는다.
    /// 원래 코드가 있었지만 주석 처리되어 있어서 아무것도 떨어지지 않았다.
    /// 떨군 것은 체크포인트에 없다 — 줍지 않고 층을 떠나면 GameManager.CollectDrops 가 대신 줍는다.
    /// </summary>
    void DropReward()
    {
        CurMonsterData mdata = _creature as CurMonsterData;
        if (mdata == null)
            return;

        // EquipData 0 번은 아이템이 아니라 "빈 자리표"다 (이름 "-", 스탯 전부 0,
        // 분류는 무기). 그걸 떨구면 줍는 순간 무기 칸이 자리표로 교체되고,
        // 무기 파티클도 "-" 라서 공격 코루틴이 예외로 죽는다. 1층 라임 슬라임의
        // 보상이 0 이라 실제로 1층에서 게임이 끝났다.
        if (mdata.RewardItem <= 0)
            return;
        if (Managers.Data.EquipDic.ContainsKey(mdata.RewardItem) == false)
            return;

        MonsterController mon = Managers.Game.Monster;
        if (mon == null)
            return;

        GameObject item = Managers.Resource.Instantiate("EquipItem", Managers.Game.DropItems.transform);
        Equip equip = Util.Find<Equip>(item);
        if (equip == null)
            return;

        // 죽은 몬스터가 서 있던 칸에 놓되, 높이는 맵에 원래 놓인 아이템에서 그대로
        // 가져온다. 몬스터의 y(=0)에 두면 밀기 광선(타일 절반 높이에서 수평으로
        // 나간다)이 스치지 못해서, 줍지도 못하는데 길은 막는 장애물이 된다.
        Vector3 pos = mon.transform.position;
        pos.y = ItemHeight(mon);
        item.transform.position = pos;

        equip._id = mdata.RewardItem;
        equip._itemIndex_forActive = -1;   // 맵 데이터에 없는 물건이라는 표시
    }

    /// <summary>맵에 이미 놓인 아이템의 높이. 숫자를 박아 두지 않고 실물에서 가져온다.</summary>
    static float ItemHeight(MonsterController mon)
    {
        GameObject map = Managers.Game.ParentMap;
        if (map != null)
        {
            ConsumableItem sample = map.GetComponentInChildren<ConsumableItem>(true);
            if (sample != null)
                return sample.transform.position.y;
        }
        return mon.transform.position.y + Define.TILE_SIZE / 2f;
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
        // 콜라이더는 루트가 아니라 자식에 붙어 있다 (MapBuilder.FitColliderToCell 도
        // GetComponentInChildren 으로 찾는다). 루트만 보면 못 찾아 그대로 남는다.
        StripColliders(Managers.Game.Monster.gameObject);
        Managers.Game.Monster.gameObject.GetComponent<Animator>().Play("Stop");
        SpriteRenderer sr = Managers.Game.Monster.gameObject.GetOrAddComponent<SpriteRenderer>();
        sr.material = Managers.Resource.Load<Material>("PaintWhiteMat");
        sr.color = Util.DamagedColor();
        GameObject go = Instantiate(Managers.Game.Monster.gameObject);
        go.transform.position = Managers.Game.Monster.gameObject.transform.position;
        go.GetComponent<Animator>().Play("Stop");

        // 이 복사본은 그림일 뿐인데 콜라이더와 MonsterController 가 그대로 복사된다.
        // 그래서 1초 동안 "살아 있는 몬스터"로 보였고, 그 사이 그 칸을 다시 밟으면
        // 같은 상대와 두 번 싸우고 경험치도 보상도 두 번 받았다 (60층·80층 보스).
        StripColliders(go);
        MonsterController copy = go.GetComponent<MonsterController>();
        if (copy != null)
            copy.MarkDead();
        Destroy(Managers.Game.Monster.gameObject);
        Destroy(go, 1);
        Destroy(sr, 1f);
    }

    /// <summary>자식까지 훑어 콜라이더를 전부 없앤다. 죽은 것은 길을 막지도 않는다.</summary>
    static void StripColliders(GameObject go)
    {
        Collider[] cols = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            Destroy(cols[i]);
    }

    private void OnDestroy()
    {
        _creature.OnDefenceAction -= ClearDefence;
        _creature.OnHitAction -= Refresh;
        _creature.OnHitAction -= StartDamagedMat;
        _creature.OnHitAction -= CheckEnrage;
        _creature.OnDeadAction -= Dead;
        _creature.OnDataRefreshAction -= Refresh;
    }
}
