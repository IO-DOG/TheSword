using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.IntroScene;
        Managers.UI.ShowSceneUI<UI_IntroScene>();
    }

    public override void Clear()
    {

    }
}
