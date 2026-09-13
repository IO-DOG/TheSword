using UnityEngine;
using static GameManager;

/// <summary>
/// Clones combat state and runs the same BattleStepper as UI_BattlePopup.
/// Assumes no active skills; changing game speed does not change the combat result.
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

        float startHP = player.CurHP;
        bool playerDead, monsterDead;
        using (var battle = new BattleStepper(player, monster, Managers.Game.AttackCount,
                                              Managers.Game.DefenceCoolTime))
        {
            float dt = Time.fixedDeltaTime;
            for (float time = 0; time < MAX_SECONDS && !battle.Finished; time += dt)
                battle.Step(dt);
            playerDead = battle.PlayerDead;
            monsterDead = battle.MonsterDead;
        }

        float remain = Mathf.Max(0f, player.CurHP);
        r.Ok = true;
        r.Win = monsterDead && playerDead == false;
        r.RemainHP = Mathf.FloorToInt(remain);
        r.Damage = Mathf.Max(0, Mathf.CeilToInt(startHP - remain));
        return r;
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
