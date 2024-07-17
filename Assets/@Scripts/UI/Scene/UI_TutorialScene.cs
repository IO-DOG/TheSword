using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_TutorialScene : UI_Scene
{
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind

        #endregion

        Managers.Game.CurPlayerData.CurStageid = 0;
        Managers.Game.InstantiateMap(Managers.Game.CurPlayerData.CurStageid);
        
        FadeEffect(Define.FadeEvent.FadnIn, Define.FADE_DURATION);
        FadeEffect(Define.FadeEvent.CenterToRight, Define.FADE_DURATION);

        return true;
    }
}
