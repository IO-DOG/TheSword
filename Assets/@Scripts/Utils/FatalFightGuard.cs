using System;
using UnityEngine;

/// <summary>
/// 지는 싸움 앞에서 한 번 묻는다 (FightGate 순서 100 — 이야기 관문(보스 등장 0·특성 수업 10·첫 ✖ 50) 다음). 전투 예측이 "진다" 거나,
/// 보스인데 잴 수 없으면 확인 창을 띄운다. 되돌릴 수 없는 일이라 처음 골라진 쪽은 "물러선다" 다.
/// 숫자는 마검의 눈이 보는 것이라 계약 뒤에만 댄다. 계약 전에는 숫자 없이 데미안의 감으로만 묻는다 — 예전에는
/// 계약 전에 아예 묻지 않아서, 1층 정령 → 늑대 길이 Lv2 를 아무 말 없이 죽였다. 자동 플레이는 건너뛴다(봇이 막히면 안 된다).
///
/// "물러선다" 뒤에 키를 쥔 채 그 몬스터 쪽으로 서 있으면 프레임마다 부딪혀 창이 다시 떴다.
/// 같은 상대와 연달아 부딪히는 동안은 묻지 않고 조용히 거둔다 — 부딪힐 때마다 기한이 늘어서
/// 키를 놓았다가 다시 와야 다시 묻는다.
/// </summary>
public static class FatalFightGuard
{
    public const int Order = 100;
    const float Hold = 0.5f;    // "물러선다" 뒤, 이만큼(실시간 초) 안 부딪혀야 다시 묻는다

    static MonsterController s_declined;
    static float s_declinedUntil;

    // 에디터는 도메인 리로드 없이 플레이를 되풀이한다 — 지난 플레이의 (부서진) 몬스터와 기한을 넘기지 않는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_declined = null;
        s_declinedUntil = 0f;
    }

    public static bool Ask(MonsterController monster, Action proceed)
    {
        GameManager g = Managers.Game;
        if (GameEvents.IsAutoPlaying)
            return false;

        if (ReferenceEquals(monster, s_declined) && Time.unscaledTime < s_declinedUntil)
        {
            s_declinedUntil = Time.unscaledTime + Hold;
            FightGate.Cancel();
            return true;
        }

        BattleForecast.Result r = BattleForecast.Of(monster.id, g.PlayerData.CurStageid);
        bool boss = monster.CompareTag("Boss") || monster is BossMonsterController;
        if (r.Ok ? r.Win : boss == false)
            return false;

        string message = g.PlayerData.IsContractedSword == false ? Managers.GetString(StoryUI.Instinct)
            : r.Ok == false ? Managers.GetString(ForecastUI.AskUnknown)
            : r.Kills ? string.Format(Managers.GetString(ForecastUI.AskFatal), r.Damage, Mathf.RoundToInt(g.PlayerData.CurHP))
            : Managers.GetString(ForecastUI.AskNever);
        UI_ConfirmPopup.AskDestructive(message, proceed, () => Decline(monster),
            Managers.GetString(ForecastUI.Fight), Managers.GetString(ForecastUI.BackOff));
        return true;
    }

    static void Decline(MonsterController monster)
    {
        s_declined = monster;
        s_declinedUntil = Time.unscaledTime + Hold;
        FightGate.Cancel();
    }
}
