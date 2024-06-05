using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_DialogPopup : UI_Popup
{
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
            ClosePopupUI();
    }
}
