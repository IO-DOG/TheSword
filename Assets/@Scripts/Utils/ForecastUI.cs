using System.Collections.Generic;
using UnityEngine;

/// <summary>전투 예측·몬스터 도감·치명 확인·워프가 같이 쓰는 문구 id(Tools/ui_text_parts/forecast.py)와 색.</summary>
public static class ForecastUI
{
    // 이미 있는 줄 (Tools/ui_text.py)
    public const int Cost = 133;            // 예상 피해
    public const int Falls = 134;           // 쓰러진다
    public const int CannotWin = 135;       // 못 이긴다
    public const int Hp = 102;              // 체력

    public const int ManualTitle = 260;
    public const int ManualLegend = 261;
    public const int Count = 262;           // {0}마리
    public const int NextFight = 263;
    public const int HitsTaken = 264;       // {0}대 맞음
    public const int AtkOneHit = 265;       // 공격 +{0} → 한 대 덜 맞음
    public const int AtkLess = 266;         // 공격 +{0} → -{1}
    public const int AtkWin = 267;          // 공격 +{0} → 이긴다
    public const int DefLess = 268;         // 방어 +1 → -{0}
    public const int DefWin = 269;          // 방어 +1 → 이긴다
    public const int CritIn = 270;          // 치명까지 {0}타
    public const int FirstCrit = 271;
    public const int Exp = 272;             // 경험치 {0}
    public const int ManualEmpty = 273;
    public const int Controls = 274;
    public const int HintKeys = 275;
    public const int ForecastOn = 276;
    public const int ForecastOff = 277;
    public const int GuardUp = 278;
    public const int AskFatal = 279;        // {0} 예상 피해, {1} 체력
    public const int AskNever = 280;
    public const int AskUnknown = 281;
    public const int Fight = 282;
    public const int BackOff = 283;
    public const int HealOverflow = 284;    // 회복 +{0} (넘침 {1})
    public const int WarpTitle = 285;
    public const int FloorN = 286;          // {0}층
    public const int Vault = 287;
    public const int WarpHelp = 288;

    // 값이 바뀐 것을 보이는 문구 (Tools/ui_text_parts/feel.py)
    public const int AtkUp = 410;           // 공격 +{0}
    public const int DefUp = 411;           // 방어 +{0}
    public const int MaxHpUp = 412;         // 최대 체력 +{0}
    public const int NoChange = 413;        // 변화 없음
    public const int NextStep = 414;        // {0}이면 {1}→{2}  (공격 +3이면 -31→-24)
    public const int Wasted = 415;          // (넘침 {0})

    // 값이 바뀐 순간. 맵 위 숫자가 이 색에서 제 색으로 돌아오고, 곁의 ▼▲ 가 이 색이다. 숫자는 거의 다 초록(Safe)이라
    // 초록으로 번쩍이면 안 보인다 — 흰빛이 도는 밝은 색으로 번쩍인다.
    public static readonly Color Cheaper = new Color32(200, 255, 210, 255);
    public static readonly Color Dearer = new Color32(255, 110, 100, 255);

    // 표시에 쓰는 색. 맵 위 숫자·도감·툴팁이 같은 눈금을 쓴다 (지금 체력에 대한 몫).
    public static readonly Color Safe = new Color32(150, 230, 140, 255);    // 1/4 이하
    public static readonly Color Caution = new Color32(245, 220, 90, 255);  // 1/2 이하
    public static readonly Color Danger = new Color32(255, 150, 60, 255);   // 그 위, 살아남는다
    public static readonly Color Fatal = new Color32(255, 70, 70, 255);     // 진다
    public static readonly Color Unknown = new Color32(170, 170, 170, 255);
    // 이 싸움으로 레벨이 오른다. 글자 하나다 — "LV+" 는 맵에서 옆 칸 숫자에 붙어 "-42 Lv42" 로 읽혔다.
    // 두 픽셀 글꼴에 화살표(↑▲)가 없어 ★ 을 쓴다. 도감 머리말(261)이 같은 표로 풀어 준다.
    public const string LevelUpMark = "<size=80%><color=#FFD84A>★</color></size>";

    /// <summary>다음 치명타가 몇 번째 공격인가 (1 = 바로 다음). 치명 주기가 없으면 0.
    /// 치명 횟수는 전투 사이에 이어진다(UI_BattlePopup) — BattleStepper 가 ">=" 로 세는 것과 같다.</summary>
    public static int HitsToCrit()
    {
        int period = (int)Managers.Game.PlayerData.Critical;
        return period > 0 ? Mathf.Max(1, period - Managers.Game.AttackCount) : 0;
    }

    static readonly List<Collider> s_colliders = new List<Collider>();

    // 에디터는 도메인 리로드 없이 플레이를 되풀이한다 — 지난 플레이의 부서진 콜라이더·지난 판의 예측을 쥐고 있지 않게.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_colliders.Clear();
        s_forecasts.Clear();
        s_state = default;
    }

    /// <summary>맵 위 숫자와 같은 모양의 값 — -31, 0, 지면 X, 잴 수 없으면 ?.</summary>
    public static string Price(BattleForecast.Result r) =>
        r.Ok == false ? "?" : r.Win == false ? "X" : r.Damage > 0 ? "-" + r.Damage : "0";

    // 툴팁은 마우스를 올릴 때마다 새로 뜨는데, 다음 임계(공격 +1..+10)와 아이템의 가정까지 재면 한 번에 열 판 남짓을
    // 끝까지 돌린다 — 못 이기는 싸움은 한 판이 시계 끝(600초)까지 간다. 그래서 기억해 두고, 값을 바꾸는 플레이어
    // 상태가 하나라도 달라지면 통째로 버린다: 전투마다 이어지는 치명 횟수·방어 게이지, 레벨·경험치·룬·장비·체력.
    static readonly Dictionary<(int id, int stage, int atk, int def), BattleForecast.Result> s_forecasts =
        new Dictionary<(int, int, int, int), BattleForecast.Result>();
    static (float, float, float, float, float, float, float, float, bool, bool, int, int, float, int, float) s_state;

    /// <summary>BattleForecast.Of 그대로다. 같은 상태에서 같은 것을 다시 물으면 기억해 둔 것을 준다.</summary>
    public static BattleForecast.Result Forecast(int id, int stage, int attackUp = 0, int defenceUp = 0)
    {
        GameManager g = Managers.Game;
        GameManager.CurPlayerData p = g.PlayerData;
        var state = (p.Attack, p.Defence, p.AttackSpeed, p.DefenceSpeed, p.Critical, p.CriticalAttack, p.CurHP, p.MaxHP,
                     p.IsDefence, p.IsCritical, p.Ability, p.Level, p.curExp, g.AttackCount, g.DefenceCoolTime);
        if (state.Equals(s_state) == false)
        {
            s_forecasts.Clear();
            s_state = state;
        }
        var key = (id, stage, attackUp, defenceUp);
        BattleForecast.Result r;
        if (s_forecasts.TryGetValue(key, out r) == false)
            s_forecasts[key] = r = BattleForecast.Of(id, stage, attackUp, defenceUp);
        return r;
    }

    /// <summary>툴팁이 다음 임계를 찾는 끝. 한 층에 오르는 공격력(레벨 2~5, 룬 1)의 두어 배다 — 도감(M)은 +30 까지 본다.</summary>
    public const int StepLimit = 10;

    /// <summary>
    /// 다음 임계 — 공격력을 몇 올리면(1..StepLimit) 이 싸움의 값이 내려가는가. 지는 싸움이면 이기게 되는 가장 작은 값.
    /// 없으면 0 이고 then 은 now 그대로다. 셈은 BattleForecast 의 가정(공격 +k)을 그대로 돌린다.
    /// </summary>
    public static int AttackStep(int id, int stage, BattleForecast.Result now, out BattleForecast.Result then)
    {
        then = now;
        if (now.Ok == false)
            return 0;
        for (int k = 1; k <= StepLimit; k++)
        {
            BattleForecast.Result r = Forecast(id, stage, k, 0);
            if (now.Win ? r.Damage < now.Damage : r.Win)
            {
                then = r;
                return k;
            }
        }
        return 0;
    }

    /// <summary>
    /// 맵에 서 있는(싸울 수 있는) 몬스터인가. 쓰러진 보스는 연출이 끝날 때까지 켜져 있지만 콜라이더를 잃고
    /// (DirectingManager.BossDeadEffect, UI_MonsterCard.CoDead), 그림이 꺼진 것은 보이지 않는 것이다.
    /// 맵 위 숫자와 도감이 같은 기준으로 센다.
    /// </summary>
    public static bool IsStanding(MonsterController mc)
    {
        if (mc == null || mc.gameObject.activeInHierarchy == false)
            return false;
        SpriteRenderer sr = mc.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.enabled == false)
            return false;
        mc.GetComponentsInChildren(false, s_colliders);
        foreach (Collider c in s_colliders)
        {
            if (c.enabled)
                return true;
        }
        return false;
    }

    /// <summary>예측 한 건의 색. 지면 빨강, 이기면 잃는 체력이 지금 체력의 몇 분의 몇인가로.</summary>
    public static Color Tone(BattleForecast.Result r, float curHp)
    {
        if (r.Ok == false)
            return Unknown;
        if (r.Win == false)
            return Fatal;
        float share = curHp > 0f ? r.Damage / curHp : 1f;
        return share <= 0.25f ? Safe : share <= 0.5f ? Caution : Danger;
    }
}
