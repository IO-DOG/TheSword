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
        if (Managers.Game.OnDirect == true)
            return;

        if (Input.anyKey == false)
            return;

        if (KeyAction != null)
            KeyAction.Invoke();
    }
}
