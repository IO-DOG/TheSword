using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class UI_BattlePopup : UI_Popup
{
    #region Enum
    enum Images
    {
        BGImage,
    }

    enum Objects
    {

    }

    #endregion

    UI_PlayerCard playerCard = null;
    UI_CreatureCard monsterCard = null;
    UI_KingSlimeCard kingSlimeCard = null;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindObject(typeof(Objects));
        #endregion

        GetImage((int)Images.BGImage).sprite = Managers.Game._screenShot2;

        // TODO
        // show Creature Card
        playerCard = Managers.UI.MakeSubItem<UI_PlayerCard>(gameObject.transform);
        //playerCard.Data = Managers.Game.Player.Data;

        switch (Managers.Game.MonsterData.id)
        {
            case Define.KingSlime:
                kingSlimeCard = Managers.UI.MakeSubItem<UI_KingSlimeCard>(gameObject.transform);
                playerCard.transform.position = new Vector3(480, 440, 0);
                kingSlimeCard.transform.position = new Vector3(1440, 640, 0);
                break;
            default:
                monsterCard = Managers.UI.MakeSubItem<UI_CreatureCard>(gameObject.transform);
                playerCard.transform.position = new Vector3(580, 540, 0);
                monsterCard.transform.position = new Vector3(1340, 540, 0);
                break;
        }

        //monsterCard.Data = Managers.Game.MonsterData;

        Managers.Game.OnBattleAction -= BattleEnd;
        Managers.Game.OnBattleAction += BattleEnd;
        Managers.Game.OnBattle = true;

        return true;
    }

    public void BattleEnd()
    {
        Destroy(playerCard.gameObject);

        switch (Managers.Game.MonsterData.id)
        {
            case Define.KingSlime:
                Destroy(kingSlimeCard.gameObject);
                break;
            default:
                Destroy(monsterCard.gameObject);
                break;
        }

        Managers.Game.OnBattleDataRefreshAction = null;
        Managers.Game.OnBattleCreatureDefeceAction = null;
        Managers.Game.OnBattleCreatureDamagedAction = null;
        Managers.Game.OnBattleAction = null;
        if (Managers.Game.GameScene != null)
            Managers.Game.GameScene.SetPlayerInfo();
        Managers.Game.SaveGame();
        ClosePopupUI();
    }
}
