using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InputManager
{
    public Action KeyAction = null;

    public void OnUpdate()
    {
        // 패드의 스틱·방향 패드·트리거는 키가 아니라 축이라 Input.anyKey 에 안 잡힌다 — GameKeys 가 같이 본다.
        if (GameKeys.AnyHeld == false)
            return;

        if (KeyAction != null)
            KeyAction.Invoke();
    }
}
