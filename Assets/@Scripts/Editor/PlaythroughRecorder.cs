using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

/// <summary>
/// 자동 플레이(AutoPlayer)를 돌리면서 Unity Recorder 로 통째로 녹화한다.
///
///   Unity.exe -projectPath . -executeMethod PlaythroughRecorder.Record
///   Unity.exe -projectPath . -executeMethod PlaythroughRecorder.DryRun   (녹화 없이 완주만 확인)
///
/// 배치모드가 아니라 GUI 로 띄워야 한다 — Recorder 의 Game View 캡처가 실제로 그려지는
/// 화면을 읽기 때문이다. -executeMethod 는 플레이모드를 켜고 바로 돌아오므로,
/// 그 다음은 EditorApplication 콜백으로 이어 붙인다(도메인 리로드를 넘겨야 해서
/// 상태는 SessionState 에 둔다).
/// </summary>
[InitializeOnLoad]
public static class PlaythroughRecorder
{
    const string StateKey = "TheSword.Playthrough";   // "record" | "dry" | ""
    const string OutputDir = "Recordings";
    const string OutputName = "TheSword_Playthrough";

    // 100층을 한 번에 담아야 하는데, 인코딩이 무거우면 그만큼 실제 시간이 늘어난다.
    // 1280x720 30fps 로는 30분에 70층 언저리에서 끊겼다. 화면이 도트 그림이라
    // 960x540 24fps 로 낮춰도 알아보는 데 지장이 없고, 처리량은 두 배 가까이 는다.
    const int Width = 960;
    const int Height = 540;
    const float Fps = 20f;

    /// <summary>이 시간이 지나면 결과와 상관없이 녹화를 끊고 나온다.
    /// 봇이 어딘가에서 맴돌아도 mp4 는 정상적으로 마무리돼야 한다.</summary>
    const double MaxMinutes = 60.0;

    /// <summary>이만큼 아무 진전이 없으면 잠긴 연출을 에디터 쪽에서 직접 푼다.</summary>
    const double StuckSeconds = 20.0;

    static RecorderController _controller;
    static double _nextLog;
    static double _startedAt;
    static double _lockedTime;
    static double _lastTick;
    static double _nextUnstickLog;

    static PlaythroughRecorder()
    {
        // 도메인 리로드 후에도 다시 붙는다.
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("TheSword/Record Full Playthrough")]
    public static void Record()
    {
        Begin("record");
    }

    [MenuItem("TheSword/Playthrough Dry Run (no recording)")]
    public static void DryRun()
    {
        Begin("dry");
    }

    static void Begin(string mode)
    {
        if (Application.isBatchMode)
        {
            // 배치모드는 Game View 가 없어서 캡처가 빈 화면이 된다. 미리 막는다.
            Debug.LogError("[Playthrough] GUI 에디터로 실행해야 한다 (-batchmode 금지).");
            EditorApplication.Exit(2);
            return;
        }

        ClearSaveData();
        SessionState.SetString(StateKey, mode);
        EditorSceneOpen();
        Debug.Log($"[Playthrough] 시작 ({mode}) — 플레이모드 진입");
        EditorApplication.EnterPlaymode();
    }

    /// <summary>지난 실행의 세이브를 지운다.
    ///
    /// 처음부터 끝까지를 담는 녹화인데, 세이브는 실행 사이에 남는다. 앞선 실행이
    /// 중간에 죽거나 멈췄으면 문·레버·기둥·아이템의 "이미 썼음" 기록이 그대로
    /// 남아서, 다음 실행은 열쇠가 사라진 층에서 시작한다. 데이터를 새로 뽑았을
    /// 때는 인덱스까지 어긋나 더 나쁘다.</summary>
    static void ClearSaveData()
    {
        int removed = 0;
        foreach (string path in System.IO.Directory.GetFiles(Application.persistentDataPath, "*.json"))
        {
            System.IO.File.Delete(path);
            removed++;
        }
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log($"[Playthrough] 지난 세이브 삭제 — json {removed}개 + PlayerPrefs");
    }

    static void EditorSceneOpen()
    {
        const string title = "Assets/@Scenes/TitleScene.unity";
        if (File.Exists(title))
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(title);
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        string mode = SessionState.GetString(StateKey, "");
        if (string.IsNullOrEmpty(mode))
            return;

        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            // 예외 스택이 로그 파일에 안 남는다. 직접 받아서 찍는다 —
            // UI_GameScene.Init 이 어디서 끊기는지 이게 없으면 알 수가 없다.
            // 이 프로젝트는 예외 스택 기록이 꺼져 있어서 조건 문자열만 남는다. 켠다.
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;

            AutoPlayer.Spawn();
            if (mode == "record")
                StartRecording();
            _nextLog = 0;
            _startedAt = EditorApplication.timeSinceStartup;
            _lockedTime = 0.0;
            _lastTick = 0.0;
            EditorApplication.update -= Watch;
            EditorApplication.update += Watch;
        }
        else if (change == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Watch;
            Finish();
        }
    }

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Exception)
            return;
        // Log 타입으로만 다시 찍는다. Exception 으로 찍으면 이 핸들러가 다시 불린다.
        Debug.Log($"[예외추적] {condition}\n{stackTrace}");
    }

    #region Recorder
    static void StartRecording()
    {
        Directory.CreateDirectory(OutputDir);

        RecorderControllerSettings settings =
            ScriptableObject.CreateInstance<RecorderControllerSettings>();
        settings.SetRecordModeToManual();          // 끝나는 시점을 봇이 정한다
        settings.FrameRatePlayback = FrameRatePlayback.Constant;
        settings.FrameRate = Fps;
        settings.CapFrameRate = false;             // 그릴 수 있는 만큼 빨리 — 실시간에 묶이지 않는다
        settings.ExitPlayMode = false;

        MovieRecorderSettings movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "TheSword Playthrough";
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            // 인코딩이 병목이라 화질을 낮추면 그만큼 실제 시간이 줄어든다.
            // 100층을 한 번에 담는 것이 화질보다 중요하다.
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Low,
        };
        movie.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = Width,
            OutputHeight = Height,
        };
        movie.AudioInputSettings.PreserveAudio = true;
        movie.OutputFile = Path.Combine(OutputDir, OutputName);

        settings.AddRecorderSettings(movie);

        _controller = new RecorderController(settings);
        _controller.PrepareRecording();
        if (_controller.StartRecording())
            Debug.Log($"[Playthrough] 녹화 시작 -> {movie.OutputFile}.mp4 ({Width}x{Height} {Fps}fps)");
        else
            Debug.LogError("[Playthrough] 녹화 시작 실패");
    }

    static void StopRecording()
    {
        if (_controller == null)
            return;
        if (_controller.IsRecording())
            _controller.StopRecording();
        _controller = null;
        Debug.Log("[Playthrough] 녹화 종료");
    }
    #endregion

    /// <summary>플레이모드가 도는 동안 진행을 지켜본다.</summary>
    static void Watch()
    {
        if (EditorApplication.isPlaying == false)
            return;

        Unstick();

        AutoPlayer bot = AutoPlayer.Instance;
        if (bot == null)
            return;

        if (EditorApplication.timeSinceStartup > _nextLog)
        {
            _nextLog = EditorApplication.timeSinceStartup + 15.0;
            LogProgress();
        }

        bool timeUp = EditorApplication.timeSinceStartup - _startedAt > MaxMinutes * 60.0;
        if (bot.Finished == false && bot.Failed == false && timeUp == false)
            return;

        string msg = bot.Result;
        if (timeUp && bot.Finished == false && bot.Failed == false)
            msg = $"{MaxMinutes:0}분 제한에 걸려 녹화를 끊었다";

        SessionState.SetString(StateKey, (bot.Failed || timeUp) && bot.Finished == false
            ? "failed" : "done");
        SessionState.SetString(StateKey + ".msg", msg);
        StopRecording();
        EditorApplication.isPlaying = false;
    }

    /// <summary>
    /// 손수 만든 연출들이 GameObject.Find 실패로 중간에 죽으면 OnDirect/OnConversation 이
    /// 켜진 채 남아 게임이 그 자리에서 멈춘다. 봇 코루틴과 무관하게 에디터 루프에서 푼다.
    /// </summary>
    static void Unstick()
    {
        try
        {
            GameManager g = Managers.Game;
            if (g == null || g.PlayerData == null)
                return;

            double now = EditorApplication.timeSinceStartup;
            double dt = _lastTick > 0.0 ? now - _lastTick : 0.0;
            _lastTick = now;
            if (dt <= 0.0 || dt > 1.0)
                return;   // 프레임이 튀면 세지 않는다

            // 죽은 연출은 잠깐씩 풀렸다 다시 걸린다. 그래서 "연속으로 잠겨 있었는가"가
            // 아니라 "잠겨 있던 시간이 쌓였는가" 로 본다. 전투는 스스로 끝나므로 뺀다.
            bool locked = g.OnConversation || g.OnDirect || g.OnInteract
                          || g.OnInputLock || g.OnFade || g.OnLever;
            if (g.OnBattle)
            {
                _lockedTime = 0.0;
                return;
            }
            _lockedTime = locked ? _lockedTime + dt : Math.Max(0.0, _lockedTime - dt * 3.0);

            if (_lockedTime < StuckSeconds)
                return;

            Debug.LogWarning($"[Playthrough] 잠금이 {_lockedTime:0}초 쌓여 강제로 푼다 " +
                             $"({g.PlayerData.CurStageid + 1}층)");
            g.OnDirect = false;
            g.OnConversation = false;
            g.OnInteract = false;
            g.OnInputLock = false;
            g.OnFade = false;
            g.OnLever = false;
            Managers.UI.ClosePopupUI();
            Managers.UI.ShowGameSceneUI();

            if (AutoPlayer.Instance != null)
                AutoPlayer.Instance.OnUnstuck();

            _lockedTime = 0.0;
        }
        catch (Exception e)
        {
            if (EditorApplication.timeSinceStartup > _nextUnstickLog)
            {
                _nextUnstickLog = EditorApplication.timeSinceStartup + 15.0;
                Debug.LogWarning($"[Playthrough] Unstick 예외: {e.Message}");
            }
        }
    }

    static void LogProgress()
    {
        try
        {
            GameManager g = Managers.Game;
            if (g == null || g.PlayerData == null)
                return;
            string plan = AutoPlayer.Instance != null ? AutoPlayer.Instance.Plan : "";
            string flags = $"{(g.OnBattle ? "B" : "-")}{(g.OnConversation ? "C" : "-")}" +
                           $"{(g.OnDirect ? "D" : "-")}{(g.OnFade ? "F" : "-")}" +
                           $"{(g.OnInteract ? "I" : "-")}{(g.OnInputLock ? "L" : "-")}";
            Debug.Log($"[Playthrough] {g.PlayerData.CurStageid + 1}층 " +
                      $"Lv{g.PlayerData.Level} HP {g.PlayerData.CurHP:0}/{g.PlayerData.MaxHP:0} " +
                      $"검{g.PlayerData.CurSword} [{flags}] | {plan}" +
                      (AutoPlayer.Instance != null && AutoPlayer.Instance.BossInfo.Length > 0
                          ? " | " + AutoPlayer.Instance.BossInfo : ""));
        }
        catch (Exception)
        {
            // 아직 초기화 전이면 조용히 넘어간다
        }
    }

    static void Finish()
    {
        string state = SessionState.GetString(StateKey, "");
        string msg = SessionState.GetString(StateKey + ".msg", "");
        SessionState.SetString(StateKey, "");

        if (state == "done")
        {
            Debug.Log($"[Playthrough] 완주: {msg}");
            EditorApplication.Exit(0);
        }
        else if (state == "failed")
        {
            Debug.LogError($"[Playthrough] 실패: {msg}");
            EditorApplication.Exit(1);
        }
        else
        {
            // 사람이 플레이모드를 껐다 — 그냥 둔다.
            Debug.Log("[Playthrough] 중단됨");
        }
    }
}
