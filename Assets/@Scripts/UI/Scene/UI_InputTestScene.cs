using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_InputTestScene : UI_Scene
{
    enum Images
    {
        Slot,
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        #endregion

        return true;
    }
}
