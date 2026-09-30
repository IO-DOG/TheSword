using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerEx
{
    public BaseScene CurrentScene { get { return GameObject.FindObjectOfType<BaseScene>(); } }

    // 씬 컨트롤러(BaseScene)가 없는 씬도 있다 — EndingScene.unity 에는 엔딩 UI 만 놓여 있다. 예전에는 지금 씬의 종류로
    // 갈라서(어느 씬이든 하는 일은 같았다) 거기서 널 예외로 멈췄다. 씬이 없어도 비우고 넘어간다.
    public void LoadScene(Define.Scene type, Transform parents = null)
    {
        Managers.Clear();
        SceneManager.LoadScene(GetSceneName(type));
    }

    string GetSceneName(Define.Scene type)
    {
        string name = System.Enum.GetName(typeof(Define.Scene), type);
        return name;
    }

    public void Clear()
    {
        CurrentScene?.Clear();
    }
}
