using UnityEngine;

/// <summary>
/// 액티브 스킬. 마검 계약 뒤부터 전투 중에 쓸 수 있다.
///
/// 왜 이런 모양인가
/// ----------------
/// 이 게임의 전투는 자동이다. 게이지가 차면 때리고 막는다. 그래서 스킬은 "무엇을
/// 누를까" 가 아니라 <b>언제 끼어들까</b> 여야 의미가 있다. 셋 다 전투당 한 번씩만
/// 쓸 수 있고 자원도 쿨타임도 없다 — 아낄지 지금 쓸지, 그 판단 하나만 남긴다.
///
///   1 강타   지금 즉시 공격력의 2.5배로 한 대. 오래 끌면 지는 상대를 끊는다.
///   2 철벽   즉시 방어 상태. 다음 한 대를 막는다. 큰 것이 들어오기 직전에 쓴다.
///   3 흡혈   공격력의 1.5배로 때리고 그만큼 회복. 물약이 없을 때의 한 모금이다.
///
/// 피해는 반드시 상대의 특성을 거쳐 들어간다(ExcuteOnHit). 갑옷의 껍질이나 불사의
/// 면역을 스킬이 통째로 무시하면, 챕터마다 다른 특성을 공략한다는 설계가 무너진다.
///
/// 수치는 코드에 둔다. 표로 뺄 만큼 항목이 많지 않고, 늘어나면 그때 CSV 로 옮기면 된다.
/// </summary>
public static class BattleSkills
{
    public const int Count = 3;

    public enum Kind { Smash = 0, Guard = 1, Drain = 2 }

    const float SmashRatio = 2.5f;
    const float DrainRatio = 1.5f;

    static readonly bool[] _used = new bool[Count];

    /// <summary>스킬을 쓸 수 있는 상태인가. 마검 계약 전에는 아무것도 못 쓴다.</summary>
    public static bool Unlocked
    {
        get
        {
            return Managers.Game != null && Managers.Game.PlayerData != null
                   && Managers.Game.PlayerData.IsContractedSword;
        }
    }

    public static bool IsUsed(int index)
    {
        return index >= 0 && index < Count && _used[index];
    }

    /// <summary>전투가 시작될 때마다 부른다. 셋 다 다시 쓸 수 있게 된다.</summary>
    public static void ResetForBattle()
    {
        for (int i = 0; i < Count; i++)
            _used[i] = false;
    }

    public static string NameOf(int index)
    {
        switch ((Kind)index)
        {
            case Kind.Smash: return "강타";
            case Kind.Guard: return "철벽";
            case Kind.Drain: return "흡혈";
            default: return "";
        }
    }

    /// <summary>스킬을 쓴다. 실제로 발동했으면 true.</summary>
    public static bool Use(int index, UI_PlayerCard playerCard, UI_MonsterCard monsterCard)
    {
        if (Unlocked == false || index < 0 || index >= Count || _used[index])
            return false;
        if (Managers.Game.OnBattle == false || monsterCard == null || playerCard == null)
            return false;

        GameManager.CurPlayerData player = Managers.Game.PlayerData;
        if (Managers.Game.MonsterData == null || Managers.Game.MonsterData.Count == 0)
            return false;
        GameManager.CurMonsterData monster = Managers.Game.MonsterData[0];
        if (player.CurHP <= 0 || monster.CurHP <= 0)
            return false;

        switch ((Kind)index)
        {
            case Kind.Smash:
                Strike(player, monster, SmashRatio, 0f, monsterCard);
                break;

            case Kind.Guard:
                // 방어 게이지를 채운 것과 같은 상태로 만든다. 다음 한 대를 막는다.
                playerCard.FillDefenceGague();
                break;

            case Kind.Drain:
                Strike(player, monster, DrainRatio, 1f, monsterCard);
                break;
        }

        _used[index] = true;
        Managers.Sound.Play(Define.Sound.Effect, "PlayerAttack0_SFX");
        return true;
    }

    /// <summary>스킬 피해를 넣는다. 회복 비율이 있으면 들어간 만큼 되돌려 받는다.</summary>
    static void Strike(GameManager.CurPlayerData player, GameManager.CurMonsterData monster,
                       float ratio, float drain, UI_MonsterCard monsterCard)
    {
        int damage = Mathf.RoundToInt(Mathf.Max(1f, player.Attack * ratio - monster.Defence));

        float before = monster.CurHP;
        // 상대의 특성을 거쳐서 넣는다. 껍질도 면역도 그대로 적용돼야 한다.
        monster.Trait.ExcuteOnHit(player, monster, damage);
        float dealt = Mathf.Max(0f, before - monster.CurHP);

        ShowFont(monsterCard, dealt, 0f);

        if (drain > 0f && dealt > 0f)
        {
            float heal = Mathf.Round(dealt * drain);
            player.CurHP = Mathf.Min(player.MaxHP, player.CurHP + heal);
            if (player.OnDataRefreshAction != null)
                player.OnDataRefreshAction.Invoke();
            ShowFont(null, 0f, heal);
        }
    }

    static void ShowFont(UI_MonsterCard card, float damage, float heal)
    {
        GameObject popup = GameObject.Find("UI_BattlePopup");
        if (popup == null)
            return;

        Vector3 pos = popup.transform.position;
        if (card != null)
            pos = card.transform.position;

        Managers.Object.ShowDamageFont(pos, damage, heal, popup.transform, false, false);
    }
}
