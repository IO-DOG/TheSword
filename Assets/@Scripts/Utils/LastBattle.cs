using UnityEngine;

/// <summary>
/// 방금 끝난 전투의 요약. UI_BattlePopup 이 전투를 열 때와 끝낼 때 채우고, GameEvents.BossDefeated·BattleEnded
/// 를 알리기 바로 앞에 확정한다. 그 이벤트를 듣는 쪽(업적 SteamHooks, 장부)이 여기서 자세한 것을 읽는다 —
/// 이벤트의 인자를 늘리면 듣는 쪽을 전부 고쳐야 해서 따로 둔다.
/// </summary>
public static class LastBattle
{
    public static int MonsterId;
    public static bool Boss;            // 챕터 보스·킹 슬라임 (UI_BattlePopup._boss 와 같은 뜻)
    public static bool Won;
    public static bool Skipped;         // 건너뛰기로 끝까지 한 번에 돌렸다
    public static bool SkillsUsed;      // 스킬을 하나라도 썼다
    public static bool FirstHitCrit;    // 플레이어의 첫 타가 치명타였다
    public static int Foretold = -1;    // 전투를 열 때의 예측 피해 (BattleForecast.Result.Damage). 잴 수 없었으면 -1
    public static float HpBefore;       // 전투를 열 때의 HP
    public static float HpAfter;        // 끝났을 때의 HP (죽었으면 0 이하)
    public static float MaxHp;          // 전투를 열 때의 최대 HP. 이겨서 레벨이 오르면 지금 체력도 는다 — 잃은 값 = HpBefore - HpAfter + (지금 MaxHP - MaxHp)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear()
    {
        MonsterId = 0; Boss = Won = Skipped = SkillsUsed = FirstHitCrit = false;
        Foretold = -1; HpBefore = HpAfter = MaxHp = 0f;
    }
}
