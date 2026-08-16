using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 게임을 처음부터 끝까지 스스로 플레이한다. 녹화(PlaythroughRecorder)가 이 봇을 띄운다.
///
/// 사람이 하는 것과 같은 경로로만 논다 — 상태를 직접 조작하지 않고
/// PlayerController.Moving() 한 번으로 이동/전투/아이템/문/계단이 전부 일어난다.
/// 조작 불가능한 지점(대사 넘김, 예/아니오 팝업)만 그 UI 의 공개 진입점을 부른다.
///
/// 층 안에서의 우선순위 = 열쇠 -> 몬스터(약한 놈부터) -> 포션(피 낮을 때) -> 장비/레버 -> 계단.
/// "문을 열 열쇠가 없으면 그 문은 벽" 규칙이 방 순서를, 따라서 전투 순서를 강제한다.
/// </summary>
public class AutoPlayer : MonoBehaviour
{
    public static AutoPlayer Instance;

    public bool Finished;
    public bool Failed;
    public string Result = "";

    /// <summary>한 칸 이동 간격. 이동 트윈(1/speed)보다 넉넉해야 입력이 씹히지 않는다.</summary>
    public float StepInterval = 0.16f;
    /// <summary>이 시간 동안 아무 진전이 없으면 실패로 본다.</summary>
    public float StallTimeout = 90f;

    /// <summary>지금 무엇을 향해 가는 중인지 (로그용).</summary>
    public string Plan { get { return _plan; } }
    /// <summary>HP 가 이 비율 아래일 때만 포션을 줍는다 (가득 찬 상태에서 낭비 방지).</summary>
    public float PotionHpRatio = 0.9f;
    /// <summary>이 아래로 떨어지면 몬스터보다 포션이 먼저다 (밸런스 시뮬레이터와 같은 값).</summary>
    public float PotionEmergency = 0.7f;
    /// <summary>이 아래인데 이 층에 포션이 없으면 아래층으로 되돌아가 찾는다.</summary>
    public float RetreatHpRatio = 0.4f;
    /// <summary>싸우고 나서 이만큼은 남아야 덤빈다. 안 되면 다른 놈을 먼저 잡는다.</summary>
    public float SafeHpAfterFight = 0.35f;
    /// <summary>이만큼 채우기 전에는 위층/보스방으로 올라가지 않는다.
    /// 위층은 언제나 더 세다 — 반피로 올라가면 그 층에서 죽는다.</summary>
    public float AscendHpRatio = 0.75f;

    /// <summary>전투 배속. 게임 안에 있는 옵션 그대로다(1/2/4). 녹화 길이를 줄인다.</summary>
    public int GameSpeed = 8;
    /// <summary>이동 배속. 걷는 시간이 영상의 절반을 먹어서 줄인다.</summary>
    public float MoveSpeedScale = 5f;

    const float Tile = 0.32f;

    // 목표 우선순위 (작을수록 먼저)
    const int PriHeal = 0;      // 위급 — 포션 먼저
    const int PriKey = 1;
    const int PriMonster = 2;
    const int PriTopUp = 3;     // 여유 있을 때 포션
    const int PriRune = 4;
    const int PriEquip = 5;
    const int PriLever = 6;
    const int PriTrigger = 7;   // 보스문 / 상호작용 / 보스 등장 트리거
    const int PriStairs = 8;
    const int PriExplore = 9;   // 목표가 없을 때 안 밟아 본 칸으로

    float _nextStep;
    float _baseMove;
    float _nextUi;
    float _lastProgress;
    string _progressKey = "";
    int _maxFloor = 1;

    // BFS 재사용 버퍼
    readonly Dictionary<Vector2Int, Vector2Int> _from = new Dictionary<Vector2Int, Vector2Int>();
    readonly Dictionary<Vector2Int, int> _dist = new Dictionary<Vector2Int, int>();
    readonly Queue<Vector2Int> _queue = new Queue<Vector2Int>();
    readonly Dictionary<Vector2Int, bool> _solid = new Dictionary<Vector2Int, bool>();
    readonly HashSet<Vector2Int> _visited = new HashSet<Vector2Int>();
    /// <summary>밀어도 아무 일이 없던 목표들. 같은 자리에서 헛돌지 않게 기억해 둔다.</summary>
    readonly HashSet<Vector2Int> _deadTargets = new HashSet<Vector2Int>();

    /// <summary>
    /// 이미 밀어 본 트리거 칸. _deadTargets 와 반드시 따로 둔다.
    /// 3층 마검 제단(9,-4)은 계약이 끝나면 바로 그 칸에 노란 열쇠를 떨군다 —
    /// 트리거를 껐다고 칸 전체를 죽이면 그 열쇠를 영영 못 줍고,
    /// 열쇠가 없으면 2층 문도 보스방도 열리지 않는다.
    /// </summary>
    readonly HashSet<Vector2Int> _firedTriggers = new HashSet<Vector2Int>();
    Vector2Int _lastBump;
    int _sameBump;
    readonly List<KeyValuePair<Transform, float>> _risky = new List<KeyValuePair<Transform, float>>();
    float _nextPush;
    int _pushDir;
    int _visitedStage = -1;
    int _retreats;
    int _totalRetreats;
    readonly Dictionary<string, float> _uiLog = new Dictionary<string, float>();

    /// <summary>UI 별로 마지막에 누른 시각. 매 프레임 연타하지 않기 위한 것이다.</summary>
    readonly Dictionary<string, float> _uiCall = new Dictionary<string, float>();

    /// <summary>"새 게임"은 한 방향 전환이라 딱 한 번만 누른다.</summary>
    bool _pressedTitle;
    /// <summary>연출이 이 시간(초) 넘게 안 끝나면 잠금을 강제로 푼다.</summary>
    public float LockTimeout = 15f;
    float _lastReal;
    string _realSig = "";
    float _lastError;
    int _guideStuck;
    bool _wasBattle;
    float _hpBefore;

    GameObject _map;
    float _probeY;
    string _plan = "";
    string _bossInfo = "";
    /// <summary>보스방 입구를 지금 갈 수 있는지 (로그용).</summary>
    public string BossInfo { get { return _bossInfo; } }

    // 길에 두면 "지나가다" 건드려 버리는 것들. 전부 막고, 목표일 때만 옆에서 부딪힌다.
    // 이걸 안 막으면 포션을 향해 가다 몬스터를 밟아 원치 않는 전투가 나고,
    // 계단을 밟아 층을 건너뛰기도 한다 — 1층에서 죽던 진짜 원인이었다.
    static readonly int BlockMask = (1 << (int)Define.Layer.Wall)
                                  | (1 << (int)Define.Layer.InteractObjects)
                                  | (1 << (int)Define.Layer.BossDoor)
                                  | (1 << (int)Define.Layer.Monster)
                                  | (1 << (int)Define.Layer.CItem)
                                  | (1 << (int)Define.Layer.EItem)
                                  | (1 << (int)Define.Layer.Portal)
                                  | (1 << (int)Define.Layer.Lever);
    static readonly int DoorMask = 1 << (int)Define.Layer.Door;
    static readonly int WallMask = 1 << (int)Define.Layer.Wall;
    static readonly int ItemMask = (1 << (int)Define.Layer.CItem)
                                 | (1 << (int)Define.Layer.EItem);
    static readonly int PushMask = (1 << (int)Define.Layer.InteractObjects)
                                 | (1 << (int)Define.Layer.BossDoor);

    static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(1, 0),
    };

    public static AutoPlayer Spawn()
    {
        GameObject go = new GameObject("@AutoPlayer");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<AutoPlayer>();
        return Instance;
    }

    void Start()
    {
        _lastProgress = Time.unscaledTime;
        _lastReal = Time.unscaledTime;
        StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        while (Finished == false && Failed == false)
        {
            yield return null;

            if (Managers.Scene != null && Managers.Scene.CurrentScene != null
                && Managers.Scene.CurrentScene.SceneType == Define.Scene.EndingScene)
            {
                Succeed($"엔딩 도달 (최고 {_maxFloor}층)");
                yield break;
            }

            // 한 프레임에서 터진 예외가 코루틴을 죽이면 봇이 통째로 멈춘다.
            // 진전 추적도 반드시 이 안에 있어야 한다 — 밖에 두면 그게 죽인다.
            try
            {
                TrackRealProgress();
                TickBattleLog();
                TickUI();
                TickPlay();
            }
            catch (Exception e)
            {
                if (Time.unscaledTime - _lastError > 5f)
                {
                    _lastError = Time.unscaledTime;
                    Debug.LogWarning($"[AutoPlayer] 프레임 예외: {e}");
                }
            }

            // 워치독은 따로 돌린다. 위에서 예외가 나면 같이 건너뛰어져서,
            // 정작 봇이 망가졌을 때 그걸 잡아야 할 감시가 먼저 꺼졌다.
            try
            {
                TickStall();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AutoPlayer] 워치독 예외: {e.Message}");
            }
        }
    }

    /// <summary>층/레벨/HP/위치 중 하나라도 달라지면 "진전"으로 본다.</summary>
    void TrackRealProgress()
    {
        GameManager g = Managers.Game;
        if (g == null || g.PlayerData == null || g.Player == null)
            return;

        // 위치는 "칸" 단위로 본다. 연출 중 트윈이 만드는 미세 이동까지 진전으로 세면
        // 워치독이 영영 안 돈다 (마검 팝업 앞에서 정확히 그랬다).
        Vector3 pos = g.Player.transform.position;
        string sig = $"{g.PlayerData.CurStageid}/{g.PlayerData.Level}/" +
                     $"{Mathf.RoundToInt(g.PlayerData.CurHP)}/" +
                     $"{Mathf.RoundToInt(pos.x / Tile)},{Mathf.RoundToInt(pos.z / Tile)}";
        if (sig == _realSig)
            return;
        _realSig = sig;
        _lastReal = Time.unscaledTime;
    }

    /// <summary>전투 하나하나를 기록한다. 어디서 왜 죽는지는 이 로그로만 알 수 있다.</summary>
    void TickBattleLog()
    {
        GameManager g = Managers.Game;
        if (g == null || g.PlayerData == null)
            return;

        if (g.OnBattle == _wasBattle)
            return;
        _wasBattle = g.OnBattle;

        if (g.OnBattle)
        {
            _hpBefore = g.PlayerData.CurHP;
            int id = g.MonsterData.Count > 0 ? g.MonsterData[0].id : -1;
            float atk = g.MonsterData.Count > 0 ? g.MonsterData[0].Attack : 0f;
            float hp = g.MonsterData.Count > 0 ? g.MonsterData[0].MaxHP : 0f;
            Debug.Log($"[AutoPlayer] 전투 {g.PlayerData.CurStageid + 1}층 몬스터{id} " +
                      $"(HP {hp:0} ATK {atk:0.#}) vs Lv{g.PlayerData.Level} " +
                      $"HP {_hpBefore:0}/{g.PlayerData.MaxHP:0}");
        }
        else
        {
            Debug.Log($"[AutoPlayer] 전투 끝 HP {_hpBefore:0} -> {g.PlayerData.CurHP:0}");
        }
    }

    #region UI — 사람이 눌러야만 넘어가는 지점들
    void TickUI()
    {
        if (Time.unscaledTime < _nextUi)
            return;
        _nextUi = Time.unscaledTime + 0.35f;

        if (Handle<UI_GameOverPopup>(p => Fail("플레이어 사망")))
            return;
        // 대사: 한 장씩 넘긴다. 마지막 장에서 스스로 닫는다.
        if (Handle<UI_ConversationPopup>(p => p.ShowNextScript()))
            return;
        // 예/아니오는 "예"로 간다 — 보스방/마검 계약 둘 다 진행 쪽이 예다.
        // 버튼을 "누르는" 대신 그 버튼이 부르는 메서드를 직접 호출한다.
        // 팝업이 스택 위에 묻히면 ClosePopupUI 가 조용히 실패해서(UIManager 제한)
        // 마우스 클릭만으로는 영영 못 닫고 OnInputLock 이 걸린 채로 멈춘다.
        if (Handle<UI_MagicalSwordCheckPopup>(p => Call(p, "YesClick")))
            return;
        if (Handle<UI_BossRoomCheckPopup>(p => Call(p, "YesClick")))
            return;
        if (Handle<UI_GuidePopup>(ClearGuide))
            return;
        if (Handle<UI_SelectLanguagePopup>(p => ClickFirst(p.gameObject)))
            return;
        // 게임 씬에 들어왔으면 타이틀/인트로는 이미 지난 화면이다.
        // 그런데도 오브젝트가 남아 있을 때가 있고, 그걸 계속 누르면
        // 씬 전환이 다시 걸려서 플레이어가 배치되기 전으로 되돌아갔다.
        if (Managers.Scene != null && Managers.Scene.CurrentScene != null
            && Managers.Scene.CurrentScene.SceneType == Define.Scene.GameScene)
            return;

        // 타이틀의 "새 게임"과 인트로의 "다음"은 한 방향 전환이다.
        // 두 번 누르면 전환이 처음부터 다시 돌아서, 플레이어가 배치되기 전으로
        // 되감긴다. 쿨다운으로는 부족했다 — 아예 한 번만 누른다.
        // 눌렀는데도 안 넘어가면 워치독이 10분 뒤에 잡는다.
        // 인트로는 여러 장이라 계속 넘겨야 한다 (Handle 이 2초 간격을 지킨다).
        // 한 번만 누르게 했더니 두 번째 장에서 영영 멈췄다.
        // 커튼콜이 끝나기 전(_lock)에는 사람도 못 넘긴다 — 그때는 손대지 않는다.
        if (Handle<UI_IntroScene>(p =>
            {
                if (Field<bool>(p, "_lock"))
                    return;
                Call(p, "NextScene");
            }))
            return;
        if (_pressedTitle == false && Handle<UI_TitleScene>(p =>
            {
                if (Field<bool>(p, "isPreload") == false || Field<bool>(p, "_lock"))
                    return;   // 아직 누를 때가 아니다 — 다음 프레임에 다시 본다
                _pressedTitle = true;
                TitleStart(p);
            }))
            return;
    }

    bool Handle<T>(Action<T> action) where T : MonoBehaviour
    {
        T target = FindFirstObjectByType<T>();
        if (target == null || target.isActiveAndEnabled == false)
            return false;

        // 어떤 UI 가 흐름을 잡고 있는지 보이게 남긴다 (같은 UI 는 2초에 한 번만).
        float now = Time.time;
        float last;
        if (_uiLog.TryGetValue(typeof(T).Name, out last) == false || now - last > 2f)
        {
            _uiLog[typeof(T).Name] = now;
            Debug.Log($"[AutoPlayer] UI 처리: {typeof(T).Name}");
        }

        // 같은 UI 를 매 프레임 누르지 않는다. 사람은 그렇게 못 누르고,
        // 무엇보다 씬을 넘기는 버튼은 누를 때마다 코루틴이 새로 돈다 —
        // 인트로에서 NextScene 을 프레임마다 불러 전환이 계속 다시 시작됐고,
        // 그 바람에 플레이어가 아예 배치되지 않은 채로 굳었다.
        float lastCall;
        if (_uiCall.TryGetValue(typeof(T).Name, out lastCall) && now - lastCall < 2f)
        {
            Progress($"ui:{typeof(T).Name}");
            return true;   // 이 UI 가 흐름을 잡고 있는 건 맞다. 기다린다.
        }
        _uiCall[typeof(T).Name] = now;

        try
        {
            action(target);
        }
        catch (Exception e)
        {
            // 여기서 true 를 돌려주면 뒤에 있는 팝업이 영영 처리되지 않는다.
            Debug.LogWarning($"[AutoPlayer] {typeof(T).Name}: {e.Message}");
            return false;
        }
        Progress($"ui:{typeof(T).Name}");
        return true;
    }

    /// <summary>타이틀은 프리로드가 끝나야 시작할 수 있다. 끝나면 새 게임으로 들어간다.</summary>
    void TitleStart(UI_TitleScene title)
    {
        if (Field<bool>(title, "isPreload") == false)
            return;
        if (Field<bool>(title, "_lock"))
            return;
        title.StartCoroutine((IEnumerator)Method(title, "CoOnClickNewGameButton").Invoke(title, null));
    }

    static void Click(GameObject root, string buttonName)
    {
        foreach (UI_EventHandler h in root.GetComponentsInChildren<UI_EventHandler>(true))
        {
            if (h.name != buttonName || h.OnClickHandler == null)
                continue;
            h.gameObject.SetActive(true);
            h.OnClickHandler.Invoke();
            return;
        }
    }

    static void ClickFirst(GameObject root)
    {
        foreach (UI_EventHandler h in root.GetComponentsInChildren<UI_EventHandler>(true))
        {
            if (h.OnClickHandler == null)
                continue;
            h.OnClickHandler.Invoke();
            return;
        }
    }

    /// <summary>
    /// 가이드 팝업. YesClick 이 OnInputLock 을 풀고 닫으려 하지만,
    /// 그 위에 다른 팝업이 쌓여 있으면 닫히지 않는다. 그때는 스택을 한 장씩 걷어낸다.
    /// </summary>
    void ClearGuide(UI_GuidePopup guide)
    {
        Call(guide, "YesClick");
        Managers.Game.OnInputLock = false;

        if (++_guideStuck > 3)
        {
            _guideStuck = 0;
            Managers.UI.ClosePopupUI();
        }
    }
    #endregion

    #region 플레이
    void TickPlay()
    {
        _waitingForGame = false;

        // 어디서 빠져나왔는지 남긴다. 이유 없이 조용히 멈추면
        // 로그만 보고는 씬을 기다리는 중인지 봇이 죽은 건지 구별할 수가 없다.
        if (Managers.Game == null || Managers.Game.Player == null)
        {
            _plan = "플레이어 없음";
            return;
        }
        if (Managers.Scene == null || Managers.Scene.CurrentScene == null)
        {
            _waitingForGame = true;
            _plan = "씬 없음";
            return;
        }
        if (Managers.Scene.CurrentScene.SceneType != Define.Scene.GameScene)
        {
            _waitingForGame = true;
            _plan = $"씬={Managers.Scene.CurrentScene.SceneType} — GameScene 을 기다린다";
            return;
        }

        GameManager g = Managers.Game;
        ApplySpeed(g);

        // 전투/연출/페이드 중에는 손대지 않는다. 전투는 알아서 끝난다.
        if (g.OnBattle || g.OnConversation || g.OnLever || g.OnFade
            || g.OnDirect || g.OnInteract || g.OnInputLock)
        {
            Progress($"busy:{g.PlayerData.CurStageid}");
            WatchLocks(g);
            return;
        }

        if (Time.time < _nextStep)
            return;
        _nextStep = Time.time + StepInterval;

        GameObject map = ResolveMap(g);
        if (map == null)
        {
            _waitingForGame = true;
            _plan = $"{g.PlayerData.CurStageid + 1}층 맵이 아직 없다";
            return;
        }

        _maxFloor = Mathf.Max(_maxFloor, g.PlayerData.CurStageid + 1);

        // 층이 바뀌면 발자국을 지운다 (탐색 폴백용).
        if (_visitedStage != g.PlayerData.CurStageid)
        {
            // 층이 올라갔으면 후퇴 횟수를 리셋한다 (내려온 거면 그대로 센다).
            if (g.PlayerData.CurStageid > _visitedStage)
                _retreats = 0;
            else
            {
                _retreats++;
                _totalRetreats++;
            }
            _visitedStage = g.PlayerData.CurStageid;
            _visited.Clear();
            _deadTargets.Clear();
            _firedTriggers.Clear();
            _movedOnFloor = false;
        }
        _visited.Add(Cell(map, g.Player.transform.position));

        Define.MoveDir dir = PlanStep(map, g);
        if (dir == Define.MoveDir.None)
            return;

        g.Player.Moving(dir, false);
        _movedOnFloor = true;
        Progress($"{g.PlayerData.CurStageid}:{Cell(map, g.Player.transform.position)}");
    }

    /// <summary>
    /// 연출이 끝나지 않는 경우가 있다. 손수 만든 컷신들이 GameObject.Find 로 짜여 있어서
    /// 하나라도 못 찾으면 코루틴이 중간에 죽고 OnDirect 가 켜진 채로 남는다 —
    /// 그러면 사람이 플레이해도 그 자리에서 영영 못 움직인다.
    /// 봇은 일정 시간 뒤 잠금을 직접 풀고 진행한다 (원인은 따로 보고한다).
    /// </summary>
    /// <summary>에디터 워치독이 잠금을 풀었을 때 불린다 (PlaythroughRecorder.Unstick).</summary>
    public void OnUnstuck()
    {
        _lastReal = Time.unscaledTime;
        _firedTriggers.Add(_lastBump);   // 같은 자리를 또 밀면 죽은 연출이 다시 켜진다
        FinishBrokenContract(Managers.Game);
    }

    void WatchLocks(GameManager g)
    {
        if (g.OnBattle)
            return;   // 전투는 스스로 끝난다

        // 잠금 플래그로 재면 안 된다 — 죽은 연출이 다시 켜지는 사이에
        // 잠깐 풀린 프레임이 끼어들어 타이머가 계속 초기화된다.
        // "층/레벨/HP/위치 중 무엇도 달라지지 않았다" 를 기준으로 잡는다.
        if (Time.unscaledTime - _lastReal < LockTimeout)
            return;

        Debug.LogWarning($"[AutoPlayer] 연출/입력 잠금이 {LockTimeout:0}초 넘게 안 풀려 강제 해제 " +
                         $"({g.PlayerData.CurStageid + 1}층)");
        g.OnDirect = false;
        g.OnConversation = false;
        g.OnInteract = false;
        g.OnInputLock = false;
        g.OnFade = false;
        g.OnLever = false;
        Managers.UI.ClosePopupUI();
        Managers.UI.ShowGameSceneUI();
        _lastReal = Time.unscaledTime;

        // 같은 자리를 또 밀면 죽은 연출이 다시 켜진다. 그 목표는 버린다.
        _deadTargets.Add(_lastBump);

        FinishBrokenContract(g);
    }

    /// <summary>
    /// 마검 계약 연출(DirectingManager.ContractSword)이 중간에 죽으면
    /// 마검(+10 ATK)을 못 받은 채로 진행이 막힌다 — 그 층 이후를 이길 수가 없다.
    /// 연출이 하려던 결과만 손으로 마무리한다. 게임 쪽 버그의 우회다.
    /// </summary>
    void FinishBrokenContract(GameManager g)
    {
        if (g.PlayerData.IsContractedSword)
            return;
        if (g.PlayerData.CurSword != Define.EQUIP_SOWRD_FIRST)
            return;

        Data.StageInfoData info;
        if (Managers.Data.StageInfoDic.TryGetValue(g.PlayerData.CurStageid, out info) == false
            || info.DungeonID != "00_002")
            return;

        Debug.LogWarning("[AutoPlayer] 마검 계약 연출이 끊겨 결과만 직접 적용한다 (게임 버그 우회)");

        g.PlayerData.IsContractedSword = true;
        List<int> swords = g.PlayerData.Inventory[(int)Define.Types.Sword];
        if (swords.Contains(Define.EQUIP_SOWRD_FIRST + 1) == false)
            swords.Add(Define.EQUIP_SOWRD_FIRST + 1);
        g.SwapEquip(Define.EQUIP_SOWRD_FIRST + 1);
        g.Player._isEquiptWeapon = true;
        g.Player._isEquiptShield = true;

        // 계약 뒤에 열리는 열쇠도 연출이 켜 주는 것이라 같이 켠다.
        GameObject map;
        if (g.Maps != null && g.Maps.TryGetValue(g.PlayerData.CurStageid, out map) && map != null)
        {
            Transform key = map.transform.Find("Items/CItem13");
            if (key != null)
            {
                key.gameObject.SetActive(true);
                SpriteRenderer sr = key.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = true;
                BoxCollider col = key.GetComponent<BoxCollider>();
                if (col != null) col.enabled = true;
            }
        }
    }

    /// <summary>
    /// 배속. 100층을 실시간으로 돌면 3시간이 넘어서 영상으로 쓸 수가 없다.
    /// 전투는 게임에 원래 있는 GameSpeed 옵션을, 이동은 MoveSpeed 를 올려서 줄인다.
    /// 데미지 계산과 전투 결과는 배속과 무관하다 — 쿨타임이 차는 속도만 바뀐다.
    /// </summary>
    void ApplySpeed(GameManager g)
    {
        if (g.GameSpeed != GameSpeed)
            g.GameSpeed = GameSpeed;

        float move = g.PlayerData.MoveSpeed;
        if (move <= 0f)
            return;

        if (_baseMove <= 0f)
            _baseMove = move;

        float want = _baseMove * MoveSpeedScale;
        if (Mathf.Abs(move - want) > 0.001f)
        {
            g.PlayerData.MoveSpeed = want;
            g.Player.Speed = 0f;   // 세터가 MoveSpeed 를 다시 읽어 _duration 을 고친다
        }

        // 트윈이 끝나기 전에 다음 입력을 넣으면 씹힌다. 이동 시간보다 살짝 길게.
        StepInterval = 1f / (want * 5f) + 0.012f;
    }

    /// <summary>지금 층에서 다음에 밟을 한 칸. 목표가 없으면 None.</summary>
    Define.MoveDir PlanStep(GameObject map, GameManager g)
    {
        _map = map;
        _probeY = g.Player.transform.position.y + Tile * 0.5f;
        _solid.Clear();
        UpdateBounds(map);

        // 씬이 뜬 직후에는 플레이어가 아직 던전 안으로 옮겨지기 전이다.
        // 그때 세운 계획은 맵 밖 허공을 가리키고, 거기엔 벽이 없어서
        // 봇이 한 방향으로 끝없이 걸어 나갔다 (한 번은 80000칸을 갔다).
        if (InsidePlayArea(g.Player.transform.position) == false)
        {
            // 층이 아직 다 조립되기 전에 잰 범위일 수도 있다. 다음 프레임에 다시 잰다.
            _hasBounds = false;
            _waitingForGame = true;
            _plan = "던전 밖 — 자리 잡기를 기다린다";
            return Define.MoveDir.None;
        }

        Vector2Int start = Cell(map, g.Player.transform.position);
        Flood(start);

        // 계단도 몬스터도 아이템도 하나같이 안 닿으면 던전 안에 있는 게 아니다.
        // 콜라이더 경계 상자는 실제 놀이 공간보다 커서 그것만으로는 못 거른다.
        // 씬이 뜬 직후 플레이어가 아직 배치되기 전이 정확히 이 상태이고,
        // 그때 움직이기 시작하면 맵 밖을 하염없이 걸어 다닌다.
        // 단, 이 층에서 아직 한 발도 못 뗐을 때만 본다. 일단 걷기 시작했다면
        // 우리가 던전 안에 있다는 건 이미 증명됐고, 그 뒤로 안 닿는 상황은
        // 막다른 곳에 선 것뿐이라 탐색/밀기로 빠져나가야 한다.
        if (_movedOnFloor == false && AnythingReachable(map) == false)
        {
            _waitingForGame = true;
            _plan = $"던전 밖 {start} — 닿는 게 하나도 없다";
            return Define.MoveDir.None;
        }

        Vector2Int best = start;
        Vector2Int bump = start;
        bool hasBump = false;
        long bestScore = long.MaxValue;

        _risky.Clear();
        // 이 층을 다 비웠는가. 계단도, 남은 포션 처리도 이 값으로 갈린다.
        bool cleared = map.GetComponentsInChildren<MonsterController>(false).Length == 0;
        float hpRatio = g.PlayerData.MaxHP > 0 ? g.PlayerData.CurHP / g.PlayerData.MaxHP : 1f;
        bool potionHere = false;
        foreach (ConsumableItem ci in map.GetComponentsInChildren<ConsumableItem>(false))
        {
            if (ci.id < ConsumableItem.NUM_OF_KEYS || ci.id >= ConsumableItem.NUM_OF_POTIONS)
                continue;
            // 있어도 못 가면 없는 것이다. 아이템 칸 자체는 길에서 막아 뒀으니 옆칸으로 본다.
            if (Reachable(Cell(map, ci.transform.position)) == false)
                continue;
            potionHere = true;
            break;
        }
        // 피가 바닥인데 이 층에 마실 게 없으면 아래층으로 되돌아간다.
        // 아래층은 이미 비웠으니 남은 포션을 줍고 다시 올라온다 — 사람이 하는 그대로다.
        // 1층에는 내려갈 곳이 없다. 그런데도 아래 계단 오브젝트가 남아 있어서
        // 그걸 밀다 멈췄다 — 게다가 SearchPortal 이 실패하면 엔딩 연출이 튄다.
        bool hasDown = g.PlayerData.CurStageid > 0;
        if (hasDown)
        {
            hasDown = false;
            foreach (PortalController p in map.GetComponentsInChildren<PortalController>(false))
            {
                if (p._portalType == PortalController.Type.DownStairs)
                {
                    hasDown = true;
                    break;
                }
            }
        }
        // 내려갈 곳도 없고 마실 것도 없으면 물러설 데가 없다. 그때는 그냥 싸운다.
        bool retreat = hpRatio < RetreatHpRatio && potionHere == false
                       && hasDown && _retreats < 3 && _totalRetreats < 8;

        // 채울 방법이 남아 있는데 피가 모자라면 위로 가지 않는다.
        // 지금 당장 할 수 있는 회복이 있을 때만 참는다. 없으면 그냥 올라간다.
        bool canHeal = potionHere || retreat;
        bool holdBack = hpRatio < AscendHpRatio && canHeal;

        // 우선순위가 낮은 숫자일수록 먼저. 같은 우선순위면 (가중치, 거리) 순.
        foreach (ConsumableItem item in map.GetComponentsInChildren<ConsumableItem>(false))
        {
            if (item.id < ConsumableItem.NUM_OF_KEYS)
            {
                Bump(map, item.transform, PriKey, 0, ref best, ref bump, ref hasBump, ref bestScore);
                continue;
            }
            if (item.id >= ConsumableItem.NUM_OF_POTIONS)
            {
                Bump(map, item.transform, PriRune, 0, ref best, ref bump, ref hasBump, ref bestScore);
                continue;
            }

            // 포션. 가득 찼는데 줍는 건 낭비고, 위험할 때는 몬스터보다 먼저다.
            // 단, 층을 다 비웠으면 남은 건 전부 마시고 올라간다 —
            // 포션은 층에 배치된 예산이라 다음 층으로 가져갈 수 없고,
            // 반피로 올라가면 그 층에서 죽는다.
            if (hpRatio > PotionHpRatio && cleared == false)
                continue;
            // 층을 다 비웠으면 피가 가득해도 남은 것은 줍는다.
            // 포션은 층에 배정된 예산이라 다음 층으로 못 가져가고,
            // 무엇보다 2층 보스 통로(11,-8)처럼 한 칸 폭 길을 막고 있을 수 있다.
            if (hpRatio >= 0.999f && cleared == false)
                continue;
            Data.ConsumableItemData cd;
            long heal = Managers.Data.ConsumableItemDic.TryGetValue(item.id, out cd)
                ? (long)cd.Heal : 0;
            // 작은 포션부터 쓴다 — 큰 것을 넘치게 마시면 뒤가 없다.
            Bump(map, item.transform, hpRatio < PotionEmergency ? PriHeal : PriTopUp,
                 heal, ref best, ref bump, ref hasBump, ref bestScore);
        }

        foreach (MonsterController mc in map.GetComponentsInChildren<MonsterController>(false))
        {
            // 약한 놈부터. 순서를 잘못 잡으면 레벨이 못 따라와서 죽는 게 이 게임의 설계다.
            // 기준은 밸런스 시뮬레이터(simulate_handmade)와 같은 MaxHP * 공격력.
            if (retreat)
                continue;   // 이 몸으로 더 싸우면 죽는다

            Data.MonsterData md;
            if (Managers.Data.MonsterDic.TryGetValue(mc.id, out md) == false)
                continue;   // 모르는 놈에게는 덤비지 않는다

            // 붙기 전에 결과를 계산한다. 지거나 위험하면 지금은 건너뛴다 —
            // 다른 몬스터를 먼저 잡아 레벨을 올리면 그때는 이긴다. 그게 이 게임의 순서다.
            float loss = PredictLoss(md, g);
            if (loss == float.MaxValue)
                continue;   // 지는 싸움

            if (g.PlayerData.CurHP - loss < g.PlayerData.MaxHP * SafeHpAfterFight)
            {
                // 이기긴 하는데 위험하다. 다른 수가 하나도 없을 때만 쓴다.
                _risky.Add(new KeyValuePair<Transform, float>(mc.transform, loss));
                continue;
            }

            Bump(map, mc.transform, PriMonster, (long)Mathf.Max(0f, loss),
                 ref best, ref bump, ref hasBump, ref bestScore);
        }

        foreach (Equip eq in map.GetComponentsInChildren<Equip>(false))
            Bump(map, eq.transform, PriEquip, 0, ref best, ref bump, ref hasBump, ref bestScore);

        foreach (Lever lever in map.GetComponentsInChildren<Lever>(false))
            Bump(map, lever.transform, PriLever, 0, ref best, ref bump, ref hasBump, ref bestScore);

        foreach (PortalController portal in map.GetComponentsInChildren<PortalController>(false))
        {
            if (portal._portalType == PortalController.Type.DownStairs)
            {
                if (retreat && hasDown)
                    Bump(map, portal.transform, PriHeal, 0, ref best, ref bump, ref hasBump, ref bestScore);
                continue;
            }
            if (portal._portalType == PortalController.Type.UpStairs && cleared == false)
                continue;
            if (holdBack)
                continue;   // 몸부터 추스르고 올라간다

            if (portal._portalType == PortalController.Type.Boss)
            {
                Vector2Int bc = Cell(map, portal.transform.position);
                var info = new System.Text.StringBuilder();
                info.Append($"보스입구{bc} 아래칸도달={_dist.ContainsKey(bc + new Vector2Int(0, -1))}");
                info.Append($" 열쇠={g.KeyInventory._keys[0]}/{g.KeyInventory._keys[1]}/{g.KeyInventory._keys[2]}");
                foreach (Door dr in map.GetComponentsInChildren<Door>(false))
                    info.Append($" 문[key{dr._keyIndex}@{Cell(map, dr.transform.position)}]");
                foreach (Lever lv in map.GetComponentsInChildren<Lever>(false))
                    info.Append($" 레버@{Cell(map, lv.transform.position)}=닿음{Reachable(Cell(map, lv.transform.position))}");
                foreach (ConsumableItem ci in map.GetComponentsInChildren<ConsumableItem>(false))
                    info.Append($" 아이템{ci.id}@{Cell(map, ci.transform.position)}");
                _bossInfo = info.ToString();

                // 보스방 입구(2층 (11,-3))는 BossDoor 레이어라 "아래에서 위로" 밀어야
                // 확인 팝업이 뜬다. 옆에서 밀면 아무 일도 안 일어나고,
                // 일반 계단과 같은 순위로 두면 가까운 계단만 타며 위아래를 오간다.
                BumpFrom(Cell(map, portal.transform.position), new Vector2Int(0, -1),
                         PriTrigger, 0, ref best, ref bump, ref hasBump, ref bestScore);
                continue;
            }
            Bump(map, portal.transform, PriStairs, 0, ref best, ref bump, ref hasBump, ref bestScore);
        }

        // 보스방 문 / 상호작용 오브젝트는 "아래에서" 밀어야 반응한다.
        // 보스 등장 트리거는 그냥 밟으면 된다 (킹슬라임이 이렇게 나온다).
        foreach (Collider col in map.GetComponentsInChildren<Collider>(false))
        {
            int layer = col.gameObject.layer;
            Vector2Int c = Cell(map, col.transform.position);

            // 상호작용 오브젝트는 막지 않는다 — 3층의 마검 계약(+10 ATK)이 여기 있고,
            // 그걸 못 받으면 킹슬라임을 이길 수가 없다. 피가 적다고 미룰 대상이 아니다.
            if (layer == (int)Define.Layer.BossDoor || layer == (int)Define.Layer.InteractObjects)
            {
                // 이것들은 "아래에서 위로" 밀어야만 반응한다 (PlayerController.GetTouchDirection).
                // 아무 옆면이나 잡으면 밀어도 아무 일이 없다.
                BumpFrom(c, new Vector2Int(0, -1), PriTrigger, 1,
                         ref best, ref bump, ref hasBump, ref bestScore);
            }
            else if (layer == (int)Define.Layer.BossEventTrigger)
                ConsiderCell(c, PriTrigger, 0, ref best, ref bump, ref hasBump, ref bestScore);
        }

        // 지금 서 있는 층을 그대로 찍는다. 예전에는 보스 포탈이 있는 층(2층)만
        // 찍어서, 3층에서 무엇이 막혔는지는 로그만 봐서는 알 수가 없었다.
        {
            var info = new System.Text.StringBuilder();
            // 열쇠 목록은 InitKeyInventory 전에는 비어 있다. 그냥 인덱싱하면
            // 매 프레임 예외가 나서 계획도 워치독 갱신도 통째로 날아간다.
            info.Append("열쇠=");
            for (int k = 0; k < 3; k++)
                info.Append(k > 0 ? "/" : "")
                    .Append(g.KeyInventory != null && g.KeyInventory._keys != null
                            && g.KeyInventory._keys.Count > k ? g.KeyInventory._keys[k].ToString() : "-");
            foreach (ConsumableItem ci in map.GetComponentsInChildren<ConsumableItem>(false))
            {
                Vector2Int c = Cell(map, ci.transform.position);
                info.Append($" 아이템{ci.id}@{c}={(Reachable(c) ? "닿음" : "막힘")}");
            }
            foreach (MonsterController mc in map.GetComponentsInChildren<MonsterController>(false))
            {
                Vector2Int c = Cell(map, mc.transform.position);
                // 보스가 왜 안 잡히는지 보려면 높이와 실제 콜라이더 유무가 필요하다.
                bool solidHere = Physics.CheckBox(CellCenter(c), ProbeHalf, Quaternion.identity,
                                                  1 << (int)Define.Layer.Monster,
                                                  QueryTriggerInteraction.Collide);
                info.Append($" 몹{mc.id}@{c}={(Reachable(c) ? "닿음" : "막힘")}" +
                            $"/y{mc.transform.position.y:0.00}/콜라이더{(solidHere ? "있음" : "없음")}");

                // 물리에 안 잡히는 몬스터는 콜라이더 상태를 그대로 펼쳐 본다.
                // 보스가 여기 걸려서 전투가 안 열렸다.
                if (solidHere == false)
                {
                    Collider[] cols = mc.GetComponentsInChildren<Collider>(true);
                    info.Append($"[{mc.gameObject.name} 활성{mc.gameObject.activeInHierarchy} 콜{cols.Length}");
                    for (int k = 0; k < cols.Length && k < 3; k++)
                        info.Append($" ({cols[k].gameObject.name} L{cols[k].gameObject.layer}" +
                                    $" {(cols[k].enabled ? "on" : "off")}" +
                                    $" {(cols[k].isTrigger ? "trig" : "solid")}" +
                                    $" y{cols[k].bounds.min.y:0.0}~{cols[k].bounds.max.y:0.0}" +
                                    $" x{cols[k].bounds.center.x:0.0}z{cols[k].bounds.center.z:0.0})");
                    info.Append($" 탐침{CellCenter(c).x:0.0}/{_probeY:0.00}/{CellCenter(c).z:0.0}]");
                }
            }
            foreach (Door dr in map.GetComponentsInChildren<Door>(false))
                info.Append($" 문key{dr._keyIndex}@{Cell(map, dr.transform.position)}");
            foreach (Lever lv in map.GetComponentsInChildren<Lever>(true))
                info.Append($" 레버@{Cell(map, lv.transform.position)}=" +
                            $"{(Reachable(Cell(map, lv.transform.position)) ? "닿음" : "막힘")}" +
                            $"{(lv._IsActive ? "(당김)" : "")}");
            // 보스방으로 가는 유일한 길이 이 기둥이다. 열렸는지 눈에 보이게 찍는다.
            foreach (Pillar pl in map.GetComponentsInChildren<Pillar>(true))
            {
                Vector2Int c = Cell(map, pl.transform.position);
                bool open;
                info.Append($" 기둥@{c}=" +
                            $"{(_dist.ContainsKey(c) ? "뚫림" : "막힘")}" +
                            $"/저장{(Managers.Data.PillarActiveDic.TryGetValue(pl._pillarIndex_forActive, out open) ? (open ? "닫힘" : "열림") : "없음")}");
            }
            foreach (Collider col in map.GetComponentsInChildren<Collider>(false))
            {
                if (col.gameObject.layer != (int)Define.Layer.InteractObjects
                    && col.gameObject.layer != (int)Define.Layer.BossDoor)
                    continue;
                Vector2Int c = Cell(map, col.transform.position);
                info.Append($" 밀기@{c}={(_dist.ContainsKey(c + new Vector2Int(0, -1)) ? "아래칸OK" : "아래칸막힘")}");
                if (_firedTriggers.Contains(c))
                    info.Append("(민적있음)");
            }
            _bossInfo = info.ToString();
        }

        // 안전한 상대가 없었다면, 이기기는 하는 싸움 중 제일 싼 것을 고른다.
        if (bestScore == long.MaxValue)
        {
            foreach (KeyValuePair<Transform, float> risky in _risky)
                Bump(map, risky.Key, PriMonster, (long)Mathf.Max(0f, risky.Value),
                     ref best, ref bump, ref hasBump, ref bestScore);
        }

        // 이 층에서 할 일이 다 떨어졌고 올라갈 데도 없으면 아래층으로 되돌아간다.
        // 3층(00_002)은 위층 계단이 아예 없고, 진행 경로는 2층의 보스문이다.
        if (bestScore == long.MaxValue && hasDown)
        {
            foreach (PortalController portal in map.GetComponentsInChildren<PortalController>(false))
            {
                if (portal._portalType != PortalController.Type.DownStairs)
                    continue;
                Bump(map, portal.transform, PriStairs, 0, ref best, ref bump, ref hasBump, ref bestScore);
            }
        }

        // 아무 목표도 없으면 아직 안 밟아 본 칸으로 간다.
        // 층에 따라서는 밟아야만 열리는 곳이 있다 (3층 킹슬라임).
        if (bestScore == long.MaxValue)
        {
            foreach (KeyValuePair<Vector2Int, int> pair in _dist)
            {
                if (pair.Value == 0 || _visited.Contains(pair.Key))
                    continue;
                ConsiderCell(pair.Key, PriExplore, 0, ref best, ref bump, ref hasBump, ref bestScore);
            }
        }

        // 갈 곳이 다 떨어졌다면 옆의 무언가를 밀어 본다.
        // 보스문/기둥/레버처럼 "부딪혀야" 열리는 것들이 있고, 방향도 가려 받는다.
        if (bestScore == long.MaxValue)
        {
            Define.MoveDir push = TryPush(start);
            if (push != Define.MoveDir.None)
                return push;

            // 볼 게 하나도 안 남았는데 포기 목록이 차 있다면, 그 목록이 교착의 원인이다.
            // 4층 보스는 연출 중에 한 칸 옮겨 간다 — 봇은 옛 자리를 열두 번 밀고
            // 포기 목록에 넣었고, 그 뒤로 보스를 영영 안 봤다.
            // 한 번 지우고 다시 본다. 그래도 안 되면 워치독이 잡는다.
            if (_deadTargets.Count > 0)
            {
                Debug.Log($"[AutoPlayer] 볼 게 없어 포기 목록 {_deadTargets.Count}개를 지운다");
                _deadTargets.Clear();
                _sameBump = 0;
                _plan = "포기 목록을 지우고 다시 본다";
                return Define.MoveDir.None;
            }

            _plan = $"목표없음 flood={_dist.Count} 방문={_visited.Count}";
            return Define.MoveDir.None;
        }

        // 트리거(마검/보스문/보스 등장)는 한 번만 밀면 된다.
        // 계속 밀면 이미 끝난 연출이 다시 켜져서 대화창만 무한히 열린다.
        // 단, 실제로 미는 순간에만 센다 — 열한 칸 떨어진 채로 계획만 세웠는데
        // 껐다가는 도착도 하기 전에 목표가 사라진다.
        if (hasBump && best == start && (int)(bestScore / 1000000000000L) == PriTrigger)
            _firedTriggers.Add(bump);

        // 목표 옆에 이미 서 있으면 그대로 부딪힌다.
        Define.MoveDir dir = (hasBump && best == start)
            ? ToDir(bump - start)
            : FirstStep(start, best);

        // 같은 목표를 계속 잡고 있는 동안만 센다.
        // 예전에는 "지금 밀고 있지 않으면" 리셋해서, 한 칸 물러섰다 미는 것을
        // 반복하면 접근 프레임마다 0 이 됐다 — 4층에서 (12,-17) 을 영원히 밀었다.
        if (hasBump && bump == _lastBump)
        {
            if (best == start && ++_sameBump > 12)
            {
                _deadTargets.Add(bump);   // 열두 번 밀어도 안 되면 그건 안 되는 것이다
                _sameBump = 0;
                Debug.Log($"[AutoPlayer] {bump} 을 열두 번 밀어도 반응이 없어 접는다");
            }
        }
        else
        {
            _sameBump = 0;
        }
        if (hasBump)
            _lastBump = bump;
        _plan = $"{start}->{best}{(hasBump ? $"+{bump}" : "")} d={_dist[best]} {dir} flood={_dist.Count}";
        return dir;
    }

    /// <summary>
    /// 이웃 칸에 부딪혀 본다. 밀어야 반응하는 것들(보스문, 기둥, 레버, 보스 등장 트리거)이
    /// 있고, 어느 쪽에서 미느냐도 따진다 — 그래서 네 방향을 돌아가며 시도한다.
    /// </summary>
    Define.MoveDir TryPush(Vector2Int start)
    {
        if (Time.time < _nextPush)
            return Define.MoveDir.None;

        for (int i = 0; i < Dirs.Length; i++)
        {
            _pushDir = (_pushDir + 1) % Dirs.Length;
            Vector2Int c = start + Dirs[_pushDir];
            if (Physics.CheckBox(CellCenter(c), ProbeHalf, Quaternion.identity,
                                 PushMask, QueryTriggerInteraction.Collide) == false)
                continue;

            _nextPush = Time.time + 1.5f;
            _plan = $"{start} -> {c} 밀기";
            return ToDir(Dirs[_pushDir]);
        }
        return Define.MoveDir.None;
    }

    static Define.MoveDir ToDir(Vector2Int step)
    {
        if (step.y > 0) return Define.MoveDir.Up;
        if (step.y < 0) return Define.MoveDir.Down;
        if (step.x < 0) return Define.MoveDir.Left;
        if (step.x > 0) return Define.MoveDir.Right;
        return Define.MoveDir.None;
    }

    #region 전투 예측
    /// <summary>
    /// 이 몬스터와 붙으면 HP 를 얼마나 잃는지 미리 계산한다.
    /// CreatureClass.DefaultTrait + UI_BaseCard/UI_MonsterCard 의 쿨타임 규칙을 그대로 옮긴 것이라
    /// 실제 결과와 거의 같다. 못 이기면 float.MaxValue.
    ///
    /// 이 게임의 설계가 "순서"인 이상, 이길 수 있는 싸움을 고르는 것이 곧 플레이다.
    /// </summary>
    float PredictLoss(Data.MonsterData md, GameManager g)
    {
        Data.StageInfoData info;
        float atkScale = 1f, defScale = 1f;
        if (Managers.Data.StageInfoDic.TryGetValue(g.PlayerData.CurStageid, out info))
        {
            atkScale = info.ATK;
            defScale = info.DEF;
        }

        float pHp = g.PlayerData.CurHP;
        float pAtk = g.PlayerData.Attack;
        float pDef = g.PlayerData.Defence;
        float pCd = 3f / Mathf.Max(0.01f, g.PlayerData.AttackSpeed);
        float pDefCd = 3f / Mathf.Max(0.01f, g.PlayerData.DefenceSpeed);
        int pCrit = Mathf.RoundToInt(g.PlayerData.Critical);
        float pCritAtk = g.PlayerData.CriticalAttack;

        float mHp = md.MaxHP;
        float mAtk = md.Attack * atkScale;
        float mDef = md.Defence * defScale;
        float mCd = 3f / Mathf.Max(0.01f, md.AttackSpeed);
        float mDefCd = 3f / Mathf.Max(0.01f, md.DefenceSpeed);
        int mCrit = Mathf.RoundToInt(md.Critical);
        float mCritAtk = md.CriticalAttack;

        float pT = 0f, mT = 0f, pDefT = 0f, mDefT = 0f;
        bool pShield = false, mShield = false;
        int pCount = 0, mCount = 0;
        float start = pHp;

        const float dt = 0.02f;
        for (float t = 0f; t < 600f; t += dt)
        {
            if (pT >= pCd)
            {
                pT = 0f;
                pCount++;
                bool crit = pCrit > 0 && pCount >= pCrit;
                if (crit) pCount = 0;
                mHp -= Damage(pAtk, pCritAtk, crit, mDef, mShield);
                if (mShield) { mShield = false; mDefT = 0f; }
                if (mHp <= 0f)
                    return start - pHp;
            }
            if (mT >= mCd)
            {
                mT = 0f;
                mCount++;
                bool crit = mCrit > 0 && mCount >= mCrit;
                if (crit) mCount = 0;
                pHp -= Damage(mAtk, mCritAtk, crit, pDef, pShield);
                if (pShield) { pShield = false; pDefT = 0f; }
                if (pHp <= 0f)
                    return float.MaxValue;   // 진다
            }

            if (pDefT >= pDefCd) pShield = true;
            if (mDefT >= mDefCd) mShield = true;

            pT += dt; mT += dt; pDefT += dt; mDefT += dt;
        }
        return float.MaxValue;   // 안 끝나는 싸움도 하면 안 된다
    }

    static int Damage(float atk, float critAtk, bool crit, float def, bool shield)
    {
        float num = (int)Mathf.Max(0f, atk);
        if (crit) num = num * (critAtk / 100f);
        int damage = Mathf.RoundToInt(num);
        damage -= (int)def;
        damage = (int)Mathf.Max(1, damage);
        if (shield && crit) damage = (int)(damage * 0.25f);
        else if (shield) damage = 1;
        return damage;
    }
    #endregion

    /// <summary>
    /// 지금 서 있는 맵을 찾는다.
    ///
    /// 보통은 CurStageid 의 맵이지만, 보스방으로 넘어간 직후에는 플레이어가
    /// 그 맵이 아닌 다른 맵 위에 서 있다 (4층에서 (-614,-5) 가 나왔다).
    /// 맵들은 서로 100 유닛 넘게 떨어져 놓이고 한 장은 7 유닛 남짓이라,
    /// 가장 가까운 맵이 곧 지금 서 있는 맵이다.
    /// </summary>
    GameObject ResolveMap(GameManager g)
    {
        if (g.Maps == null)
            return null;

        GameObject byStage;
        g.Maps.TryGetValue(g.PlayerData.CurStageid, out byStage);

        Vector3 p = g.Player.transform.position;

        // 어느 맵의 벽 안에 서 있는지로 정한다. 위치가 가깝다는 것만으로는
        // 틀린다 — 11층에서 엉뚱한 맵을 잡아 걸을 수 있는 칸이 27개뿐이었고,
        // 문 좌표가 그 층 격자에 있지도 않은 자리로 찍혔다.
        Bounds b;
        if (byStage != null && TryWallBounds(byStage, out b) && InsideXZ(b, p))
            return byStage;

        foreach (KeyValuePair<int, GameObject> pair in g.Maps)
        {
            if (pair.Value == null)
                continue;
            if (TryWallBounds(pair.Value, out b) && InsideXZ(b, p))
                return pair.Value;
        }

        // 벽을 아직 못 잰 순간(층이 조립되는 중)에는 가장 가까운 맵으로 둔다.
        GameObject best = null;
        float bestDist = float.MaxValue;
        foreach (KeyValuePair<int, GameObject> pair in g.Maps)
        {
            if (pair.Value == null)
                continue;
            float d = (pair.Value.transform.position - p).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = pair.Value;
            }
        }
        return best != null ? best : byStage;
    }

    /// <summary>이 층의 물건 중 하나라도 닿는가. 계단은 어느 층에나 있다.</summary>
    bool AnythingReachable(GameObject map)
    {
        foreach (PortalController p in map.GetComponentsInChildren<PortalController>(false))
            if (Reachable(Cell(map, p.transform.position)))
                return true;
        foreach (MonsterController m in map.GetComponentsInChildren<MonsterController>(false))
            if (Reachable(Cell(map, m.transform.position)))
                return true;
        foreach (ConsumableItem c in map.GetComponentsInChildren<ConsumableItem>(false))
            if (Reachable(Cell(map, c.transform.position)))
                return true;
        return false;
    }

    /// <summary>목표 칸 옆에 설 수 있는가.</summary>
    bool Reachable(Vector2Int cell)
    {
        for (int i = 0; i < Dirs.Length; i++)
            if (_dist.ContainsKey(cell + Dirs[i]))
                return true;
        return false;
    }

    /// <summary>목표 옆에 서서 부딪히는 자리를 찾는다.</summary>
    void Bump(GameObject map, Transform t, int priority, long weight,
              ref Vector2Int best, ref Vector2Int bump, ref bool hasBump, ref long bestScore)
    {
        BumpCell(Cell(map, t.position), priority, weight, ref best, ref bump, ref hasBump, ref bestScore);
    }

    /// <summary>정해진 방향에서만 밀어야 하는 목표 (보스문, 기둥 등).</summary>
    void BumpFrom(Vector2Int cell, Vector2Int side, int priority, long weight,
                  ref Vector2Int best, ref Vector2Int bump, ref bool hasBump, ref long bestScore)
    {
        if (_deadTargets.Contains(cell))
            return;

        Vector2Int stand = cell + side;
        int d;
        if (_dist.TryGetValue(stand, out d) == false)
        {
            // 길 트는 것부터는 이미 밀어 본 목표라도 한다.
            // 설 자리가 막혀 있었다면 애초에 제대로 밀린 적이 없다는 뜻이다.
            // 서야 할 칸에 아이템이 놓여 있으면 길이 막힌 것으로 잡힌다.
            // 2층 보스문 아래 칸(11,-4)에 포션이 있어서 보스방에 영영 못 들어갔다.
            // 그럴 때는 그 아이템을 먼저 주워 길을 튼다.
            if (Physics.CheckBox(CellCenter(stand), ProbeHalf, Quaternion.identity,
                                 ItemMask, QueryTriggerInteraction.Collide))
                BumpCell(stand, priority, weight, ref best, ref bump, ref hasBump, ref bestScore);
            return;
        }

        if (priority == PriTrigger && _firedTriggers.Contains(cell))
            return;

        long score = Score(priority, weight, d);
        if (score >= bestScore)
            return;
        bestScore = score;
        best = stand;
        bump = cell;
        hasBump = true;
    }

    void BumpCell(Vector2Int cell, int priority, long weight,
                  ref Vector2Int best, ref Vector2Int bump, ref bool hasBump, ref long bestScore)
    {
        // 벽 너머에 있는 목표는 아무리 밀어도 안 열린다.
        // (1층의 도달 불가 장비를 향해 벽을 계속 밀며 멈춰 있었다.)
        if (_deadTargets.Contains(cell))
            return;
        if (Physics.CheckBox(CellCenter(cell), ProbeHalf, Quaternion.identity,
                             WallMask, QueryTriggerInteraction.Collide))
            return;

        for (int i = 0; i < Dirs.Length; i++)
        {
            Vector2Int side = cell + Dirs[i];
            int d;
            if (_dist.TryGetValue(side, out d) == false)
                continue;

            long score = Score(priority, weight, d);
            if (score >= bestScore)
                continue;
            bestScore = score;
            best = side;
            bump = cell;
            hasBump = true;
        }
    }

    /// <summary>그 칸 자체로 걸어 들어가는 목표 (탐색, 보스 등장 트리거).</summary>
    void ConsiderCell(Vector2Int cell, int priority, long weight,
                      ref Vector2Int best, ref Vector2Int bump, ref bool hasBump, ref long bestScore)
    {
        if (_deadTargets.Contains(cell))
            return;   // 밟아도 아무 일이 없던(또는 연출이 죽은) 자리
        if (priority == PriTrigger && _firedTriggers.Contains(cell))
            return;

        int d;
        if (_dist.TryGetValue(cell, out d) == false)
            return;
        if (d == 0)
            return;   // 서 있는 칸을 목표로 잡으면 낼 수 있는 수가 없다

        long score = Score(priority, weight, d);
        if (score >= bestScore)
            return;
        bestScore = score;
        best = cell;
        bump = cell;
        hasBump = false;
    }

    /// <summary>(우선순위, 가중치, 거리) 사전순 하나의 수로.</summary>
    static long Score(int priority, long weight, int d)
    {
        return (long)priority * 1000000000000L
             + Math.Min(weight, 999999999L) * 1000L
             + Math.Min(d, 999);
    }

    /// <summary>목표까지의 경로를 되짚어 첫 한 칸의 방향을 낸다.</summary>
    Define.MoveDir FirstStep(Vector2Int start, Vector2Int goal)
    {
        Vector2Int cur = goal;
        while (true)
        {
            Vector2Int prev;
            if (_from.TryGetValue(cur, out prev) == false)
                return Define.MoveDir.None;   // 시작점이거나 경로가 끊겼다
            if (prev == start)
                break;
            cur = prev;
        }

        Vector2Int step = cur - start;
        if (step.y > 0) return Define.MoveDir.Up;
        if (step.y < 0) return Define.MoveDir.Down;
        if (step.x < 0) return Define.MoveDir.Left;
        if (step.x > 0) return Define.MoveDir.Right;
        return Define.MoveDir.None;
    }

    /// <summary>탐색 상자의 반지름. 층이 23x27 이라 32 면 어디서 재도 전부 들어온다.</summary>
    const int FloodRadius = 32;

    GameObject _boundsMap;
    Bounds _mapBounds;
    bool _hasBounds;

    /// <summary>
    /// 이 층이 실제로 차지하는 범위. 콜라이더를 전부 감싸서 잰다.
    /// 층마다 한 번만 계산한다 — 맵 오브젝트가 바뀔 때까지 그대로다.
    /// </summary>
    readonly Dictionary<GameObject, Bounds> _wallBounds = new Dictionary<GameObject, Bounds>();

    /// <summary>
    /// 이 맵이 벽으로 두르는 범위. 맵마다 한 번만 재서 들고 있는다.
    /// 벽만 센다 — 콜라이더를 전부 감싸면 연출용 트리거나 카메라 영역까지 들어와서
    /// 상자가 던전보다 훨씬 커지고, 맵 밖 (24,-23) 에 서 있어도 안으로 쳤다.
    /// </summary>
    bool TryWallBounds(GameObject map, out Bounds bounds)
    {
        if (_wallBounds.TryGetValue(map, out bounds))
            return true;

        bool found = false;
        Bounds acc = new Bounds();
        foreach (Collider c in map.GetComponentsInChildren<Collider>(false))
        {
            if (c.gameObject.layer != (int)Define.Layer.Wall)
                continue;
            if (found == false)
            {
                acc = c.bounds;
                found = true;
            }
            else
            {
                acc.Encapsulate(c.bounds);
            }
        }

        if (found == false)
            return false;   // 아직 조립 중이다. 다음 프레임에 다시 잰다.

        _wallBounds[map] = acc;
        bounds = acc;
        return true;
    }

    static bool InsideXZ(Bounds b, Vector3 world)
    {
        return world.x >= b.min.x && world.x <= b.max.x
               && world.z >= b.min.z && world.z <= b.max.z;
    }

    void UpdateBounds(GameObject map)
    {
        if (ReferenceEquals(_boundsMap, map) && _hasBounds)
            return;

        _boundsMap = map;
        _hasBounds = TryWallBounds(map, out _mapBounds);
    }

    /// <summary>탐색이 볼 수 있는 범위. 벽 칸 자체는 안에 들어야 한다.</summary>
    bool InsideMap(Vector3 world)
    {
        return Within(world, Tile);
    }

    /// <summary>
    /// 플레이어가 정말 던전 안에 서 있는가.
    /// 바깥 테두리는 벽이니, 제대로 배치됐다면 벽보다 한 칸은 안쪽에 있다.
    /// 탐색 범위와 같은 여유를 주면 딱 한 칸 밖 (-1,-23) 으로 새어 나갔다.
    /// </summary>
    bool InsidePlayArea(Vector3 world)
    {
        return Within(world, -Tile);
    }

    bool Within(Vector3 world, float margin)
    {
        if (_hasBounds == false)
            return true;
        return world.x >= _mapBounds.min.x - margin && world.x <= _mapBounds.max.x + margin
               && world.z >= _mapBounds.min.z - margin && world.z <= _mapBounds.max.z + margin;
    }

    void Flood(Vector2Int start)
    {
        _from.Clear();
        _dist.Clear();
        _queue.Clear();

        _dist[start] = 0;
        _queue.Enqueue(start);

        while (_queue.Count > 0)
        {
            Vector2Int cur = _queue.Dequeue();
            int d = _dist[cur];

            for (int i = 0; i < Dirs.Length; i++)
            {
                Vector2Int next = cur + Dirs[i];

                // 층 하나는 아무리 커도 23x27 이다. 어느 칸에서 재도 반대쪽 끝까지
                // Radius 안에 들어온다. 이 상자를 벽으로 쳐서 밖으로 새는 것을 막는다.
                // 3층 (17,-9) 처럼 경계가 뚫린 자리가 있고, 예전에는 거기서
                // 3200만 칸까지 퍼져 한 프레임이 몇 분씩 걸렸다.
                if (Mathf.Abs(next.x - start.x) > FloodRadius
                    || Mathf.Abs(next.y - start.y) > FloodRadius)
                    continue;

                // 층이 실제로 차지하는 범위 밖은 벽으로 친다.
                // 3층 (17,-9) 처럼 경계가 뚫린 자리가 있다.
                if (InsideMap(CellCenter(next)) == false)
                    continue;

                if (_dist.ContainsKey(next) || Solid(next))
                    continue;
                _dist[next] = d + 1;
                _from[next] = cur;
                _queue.Enqueue(next);
            }
        }
    }

    /// <summary>
    /// 그 칸을 지나갈 수 없는가. 콜라이더를 직접 재 본다 —
    /// 손수 만든 층은 벽 하나가 여러 칸을 덮기도 해서 오브젝트 위치로 세면 구멍이 난다.
    ///
    /// 열쇠가 없는 문도 벽으로 친다. 이 규칙이 방 순서 = 전투 순서를 강제한다.
    /// </summary>
    bool Solid(Vector2Int cell)
    {
        bool cached;
        if (_solid.TryGetValue(cell, out cached))
            return cached;

        Vector3 world = CellCenter(cell);
        Vector3 half = ProbeHalf;

        bool blocked = Physics.CheckBox(world, half, Quaternion.identity, BlockMask,
                                        QueryTriggerInteraction.Collide);

        if (blocked == false && Physics.CheckBox(world, half, Quaternion.identity, DoorMask,
                                                 QueryTriggerInteraction.Collide))
        {
            blocked = HasKeyFor(world, half) == false;
        }

        _solid[cell] = blocked;
        return blocked;
    }

    static readonly Collider[] _hits = new Collider[8];

    bool HasKeyFor(Vector3 world, Vector3 half)
    {
        List<int> keys = Managers.Game.KeyInventory != null ? Managers.Game.KeyInventory._keys : null;
        if (keys == null)
            return false;

        int n = Physics.OverlapBoxNonAlloc(world, half, _hits, Quaternion.identity, DoorMask,
                                           QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            Door door = _hits[i].GetComponentInChildren<Door>();
            if (door == null)
                door = _hits[i].GetComponentInParent<Door>();
            if (door == null)
                continue;
            int idx = door._keyIndex;
            return idx >= 0 && idx < keys.Count && keys[idx] > 0;
        }
        return false;
    }

    Vector3 CellCenter(Vector2Int cell)
    {
        Vector3 world = _map.transform.TransformPoint(new Vector3(cell.x * Tile, 0f, cell.y * Tile));
        world.y = _probeY;
        return world;
    }

    static readonly Vector3 ProbeHalf = new Vector3(Tile * 0.3f, Tile * 0.3f, Tile * 0.3f);

    static Vector2Int Cell(GameObject map, Vector3 world)
    {
        Vector3 local = map.transform.InverseTransformPoint(world);
        return new Vector2Int(Mathf.RoundToInt(local.x / Tile), Mathf.RoundToInt(local.z / Tile));
    }
    #endregion

    #region 진행 감시
    void Progress(string key)
    {
        if (key == _progressKey)
            return;
        _progressKey = key;
        _lastProgress = Time.unscaledTime;
    }

    /// <summary>최고 도달 층이 이만큼 안 오르면 맴돌고 있는 것이다.</summary>
    const float NoNewFloorTimeout = 600f;
    int _maxStage = -1;
    float _maxStageAt;
    float _waitingPlayerSince;

    /// <summary>이 층에서 한 발이라도 뗐는가. 던전 안이라는 증거로 쓴다.</summary>
    bool _movedOnFloor;

    /// <summary>이번 프레임에 일부러 손을 놓고 게임을 기다렸는가.</summary>
    bool _waitingForGame;

    void TickStall()
    {
        // 층이 자리 잡기를 기다리는 중이면 그것도 "진행 없음"이 아니다.
        // 21층(챕터가 바뀌는 층)으로 넘어가는 순간 90초 워치독에 걸렸다.
        // 다만 마냥 기다리지는 않는다 — 층 워치독(10분)이 진짜 교착을 잡는다.
        if (_waitingForGame)
        {
            _lastProgress = Time.unscaledTime;
            return;
        }

        // 플레이어가 생기기 전에는 봇이 할 수 있는 게 없다. "진행 없음"이 아니다.
        // (인트로가 길거나 맵 데이터를 새로 만드는 중이면 몇 분씩 걸린다.)
        if (Managers.Game == null || Managers.Game.Player == null)
        {
            _lastProgress = Time.unscaledTime;
            if (_waitingPlayerSince == 0f)
                _waitingPlayerSince = Time.unscaledTime;
            else if (Time.unscaledTime - _waitingPlayerSince > 600f)
                Fail("플레이어가 10분 동안 안 생긴다 — 인트로에서 멈춘 것으로 본다");
            return;
        }
        _waitingPlayerSince = 0f;

        // 층과 층 사이를 계속 오가면 "진전"으로 세어져 90초 워치독이 영영 안 돈다.
        // 실제로는 같은 두 층을 도는 중이었고, 그렇게 60분을 태웠다.
        GameManager gm = Managers.Game;
        if (gm != null && gm.PlayerData != null)
        {
            if (gm.PlayerData.CurStageid > _maxStage)
            {
                _maxStage = gm.PlayerData.CurStageid;
                _maxStageAt = Time.unscaledTime;
            }
            else if (_maxStageAt > 0f && Time.unscaledTime - _maxStageAt > NoNewFloorTimeout)
            {
                Fail($"{_maxStage + 1}층 위로 {NoNewFloorTimeout / 60f:0}분 동안 못 올라감\n" +
                     $"  plan={_plan}\n  {_bossInfo}");
                return;
            }
        }

        if (Time.unscaledTime - _lastProgress < StallTimeout)
            return;

        int stage = Managers.Game != null ? Managers.Game.PlayerData.CurStageid : -1;
        Fail($"{stage + 1}층에서 {StallTimeout:0}초 동안 진행 없음\n" +
             $"  state={_progressKey} plan={_plan}\n  {DumpMap()}");
    }

    /// <summary>막혔을 때 무엇이 남아 있고 어디까지 갈 수 있었는지.</summary>
    string DumpMap()
    {
        GameObject map;
        GameManager g = Managers.Game;
        if (g == null || g.Maps == null
            || g.Maps.TryGetValue(g.PlayerData.CurStageid, out map) == false || map == null)
            return "맵 없음";

        var sb = new System.Text.StringBuilder();
        sb.Append($"player={Cell(map, g.Player.transform.position)} ");
        sb.Append($"몬스터={map.GetComponentsInChildren<MonsterController>(false).Length} ");
        sb.Append($"아이템={map.GetComponentsInChildren<ConsumableItem>(false).Length} ");
        sb.Append($"문={map.GetComponentsInChildren<Door>(false).Length} ");
        sb.Append($"열쇠=[{string.Join(",", g.KeyInventory._keys)}]");

        // 막힌 곳 주변에 뭐가 있는지 — 대개 여기서 원인이 나온다.
        Vector2Int at = Cell(map, g.Player.transform.position);
        foreach (Vector2Int d in Dirs)
        {
            int n = Physics.OverlapBoxNonAlloc(CellCenter(at + d), ProbeHalf, _hits,
                                               Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
                sb.Append($"\n    이웃{d} {_hits[i].name} layer={_hits[i].gameObject.layer}");
        }

        foreach (MonsterController mc in map.GetComponentsInChildren<MonsterController>(false))
        {
            Vector2Int c = Cell(map, mc.transform.position);
            sb.Append($"\n    몬스터{mc.id} {c} 도달={Reachable(c)}");
        }
        foreach (PortalController p in map.GetComponentsInChildren<PortalController>(false))
        {
            Vector2Int c = Cell(map, p.transform.position);
            sb.Append($"\n    포탈{p._portalType} {c} 도달={Reachable(c)}");
        }
        return sb.ToString();
    }

    void Succeed(string message)
    {
        Result = message;
        Finished = true;
        Debug.Log($"[AutoPlayer] 완주: {message}");
    }

    void Fail(string message)
    {
        Result = message;
        Failed = true;
        Debug.LogError($"[AutoPlayer] 실패: {message}");
    }
    #endregion

    #region 리플렉션 (사람 입력으로만 열려 있는 진입점)
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static MethodInfo Method(object target, string name)
    {
        return target.GetType().GetMethod(name, Any);
    }

    static void Call(object target, string name)
    {
        MethodInfo m = Method(target, name);
        if (m != null)
            m.Invoke(target, null);
    }

    static T Field<T>(object target, string name)
    {
        FieldInfo f = target.GetType().GetField(name, Any);
        return f == null ? default(T) : (T)f.GetValue(target);
    }
    #endregion
}
