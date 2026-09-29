using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Managers : MonoBehaviour
{
    static Managers s_instance; // 유일성이 보장된다
    static Managers Instance { get { Init(); return s_instance; } } // 유일한 매니저를 갖고온다
    // 앱을 끄는 중에 OnDestroy 에서 매니저를 부르면 Init 이 @Managers 를 새로 만든다 — 있는지만 볼 때 쓴다.
    public static bool IsAlive => s_instance != null;

    #region Contents
    GameManager _game = new GameManager();
    DirectingManager _directing = new DirectingManager();
    EventManager _event = new EventManager();
    ObjectManager _object = new ObjectManager();

    public static GameManager Game { get { return Instance?._game; } }
    public static DirectingManager Directing { get { return Instance?._directing; } }
    public static EventManager Event { get { return Instance?._event; } }
    public static CursorManager Cursor = null;
    public static ObjectManager Object { get { return Instance?._object; } }
    #endregion

    #region Core
    DataManager _data = new DataManager();
    InputManager _input = new InputManager();
    ResourceManager _resource = new ResourceManager();
    SceneManagerEx _scene = new SceneManagerEx();
    SoundManager _sound = new SoundManager();
    UIManager _ui = new UIManager();
    PoolManager _pool = new PoolManager();

    public static DataManager Data { get { return Instance?._data; } }
    public static InputManager Input { get { return Instance._input; } }
    public static ResourceManager Resource { get { return Instance?._resource; } }
    public static SceneManagerEx Scene { get { return Instance?._scene; } }
    public static SoundManager Sound { get { return Instance?._sound; } }
    public static UIManager UI { get { return Instance?._ui; } }
    public static PoolManager Pool { get { return Instance?._pool; } }
    #endregion

    public static void Init()
    {
        if (s_instance == null)
        {
            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            GameObject cursor = GameObject.Find("@Cursor");
            if (cursor == null)
            {
                cursor = new GameObject { name = "@Cursor" };
                cursor.AddComponent<SpriteRenderer>();
                cursor.AddComponent<Animator>();
                cursor.AddComponent<CursorManager>();
            }

            // DontDestroyOnLoad 는 플레이 모드에서만 허용된다.
            // (에디터 배치 검증 스크립트가 Managers 를 건드릴 때 예외가 나지 않도록)
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
                DontDestroyOnLoad(cursor);
            }
            s_instance = go.GetComponent<Managers>();

            if (Application.isPlaying)
                ApplySettings();
        }
    }

    // 설정(언어·화면·소리·글꼴)은 게임 진행과 따로 산다 — 켜자마자 한 번 맞춘다.
    static void ApplySettings()
    {
        Game.ScriptType = GameSettings.Language;   // 예전 코드(UI_TitleScene)가 읽는 사본
        Game.ScreenType = GameSettings.ScreenTypeOf(Screen.fullScreenMode, Screen.height);
        // 에디터는 도메인 리로드를 끄고 돌아서 정적 이벤트가 플레이마다 쌓인다 — 빼고 건다.
        GameSettings.Changed -= SyncLanguage;
        GameSettings.Changed += SyncLanguage;
        GameSettings.ApplyDisplay();
        GameSettings.ApplyVolume();
        // 글꼴이 안 걸려도 게임은 돈다 — 빠진 글자만 네모로 나온다. 부팅을 같이 죽이지 않는다.
        try { FontFallback.Install(); }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    static void SyncLanguage() => Game.ScriptType = GameSettings.Language;

    public static string GetString(int id)
    {
        Define.ScriptType language = GameSettings.Language;

        // 없는 문자열 하나 때문에 부르는 쪽이 통째로 죽으면 안 된다.
        // 9층 이름(5008)이 없어서 층 이름 팝업이 터졌고, 그걸 부르던
        // 포탈 워프 코루틴이 같이 죽어 다음 층으로 못 넘어갔다.
        //
        // 고른 언어 → 영어 → 한국어 (→ 남은 칸) → 선로드 전 문구 순으로 찾는다. 일본어·중국어 칸이
        // 267·278 줄 비어 있어서, 예전에는 그 언어를 고르면 메뉴와 대사가 통째로 빈칸이었다.
        string ret = null;
        if (Managers.Data.ScriptDic.TryGetValue(id, out Data.ScriptData script) && script != null)
            ret = Column(script, language) ?? Column(script, Define.ScriptType.En) ?? Column(script, Define.ScriptType.Kr)
                ?? Column(script, Define.ScriptType.Jp) ?? Column(script, Define.ScriptType.Cn);
        // 어드레서블을 못 올리면 표가 비어 있다 — 타이틀의 "불러오지 못했습니다" 가 그때 뜬다.
        ret ??= GeneratedUiText.Get(id, (int)language);
        if (ret == null)
        {
            Debug.LogWarning($"[Script] {id} 번 문자열이 없다");
            return "";
        }

        ret = ret.Replace("\\n", "\n");
        ret = ret.Replace("^", ",");

        return ret;
    }

    static string Column(Data.ScriptData script, Define.ScriptType language)
    {
        string text;
        switch (language)
        {
            case Define.ScriptType.Kr: text = script.ScriptKr; break;
            case Define.ScriptType.Jp: text = script.ScriptJp; break;
            case Define.ScriptType.Cn: text = script.ScriptCn; break;
            default: text = script.ScriptEn; break;
        }
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private void Update()
    {
        //Debug.Log("Managers");
        _input.OnUpdate();
    }

    public static void Clear()
    {
        Sound.Clear();
        Scene.Clear();
        UI.Clear();
        Pool.Clear();
        //Object.Clear();
    }
}
