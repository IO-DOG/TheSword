using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_SettingPopup : UI_Popup
{
    #region Enum

    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePopupUI();
        }
    }
}
