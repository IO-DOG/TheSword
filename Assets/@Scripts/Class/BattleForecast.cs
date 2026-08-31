using UnityEngine;
using static GameManager;

/// <summary>
/// 이 몬스터와 싸우면 체력이 얼마나 줄어드는가 — 싸우기 전에 재 본다.
///
/// 매직 타워는 완전 정보 위에서 계산하는 게임인데 이 게임의 전투는 게이지식이다.
/// 공격 주기·방어 게이지·치명 주기·특성 여덟 가지가 얽혀 사람이 암산할 수 없으니,
/// 계산할 수 있는 정보를 주려면 결과를 대신 재 주는 수밖에 없다.
///
/// <b>예측을 새로 구현하지 않는다.</b> 같은 계산을 두 곳에 두면 반드시 어긋나고,
/// 어긋난 예측은 없느니만 못하다. 여기서 하는 일은 CreatureData 를 복제하고
/// 같은 ITrait 을 붙여 UI_PlayerCard/UI_MonsterCard 의 쿨타임 루프를 그대로 도는 것뿐이다 —
/// 피해 계산은 전투가 쓰는 CreatureClass 의 코드가 그대로 돈다.
///
/// 진짜 전투와 다른 점은 넷뿐이고, 넷 다 "UI 가 없다" 에서 나온다.
///  - <b>철벽(GuardianTrait)</b> 은 만들 수 없다. 생성자가 UI_BaseCard 의 방어 게이지를
///    채우기 때문이다(FillDefenceGague). 공격·피격 코드는 DefaultTrait 과 한 글자도
///    다르지 않으므로, 파이썬 시뮬레이터(Tools/thesword_balance.py)가 하듯
///    "방어 상태로 시작" 만 옮긴다.
///  - 애니메이션·파티클·소리·데미지 폰트는 하지 않는다. OnHitAction 류에는 빈 대리자를
///    넣는다 — 특성이 그 자리를 무조건 Invoke 하므로 null 이면 널참조로 죽는다.
///  - 액티브 스킬(강타/철벽/흡혈)은 사람이 언제 누를지 모르니 넣지 않는다.
///    <b>그래서 이 예측은 늘 "스킬을 안 썼을 때" 이고, 실제보다 나쁜 쪽으로 틀린다.</b>
///  - Berserk(20회 공격마다 강화)는 넣지 않는다. UI_MonsterCard._totalAttackCount 를
///    아무도 올리지 않아 진짜 전투에서도 돌지 않는 코드다.
/// </summary>
public static class BattleForecast
{
    /// <summary>못 이기는 싸움에서 영영 돌지 않게 하는 마개. 전투 시계 기준 초.</summary>
    const float MAX_SECONDS = 600f;

    public struct Result
    {
        public bool Ok;       // 잴 수 있었는가 (표에 없는 몬스터/층이면 false)
        public bool Win;      // 이기는가
        public int Damage;    // 이 싸움에서 잃는 체력
        public int RemainHP;  // 싸운 뒤 남는 체력. 0 이면 죽는다
    }

    /// <summary>지금 이 플레이어가 그 몬스터와 싸우면 어떻게 되는가.</summary>
    public static Result Of(int monsterId, int stageId)
    {
        Result r = new Result();

        Data.MonsterData table;
        Data.StageInfoData stage;
        if (Managers.Game == null || Managers.Game.PlayerData == null)
            return r;
        if (Managers.Data.MonsterDic.TryGetValue(monsterId, out table) == false)
            return r;
        if (Managers.Data.StageInfoDic.TryGetValue(stageId, out stage) == false)
            return r;

        CreatureData player = Clone(Managers.Game.PlayerData);
        CreatureData monster = MonsterOf(table, stage);

        bool playerGuards;
        bool monsterGuards;
        player.Trait = TraitOf(player, out playerGuards);
        monster.Trait = TraitOf(monster, out monsterGuards);

        // 죽음은 진짜 전투와 같은 자리에서 잡는다 — 특성이 OnDeadAction 을 부르는 그 순간이
        // 카드가 Dead() 로 전투를 끝내는 순간이다. 체력으로 판정하면 안 된다:
        // 야수(BeastTrait)는 죽은 뒤에 최대 체력의 40% 를 회복해 버려서, 실제로는
        // 전투가 끝났는데 예측에서는 계속 살아 있는 것으로 보인다.
        bool playerDead = false;
        bool monsterDead = false;
        player.OnDeadAction = () => playerDead = true;
        monster.OnDeadAction = () => monsterDead = true;

        // 공격 주기 3f/AttackSpeed, 방어 게이지 3f/DefenceSpeed — 카드의 코루틴과 같다.
        float playerMax = 3f / player.AttackSpeed;
        float monsterMax = 3f / monster.AttackSpeed;
        float playerDefMax = 3f / player.DefenceSpeed;
        float monsterDefMax = 3f / monster.DefenceSpeed;
        float playerCool = 0f;
        float monsterCool = 0f;
        // 플레이어 쪽 두 값은 전투 사이에 이어진다. 치명타는 확률이 아니라 "N 번째 공격"
        // 인데 그 셈(Managers.Game.AttackCount)도, 방어 게이지(Managers.Game.DefenceCoolTime)도
        // 전투가 시작될 때 0 으로 돌아가지 않는다. 0 에서 시작한 예측은 실제와 한 대씩 어긋난다.
        float playerDefCool = Managers.Game.DefenceCoolTime;
        float monsterDefCool = 0f;
        int playerHits = Managers.Game.AttackCount;
        int monsterHits = 0;

        if (playerGuards)
        {
            player.IsDefence = true;
            playerDefCool = playerDefMax;
        }
        if (monsterGuards)
        {
            monster.IsDefence = true;
            monsterDefCool = monsterDefMax;
        }

        float startHP = player.CurHP;
        // 한 걸음의 폭. 코루틴이 WaitForFixedUpdate 로 도니 Time.deltaTime 은 고정 간격이고,
        // 거기에 게임 배속이 곱해진다 (attackCoolTime += Time.deltaTime * GameSpeed).
        float dt = Time.fixedDeltaTime * Mathf.Max(1, Managers.Game.GameSpeed);
        float t = 0f;

        while (t < MAX_SECONDS && playerDead == false && monsterDead == false)
        {
            // 도는 순서는 코루틴이 시작된 순서다 — UI_BattlePopup 이 플레이어 카드를 먼저
            // 만들고, 카드마다 공격 코루틴이 방어 코루틴보다 먼저 시작한다.
            if (playerCool >= playerMax)
            {
                playerCool = 0f;
                playerHits++;
                if (playerHits == player.Critical)   // UI_PlayerCard.Attack 과 같은 비교
                {
                    player.IsCritical = true;
                    playerHits = 0;
                }

                if (Swing(player, monster))
                {
                    monsterDefCool = 0f;
                    // ClearDefence 는 자기 게이지와 함께 Managers.Game.DefenceCoolTime,
                    // 곧 플레이어의 방어 게이지까지 0 으로 만든다(UI_BaseCard.ClearDefence).
                    // 몬스터의 방어를 깨면 내 방어도 되돌아간다 — 진짜 전투가 그렇게 돈다.
                    playerDefCool = 0f;
                }
                if (playerDead || monsterDead)
                    break;
            }

            if (playerDefCool >= playerDefMax)
            {
                player.IsDefence = true;
                playerDefCool = playerDefMax;
            }

            if (monsterCool >= monsterMax)
            {
                monsterCool = 0f;
                monsterHits++;
                if (monsterHits == monster.Critical)
                {
                    monster.IsCritical = true;
                    monsterHits = 0;
                }

                if (Swing(monster, player))
                    playerDefCool = 0f;
                if (playerDead || monsterDead)
                    break;
            }

            if (monsterDefCool >= monsterDefMax)
            {
                monster.IsDefence = true;
                monsterDefCool = monsterDefMax;
            }

            playerCool += dt;
            monsterCool += dt;
            playerDefCool += dt;
            monsterDefCool += dt;
            t += dt;
        }

        float remain = Mathf.Max(0f, player.CurHP);
        r.Ok = true;
        r.Win = monsterDead && playerDead == false;
        r.RemainHP = Mathf.FloorToInt(remain);
        r.Damage = Mathf.Max(0, Mathf.CeilToInt(startHP - remain));
        return r;
    }

    /// <summary>
    /// 한 대 때린다. UI_BaseCard.Attack 이 하는 것과 같은 순서다.
    /// 상대의 방어를 깨뜨렸으면 true (부르는 쪽이 게이지를 되돌린다).
    /// </summary>
    static bool Swing(CreatureData attacker, CreatureData target)
    {
        int damage = attacker.Trait.ExecuteAttack(attacker, target);
        target.Trait.ExcuteOnHit(attacker, target, damage);

        // 치명타는 한 번 쓰고 내린다 (UI_PlayerCard/UI_MonsterCard.Attack).
        if (attacker.IsCritical)
            attacker.IsCritical = false;

        // 막힌 공격이면 OnDefenceAction -> ClearDefence 로 방어가 풀린다.
        bool broke = target.IsDefence;
        if (broke)
            target.IsDefence = false;

        return broke;
    }

    /// <summary>전투에 쓰이는 값만 복제한다. 진짜 데이터는 건드리지 않는다.</summary>
    static CreatureData Clone(CreatureData src)
    {
        CreatureData c = new CreatureData();
        c.Ability = src.Ability;
        c.MaxHP = src.MaxHP;
        c.CurHP = src.CurHP;
        c.Attack = src.Attack;
        c.Defence = src.Defence;
        c.AttackSpeed = src.AttackSpeed;
        c.DefenceSpeed = src.DefenceSpeed;
        c.Critical = src.Critical;
        c.CriticalAttack = src.CriticalAttack;
        // 방어 상태도 전투 사이에 남는다 — 상대가 먼저 죽으면 IsDefence 가 켜진 채로
        // 끝나고, 다음 전투는 그 상태로 시작한다. 0 으로 지우면 그 한 대가 어긋난다.
        c.IsDefence = src.IsDefence;
        c.IsCritical = src.IsCritical;
        Silence(c);
        return c;
    }

    /// <summary>
    /// 그 몬스터가 이 층에서 갖는 값. MonsterController.SetMonster 가 전투에 넣는 것과 같다
    /// (공격력·방어력은 층 배수를 곱하고, 체력은 곱하지 않는다).
    /// </summary>
    static CreatureData MonsterOf(Data.MonsterData table, Data.StageInfoData stage)
    {
        CreatureData c = new CreatureData();
        c.Ability = table.Ability;
        c.MaxHP = table.MaxHP;
        c.CurHP = table.MaxHP;
        c.Attack = stage.ATK * table.Attack;
        c.Defence = stage.DEF * table.Defence;
        c.AttackSpeed = table.AttackSpeed;
        c.DefenceSpeed = table.DefenceSpeed;
        c.Critical = table.Critical;
        c.CriticalAttack = table.CriticalAttack;
        c.IsDefence = false;
        c.IsCritical = false;
        Silence(c);
        return c;
    }

    /// <summary>
    /// UI 가 붙는 자리를 빈 대리자로 채운다. 특성들이 target.OnHitAction.Invoke() 를
    /// 조건 없이 부르기 때문에, 비워 두면 예측이 널참조로 죽는다.
    /// </summary>
    static void Silence(CreatureData c)
    {
        c.OnDataRefreshAction = () => { };
        c.OnDefenceAction = () => { };
        c.OnHitAction = () => { };
        c.OnDeadAction = () => { };
    }

    /// <summary>
    /// 특성을 만든다. 철벽만 예외로, 카드 없이는 생성자가 널참조로 죽으므로
    /// 같은 코드를 도는 DefaultTrait 으로 대신하고 "방어 상태로 시작" 을 밖에서 준다.
    /// </summary>
    static CreatureClass.ITrait TraitOf(CreatureData c, out bool startsGuarding)
    {
        startsGuarding = (c.Ability == (int)Define.Trait.Guardian);
        if (startsGuarding)
            return new CreatureClass.DefaultTrait();

        return EffectFactory.GetTrait(c);
    }
}
