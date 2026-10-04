using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마검의 장부 — 한 판 동안 치른 값(HP)을 적는다(기획 L1·L2). 상태는 CurPlayerData.Ledger 에 있어서 체크포인트와 함께
/// 저장되고, 층을 다시 시작하면 그 층 입구의 장부로 되돌아간다.
///
/// 계약(다른 레인이 쓴다): LedgerState 의 필드 이름, GameEvents.FloorTallied / BandTallied / RunScored.
/// 나머지 로직은 RUN 레인이 이 파일에 채운다.
/// </summary>
[Serializable]
public class LedgerState
{
    // 기준이 있는 싸움만 적는다 — 1~4층 도입부(기준 0, 마검의 눈이 뜨기 전)는 장부 밖이다.
    public int Paid;            // 이번 판에 싸움으로 잃은 HP 합 (흡혈로 되찾은 것은 뺀다) + 제단에 낸 HP. 점수 = 1000·Paid/Par
    public int Foretold;        // 그 싸움들의 예측(LastBattle.Foretold) 합 — 스킬 없이 싸우면 Paid 와 같다
    public int Par;             // 그 싸움들의 기준 값(MonsterData.ParLoss) 합 — 열린 층의 싸움만. 지난 층의 싸움은 Paid·Foretold 에만 든다
    public int Spill;           // 넘쳐 버린 회복 합 (점수에는 안 들어간다)
    public int AltarPaid;       // 제단에 낸 HP 합 (Paid 에도 들어간다. 층·띠 합에는 안 든다 — 별은 싸움만 잰다)
    // 층·띠 합은 기준을 매긴 싸움(열린 층의 싸움)만 센다 — 별은 같은 싸움끼리 견준다.
    public int FloorPaid;       // 이 층에서 — 처음 오는 층에 들어설 때 0 으로
    public int FloorPar;
    public int FloorForetold;
    public int BandPaid;        // 이 띠(5층)에서 — 띠가 끝날 때 집계하고 0 으로
    public int BandPar;
    public List<int> BandStars = new List<int>();   // 띠마다 별 (1~3, 기준이 없던 띠는 0), 띠 순서대로
    public int BandForetold;    // 이 띠의 예언 합 (결산 창의 "예언")
    public int FloorStage;      // Floor* 가 어느 층(stageId) 몫인가. 새 층에 처음 들어설 때 이 층을 한 번 닫는다
    // 판의 이름 — 새 게임(CurPlayerData.Clear)이 짓는다. 점수는 판마다 한 번 낸다. 장부 없이 저장된 옛 체크포인트는 비어 있다:
    // 그 장부는 불러온 층부터만 적혀 판 점수가 아니라 내지 않는다(FinishRun). 예전에는 여기서 지어 불러올 때마다 새 이름이라
    // 같은 판이 몇 번이고 점수를 냈다.
    public string RunId;
}

/// <summary>
/// 장부를 적는 곳. 적는 때는 넷이다.
///   싸움에서 이겼다(GameEvents.BattleEnded) — 잃은 HP·예측·기준을 판 합과 층·띠 합에 더한다. 기준이 없는 싸움(1~4층)은 적지 않는다.
///     기준은 열린 층(FloorStage)에서만 매긴다. 지난 층으로 돌아가 싸우면 잃은 HP·예측만 판 합에 든다 — 기준은 그 층의 정답 레벨로
///     매긴 값이라, 레벨을 올린 뒤 돌아와 곁길을 쓸면 거의 공짜로 기준만 쌓였다(100층 보스 뒤 워프로 쓸면 점수가 크게 좋아지고 띠는 ★★★).
///     진 싸움은 적지 않는다(체크포인트가 되돌린다).
///   회복이 넘쳤다(GameEvents.ItemPicked) — Spill.
///   띠의 마지막 층 위 계단을 처음 오른다(CoCloseBand, PortalController) — 띠를 닫고(별·BandTallied) 결산 창·제단을 띄운다.
///   처음 오는 층에 들어선다(EnterNewFloor, PortalController) — 열려 있던 층을 닫는다(FloorTallied).
/// 층을 닫는 것은 GameManager.EnterFloor 가 체크포인트를 쓰기 "전" 이다. FloorEntered(쓴 뒤에 온다)에서 닫으면 체크포인트에 앞 층의
/// 합이 남아, 그 층을 다시 시작한 뒤 한 번 더 닫혔다. 닫은 것은 장부 안에 남으니(FloorStage·BandStars) 같은 층·띠를 두 번 닫지 않는다.
/// </summary>
public static class SwordLedger
{
    public const int BandFloors = 5;        // 띠 = 다섯 층, (층-1)/5 — generate_content.BAND_FLOORS·MonsterTint 와 같은 경계
    const string ScoredRunKey = "LEDGER_SCORED_RUN";    // 점수를 낸 판(RunId) — 이어하기로 100층을 다시 올라도 두 번 내지 않는다

    static LedgerState s_finished;          // 100층 계단에서 끝낸 판 — 엔딩 씬의 판 카드가 한 번 가져간다

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => s_finished = null;

    // GameEvents 는 SubsystemRegistration 에서 구독자를 비우므로 그 뒤(BeforeSceneLoad)에 붙는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        GameEvents.BattleEnded += OnBattleEnded;
        GameEvents.ItemPicked += (itemId, heal, overflow) =>
        {
            if (overflow > 0)
                State.Spill += overflow;
        };
    }

    /// <summary>지금 판의 장부. 장부 없이 저장된 옛 체크포인트면 빈 장부를 세운다.</summary>
    public static LedgerState State
    {
        get
        {
            GameManager.CurPlayerData p = Managers.Game.PlayerData;
            if (p.Ledger == null || p.Ledger.BandStars == null)
                p.EnsureLists();
            return p.Ledger;
        }
    }

    /// <summary>★ 통과, ★★ 기준 이하, ★★★ 기준의 70% 이하. 기준이 없는 띠는 0 — 별을 보이지 않는다.</summary>
    public static int Stars(int paid, int par) => par <= 0 ? 0 : paid * 10 <= par * 7 ? 3 : paid <= par ? 2 : 1;

    /// <summary>판 점수 round(1000·Paid/Par), 낮을수록 좋다. 기준이 없으면 0(점수 없음).</summary>
    public static int Score(LedgerState l) => l != null && l.Par > 0 ? (int)Math.Round(1000.0 * l.Paid / l.Par) : 0;

    // 이긴 싸움의 값. 잃은 HP 는 전투창의 건너뛰기 요약(UI_BattlePopup.ShowSummary)·봇의 예측 대조와 같은 셈이다 —
    // 이겨서 레벨이 오르면 지금 체력도 같이 늘므로(GameManager.LevelUp) 그 몫을 되돌려 센다.
    static void OnBattleEnded(int monsterId, bool won)
    {
        if (won == false || LastBattle.MonsterId != monsterId)
            return;
        // 기준은 지금 쓰는 몬스터 표의 것(탑의 법이면 MonsterData_Tower). 1~4층 도입부(킹 슬라임·분열 슬라임까지)는 0 —
        // 장부 밖이다. 넣으면 누구나 같은 300 남짓을 안고 시작해서, 띠마다 ★★ 여도 판 점수는 기준을 넘었다.
        int par = Managers.Data.MonsterDic.TryGetValue(monsterId, out Data.MonsterData md) ? Mathf.RoundToInt(md.ParLoss) : 0;
        if (par <= 0)
            return;
        GameManager.CurPlayerData p = Managers.Game.PlayerData;
        int paid = Mathf.Max(0, Mathf.RoundToInt(LastBattle.HpBefore - LastBattle.HpAfter + (p.MaxHP - LastBattle.MaxHp)));
        int foretold = Mathf.Max(0, LastBattle.Foretold);     // 잴 수 없던 싸움(-1)은 예언에 넣지 않는다

        LedgerState l = State;
        l.Paid += paid;
        l.Foretold += foretold;
        // 지난 층의 싸움 — 치른 값만. 층·띠 합(별·FloorTallied)에도 넣지 않는다: 그 띠는 이미 닫혔거나 다른 층의 것이다.
        if (p.CurStageid < l.FloorStage)
            return;
        l.Par += par;
        l.FloorPaid += paid; l.FloorPar += par; l.FloorForetold += foretold;
        l.BandPaid += paid; l.BandPar += par; l.BandForetold += foretold;
    }

    /// <summary>
    /// 처음 오는 층에 들어섰다 — PortalController 가 체크포인트(EnterFloor)보다 먼저 부른다. 열려 있던 층을 닫아 알리고
    /// (FloorTallied) 이 층을 연다. 워프·내려가기·다녀온 층은 부르지 않는다 — 그 층에서 싸운 값은 열린 층에 든다.
    /// </summary>
    public static void EnterNewFloor(int stageId)
    {
        LedgerState l = State;
        if (l.FloorStage == stageId)
            return;
        GameEvents.RaiseFloorTallied(l.FloorStage, l.FloorPaid, l.FloorPar);
        l.FloorStage = stageId;
        l.FloorPaid = l.FloorPar = l.FloorForetold = 0;
    }

    /// <summary>
    /// 위 계단을 처음 오를 때(PortalController). 띠의 마지막 층(5·10·…·95, 결말 앞의 100)이면 그 띠를 닫고 결산 창(과 제단)을
    /// 띄워 닫힐 때까지 기다린다. 이미 닫은 띠(BandStars 에 있다)면 아무것도 안 한다. 닫은 장부는 다음 층 입구의 체크포인트에
    /// 실리므로 죽거나 다시 해도 두 번 닫지 않고, 이 층 입구로 되돌리면 닫기 전으로 같이 돌아간다.
    /// </summary>
    public static IEnumerator CoCloseBand(int stageId)
    {
        if ((stageId + 1) % BandFloors != 0)
            yield break;
        int band = stageId / BandFloors;
        LedgerState l = State;
        if (l.BandStars.Count > band)
            yield break;
        while (l.BandStars.Count < band)    // 장부 없이 저장된 옛 판 — 모르는 띠는 별 없음
            l.BandStars.Add(0);

        int stars = Stars(l.BandPaid, l.BandPar);
        l.BandStars.Add(stars);
        int foretold = l.BandForetold, paid = l.BandPaid, par = l.BandPar;
        l.BandForetold = l.BandPaid = l.BandPar = 0;
        if (stars > 0)                      // 알림은 별 1~3 만 (GameEvents 계약) — 기준이 없는 띠는 조용히 닫는다
            GameEvents.RaiseBandTallied(band, stars);

        // 창이 못 떠도 계단은 가야 한다 — 이 코루틴은 계단 코루틴 안에서 돈다(예외가 나면 계단째 죽는다).
        UI_TallyPopup popup = null;
        try { popup = UI_TallyPopup.Show(stageId, stars, foretold, paid, par); }
        catch (Exception e) { Debug.LogException(e); }
        while (popup != null)
            yield return null;
    }

    /// <summary>
    /// 100층 위 계단 — 판을 끝냈다(결말 직전, PortalController). 마지막 층을 닫고, 점수를 한 번 낸다(RunScored).
    /// 봇·기준 없는 판은 내지 않는다. DebugPlay 는 계단을 밟지 않으니 여기에 오지 않는다.
    /// </summary>
    public static void FinishRun()
    {
        LedgerState l = State;
        EnterNewFloor(Managers.Game.PlayerData.CurStageid + 1);    // 올라갈 층은 없지만 이 층은 닫는다
        s_finished = l;
        int score = Score(l);
        // 봇의 완주 기록에 남긴다 — 사람 판이면 판 카드와 같은 값이다.
        Debug.Log($"[Ledger] 완주: 예언 {l.Foretold} 치름 {l.Paid} (제단 {l.AltarPaid}) 기준 {l.Par} 넘침 {l.Spill} " +
                  $"점수 {score} 별 {string.Join("", l.BandStars)}");
        if (score <= 0 || GameEvents.IsAutoPlaying || string.IsNullOrEmpty(l.RunId)
            || PlayerPrefs.GetString(ScoredRunKey, "") == l.RunId)
            return;
        PlayerPrefs.SetString(ScoredRunKey, l.RunId);
        PlayerPrefs.Save();
        GameEvents.RaiseRunScored(score);
    }

    /// <summary>끝낸 판의 장부를 한 번 가져간다(엔딩 씬의 판 카드). 계단으로 끝낸 판이 아니면 null.</summary>
    public static LedgerState TakeFinishedRun()
    {
        LedgerState run = s_finished;
        s_finished = null;
        return run;
    }

    #region 제단 (기획 L6)
    /// <summary>계단의 제단이 지금 파는 것. 값·문턱은 StageInfoData 의 Altar* 열에서 generate_content 와 같은 식으로 잰다.</summary>
    public struct AltarOffer
    {
        public float Atk, Def;      // 한 번에 받는 공격·방어 (둘 중 하나)
        public double Percent;      // 이번 값, 최대 HP 의 % = AltarPrice + AltarPriceStep·n(n−1), n = 산 횟수 + 1 (altar_price)
        public int Price;           // 이번 값(HP)
        public int Keep;            // 바치고 나서 남아야 하는 HP (보이기용 — 판정은 Sells)
        public bool Sells;          // 지금 HP 로 살 수 있다 (altar_sells)
    }

    /// <summary>
    /// 그 층 계단에 제단이 있으면 true. 판정은 generate_content.altar_sells 그대로다 — 반올림 없이, 같은 순서의 배정밀도 셈:
    /// 값 = 최대HP·pct/100, 판다 = 지금HP − 값 ≥ 최대HP·문턱/100 (탑의 법이면 AltarReserveTower).
    /// 파이썬은 체력을 실수로 들고 가지만 게임의 체력은 정수라(피해·회복이 반올림된다) 내는 HP 는 값을 내린 정수다 —
    /// 식보다 더 받지 않으니 "바친 뒤에도 문턱만큼 남는다" 는 파이썬의 보장이 그대로 선다.
    /// </summary>
    public static bool Altar(int stageId, out AltarOffer offer)
    {
        offer = default;
        if (Managers.Data.StageInfoDic.TryGetValue(stageId, out Data.StageInfoData info) == false
            || info.AltarPrice <= 0f || (info.AltarAtk <= 0f && info.AltarDef <= 0f))
            return false;
        GameManager.CurPlayerData p = Managers.Game.PlayerData;
        int n = p.AltarBought + 1;
        double pct = info.AltarPrice + (double)info.AltarPriceStep * n * (n - 1);
        double reserve = p.Mode == GameMode.Tower ? info.AltarReserveTower : info.AltarReserve;
        double max = p.MaxHP;
        double price = max * pct / 100.0;
        offer.Atk = info.AltarAtk;
        offer.Def = info.AltarDef;
        offer.Percent = pct;
        offer.Price = (int)Math.Floor(price);
        offer.Keep = (int)Math.Ceiling(max * reserve / 100.0);
        offer.Sells = p.CurHP - price >= max * reserve / 100.0;
        return true;
    }

    /// <summary>
    /// 제단에서 하나를 받는다 (결산 창이 부른다 — 한 번 들를 때 하나). 능력치는 룬과 같은 길로 기본 능력치에 더한다
    /// (ConsumableItem.PickUp): 장비는 SwapEquip 이 제 몫만 빼고 더해서 섞이지 않는다. 바친 HP 는 Paid·AltarPaid 에 든다.
    /// HUD 를 다시 칠해 맵 위 예측도 다시 잰다(HudRefreshed).
    /// </summary>
    public static bool BuyAltar(int stageId, bool attack)
    {
        if (Altar(stageId, out AltarOffer o) == false || o.Sells == false || (attack ? o.Atk : o.Def) <= 0f)
            return false;
        GameManager.CurPlayerData p = Managers.Game.PlayerData;
        p.CurHP -= o.Price;
        if (attack)
            p.Attack += o.Atk;
        else
            p.Defence += o.Def;
        p.AltarBought++;
        LedgerState l = State;
        l.Paid += o.Price;
        l.AltarPaid += o.Price;

        Managers.Object?.ShowRuneGain(attack ? o.Atk : 0f, attack ? 0f : o.Def, 0f);   // 머리 위 글과 룬 차임 (예외는 그쪽이 삼킨다)
        if (Managers.Game.GameScene != null)
            Managers.Game.GameScene.Refresh();
        return true;
    }
    #endregion
}
