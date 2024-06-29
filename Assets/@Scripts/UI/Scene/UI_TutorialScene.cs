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

        Managers.Game.MainCamera = Camera.main;

        Managers.Game.CurPlayerData.CurStageid = 0;
        Managers.Game.InstantiateMap(Managers.Data.StageInfoDic[Managers.Game.CurPlayerData.CurStageid].DungeonID);

        Managers.UI.ShowPopupUI<UI_StageNamePopup>();

        return true;
    }
}
