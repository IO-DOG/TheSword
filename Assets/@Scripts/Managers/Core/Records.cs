using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 판을 넘어 남는 기록 (Records.json) — 본 결말, 본 장면, 잡은 우두머리, 평생 통계. 새 게임(DeleteGameData)은 지우지 않는다.
/// 체크포인트와 같은 폴더라 Steam 클라우드가 함께 옮긴다. 쓰다 꺼져도 옛 파일이 남게 바꿔 끼운다(SaveStore 와 같다).
///
/// 봇(GameEvents.IsAutoPlaying)이 도는 동안은 적지 않는다 — 사람이 보지 않은 결말·장면이 "본 것" 이 되면
/// 타이틀의 결말 수와 업적이 거짓말이 된다.
/// </summary>
public static class Records
{
    public const string FileName = "Records.json";
    public const int EndingCount = 3;       // seal · hold · dawn

    class Data
    {
        public int Version = 1;
        public HashSet<string> Endings = new HashSet<string>();     // StoryEnding 소문자
        public HashSet<string> Scenes = new HashSet<string>();      // StoryScene.Id
        public int FightsWon;
        public int Deaths;
        public int MaxFloor;                                         // 1 부터 (층 번호)
        public HashSet<int> Bosses = new HashSet<int>();            // 우두머리를 잡은 층 (킹 슬라임 4, 챕터 보스 20·40·…)
        // ACH_NO_SPILL 을 이어 센 값 (NoSpill — SteamHooks 가 센다)
        public int NoSpillRun;
        public int? NoSpillLast;
        public bool NoSpillSpilled;
        public Dictionary<string, int> Best = new Dictionary<string, int>();   // 판 점수 최고(낮을수록 좋다), GameMode 이름별
        public HashSet<string> ClearedModes = new HashSet<string>();         // 결말까지 간 규칙 (GameMode 이름)
        public HashSet<string> Feats = new HashSet<string>();                // 그 순간에만 풀리는 업적 id (SteamHooks.Earn)
        public HashSet<string> ScoredRuns = new HashSet<string>();           // 점수를 낸 판(LedgerState.RunId) — SwordLedger.FinishRun
        public Dictionary<string, int> BandBest = new Dictionary<string, int>();   // 띠(다섯 층) 최고 점수(낮을수록 좋다), "Normal:3" (규칙:띠 0부터)
        public Dictionary<string, int> BandStars = new Dictionary<string, int>();  // 띠에서 받은 가장 많은 별(1~3), 같은 키
    }

    static Data s_data;

    static string FilePath => Path.Combine(SaveStore.DirectoryPath, FileName);

    // 에디터는 도메인 리로드 없이 플레이를 되풀이한다 — 지난 플레이에서 읽은 것을 넘기지 않는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { s_data = null; s_blocked = false; }

    // 켤 때 Records.json 을 열지 못했다(다른 프로그램이 쥐었다) — 이번 실행은 쓰지 않는다. 빈 기록으로 좋은 파일을 덮지 않게.
    // ponytail: 실행 하나 동안의 기록(결말·업적 표시)은 메모리에만 남는다. 다음 실행이 파일을 다시 읽는다.
    static bool s_blocked;

    // GameEvents 는 SubsystemRegistration 에서 구독자를 비우므로 그 뒤(BeforeSceneLoad)에 붙는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        GameEvents.EndingReached += ending => Change(d => d.Endings.Add(ending) | d.ClearedModes.Add(CurrentMode.ToString()));
        GameEvents.RunScored += score => SubmitScore(CurrentMode, score);
        GameEvents.BattleEnded += (monsterId, won) => Change(d => { if (won) d.FightsWon++; else d.Deaths++; return true; });
        GameEvents.BossDefeated += (monsterId, stageId) => Change(d => d.Bosses.Add(stageId + 1));
        GameEvents.FloorEntered += (stageId, firstVisit) => Change(d =>
        {
            if (stageId + 1 <= d.MaxFloor) return false;
            d.MaxFloor = stageId + 1;
            return true;
        });
    }

    public static bool SceneSeenEver(string sceneId) => Current.Scenes.Contains(sceneId);
    public static void MarkSceneSeen(string sceneId) => Change(d => d.Scenes.Add(sceneId));
    public static bool EndingSeen(string ending) => Current.Endings.Contains(ending);
    public static int EndingsSeen => Current.Endings.Count;
    public static int FightsWon => Current.FightsWon;
    public static int Deaths => Current.Deaths;
    public static int MaxFloor => Current.MaxFloor;
    // 그 층(1 부터)의 우두머리를 잡은 적이 있다. Steam 없이·체험판에서 잡은 것도 남아 업적이 뒤늦게 풀린다(SteamHooks).
    public static bool BossBeaten(int floor) => Current.Bosses.Contains(floor);
    // 그 규칙의 최고 점수(낮을수록 좋다). 없으면 0.
    public static int BestScore(GameMode mode) => Current.Best != null && Current.Best.TryGetValue(mode.ToString(), out int v) ? v : 0;
    // 더 낮으면(좋으면) 적는다. 봇 판은 적지 않는다(Change).
    public static void SubmitScore(GameMode mode, int score) => Change(d =>
    {
        string key = mode.ToString();
        if (score <= 0 || (d.Best.TryGetValue(key, out int old) && old <= score))
            return false;
        d.Best[key] = score;
        return true;
    });
    // 그 규칙으로 결말까지 간 적이 있다(탑의 법 업적·타이틀 표시).
    public static bool ModeCleared(GameMode mode) => Current.ClearedModes != null && Current.ClearedModes.Contains(mode.ToString());
    // 그 순간에만 풀리는 업적(한 끗·맨손의 셈·새벽 결말의 둘 …)을 해낸 적이 있다. 다시 셀 수 없는 것이라 해낸 순간 적어 둔다 —
    // Steam 없이·체험판에서 해낸 것도 다음에 붙을 때 SteamHooks.SyncRecords 가 준다.
    public static IReadOnlyCollection<string> Feats => Current.Feats;
    public static void Earn(string achievementId) => Change(d => d.Feats.Add(achievementId));
    // 이 판(RunId)이 점수를 낸 적이 있다. 클라우드로 오가는 이 파일에 적는다 — PlayerPrefs(레지스트리)에만 두었더니 다른 PC·덱이
    // 동기화된 100층 입구 체크포인트로 같은 판을 다시 끝내 순위표에 또 올릴 수 있었다(11-21 리뷰).
    public static bool RunScored(string runId) => Current.ScoredRuns != null && Current.ScoredRuns.Contains(runId);
    public static void MarkRunScored(string runId) => Change(d => (d.ScoredRuns ??= new HashSet<string>()).Add(runId));

    // 띠(다섯 층)의 최고 점수 = round(1000·치름/기준), 낮을수록 좋다(판 점수와 같은 셈 — 곁길을 걸러 치름만 줄여서는 안 좋아진다). 없으면 0.
    public static int BandBest(GameMode mode, int band) =>
        Current.BandBest != null && Current.BandBest.TryGetValue(mode + ":" + band, out int v) ? v : 0;
    /// <summary>띠 결산을 올리고 그 전의 최고 점수를 돌려준다(없었으면 0). 점수는 더 낮을 때, 별은 더 많을 때만 적는다. 봇 판은 적지 않는다(Change).</summary>
    public static int SubmitBand(GameMode mode, int band, int score, int stars)
    {
        if (GameEvents.IsAutoPlaying)       // 봇 판은 적지도 견주지도 않는다 — 사람의 최고에 대고 "새 기록" 을 띄우지 않게
            return 0;
        int old = BandBest(mode, band);
        string key = mode + ":" + band;
        Change(d =>
        {
            bool changed = false;
            if (score > 0 && (old == 0 || score < old))
            {
                (d.BandBest ??= new Dictionary<string, int>())[key] = score;
                changed = true;
            }
            d.BandStars ??= new Dictionary<string, int>();
            if (stars > 0 && (d.BandStars.TryGetValue(key, out int had) == false || stars > had))
            {
                d.BandStars[key] = stars;
                changed = true;
            }
            return changed;
        });
        return old;
    }
    // 그 띠에서 받은 가장 많은 별(없으면 0) — 장부 창(UI_LedgerPopup)의 "최고" 줄
    public static int BandStarsBest(GameMode mode, int band) =>
        Current.BandStars != null && Current.BandStars.TryGetValue(mode + ":" + band, out int v) ? v : 0;
    /// <summary>그 규칙에서 띠마다 받은 가장 많은 별의 합(타이틀 "★ 47/60"). 띠는 20개, 띠마다 셋까지.</summary>
    public static int StarsTotal(GameMode mode)
    {
        int sum = 0;
        if (Current.BandStars != null)
            foreach (KeyValuePair<string, int> kv in Current.BandStars)
                if (kv.Key.StartsWith(mode + ":"))
                    sum += kv.Value;
        return sum;
    }

    static GameMode CurrentMode => Managers.Game?.PlayerData?.Mode ?? GameMode.Normal;

    /// <summary>
    /// ACH_NO_SPILL 의 이어 센 값 — 넘치지 않고 끝낸 층 수, 마지막으로 센 새 층(stageId), 그 층에서 체크포인트에 굳은 넘침.
    /// 끄고 다시 켜도 잇는다. 새 판은 층 번호가 이어지지 않아 SteamHooks 가 스스로 처음부터 센다.
    /// </summary>
    public static (int run, int? last, bool spilled) NoSpill
    {
        get => (Current.NoSpillRun, Current.NoSpillLast, Current.NoSpillSpilled);
        set => Change(d =>
        {
            if ((d.NoSpillRun, d.NoSpillLast, d.NoSpillSpilled) == value)
                return false;
            (d.NoSpillRun, d.NoSpillLast, d.NoSpillSpilled) = value;
            return true;
        });
    }

    static Data Current
    {
        get
        {
            if (s_data != null)
                return s_data;
            // 깨졌으면 .bak 을 읽는다(SaveStore.ReadText) — 깨진 본 파일은 .corrupt 로 비켜 두어 빈 기록으로 덮지 않는다.
            bool any = File.Exists(FilePath) || File.Exists(FilePath + ".bak");
            s_data = SaveStore.ReadText(FilePath, JsonConvert.DeserializeObject<Data>, out s_blocked) ?? new Data();
            if (any == false)
            {
                // 예전에는 마지막으로 본 결말 하나만 PlayerPrefs 에 남았다 — 처음 한 번 옮겨 적는다.
                string last = PlayerPrefs.GetString(StoryDirector.ClearedEndingKey, "");
                if (string.IsNullOrEmpty(last) == false)
                    s_data.Endings.Add(last);
            }
            return s_data;
        }
    }

    // changed 가 false 면 쓰지 않는다 (HashSet.Add 가 이미 있던 값이면 false).
    static void Change(Func<Data, bool> change)
    {
        if (GameEvents.IsAutoPlaying || change(Current) == false || s_blocked)
            return;
        try
        {
            SaveStore.WriteText(FilePath, JsonConvert.SerializeObject(s_data), durable: false);   // 싸움마다 쓴다
        }
        catch (Exception e) when (SaveStore.IsSaveError(e))
        {
            Debug.LogWarning($"[Records] {FileName} 를 쓰지 못했다: {e.Message}");
        }
    }
}
