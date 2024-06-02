using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_InvenPopup : UI_Popup
{
    #region Enum
    enum Images
    {

    }

    enum Texts
    {

    }

    enum GameObjects
    {

    }

    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindObject(typeof(GameObjects));
        #endregion

        Refresh();

        return true;
    }

    void Refresh()
    {

    }

}
