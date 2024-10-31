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

    public static GameManager Game { get { return Instance?._game; } }
    public static DirectingManager Directing { get { return Instance?._directing; } }

    #endregion

    #region Core
    DataManager _data = new DataManager();
    InputManager _input = new InputManager();
    ResourceManager _resource = new ResourceManager();
    SceneManagerEx _scene = new SceneManagerEx();
    SoundManager _sound = new SoundManager();
    UIManager _ui = new UIManager();

    public static DataManager Data { get { return Instance?._data; } }
    public static InputManager Input { get { return Instance._input; } }
    public static ResourceManager Resource { get { return Instance?._resource; } }
    public static SceneManagerEx Scene { get { return Instance?._scene; } }
    public static SoundManager Sound { get { return Instance?._sound; } }
    public static UIManager UI { get { return Instance?._ui; } }
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

            DontDestroyOnLoad(go);
            DontDestroyOnLoad(cursor);
            s_instance = go.GetComponent<Managers>();
            //s_instance._sound.Init();
            //s_instance._time = go.AddComponent<TimeManager>();

        }
    }

    public static string GetString(int id)
    {
        int scriptType = (int)Managers.Game.ScriptType;

        string ret = "";
        switch (scriptType)
        {
            case 0:
                ret = Managers.Data.ScriptDic[id].ScriptKr;
                break;
            case 1:
                ret = Managers.Data.ScriptDic[id].ScriptKr;
                break;
            case 2:
                ret = Managers.Data.ScriptDic[id].ScriptEn;
                break;
            case 3:
                ret = Managers.Data.ScriptDic[id].ScriptJp;
                break;
            case 4:
                ret = Managers.Data.ScriptDic[id].ScriptCn;
                break;
            default:
                ret = Managers.Data.ScriptDic[id].ScriptKr;
                break;
        }

        ret = ret.Replace("\\n", "\n");
        ret = ret.Replace("^", ",");

        return ret;
    }

    private void Update()
    {
        _input.OnUpdate();
    }

    public static void Clear()
    {
        Sound.Clear();
        Scene.Clear();
        UI.Clear();
        //Pool.Clear();
        //Object.Clear();
    }


}
