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

    // 표시에 쓰는 색. 맵 위 숫자·도감·툴팁이 같은 눈금을 쓴다 (지금 체력에 대한 몫).
    public static readonly Color Safe = new Color32(150, 230, 140, 255);    // 1/4 이하
    public static readonly Color Caution = new Color32(245, 220, 90, 255);  // 1/2 이하
    public static readonly Color Danger = new Color32(255, 150, 60, 255);   // 그 위, 살아남는다
    public static readonly Color Fatal = new Color32(255, 70, 70, 255);     // 진다
    public static readonly Color Unknown = new Color32(170, 170, 170, 255);
    public const string LevelUpMark = "<size=75%><color=#FFD84A>LV+</color></size>";   // 이 싸움으로 레벨이 오른다

    /// <summary>다음 치명타가 몇 번째 공격인가 (1 = 바로 다음). 치명 주기가 없으면 0.
    /// 치명 횟수는 전투 사이에 이어진다(UI_BattlePopup) — BattleStepper 가 ">=" 로 세는 것과 같다.</summary>
    public static int HitsToCrit()
    {
        int period = (int)Managers.Game.PlayerData.Critical;
        return period > 0 ? Mathf.Max(1, period - Managers.Game.AttackCount) : 0;
    }

    static readonly List<Collider> s_colliders = new List<Collider>();

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
