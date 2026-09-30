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
/// 업적·통계·Rich Presence (MASTER_PLAN §7). GameEvents 에 귀만 대고 게임 로직은 고치지 않는다.
///
/// 사람이 한 일만 센다 — 봇이 돌 때(GameEvents.IsAutoPlaying), Steam 이 없을 때(SteamManager.Initialized),
/// 체험판(DEMO)에서는 아무것도 하지 않는다. 통계는 Records 를 옮기되 올리기만 한다(Records 도 봇 판은 적지 않는다).
/// 업적·통계 id 는 Steamworks 관리 페이지에 같은 이름으로 만들어 둬야 풀린다 — 없는 id 는 Steam 이 거절하고 끝이다.
///
/// 아직 걸지 않은 업적: ACH_UNDER_PAR · ACH_CH_3STAR · ACH_DAWN_PAR (장부 L1 · 별 L2 뒤), ACH_TOWER · ACH_TOWER_DAWN (탑의 법 L4 뒤).
/// </summary>
public static class SteamHooks
{
    const int FloorsPerChapter = 20;
    const int LastFloor = 100;
    const int KingSlimeFloor = 4;
    const int NoSpillFloors = 5;
    static readonly string[] Endings = { "seal", "hold", "dawn" };     // StoryEnding 소문자 — Records 가 적는 이름

#if DEMO
    static bool Active => false;        // 체험판은 주지 않는다. 본편이 같은 기록(Records)을 보고 준다(SyncRecords)
#else
    static bool Active => SteamManager.Initialized && GameEvents.IsAutoPlaying == false;
#endif

    // ACH_NO_SPILL 에서 아직 체크포인트에 굳지 않은 넘침. 체크포인트를 부르면(씬을 다시 올린다) 물약이 제자리라 없던 일이다.
    static bool s_spillUnsaved;
    static int s_presenceFloor = -1;    // Rich Presence 에 마지막으로 적은 층 (stageId). -1 은 비웠다

    static readonly HashSet<string> s_asked = new HashSet<string>();   // 이번 실행에 Steam 에 물어본 업적

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_spillUnsaved = false;
        s_presenceFloor = -1;
        s_asked.Clear();
    }

    // GameEvents 는 SubsystemRegistration 에서 구독자를 비우므로 그 뒤(BeforeSceneLoad)에 붙는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        GameEvents.FloorEntered += OnFloorEntered;
        GameEvents.HudRefreshed += OnHudRefreshed;
        GameEvents.BossDefeated += OnBossDefeated;
        GameEvents.BattleEnded += OnBattleEnded;
        GameEvents.EndingReached += ending => { if (Active) SyncRecords(Records.MaxFloor, ending); };
        GameEvents.ItemPicked += (itemId, heal, overflow) => s_spillUnsaved |= overflow > 0;
        // SceneManager 의 이벤트는 플레이마다 비워지지 않는다(도메인 리로드를 끈 에디터) — 빼고 건다.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 게임 화면은 체크포인트(이어하기·이 층 다시·체크포인트 목록·죽음)나 새 판으로만 다시 선다 — 그 뒤의 넘침은 없던 일이다.
    // 층 밖(타이틀·인트로·엔딩)에서는 친구 목록에 지난 층을 남기지 않는다. 게임 화면은 HUD 가 서며 다시 적는다.
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        s_spillUnsaved = false;
        if (Active && scene.name != nameof(Define.Scene.GameScene))
        {
            SteamFriends.ClearRichPresence();
            s_presenceFloor = -1;
        }
    }

    static void OnFloorEntered(int stageId, bool firstVisit)
    {
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

    // 친구 목록의 "12층 · 이끼 낀 지하 묘소". 문구는 Steamworks 의 Rich Presence 현지화 파일(#S_Floor)이 맡는다.
    static void SetPresence(int stageId)
    {
        if (stageId == s_presenceFloor)
            return;
        s_presenceFloor = stageId;
        SteamFriends.SetRichPresence("floor", (stageId + 1).ToString());
        SteamFriends.SetRichPresence("chapter", (stageId / FloorsPerChapter).ToString());
        SteamFriends.SetRichPresence("mode", "normal");
        SteamFriends.SetRichPresence("steam_display", "#S_Floor");
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

    // 계약(3층, DirectingManager.FinishContract)은 알리는 이벤트가 없다 — 뒤따르는 HUD·층·전투에서 본다.
    // 계약한 세이브를 불러와도 여기서 풀린다.
    static void CheckContract()
    {
        if (Active && Managers.Game?.PlayerData?.IsContractedSword == true)
            Unlock("ACH_CONTRACT");
    }

    // Records(판을 넘어 남는 기록)를 Steam 에 옮긴다. Steam 없이·체험판에서 본 결말과 잡은 우두머리도 다음에 붙었을 때 여기서 풀린다.
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
}
#endif
