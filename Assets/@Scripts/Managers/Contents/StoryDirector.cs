using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

#region 대본 자료형 — GeneratedStory(Tools/story_gen.py)가 채운다
public enum StoryKind { Dialogue, Card, Bark, Choice, Credits }
public enum StoryTrigger { Prologue, Village, ChapterStart, FloorFirst, TraitFirst, FloorType, Mechanic, Death, BossIntro, BossDefeat, Ending, Epilogue, Credits }
public enum StoryPrologue { ContractAfter, KingslimeReveal, KingslimeClear }
public enum StoryMechanic { Forecast, Fatal, Overflow, Vault, SpareKey, Choice, Rune, NoKey, Warp, Death, Crit, LevelUp }
public enum StoryEnding { Choice, Seal, Hold, Dawn }
public enum StoryPortrait { None, Damian, Sword, Boss }
public enum StoryCueKind { Emote, Shake, Flash, White, Dark, Clear, CamUp, CamClose, CamPlayer, CamMonster, CamBoss, Pose, Bgm, BgmStop, BgmFloor, Fx, Boom, Soul, Hands, VortexIn, VortexStop, Walk, Backdrop }

public sealed class StorySpeaker
{
    public readonly string Id;
    public readonly int NameId;             // ScriptData. 0 이면 이름표를 띄우지 않는다 (내레이션)
    public readonly StoryPortrait Portrait;
    public readonly int Chapter;            // Boss 일 때 그 챕터 (GeneratedStory.BossIds)
    public StorySpeaker(string id, int nameId, StoryPortrait portrait, int chapter)
    { Id = id; NameId = nameId; Portrait = portrait; Chapter = chapter; }
}

public sealed class StoryLine
{
    public readonly int Speaker;            // GeneratedStory.Speakers 의 번호
    public readonly string Emotion;         // 대화창 감정 아이콘의 애니메이터 상태. null 이면 띄우지 않는다
    public readonly int ScriptId;
    public readonly string Image;           // 카드 그림의 스프라이트 주소. null 이면 앞 그림을 둔다
    public StoryLine(int speaker, string emotion, int scriptId, string image)
    { Speaker = speaker; Emotion = emotion; ScriptId = scriptId; Image = image; }
    public StorySpeaker Who => GeneratedStory.Speakers[Speaker];
}

public sealed class StoryChoice
{
    public readonly StoryEnding Ending;
    public readonly int ScriptId;
    public StoryChoice(StoryEnding ending, int scriptId) { Ending = ending; ScriptId = scriptId; }
}

public sealed class StoryCue
{
    public const int End = 999;             // 장면이 끝난 뒤
    public readonly int Line;               // 그 줄(1부터)이 뜰 때. 0 은 대화창이 뜨기 전
    public readonly StoryCueKind Kind;
    public readonly string Arg;
    public StoryCue(int line, StoryCueKind kind, string arg) { Line = line; Kind = kind; Arg = arg; }
}

public sealed class StoryScene
{
    public readonly string Id;
    public readonly StoryKind Kind;
    public readonly StoryTrigger Trigger;
    public readonly int Arg;                // 챕터·층·특성·층 유형·StoryMechanic·StoryEnding …
    public readonly StoryLine[] Lines;
    public readonly StoryChoice[] Choices;
    public readonly StoryCue[] Cues;
    public StoryScene(string id, StoryKind kind, StoryTrigger trigger, int arg, StoryLine[] lines, StoryChoice[] choices, StoryCue[] cues)
    { Id = id; Kind = kind; Trigger = trigger; Arg = arg; Lines = lines; Choices = choices; Cues = cues; }
}
#endregion

/// <summary>
/// 이야기 장면을 언제 틀지 정하고 튼다 (Tools/story/STORY_BIBLE.md 9절, 대본은 GeneratedStory).
///
/// 게임 로직은 GameEvents 로 "일어난 일" 만 알리고, 여기서 그중 이야기가 걸린 것을 고른다.
/// 전투 직전에 끼어드는 것(보스 등장, 특성 수업, 첫 ✖)은 FightGate 로 들어간다. 1~4층 프롤로그만은
/// 손으로 짠 연출(DirectingManager)이 제 차례에 CoPrologue 를 부른다.
///
/// 한 번 튼 장면은 StorySeen.json 에 적는다 — 틀기 시작할 때 적으므로 죽어도, 불러와도 다시 뜨지 않는다
/// (도는 중에 꺼지면 그 장면은 잃는다. 되풀이되는 것보다 낫다. 층 입구에 줄 선 뒷장면은 다음에 씬을 올릴 때 잇는다).
/// 새 게임이 지운다(GameManager.DeleteGameData).
/// </summary>
public class StoryDirector : MonoBehaviour
{
    /// <summary>새벽 결말(dawn)의 문턱 레벨 (바이블 6.1). 봇이 몬스터를 다 잡는 길로 100층 보스를 잡은 직후의
    /// 레벨 L 에서 T = L - 3 으로 정한다. 봇 실측 전의 잠정값이다.</summary>
    public const int DawnLevel = 109;

    /// <summary>한 번이라도 결말을 봤다 (PlayerPrefs, 1). 새 게임으로 지워지지 않는다.</summary>
    public const string ClearedKey = "CLEARED";
    /// <summary>마지막으로 본 결말 (PlayerPrefs, seal·hold·dawn).</summary>
    public const string ClearedEndingKey = "CLEARED_ENDING";

    const int VillageFloor = 5;             // "4층 계단을 올라 5층에 처음 들어설 때" (FORMAT 트리거 표)
    const string SeenFile = "StorySeen.json";
    const int TraitOrder = 10;              // 전투 관문 순서 — 특성 수업은 보스 등장(0) 뒤, 첫 ✖(50) 앞. 20층 늑대가 첫 야수라
                                            // 먼저 두면 규칙 설명이 "…또… 너냐." 보다 앞에 섰다. 등장이 먼저 서고, 규칙은 싸움 바로 앞에 온다

    static StoryDirector s_instance;
    static HashSet<string> s_seen;
    static Dictionary<(StoryTrigger, int), List<StoryScene>> s_index;
    static int s_hold;                      // OnDirect 를 이 도구가 쥐고 있는 겹 수
    static int s_epoch;                     // AbortAll 이 올린다. 그 전에 시작한 장면은 다음 걸음에서 멈춘다 (Safe)
    static bool s_debug;                    // DebugPlay 가 도는 중 — 본 것으로도, 결말로도 적지 않는다

    /// <summary>이야기가 흐름을 쥐고 있다 (장면·연출이 도는 중).</summary>
    public static bool IsPlaying => s_instance != null && s_instance._running > 0;

    /// <summary>치명타 수업(mechanic_first:crit)이 전투를 붙들고 있다. 전투창은 이동안 시계를 세지 않는다
    /// (UI_BattlePopup.FixedUpdate·Update). OnBattle 은 켠 채 둔다 — 캐릭터·봇·메뉴가 계속 전투 중으로 본다.
    /// 예전에는 OnBattle 을 꺼서 멈췄고, 그 틈에 같은 몬스터와 두 번째 전투가 열려 두 창이 같이 때렸다.</summary>
    public static bool HoldsBattle { get; private set; }

    /// <summary>방금 본 결말. 엔딩 씬이 그림을 고를 때 읽는다.</summary>
    public static StoryEnding LastEnding
    {
        get
        {
            string saved = PlayerPrefs.GetString(ClearedEndingKey, "");
            return s_lastEnding ?? (Enum.TryParse(saved, true, out StoryEnding e) ? e : StoryEnding.Seal);
        }
    }
    static StoryEnding? s_lastEnding;

    struct Job { public float NotBefore; public Func<IEnumerator> Make; }
    readonly List<Job> _jobs = new List<Job>();
    int _running;
    StoryChoice _lastChoice;
    UI_Popup _popup;                        // 지금 띄운 이야기 창 — AbortAll 이 닫는다
    UI_StoryCardPopup _backdrop;
    bool _letterbox;                        // 레터박스를 이 도구가 내렸다
    bool _levelUpPending;
    bool _endingRunning;
    float _nextWatch;
    bool _offsetMoved, _exposureMoved;
    GameObject _mark;                       // 보스가 사라진 자리를 카메라가 볼 때
    GameObject _portal, _vortex;            // 결말의 왕좌의 입
    StoryScene _scene;                      // 지금 창이 떠 있는 장면 (CoScene). 건너뛰기가 본다
    bool _title;                            // 챕터 이름 카드가 떠 있다 (CoChapterTitle) — 이것도 건너뛴다
    bool _sceneSeenBefore;                  // 그 장면을 지난 판들에서 본 적이 있다 — Tab 한 번으로 넘긴다
    float _skipHeld;                        // Tab 을 누르고 있은 시간 (실시간)

    // 보스 자리. 결말·쓰러진 뒤의 연출이 쓴다 — 그 무렵이면 보스는 이미 없다.
    static Vector3? s_bossPos;
    static GameObject s_bossObject;

    #region 설치
    // 에디터는 플레이할 때 도메인·씬을 다시 읽지 않는다(Enter Play Mode Options). 정적 값이 지난 플레이에서
    // 그대로 넘어온다 — 첫 씬이 뜨기 전에 비운다. 빌드에는 영향이 없다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_instance = null;
        s_seen = null;
        s_index = null;
        s_hold = 0;
        s_debug = false;
        s_lastEnding = null;
        s_bossPos = null;
        s_bossObject = null;
        HoldsBattle = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (s_instance != null)
            return;
        GameObject go = new GameObject("@StoryDirector");
        DontDestroyOnLoad(go);
        s_instance = go.AddComponent<StoryDirector>();
    }

    void OnEnable()
    {
        GameEvents.FloorEntered += OnFloorEntered;
        GameEvents.BossDefeated += OnBossDefeated;
        GameEvents.LevelUp += OnLevelUp;
        GameEvents.ItemPicked += OnItemPicked;
        GameEvents.DoorBlocked += OnDoorBlocked;
        GameEvents.Respawned += OnRespawned;
        GameEvents.BattleEnded += OnBattleEnded;
        GameEvents.EquipPicked += OnEquipPicked;
        FightGate.Add(TraitGate, TraitOrder);
        FightGate.Add(BossIntroGate, 0);
        FightGate.Add(FatalGate, 50);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        GameEvents.FloorEntered -= OnFloorEntered;
        GameEvents.BossDefeated -= OnBossDefeated;
        GameEvents.LevelUp -= OnLevelUp;
        GameEvents.ItemPicked -= OnItemPicked;
        GameEvents.DoorBlocked -= OnDoorBlocked;
        GameEvents.Respawned -= OnRespawned;
        GameEvents.BattleEnded -= OnBattleEnded;
        GameEvents.EquipPicked -= OnEquipPicked;
        FightGate.Remove(TraitGate);
        FightGate.Remove(BossIntroGate);
        FightGate.Remove(FatalGate);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 바뀌면(죽어서 다시 올림·타이틀·엔딩) 도는 장면·기다리던 장면·늦게 뜰 바크를 버린다 — 체크포인트가 그 일을
    // 되돌렸다. 예전에는 줄만 비워서 기다리던 바크가 타이틀 화면에 떴다.
    // 체크포인트는 이 층을 이미 다녀간 것으로 적었으니 FloorEntered(처음)는 다시 오지 않는다. 지난번에 층 입구 장면
    // 도중에 꺼졌으면(창을 닫았다·녹화 조각이 끊겼다) 남은 것을 여기서 잇는다. 바크는 없다.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AbortAll();
        s_bossObject = null;
        if (scene.name == nameof(Define.Scene.GameScene) && Contracted && CurFloor >= VillageFloor)
            EnqueueScenes(FloorChain(CurFloor), false, 0.5f);
    }

    /// <summary>
    /// 이야기를 전부 거둔다: 도는 장면, 줄 선 장면, 늦게 뜰 바크. 쥐고 있던 것(캐릭터 OnDirect·전투 멈춤·전투 관문·
    /// 레터박스·HUD·카메라)을 놓고 제 창을 닫는다. 씬이 바뀔 때 스스로 부르고, 메뉴가 "이 층 다시"·"체크포인트"·
    /// "타이틀로" 앞에서 부른다. 코루틴을 멈추면 finally 가 돌지 않아서 여기서 하나하나 되돌린다.
    /// </summary>
    public static void AbortAll()
    {
        s_epoch++;          // DirectingManager 가 돌리던 프롤로그 장면도 다음 걸음에서 멈춘다 (Safe)
        s_debug = false;
        HoldsBattle = false;
        bool held = s_hold > 0;
        s_hold = 0;
        if (held && Managers.IsAlive && Managers.Game != null)
            Managers.Game.OnDirect = false;
        UI_StoryBark.Clear();
        if (s_instance != null)
            s_instance.Abort(held);
    }

    void Abort(bool held)
    {
        held |= _running > 0;
        StopAllCoroutines();
        _jobs.Clear();
        _running = 0;
        _levelUpPending = false;
        _endingRunning = false;
        _scene = null;
        _title = false;
        StoryUI.EndSkip();
        if (held == false || Managers.IsAlive == false)
            return;     // 줄만 서 있었다 — 화면은 건드린 적이 없다
        if (_popup != null)
            Managers.UI.ClosePopupUI(_popup);   // 대화창은 닫히며 제 OnConversation 을 푼다
        if (_backdrop != null)
            Managers.UI.ClosePopupUI(_backdrop);
        _popup = _backdrop = null;
        Letterbox(false);
        if (FightGate.Pending)
            FightGate.Cancel();     // 보스 등장·첫 ✖ 수업이 전투 관문을 쥐고 있었다
        Restore();
    }
    #endregion

    #region 본 장면 (StorySeen.json)
    static string SeenPath => Path.Combine(SaveStore.DirectoryPath, SeenFile);

    static HashSet<string> Seen
    {
        get
        {
            if (s_seen != null)
                return s_seen;
            s_seen = new HashSet<string>();
            try
            {
                if (File.Exists(SeenPath))
                    s_seen.UnionWith(JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(SeenPath)) ?? new List<string>());
            }
            catch (Exception e) when (SaveStore.IsSaveError(e))
            {
                Debug.LogWarning($"[Story] {SeenFile} 를 읽지 못했다 — 처음부터 센다: {e.Message}");
            }
            return s_seen;
        }
    }

    public static bool HasSeen(StoryScene scene) => Seen.Contains(scene.Id);

    /// <summary>그 기능 수업(mechanic_first:…)을 이번 판에 다 봤다. 전투 건너뛰기는 치명타 수업(3~4층) 뒤에 열린다.</summary>
    public static bool MechanicSeen(StoryMechanic mechanic) => Unseen(mechanic).Count == 0;

    static void MarkSeen(StoryScene scene)
    {
        if (s_debug || Seen.Add(scene.Id) == false)
            return;
        Records.MarkSceneSeen(scene.Id);    // 판을 넘어 남는 "본 적 있다" (새 게임 뒤 다시 볼 때 건너뛰기)
        // 체크포인트와 같은 방식으로 바꿔 끼운다. 쓰다 꺼져도 옛 파일은 남는다.
        try
        {
            Directory.CreateDirectory(SaveStore.DirectoryPath);
            string temp = SeenPath + ".tmp";
            File.WriteAllText(temp, JsonConvert.SerializeObject(new List<string>(s_seen)));
            if (File.Exists(SeenPath))
                File.Replace(temp, SeenPath, null);
            else
                File.Move(temp, SeenPath);
        }
        catch (Exception e) when (SaveStore.IsSaveError(e))
        {
            Debug.LogWarning($"[Story] {SeenFile} 를 쓰지 못했다: {e.Message}");
        }
    }

    /// <summary>새 게임. 저장을 지우는 GameManager.DeleteGameData 가 부른다. 결말을 봤다는 표시(CLEARED)는 남는다.
    /// 판을 메모리에만 새로 세우는 ResetRun 에 두면 안 된다 — 켤 때 체크포인트를 못 읽어도 그것이 불려서
    /// 이 파일까지 지웠고, 다음에 체크포인트가 읽히면 본 장면이 전부 다시 떴다.</summary>
    public static void ClearSeen()
    {
        s_seen = new HashSet<string>();
        try { File.Delete(SeenPath); }
        catch (Exception e) when (SaveStore.IsSaveError(e)) { Debug.LogWarning($"[Story] {SeenFile} 를 지우지 못했다: {e.Message}"); }
    }
    #endregion

    #region 장면 찾기
    static List<StoryScene> All(StoryTrigger trigger, int arg)
    {
        if (s_index == null)
        {
            s_index = new Dictionary<(StoryTrigger, int), List<StoryScene>>();
            foreach (StoryScene s in GeneratedStory.Scenes)
            {
                if (s_index.TryGetValue((s.Trigger, s.Arg), out List<StoryScene> list) == false)
                    s_index.Add((s.Trigger, s.Arg), list = new List<StoryScene>());
                list.Add(s);        // 파일 순서 (R1)
            }
        }
        return s_index.TryGetValue((trigger, arg), out List<StoryScene> found) ? found : new List<StoryScene>();
    }

    static StoryScene First(StoryTrigger trigger, int arg)
    {
        List<StoryScene> list = All(trigger, arg);
        return list.Count > 0 ? list[0] : null;
    }

    static List<StoryScene> Unseen(StoryTrigger trigger, int arg) => All(trigger, arg).FindAll(s => HasSeen(s) == false);

    static List<StoryScene> Unseen(StoryMechanic mechanic) => Unseen(StoryTrigger.Mechanic, (int)mechanic);

    static bool HasCue(StoryScene scene, StoryCueKind kind) => scene.Cues != null && Array.Exists(scene.Cues, c => c.Kind == kind);

    static bool Contracted => Managers.Game != null && Managers.Game.PlayerData != null && Managers.Game.PlayerData.IsContractedSword;

    static int CurFloor => Managers.Game.PlayerData.CurStageid + 1;

    static int BossChapter(int monsterId) => Array.IndexOf(GeneratedStory.BossIds, monsterId);

    static int FloorFact(int[] facts, int floor, int none) => floor >= 0 && floor < facts.Length ? facts[floor] : none;
    #endregion

    #region 차례 — 한 번에 하나만, 게임이 한가할 때
    /// <summary>게임이 한가하다: 전투·연출·대화·계단·창이 없다. 맨 위 창은 저절로 사라지는 이름 팝업뿐이다.</summary>
    static bool GameIdle()
    {
        GameManager g = Managers.Game;
        if (g == null || g.GameScene == null || g.Player == null || g.PlayerData == null)
            return false;
        if (g.OnBattle || g.OnConversation || g.OnLever || g.OnFade || g.OnDirect || g.OnInteract || g.OnInputLock || g.IsPlayerDead)
            return false;
        if (FightGate.Pending || Managers.UI.IsPaused || UI_ConfirmPopup.IsOpen || GameEvents.RespawnPending)
            return false;
        UI_Popup top = Managers.UI.TopPopup;
        return top == null || top is UI_StageNamePopup || top is UI_BossNamePopup;
    }

    void Enqueue(Func<IEnumerator> make, float delay = 0f)
    {
        _jobs.Add(new Job { NotBefore = Time.unscaledTime + delay, Make = make });
    }

    void StartNow(IEnumerator job) => StartCoroutine(Run(job));

    IEnumerator Run(IEnumerator job)
    {
        _running++;
        try { yield return Safe(job); }
        finally { _running--; }
    }

    void Update()
    {
        if (_running == 0 && _jobs.Count > 0 && Time.unscaledTime >= _jobs[0].NotBefore && GameIdle())
        {
            Job job = _jobs[0];
            _jobs.RemoveAt(0);
            StartNow(job.Make());
        }

        WatchCrit();
        WatchSkip();
        if (Time.unscaledTime >= _nextWatch)
        {
            _nextWatch = Time.unscaledTime + 0.2f;
            WatchFloor();
        }
    }

    // 건너뛰기: 대화창·이야기 카드가 떠 있으면 Tab 을 SkipHold 초 누르고 있을 때(지난 판들에서 본 장면은 한 번 누를 때)
    // 지금 장면을 끝까지 넘긴다(StoryUI.Skipping). 선택지 앞에서는 서고, 결말의 선택은 건너뛰지 못한다(대화창이 끈다).
    // 1~4층의 예전 대사(EventData)도 같은 창이라 같이 넘어간다 — 장면이 없으니 누르고 있어야만 한다.
    void WatchSkip()
    {
        UI_Popup top = Managers.UI.TopPopup;
        UI_ConversationPopup talk = top as UI_ConversationPopup;
        bool story = talk != null || top is UI_StoryCardPopup;
        if (StoryUI.Skipping && story == false)
            StoryUI.EndSkip();      // 장면을 끝낸 길이 따로 있었다 (창이 걷혔다)
        // 카드는 장면이 걸려 있거나 챕터 이름일 때만 — 다 넘긴 뒤 다음 대화의 배경으로 깔린 카드는 넘길 것이 없다.
        bool open = StoryUI.Auto == false && StoryUI.Skipping == false
                    && (talk != null ? talk.Choosing == false : story && (_scene != null || _title));
        if (open == false)
        {
            _skipHeld = 0f;
            StoryUI.SkipHint(false, false, 0f);
            return;
        }
        bool seen = _scene != null && _sceneSeenBefore;
        _skipHeld = Input.GetKey(StoryUI.SkipKey) ? _skipHeld + Time.unscaledDeltaTime : 0f;
        if (_skipHeld >= StoryUI.SkipHold || (seen && Input.GetKeyDown(StoryUI.SkipKey)))
        {
            _skipHeld = 0f;
            StoryUI.BeginSkip();
            StoryUI.SkipHint(false, false, 0f);
            return;
        }
        StoryUI.SkipHint(true, seen, _skipHeld / StoryUI.SkipHold);
    }

    /// <summary>코루틴을 직접 돌린다. 안쪽 어디서 예외가 나도 그 코루틴만 접고(제 finally 는 돈다) 바깥은 이어 간다 —
    /// 이펙트 하나가 null 이라서 결말·전투 관문·HUD 복구가 통째로 멈추는 일을 이 프로젝트는 세 번 겪었다.</summary>
    public static IEnumerator Safe(IEnumerator root)
    {
        int epoch = s_epoch;
        Stack<IEnumerator> stack = new Stack<IEnumerator>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            // AbortAll 뒤다. 남의 코루틴이 돌리던 것(DirectingManager 가 기다리는 프롤로그)은 StopAllCoroutines 가
            // 못 멈춘다 — 여기서 끝낸다. 닫힌 대화창이 알린 "끝" 을 받고 다음 장면을 띄우지 않게.
            if (epoch != s_epoch)
                yield break;
            IEnumerator top = stack.Peek();
            bool moved;
            try { moved = top.MoveNext(); }
            catch (Exception e) { Debug.LogException(e); moved = false; }
            if (moved == false)
            {
                stack.Pop();
                continue;
            }
            if (top.Current is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }
            yield return top.Current;
        }
    }

    // 캐릭터를 붙잡는다. 여러 겹이어도 마지막이 놓을 때 푼다.
    static void Acquire()
    {
        if (s_hold++ == 0 && Managers.Game != null)
            Managers.Game.OnDirect = true;
    }

    static void Release()
    {
        if (s_hold > 0 && --s_hold == 0 && Managers.Game != null)
            Managers.Game.OnDirect = false;
    }
    #endregion

    #region 게임이 알린 일
    // 층에 들어설 때의 장면 중 아직 안 본 것. R2: village → chapter_start → floor_first.
    // 특성 수업(trait_first)은 여기 없다 — 그 특성의 몬스터에게 처음 부딪힐 때 튼다(TraitGate). 들어서자마자 쌓으면
    // 5층은 첫 걸음 전에 23줄이었고, 특성이 둘씩 오는 41·61층도 그랬다.
    static List<StoryScene> FloorChain(int floor)
    {
        List<StoryScene> chain = new List<StoryScene>();
        if (floor == VillageFloor)
            chain.AddRange(Unseen(StoryTrigger.Village, 0));
        int chapter = Array.IndexOf(GeneratedStory.ChapterFirstFloors, floor);
        if (chapter >= 0)
            chain.AddRange(Unseen(StoryTrigger.ChapterStart, chapter));
        chain.AddRange(Unseen(StoryTrigger.FloorFirst, floor));
        return chain;
    }

    static void OnFloorEntered(int stageId, bool firstVisit)
    {
        if (s_instance == null || firstVisit == false || Contracted == false)
            return;
        List<StoryScene> chain = FloorChain(stageId + 1);
        if (chain.Count > 0)
        {
            Acquire();      // 들어서자마자 붙잡는다 — 층 이름이 걷히는 사이에 걸어가 버리지 않게
            s_instance.StartNow(s_instance.CoFloorChain(chain));
            return;
        }
        s_instance.FloorBark(stageId + 1);
    }

    // 층 유형 바크. 어느 층에서 무엇이 뜰지는 story_gen 이 정해 굽는다(GeneratedStory.FloorBarks) — 유형마다 변주를
    // 돌려 가며 다 한 번은 뜨게, 이야기 장면·보스가 있는 층은 비우고, 81~89층은 데미안 것만 (바이블 R13·R15).
    // 예전의 층 번호 씨앗 난수로는 두 변주가 한 번도 뽑히지 않았다.
    void FloorBark(int floor)
    {
        int scene = FloorFact(GeneratedStory.FloorBarks, floor, -1);
        if (scene >= 0)
            StartCoroutine(CoBarkLater(GeneratedStory.Scenes[scene].Lines[0], 2f));
    }

    static IEnumerator CoBarkLater(StoryLine line, float delay)
    {
        yield return new WaitForSeconds(delay);
        UI_StoryBark.Show(line);
    }

    static void OnBossDefeated(int monsterId, int stageId)
    {
        int chapter = BossChapter(monsterId);
        if (s_instance == null || chapter < 0 || Contracted == false)
            return;
        if (chapter == GeneratedStory.BossIds.Length - 1)
        {
            Acquire();
            s_instance.StartNow(s_instance.CoEnding());
            return;
        }
        List<StoryScene> chain = Unseen(StoryTrigger.BossDefeat, chapter);
        if (chain.Count == 0)
            return;
        Acquire();
        s_instance.StartNow(s_instance.CoBossScenes(chain, null, null));
    }

    // 늑대가 떨군 반지를 주웠다 — 그때 반지 안쪽의 글을 읽는다 (바이블 R14). 예전에는 늑대가 쓰러진 장면 뒤에
    // 곧장 붙어, 줍지도 않은 반지를 읽고 설명했다.
    static void OnEquipPicked(int equipId)
    {
        if (s_instance != null && Contracted && EquipUtility.AbilityOf(equipId) == EquipUtility.AbilityWarp)
            s_instance.EnqueueScenes(Unseen(StoryMechanic.Warp), true, 0.5f);
    }

    static void OnLevelUp(int level)
    {
        if (s_instance == null || Contracted == false || CurFloor < VillageFloor || Unseen(StoryMechanic.LevelUp).Count == 0)
            return;
        // 싸움 도중에 온다. 전투창이 닫힌 뒤(BattleEnded)에 튼다 — 져서 되돌아가면 없던 일이다.
        s_instance._levelUpPending = true;
    }

    static void OnBattleEnded(int monsterId, bool won)
    {
        if (s_instance == null || s_instance._levelUpPending == false)
            return;
        s_instance._levelUpPending = false;
        if (won)
            s_instance.EnqueueScenes(Unseen(StoryMechanic.LevelUp), false, 1f);
    }

    static void OnItemPicked(int itemId, int heal, int overflow)
    {
        if (s_instance == null || Contracted == false)
            return;
        if (itemId >= ConsumableItem.NUM_OF_POTIONS && itemId < ConsumableItem.NUM_OF_RUNES)
            s_instance.EnqueueScenes(Unseen(StoryMechanic.Rune), false, 1f);        // 룬 이펙트가 잦아든 뒤
        else if (overflow > 0)
            s_instance.EnqueueScenes(Unseen(StoryMechanic.Overflow), false, 1f);    // 회복 숫자가 사라진 뒤
        else if (itemId < ConsumableItem.NUM_OF_KEYS && SparePicked())
            BarkOnce(StoryMechanic.SpareKey);
    }

    // 이 층의 여분 열쇠를 방금(또는 이미) 주웠다.
    static bool SparePicked()
    {
        int index = FloorFact(GeneratedStory.SpareKeys, CurFloor, -1);
        return index >= 0 && Managers.Data.CItemActiveDic.TryGetValue(index, out bool alive) && alive == false;
    }

    static void OnDoorBlocked(int color)
    {
        if (s_instance == null || Contracted == false)
            return;
        Door door = NearestDoor();
        if (door != null && door._doorIndex_forActive == FloorFact(GeneratedStory.VaultDoors, CurFloor, -1))
            return;     // 금고 문은 그 앞에 설 때 따로 가르친다 (WatchFloor)
        BarkOnce(StoryMechanic.NoKey);
    }

    static void BarkOnce(StoryMechanic mechanic)
    {
        List<StoryScene> scenes = Unseen(mechanic);
        if (scenes.Count == 0 || scenes[0].Lines.Length == 0)
            return;
        MarkSeen(scenes[0]);
        UI_StoryBark.Show(scenes[0].Lines[0]);
    }

    static void OnRespawned()
    {
        // R3: 검이 없던 1~2층의 죽음은 세지 않는다.
        if (s_instance == null || Contracted == false)
            return;
        List<StoryScene> first = Unseen(StoryMechanic.Death);
        if (first.Count > 0)
        {
            s_instance.EnqueueScenes(first, false, 0.3f);
            return;
        }
        List<StoryScene> barks = All(StoryTrigger.Death, 0);
        // R13: 81~89층의 마검은 값만 말한다 — 죽음 바크도 데미안 것만.
        if (CurFloor >= GeneratedStory.SwordQuietFrom && CurFloor <= GeneratedStory.SwordQuietTo)
            barks = barks.FindAll(b => b.Lines[0].Who.Portrait != StoryPortrait.Sword);
        if (barks.Count > 0)
            s_instance.StartCoroutine(CoBarkLater(barks[UnityEngine.Random.Range(0, barks.Count)].Lines[0], 1.5f));
    }
    #endregion

    #region 지켜보는 것 — 치명타 한 대 전, 금고 문 앞, 둘 중 하나 앞
    // 첫 진심은 "한 대 전" 에 전투창 위로 가르친다. 그동안 HoldsBattle 로 전투 시계를 멈춰 두고(UI_BattlePopup 이 본다),
    // 닫히면 바로 그 진심이 나간다. AttackCount 는 전투창이 걸음마다 옮겨 적는다.
    void WatchCrit()
    {
        GameManager g = Managers.Game;
        if (g == null || g.OnBattle == false || g.PlayerData == null || Contracted == false || _running > 0)
            return;
        int period = (int)g.PlayerData.Critical;
        if (period <= 0 || g.AttackCount + 1 < period)
            return;
        List<StoryScene> scenes = Unseen(StoryMechanic.Crit);
        UI_BattlePopup battle = Managers.UI.FindPopup<UI_BattlePopup>();
        if (scenes.Count == 0 || battle == null || battle.BattleOver)
            return;
        StartNow(CoCrit(scenes));
    }

    // OnBattle 은 건드리지 않는다 — 켜져 있어야 캐릭터 입력·봇·메뉴가 막히고 같은 몬스터와 다시 부딪혀도 전투가 안 열린다.
    IEnumerator CoCrit(List<StoryScene> scenes)
    {
        HoldsBattle = true;
        try { yield return CoChain(scenes); }
        finally { HoldsBattle = false; }
    }

    void WatchFloor()
    {
        if (_running > 0 || _jobs.Count > 0 || Contracted == false || GameIdle() == false)
            return;
        int floor = CurFloor;
        Vector3 me = Managers.Game.Player._cellPos;

        int vault = FloorFact(GeneratedStory.VaultDoors, floor, -1);
        if (vault >= 0 && Managers.Data.DoorActiveDic.TryGetValue(vault, out bool closed) && closed)
        {
            List<StoryScene> scenes = Unseen(StoryMechanic.Vault);
            Door door = scenes.Count > 0 ? FindDoor(vault) : null;
            if (door != null && Near(me, door.transform.position))
            {
                EnqueueScenes(scenes, false, 0f);
                return;
            }
        }

        if (floor < GeneratedStory.ChoiceFloors.Length && GeneratedStory.ChoiceFloors[floor])
        {
            List<StoryScene> scenes = Unseen(StoryMechanic.Choice);
            GameObject map = CurrentMap();
            if (scenes.Count == 0 || map == null)
                return;
            foreach (ConsumableItem item in map.GetComponentsInChildren<ConsumableItem>(false))
            {
                if (item.ChoicePartner != null && Near(me, item.transform.position))
                {
                    EnqueueScenes(scenes, false, 0f);
                    return;
                }
            }
        }
    }

    // 바로 옆 칸 (대각선·문짝의 두께까지).
    static bool Near(Vector3 a, Vector3 b)
    {
        Vector2 d = new Vector2(a.x - b.x, a.z - b.z);
        return d.magnitude <= Define.TILE_SIZE * 1.5f;
    }

    static GameObject CurrentMap()
    {
        GameManager g = Managers.Game;
        return g.Maps != null && g.Maps.TryGetValue(g.PlayerData.CurStageid, out GameObject map) ? map : null;
    }

    static Door FindDoor(int index)
    {
        GameObject map = CurrentMap();
        if (map == null)
            return null;
        foreach (Door door in map.GetComponentsInChildren<Door>(false))
            if (door._doorIndex_forActive == index)
                return door;
        return null;
    }

    static Door NearestDoor()
    {
        GameObject map = CurrentMap();
        if (map == null || Managers.Game.Player == null)
            return null;
        Vector3 me = Managers.Game.Player._cellPos;
        Door best = null;
        float bestDist = float.MaxValue;
        foreach (Door door in map.GetComponentsInChildren<Door>(false))
        {
            float d = Vector3.Distance(me, door.transform.position);
            if (d < bestDist) { bestDist = d; best = door; }
        }
        return best;
    }
    #endregion

    #region 전투 관문 — 보스 등장(0), 특성 수업(10), 첫 ✖(50)
    // 그 특성의 몬스터에게 처음 부딪혔다. 싸움 직전에 규칙을 가르치고, 끝나면 다음 관문(첫 ✖·확인 창)으로 넘긴다 —
    // 배운 규칙을 바로 그 싸움에 쓴다. 지는 싸움이어도 가르친다(왜 지는지가 그 규칙이다).
    static bool TraitGate(MonsterController monster, Action proceed)
    {
        if (s_instance == null || Contracted == false
            || Managers.Data.MonsterDic.TryGetValue(monster.id, out Data.MonsterData data) == false)
            return false;
        List<StoryScene> chain = Unseen(StoryTrigger.TraitFirst, data.Ability);
        if (chain.Count == 0)
            return false;
        Define.MoveDir facing = Toward(monster.transform.position);     // 붙잡기 전에 센다 — 여기서 터지면 캐릭터가 굳는다
        Acquire();
        s_instance.StartNow(s_instance.CoTrait(chain, facing, proceed));
        return true;
    }

    IEnumerator CoTrait(List<StoryScene> chain, Define.MoveDir facing, Action proceed)
    {
        try
        {
            Managers.Game.Player.SetIdleState(facing);
            Managers.UI.CloseGameSceneUI();
            yield return CoChain(chain);
        }
        finally
        {
            Restore();
            Release();
            // 수업이 어디서 끊겼든 다음 관문으로 — 관문을 쥔 채 두면 캐릭터가 굳는다 (보스 등장과 같다).
            proceed();
        }
    }

    static bool BossIntroGate(MonsterController monster, Action proceed)
    {
        int chapter = BossChapter(monster.id);
        if (chapter < 0)
            return false;
        s_bossPos = monster.transform.position;
        s_bossObject = monster.gameObject;
        List<StoryScene> chain = Unseen(StoryTrigger.BossIntro, chapter);
        if (s_instance == null || chain.Count == 0 || Contracted == false)
            return false;
        // 이길 수 있을 때 처음 부딪힌 순간에만 소개한다 (바이블 R12). 지는 싸움이면 뒤의 관문(첫 ✖·확인 창)이 싸움을
        // 거둔다 — 허세 섞인 소개 바로 뒤에 물러서고, 소개는 다시 안 나오고, 등장곡이 챕터 내내 남았다.
        if (BattleForecast.Of(monster.id, Managers.Game.PlayerData.CurStageid).Win == false)
            return false;
        Acquire();
        s_instance.StartNow(s_instance.CoBossScenes(chain, monster, proceed));
        return true;
    }

    // 계약 뒤 처음으로 못 이기는 상대에게 부딪혔다. 싸움을 열지 않고 가르친 뒤 한 걸음 물러선다.
    // 그 다음부터는 확인 창(순서 100)이 묻는다.
    static bool FatalGate(MonsterController monster, Action proceed)
    {
        if (s_instance == null || Contracted == false)
            return false;
        List<StoryScene> chain = Unseen(StoryMechanic.Fatal);
        if (chain.Count == 0)
            return false;
        BattleForecast.Result r = BattleForecast.Of(monster.id, Managers.Game.PlayerData.CurStageid);
        if (r.Ok == false || r.Win)
            return false;
        Define.MoveDir facing = Toward(monster.transform.position);     // 붙잡기 전에 센다 — 여기서 터지면 캐릭터가 굳는다
        Acquire();
        s_instance.StartNow(s_instance.CoFatal(chain, facing));
        return true;
    }

    IEnumerator CoFatal(List<StoryScene> chain, Define.MoveDir facing)
    {
        try
        {
            Managers.Game.Player.SetIdleState(facing);
            Managers.UI.CloseGameSceneUI();
            yield return CoChain(chain);
        }
        finally
        {
            Restore();
            FightGate.Cancel();
            StepBack(facing);
            Release();
        }
    }

    /// <summary>플레이어 칸에서 target 을 보는 방향. 부딪힌 상대 쪽은 _moveDir 로 알 수 없다 — 연출 중에 누른 키
    /// (대화를 넘기는 Enter·클릭)가 그것을 None 으로 지우고(PlayerController.OnKeyboard), 봇은 _moveDir 없이 걷는다.</summary>
    static Define.MoveDir Toward(Vector3 target)
    {
        Vector3 d = target - Managers.Game.Player._cellPos;
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.z))
            return d.x >= 0f ? Define.MoveDir.Right : Define.MoveDir.Left;
        return d.z >= 0f ? Define.MoveDir.Up : Define.MoveDir.Down;
    }

    // 상대 반대쪽으로 한 칸 (대본: 데미안은 뒷걸음). 등 뒤 칸에 무엇이 있으면 제자리에 선다 — 걸음은 그 칸의 것을
    // 건드린다(PlayerController.isObstacled: 문은 열쇠를 쓰고, 계단은 층을 옮기고, 물약은 마시고, 몬스터와는 싸운다).
    // 고르지 않은 걸음이 그러면 안 된다. 그쪽과 같은 두 가지(키높이 광선, 칸 상자)로 본다.
    static void StepBack(Define.MoveDir facing)
    {
        PlayerController p = Managers.Game.Player;
        if (p == null)
            return;
        // 걸음이 끝나면 PlayerController.Update 가 _moveDir 쪽 대기로 세운다. 대화가 그것을 None 으로 지워 두었으니
        // 그대로 두면 뒷걸음 그림으로 굳는다.
        p._moveDir = facing;
        Vector3 away = facing == Define.MoveDir.Up ? Vector3.back : facing == Define.MoveDir.Down ? Vector3.forward
                     : facing == Define.MoveDir.Left ? Vector3.right : Vector3.left;
        if (Physics.Raycast(p.transform.position + Vector3.up * (Define.TILE_SIZE / 2f), away, Define.TILE_SIZE * 1.3f))
            return;
        foreach (Collider c in Physics.OverlapBox(p._cellPos + away * Define.TILE_SIZE, Vector3.one * (Define.TILE_SIZE * 0.45f),
                                                  Quaternion.identity, ~0, QueryTriggerInteraction.Collide))
        {
            if (Util.Find<MonsterController>(c.gameObject) != null || Util.Find<ConsumableItem>(c.gameObject) != null
                || Util.Find<Equip>(c.gameObject) != null)
                return;
        }
        switch (facing)
        {
            case Define.MoveDir.Up: p.Moving(Define.MoveDir.Back, true); break;     // 뒷걸음 그림 (BackStep)
            case Define.MoveDir.Down: p.Moving(Define.MoveDir.Up, true); break;
            case Define.MoveDir.Left: p.Moving(Define.MoveDir.Right, true); break;
            case Define.MoveDir.Right: p.Moving(Define.MoveDir.Left, true); break;
        }
    }
    #endregion

    #region 프롤로그 — DirectingManager 가 제 연출 사이에 부른다
    /// <summary>
    /// 1~4층의 손수 짠 연출이 제 차례에 부른다(계약 직후, 킹 슬라임 등장, 분열 슬라임을 다 잡은 뒤).
    /// 부른 쪽이 캐릭터·레터박스·HUD 를 쥐고 있으니 여기서는 대화만 튼다. 이미 본 장면은 건너뛴다.
    /// 계약 뒤 장면에는 첫 예측(mechanic_first:forecast)이 곧장 따라붙는다 (바이블 9.1).
    /// </summary>
    public static IEnumerator CoPrologue(StoryPrologue which)
    {
        if (s_instance == null)
            return null;
        List<StoryScene> chain = Unseen(StoryTrigger.Prologue, (int)which);
        if (which == StoryPrologue.ContractAfter)
            chain.AddRange(Unseen(StoryMechanic.Forecast));
        return chain.Count == 0 ? null : Safe(s_instance.Run(s_instance.CoChain(chain)));
    }

    /// <summary>분열 슬라임을 다 잡았다. 그 전투가 끝나 한가해지면 레터박스를 내리고 튼다.</summary>
    public static void QueuePrologue(StoryPrologue which)
    {
        if (s_instance != null)
            s_instance.EnqueueScenes(Unseen(StoryTrigger.Prologue, (int)which), true, 0.5f);
    }
    #endregion

    #region 장면 무리
    void EnqueueScenes(List<StoryScene> chain, bool letterbox, float delay)
    {
        if (chain.Count == 0)
            return;
        Enqueue(() => CoStandalone(chain, letterbox), delay);
    }

    // 한가할 때 뜨는 대화 (첫 룬·넘친 물약·첫 레벨업·금고·둘 중 하나·첫 죽음·킹 슬라임 뒤).
    IEnumerator CoStandalone(List<StoryScene> chain, bool letterbox)
    {
        // 차례를 기다리는 동안 이미 봤을 수 있다 (같은 일이 두 번 알려졌다)
        chain = chain.FindAll(s => HasSeen(s) == false);
        if (chain.Count == 0)
            yield break;
        Acquire();
        try
        {
            Managers.Game.Player.SetIdleState(Managers.Game.Player._moveDir);
            Managers.UI.CloseGameSceneUI();
            if (letterbox)
                Letterbox(true);
            yield return CoChain(chain);
            if (letterbox)
            {
                Letterbox(false);
                yield return new WaitForSeconds(StoryUI.Auto ? 0.2f : 1f);
            }
        }
        finally
        {
            Letterbox(false);
            Restore();
            Release();
        }
    }

    // 레터박스는 제가 내린 것만 걷는다 — 손수 짠 연출(DirectingManager)의 것을 걷지 않게.
    void Letterbox(bool on)
    {
        if (on)
            Managers.Directing.PlayLetterBox();
        else if (_letterbox)
            Managers.Directing.CloseLetterBox();
        _letterbox = on;
    }

    // 층에 들어섰을 때 (Acquire 는 부른 쪽이 이미 했다).
    IEnumerator CoFloorChain(List<StoryScene> chain)
    {
        try
        {
            Managers.Game.Player.SetIdleState(Managers.Game.Player._moveDir);
            // 카드(마을·챕터)로 여는 층은 층 이름을 기다리지 않는다 — 카드가 그 자리를 덮는다.
            bool card = chain[0].Kind == StoryKind.Card || chain[0].Trigger == StoryTrigger.ChapterStart;
            yield return WaitSettled(card == false);
            Managers.UI.CloseGameSceneUI();
            yield return CoChain(chain);
        }
        finally
        {
            Restore();
            Release();
        }
    }

    // 보스 등장(boss 가 있다 — 끝나면 proceed 로 전투를 연다)과 쓰러진 뒤(boss 가 null).
    // 레터박스를 내리고 카메라가 보스를 잡는다. 데미안은 보스(가 섰던) 쪽을 본다.
    IEnumerator CoBossScenes(List<StoryScene> chain, MonsterController boss, Action proceed)
    {
        try
        {
            PlayerController p = Managers.Game.Player;
            p.SetIdleState(s_bossPos.HasValue ? Toward(s_bossPos.Value) : p._moveDir);
            if (boss == null)
                yield return WaitSettled(false);
            Managers.UI.CloseGameSceneUI();
            Letterbox(true);
            FollowBoss();
            if (boss != null)
            {
                Managers.Sound.FadeAndPlayBGM("Boss_Entry_BGM", 1f);
                // 이름표는 부딪힌 그 보스로 쓴다. 창이 스스로 찾는 보스(GameManager.GetBoss)는 챕터의 첫 보스 층
                // (BossRoomId)에서 찾는데, 챕터 0 은 그게 4층 킹 슬라임 방이라 20층 보스에 킹 슬라임(이나 남은 몹) 이름이 붙었다.
                Managers.UI.ShowBossNamePopup(1.5f);
                if (Managers.UI.BossNamePopup != null && Managers.Data.MonsterDic.TryGetValue(boss.id, out Data.MonsterData data))
                    Managers.UI.BossNamePopup.GetText(0).text = Managers.GetString(data.MonsterNameId);   // 그 창의 글은 BossNameText 하나다
                yield return new WaitForSeconds(StoryUI.Auto ? 0.3f : 2.2f);
            }
            else
            {
                yield return new WaitForSeconds(StoryUI.Auto ? 0.2f : 0.8f);
            }
            yield return CoChain(chain);
            Letterbox(false);
            FollowPlayer();
            // 레터박스가 걷히기를 기다린다 — 전투창이 이 화면을 찍어 배경으로 쓴다.
            yield return new WaitForSeconds(StoryUI.Auto ? 0.3f : 1f);
        }
        finally
        {
            Letterbox(false);
            Restore();
            Release();
            // 등장 연출이 어디서 끊겼든 전투는 연다 — 관문을 쥔 채 두면 캐릭터가 굳는다.
            proceed?.Invoke();
        }
    }

    // 장면들을 파일 순서대로 잇는다 (R1). 챕터 첫 장면 앞에는 챕터 카드, 앞 카드를 깔고 뜨는 대화(Backdrop)도 여기서.
    IEnumerator CoChain(List<StoryScene> chain)
    {
        try
        {
            for (int i = 0; i < chain.Count; i++)
            {
                StoryScene s = chain[i];
                if (s.Trigger == StoryTrigger.ChapterStart && (i == 0 || chain[i - 1].Trigger != StoryTrigger.ChapterStart))
                    yield return CoChapterTitle(s.Arg);
                bool keep = s.Kind == StoryKind.Card && i + 1 < chain.Count && HasCue(chain[i + 1], StoryCueKind.Backdrop);
                yield return CoScene(s, keep);
                if (keep == false && s.Kind != StoryKind.Card)
                    CloseBackdrop();
            }
        }
        finally
        {
            CloseBackdrop();
        }
    }

    IEnumerator CoChapterTitle(int chapter)
    {
        if (chapter < 0 || chapter >= GeneratedStory.ChapterNameIds.Length)
            yield break;
        bool done = false;
        _title = true;
        _popup = UI_StoryCardPopup.ShowTitle(GeneratedStory.ChapterNameIds[chapter], GeneratedStory.ChapterSubtitleIds[chapter],
            () => { done = true; StoryUI.EndSkip(); });
        while (done == false)
            yield return null;
        _title = false;
    }

    IEnumerator CoScene(StoryScene scene, bool keepCard)
    {
        bool seenBefore = Records.SceneSeenEver(scene.Id);     // MarkSeen 이 곧 적는다 — 그 전에 잰다
        MarkSeen(scene);
        yield return CoCues(scene, 0);
        bool done = false;
        _scene = scene;
        _sceneSeenBefore = seenBefore;
        // 장면이 끝나면 건너뛰기도 끝난다 — 이어지는 장면은 다시 눌러야(누르고 있어야) 넘어간다.
        Action finished = () => { done = true; StoryUI.EndSkip(); };
        switch (scene.Kind)
        {
            case StoryKind.Dialogue:
            case StoryKind.Choice:
                _lastChoice = null;
                _popup = UI_ConversationPopup.ShowStory(scene, n => Cue(scene, n), c => { _lastChoice = c; finished(); });
                break;
            case StoryKind.Card:
                UI_StoryCardPopup card = UI_StoryCardPopup.Show(scene, n => Cue(scene, n), finished, keepCard);
                if (keepCard)
                    _backdrop = card;
                else
                    _popup = card;
                break;
            case StoryKind.Credits:
                _popup = UI_StoryCreditsPopup.Show(scene, finished);
                break;
            default:
                if (scene.Lines.Length > 0)
                    UI_StoryBark.Show(scene.Lines[0]);
                finished();
                break;
        }
        while (done == false)
            yield return null;
        _scene = null;
        yield return CoCues(scene, StoryCue.End);
    }

    void CloseBackdrop()
    {
        if (_backdrop != null)
            _backdrop.Close();
        _backdrop = null;
    }

    // 층을 옮기는 일(삽화·페이드·계단)이 끝나기를 기다린다. waitName 이면 층 이름 팝업이 걷히기까지.
    static IEnumerator WaitSettled(bool waitName)
    {
        yield return null;
        float until = Time.unscaledTime + 4f;
        while (Time.unscaledTime < until)
        {
            GameManager g = Managers.Game;
            bool busy = g.OnFade || g.OnInteract || g.OnInputLock || g.OnBattle || g.OnLever
                        || FightGate.Pending || Managers.UI.IsPaused || UI_ConfirmPopup.IsOpen;
            if (busy == false && (waitName == false || Managers.UI.FindPopup<UI_StageNamePopup>() == null))
                yield break;
            yield return null;
        }
    }

    // 장면이 끝났다. 카메라·노출·HUD 를 되돌린다.
    void Restore()
    {
        FollowPlayer();
        if (_offsetMoved)
            MoveOffset(Define.DEFALUT_CAMERA_OFFSET, 0.6f);
        _offsetMoved = false;
        if (_exposureMoved && Managers.Game.MainCamera != null)
            StartCoroutine(Safe(CameraController.CoExposure(0.3f, CameraController.Exposure.Default)));
        _exposureMoved = false;
        if (_mark != null)
            Destroy(_mark);
        if (Managers.UI.UI_GameScene != null)
            Managers.UI.ShowGameSceneUI();
    }
    #endregion

    #region 결말 (바이블 6절)
    /// <summary>100층 위층 계단 (다음 층이 없다). 결말을 아직 안 봤으면 결말로, 봤으면 크레딧과 엔딩 씬으로.</summary>
    public static void OnFinalStairs()
    {
        if (s_instance == null)
        {
            Managers.Scene.LoadScene(Define.Scene.EndingScene);
            return;
        }
        if (s_instance._endingRunning)
            return;
        StoryScene choice = First(StoryTrigger.Ending, (int)StoryEnding.Choice);
        Acquire();
        s_instance.StartNow(choice == null || HasSeen(choice) ? s_instance.CoCreditsOnly() : s_instance.CoEnding());
    }

    IEnumerator CoEnding()
    {
        _endingRunning = true;
        try
        {
            Managers.Sound.FadeAndStopBGM(1f);
            yield return WaitSettled(false);
            // 100층 보스를 쓰러뜨리면 계단을 밟지 않고 곧장 여기로 온다 — 마지막 띠를 닫고 판의 점수를 낸다(장부·순위표·판 카드).
            // 계단 쪽(PortalController)과 겹쳐도 장부가 같은 띠·같은 판을 두 번 세지 않는다. DebugPlay 는 판이 아니다.
            if (s_debug == false)
            {
                yield return SwordLedger.CoCloseBand(Managers.Game.PlayerData.CurStageid);
                SwordLedger.FinishRun();
            }
            Managers.UI.CloseGameSceneUI();
            yield return new WaitForSeconds(StoryUI.Auto ? 0.3f : 1.5f);   // 거수의 폭발·흰 빛은 전투 결말이 튼다

            // 왕좌의 입: 쓰러진 자리에 보스 포탈(검은 손)과 보라 소용돌이. 화면이 흔들리고 레터박스, 카메라는 내려다본다.
            OpenThroneMouth();
            StartCoroutine(Safe(CameraController.CoShakeCamera(0.8f, 0.6f)));
            Letterbox(true);
            MoveOffset(new Vector3(0f, 14f, -3f), 2f);
            yield return new WaitForSeconds(StoryUI.Auto ? 0.3f : 2f);

            StoryEnding ending = StoryEnding.Seal;
            StoryScene choice = First(StoryTrigger.Ending, (int)StoryEnding.Choice);
            if (choice != null)
            {
                yield return CoScene(choice, false);
                if (_lastChoice != null)
                    ending = _lastChoice.Ending;
            }
            // R4: 놓지 않기로 했고 검이 배부르면(= 레벨) 새벽.
            if (ending == StoryEnding.Hold && Managers.Game.PlayerData.Level >= DawnLevel
                && First(StoryTrigger.Ending, (int)StoryEnding.Dawn) != null)
                ending = StoryEnding.Dawn;
            s_lastEnding = ending;
            if (s_debug == false)
            {
                PlayerPrefs.SetInt(ClearedKey, 1);
                PlayerPrefs.SetString(ClearedEndingKey, ending.ToString().ToLowerInvariant());
                PlayerPrefs.Save();
                GameEvents.RaiseEndingReached(ending.ToString().ToLowerInvariant());
            }

            yield return CoChain(All(StoryTrigger.Ending, (int)ending));
            Letterbox(false);
            yield return CoChain(All(StoryTrigger.Epilogue, (int)ending));
            yield return CoCredits(ending);
        }
        finally
        {
            _endingRunning = false;
            Release();
        }
        Managers.Scene.LoadScene(Define.Scene.EndingScene);
    }

    // 결말을 이미 본 판에서 100층 계단을 밟았다.
    IEnumerator CoCreditsOnly()
    {
        _endingRunning = true;
        try
        {
            Managers.UI.CloseGameSceneUI();
            yield return CoCredits(LastEnding);
        }
        finally
        {
            _endingRunning = false;
            Release();
        }
        Managers.Scene.LoadScene(Define.Scene.EndingScene);
    }

    // 크레딧 음악은 직전 결말을 잇는다 (바이블 6.7): seal 은 여기서 타이틀곡, dawn 은 타이틀곡, hold 는 마검 조우곡.
    IEnumerator CoCredits(StoryEnding ending)
    {
        Managers.Sound.FadeAndPlayBGM(ending == StoryEnding.Hold ? "EgoSword_Encounter_Event" : "MainTitle_BGM", 2f);
        yield return CoChain(All(StoryTrigger.Credits, 0));
    }

    void OpenThroneMouth()
    {
        Vector3 pos = s_bossPos ?? Managers.Game.Player.transform.position + Vector3.forward * Define.TILE_SIZE * 2f;
        _portal = Managers.Resource.Instantiate("BossPortal");
        if (_portal != null)
        {
            _portal.transform.position = pos;
            Animator anim = _portal.GetComponentInChildren<Animator>();
            if (anim != null)
                anim.Play("BossPortal_Activation");
        }
        _vortex = Managers.Resource.Instantiate("FX_BossPortal_A");
        if (_vortex != null)
            _vortex.transform.position = pos;
        Managers.Sound.Play(Define.Sound.Effect, "BossGate_Event");
    }
    #endregion

    #region 연출 신호 (GeneratedStory 의 Cues — Tools/story_gen.py 의 CUES)
    // 줄이 뜰 때 (대화창·카드가 부른다). 기다리지 않는다.
    void Cue(StoryScene scene, int line)
    {
        if (scene.Cues == null)
            return;
        foreach (StoryCue cue in scene.Cues)
        {
            if (cue.Line == line)
            {
                try { RunCue(cue); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }

    // 대화창이 뜨기 전(0)과 끝난 뒤(End). 이 신호들은 제 몫의 시간을 기다린다 — 카메라가 다 옮겨 간 뒤에 말한다.
    IEnumerator CoCues(StoryScene scene, int line)
    {
        if (scene.Cues == null)
            yield break;
        float wait = 0f;
        foreach (StoryCue cue in scene.Cues)
        {
            if (cue.Line != line)
                continue;
            try { wait = Mathf.Max(wait, RunCue(cue)); }
            catch (Exception e) { Debug.LogException(e); }
        }
        if (wait > 0f)
            yield return new WaitForSeconds(StoryUI.Auto ? Mathf.Min(wait, 0.3f) : wait);
    }

    // 신호 하나를 친다. 돌려주는 값은 그 신호가 화면을 차지하는 시간이다.
    float RunCue(StoryCue cue)
    {
        PlayerController player = Managers.Game.Player;
        switch (cue.Kind)
        {
            case StoryCueKind.Emote:
                if (player != null)     // 머리 위 — 맵 위 이모티콘(Emoji 프리팹)과 같은 자리
                    Spawn($"FX_Emoji_{cue.Arg}", player.transform, new Vector3(0f, 0.8f, -0.1f), 3f);
                return 0f;
            case StoryCueKind.Shake:
                StartCoroutine(Safe(CameraController.CoShakeCamera(0.35f, 0.5f)));
                return 0.4f;
            case StoryCueKind.Flash:
                StartCoroutine(Safe(CameraController.WhiteBang(0.12f)));
                return 0.3f;
            case StoryCueKind.White:
            case StoryCueKind.Dark:
            case StoryCueKind.Clear:
                CameraController.Exposure to = cue.Kind == StoryCueKind.White ? CameraController.Exposure.White
                    : cue.Kind == StoryCueKind.Dark ? CameraController.Exposure.Black : CameraController.Exposure.Default;
                _exposureMoved = cue.Kind != StoryCueKind.Clear;
                StartCoroutine(Safe(CameraController.CoExposure(0.4f, to)));
                return 0.4f;
            case StoryCueKind.CamUp:
                StartCoroutine(Safe(CoCamUp()));
                return 2.2f;
            case StoryCueKind.CamClose:
                // 데미안 머리 위로. 보스 장면은 카메라가 보스를 따라가고 있어서, 오프셋만 당기면 보스에게 붙었다(80층 09).
                FollowPlayer();
                MoveOffset(new Vector3(0f, 6.5f, -3.2f), 1.5f);
                return 1.5f;
            case StoryCueKind.CamPlayer:
                FollowPlayer();
                if (_offsetMoved)
                    MoveOffset(Define.DEFALUT_CAMERA_OFFSET, 0.8f);
                return 0.8f;
            case StoryCueKind.CamMonster:
                Follow(NearestMonster());
                return 1f;
            case StoryCueKind.CamBoss:
                FollowBoss();
                if (_offsetMoved)
                    MoveOffset(Define.DEFALUT_CAMERA_OFFSET, 0.8f);
                return 1f;
            case StoryCueKind.Pose:
                StartCoroutine(Safe(CoPose(cue.Arg)));
                return cue.Arg == "DrawSword" ? 0.8f : 0f;
            case StoryCueKind.Bgm:
                Managers.Sound.FadeAndPlayBGM(cue.Arg, 1.5f);
                return 0f;
            case StoryCueKind.BgmStop:
                Managers.Sound.FadeAndStopBGM(1f);
                return 0f;
            case StoryCueKind.BgmFloor:
                PlayFloorBgm();
                return 0f;
            case StoryCueKind.Fx:
                GameObject fx = player != null ? Spawn(cue.Arg, player.transform, Vector3.zero, 3f) : null;
                if (fx != null && FxScale.TryGetValue(cue.Arg, out Vector3 scale))
                    fx.transform.localScale = scale;
                return 0f;
            case StoryCueKind.Boom:
                StartCoroutine(Safe(CoBoom()));
                return 1.2f;
            case StoryCueKind.Soul:
                if (s_bossPos.HasValue)
                {
                    GameObject soul = Spawn("DeathSoulPurple", null, Vector3.zero, 4f);
                    if (soul != null)
                    {
                        soul.transform.position = s_bossPos.Value;
                        // 넋은 제자리에서 스러진다 (바이블 R17 — 위로 오르면 검은 태양에 빨려 드는 것으로 읽힌다).
                        // 이 파티클은 중력이 음수라 스스로 떠오른다. 이 한 벌만 끈다 — 몬스터가 쓰러질 때(UI_MonsterCard)도 쓴다.
                        foreach (ParticleSystem ps in soul.GetComponentsInChildren<ParticleSystem>())
                        {
                            ParticleSystem.MainModule main = ps.main;
                            main.gravityModifier = 0f;
                        }
                    }
                }
                return 0.5f;
            case StoryCueKind.Hands:
                Transform hands = _portal != null ? _portal.transform.Find("Hands") : null;
                if (hands != null)
                    hands.gameObject.SetActive(true);
                return 0.5f;
            case StoryCueKind.VortexIn:
                if (_vortex != null && player != null)
                    _vortex.transform.DOMove(player.transform.position, 1.5f).SetLink(_vortex);
                return 0f;
            case StoryCueKind.VortexStop:
                if (_vortex != null)
                    foreach (ParticleSystem ps in _vortex.GetComponentsInChildren<ParticleSystem>())
                        ps.Stop();
                return 0f;
            case StoryCueKind.Walk:
                if (player != null)
                    player.Moving(Define.MoveDir.Up, true);
                return 0f;
            default:    // Backdrop: 장면을 잇는 쪽(CoChain)이 본다
                return 0f;
        }
    }

    // 계약 연출(DirectingManager.ContractSword)이 데미안에게 붙일 때 쓰는 크기 — 결말의 빛기둥도 같은 크기로.
    static readonly Dictionary<string, Vector3> FxScale = new Dictionary<string, Vector3>
    {
        { "FX_ContractSwordEffect", new Vector3(0.3f, 0.3f, 0.15f) },
        { "FX_PowerWave", new Vector3(0.2f, 0.2f, 0.1f) },
    };

    static GameObject Spawn(string key, Transform parent, Vector3 local, float life)
    {
        GameObject go = Managers.Resource.Instantiate(key, parent);
        if (go == null)
            return null;
        if (parent != null)
            go.transform.localPosition = local;
        Destroy(go, life);
        return go;
    }

    IEnumerator CoCamUp()
    {
        MoveOffset(new Vector3(0f, 14.5f, -5f), 1f);
        yield return new WaitForSeconds(1.4f);
        MoveOffset(Define.DEFALUT_CAMERA_OFFSET, 0.8f);
    }

    // 검 뽑기·계약 자세는 무기·방패 그림을 끈다(PlayerController.PlayAnimation). 대기로 돌아올 때 되켠다.
    static IEnumerator CoPose(string pose)
    {
        PlayerController p = Managers.Game.Player;
        if (p == null)
            yield break;
        if (pose == "ContractSword")
        {
            p.SetState(Define.PlayerState.ContractSword);
            yield break;
        }
        // 뽑은 뒤에는 뽑기 전 자세로 선다 — 장면을 연 쪽이 보스를 보고 세워 두었다(CoBossScenes·킹 슬라임 연출).
        // _moveDir 로 다시 세우면 대화를 넘긴 키가 그것을 None 으로 지워(PlayerController.OnKeyboard) 카메라를 봤다.
        Define.PlayerState idle = pose == "DrawSword" ? p._state : Define.PlayerState.IdleFront;
        if (pose == "DrawSword")
        {
            p.SetState(Define.PlayerState.DrawSword);
            yield return new WaitForSeconds(0.7f);
        }
        if (p == null)
            yield break;
        p._isEquiptWeapon = true;
        p._isEquiptShield = true;
        p.SetState(idle);
    }

    IEnumerator CoBoom()
    {
        if (s_bossPos.HasValue == false)
            yield break;
        Vector3 pos = s_bossPos.Value;
        Managers.Sound.Play(Define.Sound.Effect, "BossDeath_SFX");
        GameObject boom = Managers.Resource.Instantiate("BossDeathBoom");
        if (boom != null)
        {
            boom.transform.position = pos;
            foreach (Transform fog in boom.transform)
                fog.position = pos + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(0f, 0.5f), -0.01f);
            Destroy(boom, 1.2f);
        }
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(Safe(CameraController.WhiteBang(0.1f)));
        GameObject light = Managers.Resource.Instantiate("BossDeathLight");
        if (light != null)
        {
            light.transform.position = pos;
            light.transform.localScale = new Vector3(1f, 2f, 1f);
            Destroy(light, 1f);
        }
    }

    static CameraController Cam => Managers.Game.MainCamera != null ? Managers.Game.MainCamera.GetComponentInChildren<CameraController>() : null;

    void MoveOffset(Vector3 to, float time)
    {
        CameraController cam = Cam;
        if (cam == null || CameraController._transposer == null)
            return;
        cam.StartCoVirtualCameraMove(CameraController._transposer.m_FollowOffset, to, time);
        _offsetMoved = to != Define.DEFALUT_CAMERA_OFFSET;
    }

    static void Follow(Transform target)
    {
        CameraController cam = Cam;
        if (cam != null && target != null)
            cam.SetCameraTarget(target.gameObject);
    }

    static void FollowPlayer()
    {
        if (Managers.Game.Player != null)
            Follow(Managers.Game.Player.transform);
    }

    void FollowBoss()
    {
        if (s_bossObject != null && s_bossObject.activeInHierarchy)
        {
            Follow(s_bossObject.transform);
            return;
        }
        if (s_bossPos.HasValue == false)
            return;
        // 보스는 쓰러지며 사라졌다 — 그 자리에 빈 표식을 두고 본다.
        if (_mark == null)
            _mark = new GameObject("@StoryBossMark");
        _mark.transform.position = s_bossPos.Value;
        Follow(_mark.transform);
    }

    static Transform NearestMonster()
    {
        GameObject map = CurrentMap();
        if (map == null)
            return null;
        Vector3 me = Managers.Game.Player.transform.position;
        Transform best = null;
        float bestDist = float.MaxValue;
        foreach (MonsterController m in map.GetComponentsInChildren<MonsterController>(false))
        {
            float d = Vector3.Distance(me, m.transform.position);
            if (d < bestDist) { bestDist = d; best = m.transform; }
        }
        return best;
    }

    // 이 층의 챕터곡 — GameManager.PlayChapterBGM 과 같은 곡·조다 (그쪽이 층을 지을 때 트는 것).
    static void PlayFloorBgm()
    {
        int stage = Managers.Game.PlayerData.CurStageid;
        string key = Managers.Data.StageInfoDic.TryGetValue(stage, out Data.StageInfoData info) ? info.BGM : null;
        if (string.IsNullOrEmpty(key) || Managers.Resource.Load<AudioClip>(key) == null)
            key = "Chapter0_BGM";
        Managers.Sound.FadeAndPlayBGM(key, 2f, ChapterTheme.Get(MapBuilder.GetChapter(stage)).BgmPitch);
    }

    /// <summary>엔딩 씬이 검은 화면에 띄우는 크레딧 마지막 줄. 없으면 0.</summary>
    public static int LastCreditsLineId()
    {
        StoryScene credits = First(StoryTrigger.Credits, 0);
        return credits != null && credits.Lines.Length > 0 ? credits.Lines[credits.Lines.Length - 1].ScriptId : 0;
    }
    #endregion

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>개발용(eval): 장면 하나를 제 연출째 다시 튼다. 본 것으로도, 결말로도 적지 않는다.
    /// 챕터 첫 장면은 챕터 카드부터, 카드 뒤에 깔고 뜨는 대화(무덤 카드 → 촌장)는 같이 튼다.
    /// 결말 선택(ending_choice)은 고른 결말·후일담·크레딧을 지나 엔딩 씬까지 간다.</summary>
    public static void DebugPlay(string sceneId)
    {
        int at = Array.FindIndex(GeneratedStory.Scenes, s => s.Id == sceneId);
        if (s_instance == null || at < 0 || Managers.Game.Player == null)
        {
            Debug.LogWarning($"[Story] DebugPlay: '{sceneId}' 를 틀 수 없다 (게임 씬에서, 있는 id 로)");
            return;
        }
        AbortAll();
        StoryScene scene = GeneratedStory.Scenes[at];
        if (scene.Kind == StoryKind.Bark)
        {
            UI_StoryBark.Show(scene.Lines[0]);
            return;
        }
        List<StoryScene> chain = new List<StoryScene> { scene };
        if (scene.Kind == StoryKind.Card && at + 1 < GeneratedStory.Scenes.Length && HasCue(GeneratedStory.Scenes[at + 1], StoryCueKind.Backdrop))
            chain.Add(GeneratedStory.Scenes[at + 1]);
        s_debug = true;
        Acquire();
        IEnumerator job = scene.Kind == StoryKind.Choice ? s_instance.CoEnding()
            : scene.Trigger == StoryTrigger.BossIntro || scene.Trigger == StoryTrigger.BossDefeat ? s_instance.CoBossScenes(chain, null, null)
            : s_instance.CoFloorChain(chain);
        s_instance.StartNow(CoDebug(job));
    }

    static IEnumerator CoDebug(IEnumerator job)
    {
        try { yield return job; }
        finally { s_debug = false; }
    }
#endif
}
