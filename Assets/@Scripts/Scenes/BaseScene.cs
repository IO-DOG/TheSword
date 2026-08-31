using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class BaseScene : MonoBehaviour
{
    public Define.Scene SceneType { get; protected set; } = Define.Scene.Unknown;

    void Awake()
    {
        Init();
    }

    protected virtual void Init()
    {
        Object obj = GameObject.FindObjectOfType(typeof(EventSystem));
        if (obj == null)
        {
            // 예전에는 "UI/EventSystem" 을 어드레서블에서 불렀는데 <b>그런 프리팹이
            // 프로젝트에 없다.</b> 씬에 EventSystem 이 하나도 없을 때만 타는 길이라
            // 여태 안 걸렸을 뿐, 타는 순간 null 에 .name 을 대입해 씬 초기화가
            // 통째로 끊긴다. 없는 것을 부르는 대신 직접 만든다.
            GameObject made = new GameObject("@EventSystem");
            made.AddComponent<EventSystem>();
            made.AddComponent<StandaloneInputModule>();
        }
    }

    public abstract void Clear();
}
