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
    UI_MonsterCard monsterCard = null;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindObject(typeof(Objects));
        #endregion

        GetImage((int)Images.BGImage).sprite = Managers.Game._screenShot2;

        Managers.Sound.Play(Define.Sound.Effect, "BattleStart_SFX");

        // TODO
        // show Creature Card
        playerCard = Managers.UI.SetBattleCard<UI_PlayerCard>(gameObject.transform, Managers.Game.PlayerData);

        float width = Screen.width;
        float height = Screen.height;
        playerCard.transform.position = new Vector3(width * 0.3f, height * 0.6f, 0);
        if (Managers.Game.ScreenType == Define.ScreenType.Window)
            playerCard.transform.localScale = new Vector3(width / 1920 * 8f, height / 1080 * 8f, 1);
        else
            playerCard.transform.localScale = new Vector3(width / 1920 * 4.5f, height / 1080 * 4.5f, 1);
        //playerCard.Data = Managers.Game.Player.Data;

        for (int i = 0; i < Managers.Game.MonsterData.Count; i++)
        {
            monsterCard = Managers.UI.SetBattleCard<UI_MonsterCard>(gameObject.transform, Managers.Game.MonsterData[i]);

            monsterCard.transform.position = new Vector3(width * 0.7f, height * 0.6f, 0);
            if (Managers.Game.ScreenType == Define.ScreenType.Window)
                monsterCard.transform.localScale = new Vector3(width / 1920 * 8f, height / 1080 * 8f, 1);
            else
                monsterCard.transform.localScale = new Vector3(width / 1920 * 4.5f, height / 1080 * 4.5f, 1);
        }

        //monsterCard.Data = Managers.Game.MonsterData;

        Managers.Game.OnBattle = true;
        Managers.Game.OnBattleAction -= BattleEnd;
        Managers.Game.OnBattleAction += BattleEnd;

        return true;
    }

    public void BattleEnd()
    {
        float closeTime = 0.3f;
        StartCoroutine(CoBattleEnd(closeTime));
    }

    IEnumerator CoBattleEnd(float time)
    {
        yield return new WaitForSeconds(time);

        Destroy(playerCard.gameObject);
        Destroy(monsterCard.gameObject);

        Managers.Game.OnBattleAction = null;
        if (Managers.Game.GameScene != null)
        {
            Managers.Game.GameScene.SetPlayerInfo();
            Managers.Game.GameScene.Refresh();
        }

        Managers.Game.OnBattle = false;

        ClosePopupUI();

        // Game Over Popup
        if (Managers.Game.IsPlayerDead)
        {
            int moveCount = PlayerPrefs.GetInt("DEATHCOUNT", 0);
            moveCount++;
            PlayerPrefs.SetInt("DEATHCOUNT", moveCount);
            Managers.UI.ShowPopupUI<UI_GameOverPopup>();
        }

        // 최초 포션인지 확인
        if (PlayerPrefs.GetInt("ISFIRSTBATTLE") == 0)
        {
            PlayerPrefs.SetInt("ISFIRSTBATTLE", 1);
            UI_GuidePopup guidePopup = Managers.UI.ShowPopupUI<UI_GuidePopup>();
            guidePopup.SetInfo(Define.GUIDE_BATTLE);
        }
    }
}
