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
                Strike(player, monster, SmashRatio, 0f, playerCard, monsterCard);
                break;

            case Kind.Guard:
                // 방어 게이지를 채운 것과 같은 상태로 만든다. 다음 한 대를 막는다.
                // FillDefenceGague 가 아니라 Defence 를 부른다 — 만드는 상태는 똑같은데
                // (게이지 가득 + IsDefence), UI_PlayerCard 의 override 가 방패 애니메이션까지
                // 재생한다. 안 그러면 화면에서는 아무 일도 안 일어난 것으로 보인다.
                playerCard.Defence();
                break;

            case Kind.Drain:
                Strike(player, monster, DrainRatio, 1f, playerCard, monsterCard);
                break;
        }

        _used[index] = true;
        // 어드레서블에 실재하는 키여야 한다. 없는 키를 주면 ResourceManager 가 null 을
        // 돌려주고 SoundManager 가 그 null 의 .length 를 읽어 예외를 던진다 —
        // 스킬을 부른 쪽(봇의 코루틴)이 통째로 죽는다.
        Managers.Sound.Play(Define.Sound.Effect, "HeroAttack0_SFX");
        return true;
    }

    /// <summary>스킬 피해를 넣는다. 회복 비율이 있으면 들어간 만큼 되돌려 받는다.</summary>
    static void Strike(GameManager.CurPlayerData player, GameManager.CurMonsterData monster,
                       float ratio, float drain, UI_PlayerCard playerCard, UI_MonsterCard monsterCard)
    {
        Swing(playerCard);

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
            // 회복 숫자는 회복되는 쪽, 곧 플레이어 카드 위에 띄운다.
            // null 을 넘기면 전투창 원점(화면 구석)에 떠서 무엇이 회복됐는지 안 보였다.
            ShowFont(playerCard, 0f, heal);
        }
    }

    /// <summary>
    /// 때리는 시늉을 보여 준다. 평타(UI_PlayerCard.Attack)가 재생하는 것과 같은 클립이다.
    ///
    /// 이게 없으면 강타/흡혈은 숫자만 뜨고 플레이어 쪽은 가만히 서 있다 —
    /// 녹화에서 무엇이 일어났는지 알아볼 수가 없다.
    /// 애니메이터가 없거나 상태가 없어도 게임은 굴러가야 하니 전부 조용히 건너뛴다.
    /// (Images.CreatureImage = 0, Images.CreatureSwordImage = 7 — 그 enum 이 protected 라 숫자로 쓴다.)
    /// </summary>
    static void Swing(UI_PlayerCard playerCard)
    {
        if (playerCard == null)
            return;

        Play(playerCard.GetImage(0), "UIPlayerAttackAnim");

        int sword = Managers.Game.PlayerData.CurSword - Define.EQUIP_SOWRD_FIRST;
        if (sword >= 0)
            Play(playerCard.GetImage(7), $"UISword{sword}AttackAnim");
    }

    static void Play(UnityEngine.UI.Image image, string state)
    {
        if (image == null)
            return;

        Animator animator = image.gameObject.GetComponent<Animator>();
        if (animator == null)
            return;

        animator.Play(state);
    }

    static void ShowFont(Component card, float damage, float heal)
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
