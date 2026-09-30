using System;
using static GameManager;

/// <summary>The combat clock used by both the live battle and the forecast. No UI dependency.</summary>
public sealed class BattleStepper : IDisposable
{
    public readonly CreatureData Player, Monster;
    public float PlayerAttackTime, MonsterAttackTime, PlayerDefenceTime, MonsterDefenceTime;
    public readonly float PlayerAttackPeriod, MonsterAttackPeriod, PlayerDefencePeriod, MonsterDefencePeriod;
    public int PlayerHits, MonsterHits;
    public bool PlayerDead { get; private set; }
    public bool MonsterDead { get; private set; }
    public bool Finished => PlayerDead || MonsterDead;
    public Action<bool, int, bool, bool> OnStrike;
    public Action<bool> OnGuard;

    public BattleStepper(CreatureData player, CreatureData monster, int playerHits = 0, float defenceTime = 0)
    {
        Player = player;
        Monster = monster;
        PlayerHits = playerHits;
        PlayerDefenceTime = defenceTime;
        PlayerAttackPeriod = Period(player.AttackSpeed);
        MonsterAttackPeriod = Period(monster.AttackSpeed);
        PlayerDefencePeriod = Period(player.DefenceSpeed);
        MonsterDefencePeriod = Period(monster.DefenceSpeed);
        player.Trait = EffectFactory.GetTrait(player);
        monster.Trait = EffectFactory.GetTrait(monster);
        // Record death before reward/animation callbacks. Beast can heal after its death event.
        player.OnDeadAction = MarkPlayerDead + player.OnDeadAction;
        monster.OnDeadAction = MarkMonsterDead + monster.OnDeadAction;
        if (player.Ability == (int)Define.Trait.Guardian) Guard(true);
        if (monster.Ability == (int)Define.Trait.Guardian) Guard(false);
    }

    static float Period(float speed) => speed > 0 ? 3f / speed : float.PositiveInfinity;
    void MarkPlayerDead() => PlayerDead = true;
    void MarkMonsterDead() => MonsterDead = true;

    public void Guard(bool player)
    {
        if (Finished) return;
        if (player) { Player.IsDefence = true; PlayerDefenceTime = PlayerDefencePeriod; }
        else { Monster.IsDefence = true; MonsterDefenceTime = MonsterDefencePeriod; }
        OnGuard?.Invoke(player);
    }

    public void Step(float dt)
    {
        if (Finished || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
        // Stable ordering: player attack, player guard, monster attack, monster guard.
        if (PlayerAttackTime >= PlayerAttackPeriod)
        {
            PlayerAttackTime = 0;
            Strike(true);
            if (Finished) return;
        }
        if (PlayerDefenceTime >= PlayerDefencePeriod)
        {
            if (!Player.IsDefence) Guard(true);
            PlayerDefenceTime = PlayerDefencePeriod;
        }
        if (MonsterAttackTime >= MonsterAttackPeriod)
        {
            MonsterAttackTime = 0;
            Strike(false);
            if (Finished) return;
        }
        if (MonsterDefenceTime >= MonsterDefencePeriod)
        {
            if (!Monster.IsDefence) Guard(false);
            MonsterDefenceTime = MonsterDefencePeriod;
        }
        PlayerAttackTime += dt;
        MonsterAttackTime += dt;
        PlayerDefenceTime += dt;
        MonsterDefenceTime += dt;
    }

    void Strike(bool fromPlayer)
    {
        var attacker = fromPlayer ? Player : Monster;
        var target = fromPlayer ? Monster : Player;
        int hits = (fromPlayer ? PlayerHits : MonsterHits) + 1;
        // ">=" 여야 한다 (thesword_balance._next_crit). 전투 사이에 이어 온 횟수가 주기보다
        // 크면 (장비로 주기가 줄었을 때) "==" 는 다시는 맞지 않아 치명타가 영영 안 나왔다.
        int period = (int)attacker.Critical;
        attacker.IsCritical = period > 0 && hits >= period;
        if (attacker.IsCritical) hits = 0;
        if (fromPlayer) PlayerHits = hits; else MonsterHits = hits;
        int damage = attacker.Trait.ExecuteAttack(attacker, target);
        bool critical = attacker.IsCritical;
        bool guarded = target.IsDefence;
        float before = target.CurHP;
        target.Trait.ExcuteOnHit(attacker, target, damage);
        // 전투창 숫자는 맞은 쪽이 실제로 잃은 HP 다(스킬 BattleSkills.Strike 와 같다). 공격 값을 그대로 띄우면 불사(20%)·
        // 은신한 암살·갑옷 껍질 앞에서 체력 막대와 따로 놀았고, 야수가 방어력 아래로 때리면 -5 가 떴다.
        // 야수만은 맞은 뒤 제 손으로 회복해 차이가 줄어든다 — 그 특성은 회복 전 몫(0 아래는 0, 남은 HP 까지)을 띄운다.
        int dealt = target.Trait is CreatureClass.BeastTrait
            ? (int)Math.Min(Math.Max(0, damage), Math.Ceiling(Math.Max(0f, before)))
            : (int)Math.Round(Math.Max(0f, before - target.CurHP));
        attacker.IsCritical = false;
        if (guarded)
        {
            target.IsDefence = false;
            if (fromPlayer) MonsterDefenceTime = 0; else PlayerDefenceTime = 0;
            target.OnDefenceAction?.Invoke();
        }
        OnStrike?.Invoke(fromPlayer, dealt, critical, guarded);
    }

    public void Dispose()
    {
        Player.OnDeadAction -= MarkPlayerDead;
        Monster.OnDeadAction -= MarkMonsterDead;
        OnStrike = null;
        OnGuard = null;
    }
}
