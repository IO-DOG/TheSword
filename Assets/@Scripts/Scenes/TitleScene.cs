using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.TitleScene;

        // 현재야 생각을 해볼까?
        // 혹시 알고있는지 모르겠지만 넌 사람이란다.

        GameObject ui_titleScene = GameObject.Find("UI_TitleScene");


        //TitleUI
    }

    public override void Clear()
    {

    }
}
