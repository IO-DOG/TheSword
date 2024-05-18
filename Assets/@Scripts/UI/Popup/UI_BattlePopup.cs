using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_BattlePopup : UI_Popup
{
    #region Enum

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
        BindObject(typeof(Objects));
        #endregion

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
        Managers.Game.OnBattleAction = null;
        Managers.Game.SaveGame();
        ClosePopupUI();
    }
}
