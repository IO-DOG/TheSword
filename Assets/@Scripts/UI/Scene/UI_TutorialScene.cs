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

    private void Update()
    {
        #region for_test
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Managers.Game.CurPlayerData.CurExp += 10;
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Managers.Game.CurPlayerData.Attack += 10;
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Managers.Game.SaveGame();
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Managers.Game.LoadGame();
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword].Add(0);
        }
        #endregion
    }
}
