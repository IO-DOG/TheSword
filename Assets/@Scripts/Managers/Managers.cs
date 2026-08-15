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
        }
    }

    public static string GetString(int id)
    {
        int scriptType = (int)Managers.Game.ScriptType;

        // 없는 문자열 하나 때문에 부르는 쪽이 통째로 죽으면 안 된다.
        // 9층 이름(5008)이 없어서 층 이름 팝업이 터졌고, 그걸 부르던
        // 포탈 워프 코루틴이 같이 죽어 다음 층으로 못 넘어갔다.
        Data.ScriptData script;
        if (Managers.Data.ScriptDic.TryGetValue(id, out script) == false || script == null)
        {
            Debug.LogWarning($"[Script] {id} 번 문자열이 없다");
            return "";
        }

        string ret = "";
        switch (scriptType)
        {
            case 0:
                ret = script.ScriptEn;
                break;
            case 1:
                ret = script.ScriptKr;
                break;
            case 2:
                ret = script.ScriptEn;
                break;
            case 3:
                ret = script.ScriptJp;
                break;
            case 4:
                ret = script.ScriptCn;
                break;
            default:
                ret = script.ScriptEn;
                break;
        }
        if (ret == null)
            ret = "";

        ret = ret.Replace("\\n", "\n");
        ret = ret.Replace("^", ",");

        return ret;
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
