using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_GameOverPopup : UI_Popup
{
    #region Enum
    enum Buttons
    {
        ReGameButton
    }
    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButton(typeof(Buttons));

        GetButton((int)Buttons.ReGameButton).gameObject.BindEvent(() => {
            Managers.Game.Player.gameObject.SetActive(true);
            Managers.Game.PlayerData.Ability = (int)Define.Trait.None;
            Debug.Log("Cllck OnClickNewGameButton");
            Managers.Game.DeleteGameData();
            Managers.Data.Init();
            Managers.Scene.LoadScene(Define.Scene.GameScene); 
        });

        return true;
    }
}
