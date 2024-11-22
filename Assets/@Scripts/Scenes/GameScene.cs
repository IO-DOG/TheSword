using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.GameScene;
        Managers.Game.GameScene = Managers.UI.ShowSceneUI<UI_GameScene>();

        Managers.Game.DirectionalLight = GameObject.Find("Directional Light").GetComponent<Light>();
    }


    public override void Clear()
    {

    }

}
