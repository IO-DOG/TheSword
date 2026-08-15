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

    const int Width = 1280;
    const int Height = 720;
    const float Fps = 30f;

    /// <summary>이 시간이 지나면 결과와 상관없이 녹화를 끊고 나온다.
    /// 봇이 어딘가에서 맴돌아도 mp4 는 정상적으로 마무리돼야 한다.</summary>
    const double MaxMinutes = 25.0;

    static RecorderController _controller;
    static double _nextLog;
    static double _startedAt;

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

        SessionState.SetString(StateKey, mode);
        EditorSceneOpen();
        Debug.Log($"[Playthrough] 시작 ({mode}) — 플레이모드 진입");
        EditorApplication.EnterPlaymode();
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
            AutoPlayer.Spawn();
            if (mode == "record")
                StartRecording();
            _nextLog = 0;
            _startedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Watch;
            EditorApplication.update += Watch;
        }
        else if (change == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Watch;
            Finish();
        }
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
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Medium,
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
                      $"검{g.PlayerData.CurSword} [{flags}] | {plan}");
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
