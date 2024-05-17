using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputTestScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.InputTestScene;
        //TitleUI

        Managers.UI.ShowSceneUI<UI_InputTestScene>();
    }

    public override void Clear()
    {

    }
}
