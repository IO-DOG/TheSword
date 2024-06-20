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
        Contents,
    }

    #endregion

    UI_PlayerCard playerCard;
    UI_CreatureCard monsterCard;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindObject(typeof(Objects));
        #endregion

        GetImage((int)Images.BGImage).sprite = Managers.Game._screenShot2;

        GameObject go = GetObject((int)Objects.Contents).gameObject;
        // TODO
        // show Creature Card
        playerCard = Managers.UI.MakeSubItem<UI_PlayerCard>(go.transform);
        //playerCard.Data = Managers.Game.Player.Data;
        monsterCard = Managers.UI.MakeSubItem<UI_CreatureCard>(go.transform);
        //monsterCard.Data = Managers.Game.MonsterData;

        Managers.Game.OnBattleAction -= BattleEnd;
        Managers.Game.OnBattleAction += BattleEnd;
        Managers.Game.OnBattle = true;

        return true;
    }

    public void BattleEnd()
    {
        Destroy(playerCard.gameObject);
        Destroy(monsterCard.gameObject);

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
