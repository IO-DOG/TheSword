using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_TutorialScene : UI_Scene
{
    #region Enum
    //enum Buttons
    //{
    //    //ToTitleButton,
    //    PlayConversation,
    //}

    enum GameObjects
    {
        KeyInventory,
        GreenKey,
        YellowKey,
        RedKey,
    }

    enum Texts
    {
        //PlayerNameText,
        PlayerHPText,
        PlayerAttackText,
        PlayerDefenseText,
        PlayerLevelText,
    }

    enum Images
    {
        MainUIEXPGaugeImage,
        MainUIAuxiliaryHPGaugeImage,
        MainUIOptionAImage,
        MainUIOptionBImage,
        //MainUIInventoryAImage,
        //MainUIInventoryBImage,
        //MainUISwordAImage,
        //MainUISwordBImage,
        //MainUIWarpAImage,
        //MainUIWarpBImage,
    }

    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        //BindButton(typeof(Buttons));
        BindObject(typeof(GameObjects));
        BindText(typeof(Texts));
        BindImage(typeof(Images));
        #endregion

        Managers.Game.InstantiateMap(Managers.Game.CurPlayerData.CurStageid);
        Managers.Game.CurPlayerData.MoveSpeed = 1f;

        Managers.Game.Player._keyInventory = GetObject((int)GameObjects.KeyInventory);
        GetObject((int)GameObjects.GreenKey).SetActive(false);
        GetObject((int)GameObjects.YellowKey).SetActive(false);
        GetObject((int)GameObjects.RedKey).SetActive(false);

        Fade();

        return true;
    }

    public void Refresh()
    {
        GetText((int)Texts.PlayerLevelText).text = Managers.Game.CurPlayerData.Level.ToString();
        int level = Managers.Game.CurPlayerData.Level;
        Debug.Log($"{Managers.Game.CurPlayerData.CurExp} , {Managers.Data.PlayerDic[level].NeedExp}");
        GetImage((int)Images.MainUIEXPGaugeImage).fillAmount = Managers.Game.CurPlayerData.CurExp / Managers.Data.PlayerDic[level].NeedExp;
        GetImage((int)Images.MainUIAuxiliaryHPGaugeImage).fillAmount = Managers.Game.CurPlayerData.CurHP / Managers.Game.CurPlayerData.MaxHP;
        Managers.Game.KeyInventory.ShowKeySlot(Managers.Game.Player._keyInventory);
        SetPlayerInfo();
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
            switch (Managers.Game.CurPlayerData.MoveSpeed)
            {
                case 1f:
                    Managers.Game.CurPlayerData.MoveSpeed = 1.5f;
                    Managers.Game.Player.Speed = Managers.Game.CurPlayerData.MoveSpeed * 5;
                    break;
                case 1.5f:
                    Managers.Game.CurPlayerData.MoveSpeed = 2f;
                    Managers.Game.Player.Speed = Managers.Game.CurPlayerData.MoveSpeed * 5;
                    break;
                case 2f:
                    Managers.Game.CurPlayerData.MoveSpeed = 1f;
                    Managers.Game.Player.Speed = Managers.Game.CurPlayerData.MoveSpeed * 5;
                    break;
            }

            Debug.Log($"Managers.Game.CurPlayerData.MoveSpeed : {Managers.Game.CurPlayerData.MoveSpeed}");
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            GameObject monsters = GameObject.Find("Monsters");
            if (monsters != null ) monsters.gameObject.SetActive(false);
            GameObject pillars = GameObject.Find("Pillars");
            if (pillars != null) pillars.gameObject.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            Managers.Game.OnMeetKingSlime = true;

            Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset =
                Vector3.Lerp(Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset, new Vector3(0f, 20f, -5f), 2f);
        }
        
        #endregion
    }

    /// <summary>
    /// �������� �÷��̾� ������ �����ϴ� �Լ�
    /// �÷��̾� ������ �߰��Ǹ� ���Լ� ���� �߰��Ǿ����.
    /// </summary>
    public void SetPlayerInfo()
    {
        //GetText((int)Texts.PlayerNameText).text = "PlayerName";
        GetText((int)Texts.PlayerHPText).text = $"{Managers.Game.CurPlayerData.CurHP}";
        GetText((int)Texts.PlayerAttackText).text = $"{Managers.Game.CurPlayerData.Attack}";
        GetText((int)Texts.PlayerDefenseText).text = $"{Managers.Game.CurPlayerData.Defence}";
    }
}
