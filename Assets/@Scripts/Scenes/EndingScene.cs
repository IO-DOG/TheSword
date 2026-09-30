using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class EndingScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        // 예전에는 GameScene 이라 적혀 있어, 자동 플레이가 엔딩에 닿은 것을 씬 이름으로만 알았다.
        SceneType = Define.Scene.EndingScene;
        Managers.UI.ShowSceneUI<UI_EndingScene>();
    }


    public override void Clear()
    {

    }
}
