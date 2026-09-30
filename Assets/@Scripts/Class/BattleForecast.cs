using UnityEngine;
using static GameManager;

/// <summary>
/// Clones combat state and runs the same BattleStepper as UI_BattlePopup.
/// Assumes no active skills; changing game speed does not change the combat result.
///
/// 값은 "다음에 싸우면" 이다. 플레이어의 치명 횟수·방어 게이지·방패는 전투가 끝나도 이어지므로
/// (UI_BattlePopup) 한 번 싸울 때마다 층의 나머지 값이 다시 매겨진다 — 보여 주는 쪽은 전투가 끝나면 다시 잰다.
/// </summary>
public static class BattleForecast
{
    /// <summary>못 이기는 싸움에서 영영 돌지 않게 하는 마개. 전투 시계 기준 초.</summary>
    const float MAX_SECONDS = 600f;

    /// <summary>
    /// 값을 잴 때 플레이어에게 주는 체력. 전투에서 플레이어의 체력을 읽는 곳은 "쓰러졌나" 하나뿐이라
    /// (플레이어 특성은 기본이다) 체력을 넉넉히 주고 돌려도 쓰러뜨리기까지의 과정은 똑같다.
    /// 그래서 한 번 돌려 이 싸움의 값을 얻고, 이기는지는 그 값을 지금 체력과 견줘 정한다 —
    /// 지는 싸움도 "얼마나 모자라는가" 가 나온다. float 가 정수를 정확히 담는 범위(2^24) 안의 값이다.
    /// </summary>
    const float PriceHP = 1e7f;

    public struct Result
    {
        public bool Ok;              // 잴 수 있었는가 (표에 없는 몬스터/층이면 false)
        public bool Win;             // 지금 체력으로 이기는가
        public bool Kills;           // 체력만 넉넉하면 쓰러뜨리기는 하는가. false 면 시계가 다 돌도록 못 끝낸다
        public int Damage;           // 이 싸움의 값 — 쓰러뜨리기까지 잃는 체력. 지는 싸움이면 지금 체력 이상이다
        public int RemainHP;         // 싸운 뒤 남는 체력. 지면 0
        public int HitsTaken;        // 몸에 맞은 상대 공격 수 (방패에 막힌 것은 뺀다)
        public int HitsDealt;        // 내가 휘두른 수
        public bool FirstStrikeCrit; // 이 싸움의 내 첫 공격이 치명타인가 (이어 온 치명 횟수로 정해진다)
        public int Exp;              // 쓰러뜨리면 얻는 경험치
        public bool LevelUp;         // 이겨서 그 경험치로 레벨이 오르는가. 지는 싸움이면 false
    }

    /// <summary>지금 이 플레이어가 그 몬스터와 싸우면 어떻게 되는가.</summary>
    public static Result Of(int monsterId, int stageId) => Of(monsterId, stageId, 0, 0);

    /// <summary>
    /// 공격력·방어력이 그만큼 더 있었다면 어떻게 되는가. 임계(몇을 올려야 한 대 덜 맞는가)를 잰다.
    /// 셈은 위와 똑같은 전투 시계다 — 올린 값만 복제본에 더한다.
    /// </summary>
    public static Result Of(int monsterId, int stageId, int attackUp, int defenceUp)
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

        CurPlayerData live = Managers.Game.PlayerData;
        CreatureData player = Clone(live);
        player.Attack += attackUp;
        player.Defence += defenceUp;
        player.CurHP = PriceHP;
        CreatureData monster = MonsterOf(table, stage);

        int taken = 0, dealt = 0;
        bool firstCrit = false;
        bool monsterDead;
        using (var battle = new BattleStepper(player, monster, Managers.Game.AttackCount,
                                              Managers.Game.DefenceCoolTime))
        {
            battle.OnStrike = (fromPlayer, damage, critical, guarded) =>
            {
                if (fromPlayer)
                {
                    if (dealt == 0)
                        firstCrit = critical;
                    dealt++;
                }
                else if (guarded == false && damage > 0)
                {
                    taken++;
                }
            };

            float dt = Time.fixedDeltaTime;
            for (float time = 0; time < MAX_SECONDS && !battle.Finished; time += dt)
                battle.Step(dt);
            monsterDead = battle.MonsterDead;
        }

        float hp = live.CurHP;
        r.Ok = true;
        r.Kills = monsterDead;
        r.Damage = Mathf.Max(0, Mathf.RoundToInt(PriceHP - player.CurHP));
        // 쓰러지는가는 합계 하나로 정해진다 — 전투 중에 플레이어가 회복하는 길이 없어(스킬은 빼고 잰다)
        // 체력은 줄기만 한다. 거대의 다섯 번째 대 포효로 같이 쓰러지는 것도 합계에 들어 있다.
        r.Win = monsterDead && r.Damage < hp;
        r.RemainHP = r.Win ? Mathf.FloorToInt(hp - r.Damage) : 0;
        r.HitsTaken = taken;
        r.HitsDealt = dealt;
        r.FirstStrikeCrit = firstCrit;

        // MonsterController.SetMonster 가 전투에 넣는 것과 같은 식, CurExp 세터와 같은 비교다.
        // 경험치는 이겨야 받는다 — 여기서 막아야 맵(X)·툴팁·도감이 한 싸움을 두고 딴말을 하지 않는다.
        float exp = stage.EXP / (float)100 * table.RewardExp;
        Data.PlayerData next;
        r.Exp = Mathf.RoundToInt(exp);
        r.LevelUp = r.Win && Managers.Data.PlayerDic.TryGetValue(live.Level + 1, out next) && live.CurExp + exp >= next.NeedExp;
        return r;
    }

    /// <summary>
    /// 임계 — 공격력을 몇 올려야 이 싸움이 싸지는가. 이기는 싸움이면 한 대라도 덜 맞거나 값이 줄어드는,
    /// 지는 싸움이면 이기게 되는 가장 작은 k. limit 까지 없으면 0 이고 then 은 now 그대로다.
    /// </summary>
    public static int AttackStep(int monsterId, int stageId, Result now, int limit, out Result then)
    {
        then = now;
        if (now.Ok == false)
            return 0;

        for (int k = 1; k <= limit; k++)
        {
            Result r = Of(monsterId, stageId, k, 0);
            bool better = now.Win ? r.HitsTaken < now.HitsTaken || r.Damage < now.Damage : r.Win;
            if (better)
            {
                then = r;
                return k;
            }
        }
        return 0;
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

}
