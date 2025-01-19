using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class EndingScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.GameScene;
        Managers.UI.ShowSceneUI<UI_EndingScene>();
    }


    public override void Clear()
    {

    }
}
