using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_SHJTestScene : UI_Scene
{
    #region Enum

    enum Buttons
    {
        ToTitleButton,
    }

    enum Objects
    {

    }

    enum Images
    {

    }

    enum Texts
    {

    }

    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindButton(typeof(Buttons));
        #endregion

        GetButton((int)Buttons.ToTitleButton).gameObject.BindEvent(() => Managers.Scene.LoadScene(Define.Scene.TitleScene));

        return true;
    }
}
