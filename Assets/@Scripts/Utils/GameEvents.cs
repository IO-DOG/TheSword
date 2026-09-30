using System;
using UnityEngine;

// 게임 진행의 "일어난 일" 을 알리는 한 곳.
// 스토리·전투 예측·튜토리얼 같은 곁가지가 여기에 귀를 대고, 핵심 로직은 알리기만 한다.
//
// 듣는 쪽에서 난 예외는 여기서 삼킨다. 이 프로젝트는 이펙트 하나가 null 이라서
// 부르는 쪽 코루틴이 통째로 죽는 사고를 세 번 겪었다 — 연출 버그가 층 이동이나
// 전투 결말을 멈추게 두지 않는다.
public static class GameEvents
{
    // 층에 들어섰다 (계단·워프가 끝나고 체크포인트를 쓴 뒤). stageId 는 0 부터, firstVisit 은 처음 온 층인가.
    public static event Action<int, bool> FloorEntered;
    // 챕터 보스(와 킹 슬라임)를 쓰러뜨렸다. monsterId 는 MonsterData id.
    // 전투에서 이긴 뒤, 전투창이 닫히고 BattleEnded 바로 앞에 한 번. 둘 다 쓰러지면 오지 않는다.
    public static event Action<int, int> BossDefeated;
    // 레벨이 올랐다. 한 번에 여러 레벨이 오르면 레벨마다 한 번씩.
    // 전투 중에는 몬스터가 쓰러진 순간 온다 — 같은 순간 거대의 포효로 플레이어가 죽으면 그 전투는
    // 진 것이고(BattleEnded won=false) 체크포인트가 레벨을 되돌린다. 오래 남길 기록은 BattleEnded·FloorEntered 에서.
    public static event Action<int> LevelUp;
    // 소비 아이템을 먹었다. heal 은 실제로 오른 HP, overflow 는 넘쳐서 버려진 HP (물약이 아니면 둘 다 0).
    public static event Action<int, int, int> ItemPicked;
    // 열쇠가 없어 문 앞에서 막혔다. color 0 초록, 1 노랑, 2 빨강.
    public static event Action<int> DoorBlocked;
    // 문을 열었다.
    public static event Action<int> DoorOpened;
    // 전투가 끝났다. (monsterId, 플레이어가 이겼나)
    public static event Action<int, bool> BattleEnded;
    // 죽고 체크포인트에서 되살아나 게임 화면이 다시 섰다.
    public static event Action Respawned;
    // HUD 가 다시 그려졌다 (전투·아이템·장비·층 이동 뒤). 맵 위 표시를 새로 칠할 때.
    public static event Action HudRefreshed;
    // 장비를 주웠다 (EquipData id). 워프석 반지(32)를 주울 때 반지의 새긴 글을 읽는 장면이 여기서 걸린다.
    public static event Action<int> EquipPicked;
    // 결말을 봤다 (seal · hold · dawn, StoryEnding 소문자). 크레디트보다 앞, DebugPlay 에서는 오지 않는다.
    // 판을 넘어 남는 기록(Records)과 업적(SteamHooks)이 듣는다.
    public static event Action<string> EndingReached;

    // 자동 플레이(완주 녹화 봇)가 돌고 있다. 확인 창은 스스로 "예" 를 고르고,
    // 대사는 기다리지 않고 넘어가야 한다.
    public static bool IsAutoPlaying;

    // 게임오버에서 체크포인트를 불러 씬을 다시 올리는 중이다.
    // 새 GameScene 이 다 서면 UI_GameScene 이 이것을 보고 Respawned 를 한 번 알린다.
    public static bool RespawnPending;

    public static void RaiseFloorEntered(int stageId, bool firstVisit) => Safe(FloorEntered, h => h(stageId, firstVisit));
    public static void RaiseBossDefeated(int monsterId, int stageId) => Safe(BossDefeated, h => h(monsterId, stageId));
    public static void RaiseLevelUp(int level) => Safe(LevelUp, h => h(level));
    public static void RaiseItemPicked(int itemId, int heal, int overflow) => Safe(ItemPicked, h => h(itemId, heal, overflow));
    public static void RaiseDoorBlocked(int color) => Safe(DoorBlocked, h => h(color));
    public static void RaiseDoorOpened(int color) => Safe(DoorOpened, h => h(color));
    public static void RaiseBattleEnded(int monsterId, bool won) => Safe(BattleEnded, h => h(monsterId, won));
    public static void RaiseRespawned() => Safe(Respawned, h => h());
    public static void RaiseHudRefreshed() => Safe(HudRefreshed, h => h());
    public static void RaiseEquipPicked(int equipId) => Safe(EquipPicked, h => h(equipId));
    public static void RaiseEndingReached(string ending) => Safe(EndingReached, h => h(ending));

    // 에디터는 플레이할 때 도메인·씬을 다시 읽지 않는다(Enter Play Mode Options). 정적 값이 지난 플레이에서
    // 그대로 넘어와, 파괴된 오브젝트에 묶인 구독자가 남는다 — 플레이를 시작할 때마다 비운다. 빌드에는 영향이 없다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        FloorEntered = null; BossDefeated = null; LevelUp = null; ItemPicked = null; DoorBlocked = null;
        DoorOpened = null; BattleEnded = null; Respawned = null; HudRefreshed = null; EquipPicked = null;
        EndingReached = null;
        IsAutoPlaying = false;
        RespawnPending = false;
    }

    static void Safe<T>(T handlers, Action<T> call) where T : Delegate
    {
        if (handlers == null)
            return;
        foreach (Delegate d in handlers.GetInvocationList())
        {
            try { call((T)d); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
