using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

/// <summary>
/// 스토어 트레일러 클립을 녹화한다 (Build/Store/plan.md 5절). 열린 에디터의 플레이 중에 eval 로 짧게 부른다.
///
///   StoreCapture.StartRecording(@"C:\...\Build\Store\clips\en", "C01");   // 녹화 시작
///   StoreCapture.Mark("m_bump");                                          // 영상 시작부터 몇 초인지 적는다
///   StoreCapture.StopRecording();                                         // mp4 를 닫고 마커를 C01.json 으로 쓴다
///
/// 1920x1080 60fps 고정 프레임(Constant)이다. 게임 시간이 프레임마다 1/60 초씩 가므로 에디터가 느리게 그려도 영상은
/// 매끈하고, 소리(AudioRenderer)도 프레임에 맞춰 뽑힌다. CapFrameRate 는 끈다 — 실시간에 묶지 않고 그릴 수 있는 만큼 돈다.
/// 그래서 녹화 중의 타이밍은 eval 왕복(실시간)이 아니라 게임 시간(DOTween·코루틴)으로 잡아야 영상 시간과 맞는다.
///
/// 마커는 프레임 수로 잰다. 타임스케일을 0 으로 세워도(정지 연출) 영상 시간은 계속 흐르기 때문이다.
/// 같은 이름은 처음 것만 남는다 — "처음 일어난 때" 가 편집이 자르는 자리다.
/// </summary>
public static class StoreCapture
{
    const int Width = 1920, Height = 1080, Fps = 60;

    static RecorderController _controller;
    static string _file;
    static int _firstFrame;
    static bool _wasMuted;      // Game view 의 "Mute Audio". 켜져 있으면 AudioRenderer 도 무음만 받는다 — 녹화 동안만 끈다
    static readonly List<KeyValuePair<string, double>> _marks = new List<KeyValuePair<string, double>>();

    public static bool IsRecording => _controller != null && _controller.IsRecording();

    /// <summary>녹화를 시작한다. dir 에 name.mp4 가 생긴다(있으면 덮어쓴다). 시작하지 못하면 null.</summary>
    public static string StartRecording(string dir, string name)
    {
        if (IsRecording)
            StopRecording();
        Directory.CreateDirectory(dir);

        RecorderControllerSettings settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        settings.SetRecordModeToManual();
        settings.FrameRatePlayback = FrameRatePlayback.Constant;
        settings.FrameRate = Fps;
        settings.CapFrameRate = false;
        settings.ExitPlayMode = false;

        MovieRecorderSettings movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "Store " + name;
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
        };
        // Game view 를 미리 1920x1080 으로 골라 둔다(Full HD). 다르면 Recorder 가 "Recording Resolution" 칸을 이 크기로 고쳐 쓴다.
        movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = Width, OutputHeight = Height };
        movie.AudioInputSettings.PreserveAudio = true;
        movie.OutputFile = Path.Combine(dir, name);     // 확장자는 Recorder 가 붙인다
        settings.AddRecorderSettings(movie);

        _marks.Clear();
        _file = Path.Combine(dir, name);
        _controller = new RecorderController(settings);
        _controller.PrepareRecording();
        _firstFrame = Time.frameCount;
        // 녹화 중에는 AudioRenderer 가 스피커로 내보내지 않으니 꺼도 시끄럽지 않다.
        _wasMuted = UnityEditor.EditorUtility.audioMasterMute;
        UnityEditor.EditorUtility.audioMasterMute = false;
        if (_controller.StartRecording())
            return _file + ".mp4";
        UnityEditor.EditorUtility.audioMasterMute = _wasMuted;
        _controller = null;
        return null;
    }

    /// <summary>지금이 영상 시작부터 몇 초인지 key 로 적는다. 녹화 중이 아니면 -1.</summary>
    public static double Mark(string key)
    {
        if (IsRecording == false)
            return -1;
        double t = (Time.frameCount - _firstFrame) / (double)Fps;
        if (_marks.Exists(m => m.Key == key) == false)
            _marks.Add(new KeyValuePair<string, double>(key, t));
        return t;
    }

    /// <summary>녹화를 끝내고 마커를 같은 이름의 .json 으로 쓴다. 쓴 마커를 돌려준다.</summary>
    public static string StopRecording()
    {
        if (_controller == null)
            return "not recording";
        double length = (Time.frameCount - _firstFrame) / (double)Fps;
        if (_controller.IsRecording())
            _controller.StopRecording();
        _controller = null;
        UnityEditor.EditorUtility.audioMasterMute = _wasMuted;

        StringBuilder json = new StringBuilder("{");
        foreach (KeyValuePair<string, double> m in _marks)
            json.Append('"').Append(m.Key).Append("\": ").Append(m.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)).Append(", ");
        json.Append("\"length\": ").Append(length.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)).Append('}');
        File.WriteAllText(_file + ".json", json.ToString());
        return json.ToString();
    }
}
