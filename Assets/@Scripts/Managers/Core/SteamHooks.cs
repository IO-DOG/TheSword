// SteamManager 와 같은 조건 — Steam 코드가 없는 플랫폼에서는 이 파일이 통째로 빠진다(아무도 부르지 않는다).
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if !DISABLESTEAMWORKS
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 업적·통계·순위표·Rich Presence (MASTER_PLAN §7). GameEvents 에 귀만 대고 게임 로직은 고치지 않는다.
///
/// 사람이 한 일만 센다 — 봇이 돌 때(GameEvents.IsAutoPlaying), Steam 이 없을 때(SteamManager.Initialized),
/// 체험판(DEMO)에서는 Steam 에 아무것도 하지 않는다(판 점수만 적어 뒀다가 다음에 붙으면 올린다 — 순위표 절).
/// 통계는 Records 를 옮기되 올리기만 한다(Records 도 봇 판은 적지 않는다).
/// 업적·통계 id 는 Steamworks 관리 페이지에 같은 이름으로 만들어 둬야 풀린다 — 없는 id 는 Steam 이 거절하고 끝이다.
/// 순위표 이름·자세한 값의 순서는 Steam/leaderboards.md 와 같아야 한다.
/// </summary>
public static class SteamHooks
{
    const int FloorsPerChapter = 20;
    const int BandsPerChapter = 4;      // 띠 = 5층, 번호 (층-1)/5 — 장부의 BandStars 와 같은 경계
    const int Chapters = 5;
    const int LastFloor = 100;
    const int KingSlimeFloor = 4;
    const int ForestFloors = 4;         // 손수 만든 1~4층 — 친구 목록에는 숲 이름(#S_Forest)
    const int NoSpillFloors = 5;
    static readonly string[] Endings = { "seal", "hold", "dawn" };     // StoryEnding 소문자 — Records 가 적는 이름

    // 순위표. 점수는 콘텐츠(지도·몬스터 표)에 달려 있다 — 콘텐츠를 바꾸면 _V 를 올려 새 표에서 센다(leaderboards.md).
    const string BoardNormal = "LB_NORMAL_V1";
    const string BoardTower = "LB_TOWER_V1";

#if DEMO
    static bool Active => false;        // 체험판은 주지 않는다. 본편이 같은 기록(Records)을 보고 준다(SyncRecords)
#else
    static bool Active => SteamManager.Initialized && GameEvents.IsAutoPlaying == false;
#endif

    // ACH_NO_SPILL 에서 아직 체크포인트에 굳지 않은 넘침. 체크포인트를 부르면(씬을 다시 올린다) 물약이 제자리라 없던 일이다.
    static bool s_spillUnsaved;
    static int s_presenceFloor = -1;    // Rich Presence 에 마지막으로 적은 층 (stageId). -1 은 비웠다

    static readonly HashSet<string> s_asked = new HashSet<string>();   // 이번 실행에 Steam 에 물어본 업적

    // 판 점수(RunScored)와 결말(EndingReached)은 어느 쪽이 먼저 올지 모른다 — 둘이 모이면 올린다. 결말 뒤 엔딩 씬까지는
    // 그 판이라 거기서 점수가 와도 결말을 붙인다. 결말이 오지 않은 채 게임 화면·타이틀이 서면(결말을 이미 본 판·선택 전에 끔)
    // 결말을 모른다고 적고 올린다(OnSceneLoaded).
    // 점수는 받자마자 PlayerPrefs 에 적는다. 장부는 알리기 전에 "낸 판" 을 적어서(SwordLedger.FinishRun) 같은 판을 다시 알리지
    // 않는다 — 결말을 기다리는 사이 꺼지면 정적 값과 함께 그 점수가 Steam 에서 영영 사라졌다. 이제는 다음에 씬이 설 때 올리고,
    // Steam 이 받았다고 답해야 지운다. Steam 없이 끝낸 판도 다음에 붙었을 때 그렇게 올라간다.
    // ponytail: 한 칸뿐이다 — 올리기 전에 다음 판 점수가 오면 앞의 것을 덮는다. 둘 다 Steam 없이 끝낸 때만 생긴다.
    const string PendingKey = "STEAM_PENDING_SCORE";
    [System.Serializable]
    class PendingScore { public string RunId; public string Board; public int Score; public int[] Details; }
    static string s_ending;             // 이 게임 화면에서 본 결말
    static bool s_uploading;            // Steam 의 답을 기다린다 — 그 사이 씬이 바뀌어도 같은 점수를 또 올리지 않는다

    // 순위표는 CallResult 로 답을 받는다. 답이 올 때까지 붙잡아 둬야 한다 — GC 가 거두면 답이 오지 않는다.
    // Steam 은 올리기를 한 번에 하나만 받는다(10분에 10번). 판 끝에 한 번이라 하나씩이면 된다.
    static CallResult<LeaderboardFindResult_t> s_find;
    static CallResult<LeaderboardScoreUploaded_t> s_upload;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_spillUnsaved = false;
        s_presenceFloor = -1;
        s_asked.Clear();
        s_ending = null;
        s_uploading = false;
        s_find = null;          // 지난 플레이의 답 기다리기는 SteamAPI.Shutdown 이 이미 풀었다
        s_upload = null;
    }

    // GameEvents 는 SubsystemRegistration 에서 구독자를 비우므로 그 뒤(BeforeSceneLoad)에 붙는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        GameEvents.FloorEntered += OnFloorEntered;
        GameEvents.HudRefreshed += OnHudRefreshed;
        GameEvents.BossDefeated += OnBossDefeated;
        GameEvents.BattleEnded += OnBattleEnded;
        GameEvents.EndingReached += OnEndingReached;
        GameEvents.ItemPicked += (itemId, heal, overflow) => s_spillUnsaved |= overflow > 0;
        // ACH_UNDER_PAR — 기준 값이 있는 층(싸운 층)을 그보다 싸게 끝냈다.
        GameEvents.FloorTallied += (stageId, paid, par) => { if (par > 0 && paid < par) Unlock("ACH_UNDER_PAR"); };
        GameEvents.BandTallied += (band, stars) => { if (ChapterAllThree(band / BandsPerChapter, band, stars)) Unlock("ACH_CH_3STAR"); };
        GameEvents.RunScored += OnRunScored;
        // SceneManager 의 이벤트는 플레이마다 비워지지 않는다(도메인 리로드를 끈 에디터) — 빼고 건다.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 게임 화면은 체크포인트(이어하기·이 층 다시·체크포인트 목록·죽음)나 새 판으로만 다시 선다 — 그 뒤의 넘침은 없던 일이다.
    // 층 밖(타이틀·인트로·엔딩)에서는 친구 목록에 지난 층을 남기지 않는다. 게임 화면은 HUD 가 서며 다시 적는다.
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        s_spillUnsaved = false;
        if (scene.name != nameof(Define.Scene.EndingScene))
        {
            UploadPending();                // 지난 실행에서 못 올린 것도 여기서 (켜서 처음 서는 타이틀)
            s_ending = null;
        }
        if (Active && scene.name != nameof(Define.Scene.GameScene))
        {
            SteamFriends.ClearRichPresence();
            s_presenceFloor = -1;
        }
    }

    static void OnFloorEntered(int stageId, bool firstVisit)
    {
        RememberChapterStart(stageId, firstVisit);
        if (Active == false)
            return;
        CheckContract();
        SetPresence(stageId);
        CountNoSpill(stageId, firstVisit);
        SyncRecords(Mathf.Max(Records.MaxFloor, stageId + 1));     // Records 가 이 층을 아직 안 적었을 수 있다
    }

    // HUD 는 계단·워프·보스 문 끝(FloorEntered 바로 앞)과 게임 화면이 설 때 다시 그려진다. 뒤쪽(이어하기·이 층 다시·죽음)은
    // FloorEntered 가 오지 않으니 여기서 층을 적는다. 같은 층이면 다시 쓰지 않는다(전투·아이템마다 온다).
    static void OnHudRefreshed()
    {
        if (Active == false)
            return;
        CheckContract();
        int? stageId = Managers.Game?.PlayerData?.CurStageid;
        if (stageId.HasValue)
            SetPresence(stageId.Value);
    }

    // 친구 목록의 "12층 · 이끼 낀 지하 묘소". 문구는 Steamworks 의 Rich Presence 현지화 파일이 맡는다 — 1~4층은 숲 이름,
    // 탑의 법이면 앞에 규칙 이름이 붙은 토큰(…Tower). 규칙은 새 판에서만 바뀌고 그때는 씬이 바뀌며 비운다(OnSceneLoaded).
    static void SetPresence(int stageId)
    {
        if (stageId == s_presenceFloor)
            return;
        s_presenceFloor = stageId;
        bool tower = Managers.Game?.PlayerData?.Mode == GameMode.Tower;
        SteamFriends.SetRichPresence("floor", (stageId + 1).ToString());
        SteamFriends.SetRichPresence("chapter", (stageId / FloorsPerChapter).ToString());
        SteamFriends.SetRichPresence("mode", tower ? "tower" : "normal");
        SteamFriends.SetRichPresence("steam_display", stageId < ForestFloors
            ? (tower ? "#S_ForestTower" : "#S_Forest")
            : (tower ? "#S_FloorTower" : "#S_Floor"));
    }

    // ACH_NO_SPILL — 새 층에 처음 들어설 때마다 방금 끝낸 층을 하나 센다. 그 층에서 회복이 넘쳤으면 처음부터.
    // 센 값은 Records 에 둔다(끄고 다시 켜도 잇는다). 넘침은 체크포인트를 따른다: 층을 옮기면 EnterFloor 가 체크포인트를
    // 새로 쓰니 그때까지의 넘침은 굳고(spilled), 그 전에 체크포인트를 부르면 없던 일이다(OnSceneLoaded).
    static void CountNoSpill(int stageId, bool firstVisit)
    {
        var (run, last, spilled) = Records.NoSpill;
        spilled |= s_spillUnsaved;
        s_spillUnsaved = false;
        if (firstVisit && stageId != last)
        {
            // 방금 끝낸 층이 앞서 센 층 바로 다음일 때만 잇는다. 첫 층, 새 판, 옛 체크포인트를 부른 뒤는 새로 센다.
            run = spilled || stageId != last + 1 ? 0 : run + 1;
            last = stageId;
            spilled = false;
            if (run >= NoSpillFloors)
                Unlock("ACH_NO_SPILL");
        }
        Records.NoSpill = (run, last, spilled);
    }

    static void OnBossDefeated(int monsterId, int stageId)
    {
        if (monsterId == Define.KingSlime)
            Unlock("ACH_KINGSLIME");
        int floor = stageId + 1;
        if (floor % FloorsPerChapter != 0)
            return;
        Unlock("ACH_BOSS" + floor);                                     // 20·40·60·80·100층 챕터 보스
        // LastBattle 은 전투창이 이 알림 바로 앞에 채운다. 다른 전투의 것이면(채우지 못한 길) 믿지 않는다.
        if (LastBattle.MonsterId == monsterId && LastBattle.SkillsUsed == false)
            Unlock("ACH_BOSS_BARE");
    }

    static void OnBattleEnded(int monsterId, bool won)
    {
        CheckContract();
        if (Active == false || won == false || LastBattle.MonsterId != monsterId || LastBattle.Won == false)
            return;
        // 그 한 대로 레벨이 오르면 늘어난 최대 HP 만큼이 HP 에 얹힌다(GameManager.LevelUp) — 이긴 순간의 HP 로 잰다.
        float hp = LastBattle.HpAfter - (Managers.Game.PlayerData.MaxHP - LastBattle.MaxHp);
        if (hp > 0f && hp <= LastBattle.MaxHp * 0.01f)
            Unlock("ACH_SLIVER");               // HP 1% 이하로 이겼다
        if (LastBattle.FirstHitCrit)
            Unlock("ACH_CRIT_CARRY");           // 첫 타가 치명타
    }

    // 결말. 탑의 법 둘과 "배불리, 싸게" 는 그 판의 규칙·장부를 본다 — 장부는 체크포인트째 이 판의 것이다.
    static void OnEndingReached(string ending)
    {
        s_ending = ending;
        UploadPending();
        if (Active == false)
            return;
        var player = Managers.Game?.PlayerData;
        bool tower = player?.Mode == GameMode.Tower;
        bool dawn = ending == "dawn";
        if (tower)
            Unlock("ACH_TOWER");                // Records.ModeCleared 도 같은 것을 남긴다 — Steam 이 없던 판은 SyncRecords 가 준다
        if (tower && dawn)
            Unlock("ACH_TOWER_DAWN");
        LedgerState ledger = player?.Ledger;
        if (dawn && ledger != null && ledger.Par > 0 && ledger.Paid <= ledger.Par)
            Unlock("ACH_DAWN_PAR");
        SyncRecords(Records.MaxFloor, ending);
    }

    // 계약(3층, DirectingManager.FinishContract)은 알리는 이벤트가 없다 — 뒤따르는 HUD·층·전투에서 본다.
    // 계약한 세이브를 불러와도 여기서 풀린다.
    static void CheckContract()
    {
        if (Active && Managers.Game?.PlayerData?.IsContractedSword == true)
            Unlock("ACH_CONTRACT");
    }

    /// <summary>
    /// ACH_CH_3STAR — 한 챕터의 띠가 모두 ★★★ 인가. 별 0 은 기준 값이 없던 띠라 건너뛰고, 아직 없는 띠가 있으면 아니다.
    /// 챕터 0 은 띠 1~3(6~20층)만 본다 — 띠 0 은 손수 만든 1~4층에 생성 층이 5층 하나뿐이다. 챕터 c≥1 은 띠 4c~4c+3.
    /// band·stars 는 방금 닫은 띠(BandTallied) — 장부가 BandStars 에 적기 전에 알릴 수도 있어서 그 띠는 인자를 믿는다.
    /// </summary>
    static bool ChapterAllThree(int chapter, int band = -1, int stars = 0)
    {
        List<int> all = Managers.Game?.PlayerData?.Ledger?.BandStars;
        int full = 0;
        for (int b = chapter == 0 ? 1 : chapter * BandsPerChapter; b < (chapter + 1) * BandsPerChapter; b++)
        {
            int s = b == band ? stars : all != null && b < all.Count ? all[b] : -1;
            if (s == 3)
                full++;
            else if (s != 0)
                return false;
        }
        return full > 0;
    }

    // Records(판을 넘어 남는 기록)를 Steam 에 옮긴다. Steam 없이·체험판에서 본 결말과 잡은 우두머리도 다음에 붙었을 때 여기서 풀린다.
    // 별(★★★ 챕터)은 체크포인트의 장부에서 본다 — 체험판에서 끝낸 챕터 0 도 본편이 그 저장을 이어 층을 옮길 때 풀린다.
    // justReached: Records 도 같은 EndingReached 를 듣는데 누가 먼저 들을지 모른다 — 방금 본 결말은 직접 센다.
    static void SyncRecords(int maxFloor, string justReached = null)
    {
        int seen = 0;
        foreach (string ending in Endings)
        {
            if (ending != justReached && Records.EndingSeen(ending) == false)
                continue;
            seen++;
            Unlock("ACH_END_" + ending.ToUpperInvariant());
        }
        if (seen >= Records.EndingCount)
            Unlock("ACH_END_ALL");
        if (Records.BossBeaten(KingSlimeFloor))
            Unlock("ACH_KINGSLIME");
        for (int floor = FloorsPerChapter; floor <= LastFloor; floor += FloorsPerChapter)
        {
            if (Records.BossBeaten(floor))
                Unlock("ACH_BOSS" + floor);
        }
        if (Records.ModeCleared(GameMode.Tower))
            Unlock("ACH_TOWER");
        for (int chapter = 0; chapter < Chapters; chapter++)
        {
            if (ChapterAllThree(chapter))
                Unlock("ACH_CH_3STAR");
        }
        RaiseStat("MAX_FLOOR", maxFloor);
        RaiseStat("ENDINGS_SEEN", seen);
        RaiseStat("FIGHTS_WON", Records.FightsWon);
        RaiseStat("DEATHS", Records.Deaths);
        SteamUserStats.StoreStats();
    }

    // 올리기만 한다 — 다른 PC 나 클라우드가 붙기 전의 Records 는 비어 있어서, 그대로 옮기면 Steam 의 값을 깎는다.
    // 켠 직후라 값을 아직 못 받았거나 Steamworks 에 없는 통계면 GetStat 이 거짓이다 — 건너뛰고 다음 층에서 다시 온다.
    static void RaiseStat(string name, int value)
    {
        if (SteamUserStats.GetStat(name, out int current) && current < value)
            SteamUserStats.SetStat(name, value);
    }

    // 실행마다 id 하나에 한 번만 묻는다(HUD 가 그릴 때마다 계약을 다시 묻지 않게). 이미 풀린 것은 조용히 넘기고,
    // 푼 것과 거절된 것은 한 줄 적는다 — 앱 등록 전(Spacewar 480)에는 전부 거절로 찍히니 그 줄로 어느 업적이 걸리는지 본다.
    // 올려야(StoreStats) 풀리고 알림도 그때 뜬다.
    static void Unlock(string id)
    {
        if (Active == false || s_asked.Add(id) == false)
            return;
        if (SteamUserStats.GetAchievement(id, out bool achieved) && achieved)
            return;
        bool ok = SteamUserStats.SetAchievement(id) && SteamUserStats.StoreStats();
        Debug.Log(ok ? $"[Steam] 업적 {id}" : $"[Steam] 업적 {id} — Steam 이 받지 않았다 (Steamworks 에 없는 id)");
    }

    #region 순위표
    // 판 점수 = round(1000·치른 값/기준 값), 낮을수록 좋다(장부가 계산해 알린다). 표(규칙)와 자세한 값은 받은 순간의 판으로
    // 적어 둔다 — 결말은 뒤에 올 수 있어서 비워 두고 올릴 때 채운다.
    static void OnRunScored(int score)
    {
        GameManager.CurPlayerData player = Managers.Game?.PlayerData;
        PendingScore pending = new PendingScore {
            RunId = player?.Ledger?.RunId, Board = player?.Mode == GameMode.Tower ? BoardTower : BoardNormal,
            Score = score, Details = Details() };
        PlayerPrefs.SetString(PendingKey, JsonUtility.ToJson(pending));
        PlayerPrefs.Save();
        if (s_ending != null)
            UploadPending();
    }

    /// <summary>
    /// 점수의 자세한 값 (Steam/leaderboards.md 의 표와 같은 순서):
    /// [0] 결말 0 모름·1 seal·2 hold·3 dawn · [1] 치른 값 · [2] 기준 값 · [3] 예언한 값 · [4..8] 챕터 0~4 에서 치른 값(-1 모름).
    /// 치른 값이 예언보다 적으면 그만큼 스킬로 아꼈다 — 스킬 쓴 횟수는 장부에 없어 이 차이로 대신한다.
    /// </summary>
    static int[] Details()
    {
        LedgerState ledger = Managers.Game?.PlayerData?.Ledger ?? new LedgerState();
        int[] d = new int[4 + Chapters];
        d[1] = ledger.Paid;
        d[2] = ledger.Par;
        d[3] = ledger.Foretold;
        int start = 0;                          // 챕터 0 은 0 에서 시작한다
        for (int c = 0; c < Chapters; c++)
        {
            int end = c + 1 < Chapters ? PlayerPrefs.GetInt(ChapterKey(c + 1), -1) : ledger.Paid;
            d[4 + c] = start >= 0 && end >= start ? end - start : -1;
            start = end;
        }
        return d;
    }

    // 챕터별 값 — 챕터 첫 층(21·41·61·81층)에 처음 들어설 때의 누적 치른 값을 적어 둔다. 앞 층의 체크포인트로 돌아가면
    // 그 첫 층도 다시 처음 밟으니 덮어쓴다. 새 판(2층에 처음)이 지운다. 봇 판은 적지 않는다. 체험판도 적는다 — 같은 PC 의 본편이 잇는다.
    // ponytail: 이 PC 의 PlayerPrefs 에 둔다 — 다른 PC 에서 넘긴 챕터는 -1(모름). 장부(LedgerState)에 챕터별 합이 생기면 그것을 읽는다.
    static void RememberChapterStart(int stageId, bool firstVisit)
    {
        if (firstVisit == false || GameEvents.IsAutoPlaying)
            return;
        if (stageId == 1)
        {
            for (int c = 1; c < Chapters; c++)
                PlayerPrefs.DeleteKey(ChapterKey(c));
        }
        else if (stageId > 0 && stageId % FloorsPerChapter == 0)
        {
            PlayerPrefs.SetInt(ChapterKey(stageId / FloorsPerChapter), Managers.Game?.PlayerData?.Ledger?.Paid ?? 0);
        }
        else
        {
            return;
        }
        PlayerPrefs.Save();
    }

    static string ChapterKey(int chapter) => "STEAM_CHAPTER_PAID_" + chapter;

    // 적어 둔 점수를 올린다. Steam 이 없거나(다음 씬에서 다시) 답을 기다리는 중이면 그대로 둔다. 못 올리면 남겨 두고 다음 씬에서 다시 —
    // 같은 판을 두 번 올려도 KeepBest 라 기록은 하나다. sceneLoaded 에서도 부르므로 예외를 내지 않는다(뒤의 구독자가 못 듣는다).
    static void UploadPending()
    {
        string saved = PlayerPrefs.GetString(PendingKey, "");
        if (saved.Length == 0 || s_uploading)
            return;
        PendingScore pending;
        try { pending = JsonUtility.FromJson<PendingScore>(saved); }
        catch (System.ArgumentException) { pending = null; }
        if (pending == null || pending.Score <= 0 || string.IsNullOrEmpty(pending.Board) || pending.Details == null || pending.Details.Length == 0)
        {
            PlayerPrefs.DeleteKey(PendingKey);      // 읽을 수 없는 것은 버린다 — 씬마다 다시 읽지 않게
            return;
        }
        // 결말은 같은 판의 것만 붙인다. 붙인 것은 적어 둔 쪽에도 남겨서, 못 올려 다시 할 때도 결말이 있다.
        // Steam 이 없어도 붙여 둔다 — 다음 실행에서 올릴 때는 결말(s_ending)이 이미 없어서, 예전에는 "모름"(0)으로 올라갔다.
        if (s_ending != null && GameEvents.IsAutoPlaying == false && pending.RunId == Managers.Game?.PlayerData?.Ledger?.RunId)
        {
            pending.Details[0] = System.Array.IndexOf(Endings, s_ending) + 1;
            saved = JsonUtility.ToJson(pending);
            PlayerPrefs.SetString(PendingKey, saved);
            PlayerPrefs.Save();
        }
        if (Active == false)
            return;
        string board = pending.Board;
        int score = pending.Score;
        int[] details = pending.Details;

        // 표가 없으면 만든다(오름차순·숫자). 이미 있으면 정렬·표시 인자는 무시된다 — Steamworks 에 먼저 만들어 두는 것이 정석이다.
        SteamAPICall_t find = SteamUserStats.FindOrCreateLeaderboard(board,
            ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending, ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
        if (find == SteamAPICall_t.Invalid)
        {
            Debug.Log($"[Steam] 순위표 {board} 를 찾지 못했다 — 점수 {score} 는 다음 씬에서 다시 올린다");
            return;
        }
        s_uploading = true;
        s_find = CallResult<LeaderboardFindResult_t>.Create((found, ioFailure) =>
        {
            if (ioFailure || found.m_bLeaderboardFound == 0 || SteamManager.Initialized == false)
            {
                s_uploading = false;
                Debug.Log($"[Steam] 순위표 {board} 를 찾지 못했다 — 점수 {score} 는 다음 씬에서 다시 올린다");
                return;
            }
            // 가장 좋은 점수만 남긴다(KeepBest). 오름차순 표라 낮은 쪽이다.
            SteamAPICall_t upload = SteamUserStats.UploadLeaderboardScore(found.m_hSteamLeaderboard,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, score, details, details.Length);
            if (upload == SteamAPICall_t.Invalid)
            {
                s_uploading = false;
                Debug.Log($"[Steam] 순위표 {board} 에 점수 {score} 를 올리지 못했다 — 다음 씬에서 다시");
                return;
            }
            s_upload = CallResult<LeaderboardScoreUploaded_t>.Create((done, failed) =>
            {
                s_uploading = false;
                bool ok = failed == false && done.m_bSuccess != 0;
                Debug.Log(ok
                    ? $"[Steam] 순위표 {board} 점수 {score} (바뀜 {done.m_bScoreChanged != 0}, 순위 {done.m_nGlobalRankNew})"
                    : $"[Steam] 순위표 {board} 에 점수 {score} 를 올리지 못했다 — 다음 씬에서 다시");
                // 받았다. 그 사이 다음 판의 점수가 적혔으면 그것은 남긴다.
                if (ok && PlayerPrefs.GetString(PendingKey, "") == saved)
                {
                    PlayerPrefs.DeleteKey(PendingKey);
                    PlayerPrefs.Save();
                }
            });
            s_upload.Set(upload);
        });
        s_find.Set(find);
    }
    #endregion
}
#endif
