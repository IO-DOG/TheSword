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
    }

    static Data s_data;

    static string FilePath => Path.Combine(SaveStore.DirectoryPath, FileName);

    // 에디터는 도메인 리로드 없이 플레이를 되풀이한다 — 지난 플레이에서 읽은 것을 넘기지 않는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => s_data = null;

    // GameEvents 는 SubsystemRegistration 에서 구독자를 비우므로 그 뒤(BeforeSceneLoad)에 붙는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        GameEvents.EndingReached += ending => Change(d => d.Endings.Add(ending));
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
            s_data = new Data();
            try
            {
                if (File.Exists(FilePath))
                    s_data = JsonConvert.DeserializeObject<Data>(File.ReadAllText(FilePath)) ?? new Data();
                else
                {
                    // 예전에는 마지막으로 본 결말 하나만 PlayerPrefs 에 남았다 — 처음 한 번 옮겨 적는다.
                    string last = PlayerPrefs.GetString(StoryDirector.ClearedEndingKey, "");
                    if (string.IsNullOrEmpty(last) == false)
                        s_data.Endings.Add(last);
                }
            }
            catch (Exception e) when (SaveStore.IsSaveError(e))
            {
                Debug.LogWarning($"[Records] {FileName} 를 읽지 못했다 — 빈 기록으로 센다: {e.Message}");
            }
            return s_data;
        }
    }

    // changed 가 false 면 쓰지 않는다 (HashSet.Add 가 이미 있던 값이면 false).
    static void Change(Func<Data, bool> change)
    {
        if (GameEvents.IsAutoPlaying || change(Current) == false)
            return;
        try
        {
            Directory.CreateDirectory(SaveStore.DirectoryPath);
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonConvert.SerializeObject(s_data));
            if (File.Exists(FilePath))
                File.Replace(temp, FilePath, null);
            else
                File.Move(temp, FilePath);
        }
        catch (Exception e) when (SaveStore.IsSaveError(e))
        {
            Debug.LogWarning($"[Records] {FileName} 를 쓰지 못했다: {e.Message}");
        }
    }
}
