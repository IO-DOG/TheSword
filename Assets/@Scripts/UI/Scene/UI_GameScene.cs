using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UI_GameScene : UI_Scene
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
        MainUIMapNameText,
    }

    enum Images
    {
        MainUIEXPGaugeImage,
        MainUIAuxiliaryHPGaugeImage,
        MainUIOptionAImage,
        MainUIOptionBImage,
        MainUIInventoryAImage,
        MainUIInventoryBImage,
        MainUISwordAImage,
        MainUISwordBImage,
        MainUIWarpAImage,
        MainUIWarpBImage,
    }

    #endregion

    public bool isOpenMenuPopup = false;

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

        Managers.UI.UI_GameScene = this;
        #region PointerEnter&PointerExit
        GetImage((int)Images.MainUIOptionAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIOptionBImage).gameObject.SetActive(true); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUIOptionAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIOptionBImage).gameObject.SetActive(false); ; }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.MainUIInventoryAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(true); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUIInventoryAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(false); }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.MainUISwordAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(true); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUISwordAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(false); ; }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.MainUIWarpAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(true); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUIWarpAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(false); ; }, null, Define.UIEvent.PointerExit);
        #endregion

        Managers.Game.GenerateMap(Managers.Game.PlayerData.CurStageid);
        Managers.Game.PlayerData.MoveSpeed = 1f;
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetupCameraConfiner();

        Managers.Game.PlayerData.CurShield = 0;

        Managers.Game.Player._keyInventory = GetObject((int)GameObjects.KeyInventory);
        GetObject((int)GameObjects.GreenKey).SetActive(false);
        GetObject((int)GameObjects.YellowKey).SetActive(false);
        GetObject((int)GameObjects.RedKey).SetActive(false);

        // UI 활성화 여부 체크
        if (PlayerPrefs.GetInt("ISOPENINVENUI") == 0) // 인벤 활성화 x
            OffUIInventory();
        if (PlayerPrefs.GetInt("ISOPENWARPUI") == 0)
            OffUIWarp();
        if (PlayerPrefs.GetInt("ISOPENCLASSUI") == 0)
            OffUISword();

        GetImage((int)Images.MainUIOptionAImage).gameObject.BindEvent(() =>
        {
            GameObject go = GameObject.Find("UI_MenuPopup");
            if (go == null)
            {
                isOpenMenuPopup = true;
                Managers.UI.ShowPopupUI<UI_MenuPopup>();
            }
            else
                go.GetComponent<UI_MenuPopup>().OpenOtherUI();
        });
        GetImage((int)Images.MainUIInventoryAImage).gameObject.BindEvent(OnClickMainUIInventoryAImage);

        //GetButton((int)Buttons.PlayConversation).gameObject.BindEvent(() =>
        //{
        //    if (!Managers.Game.OnBattle)
        //    {
        //        Managers.UI.ShowPopupUI<UI_ConversationPopup>();
        //        Managers.Game.CurEventID = Define.EVENT_SWORD_FIRST;
        //    }
        //});

        Refresh();
        Data.MyVector3 loadPos = Managers.Game.PlayerData.CurPosition;

        // 최초 실행 시 스폰 포인트 못 찾는 문제 예외 처리
        if (loadPos.X == 0 && loadPos.Z == 0)
            Managers.Game.Player.SetPlayerPosition(Managers.Game.SpawnPoints[0].position);
        else
        {
            Vector3 playerPos = new Vector3(loadPos.X, loadPos.Y, loadPos.Z);
            Managers.Game.Player.SetPlayerPosition(playerPos);
        }

        if (PlayerPrefs.GetInt("ISOPENSWORD") == 0)
            GetImage((int)Images.MainUISwordAImage).gameObject.SetActive(false);
        if (PlayerPrefs.GetInt("ISOPENPORTAL") == 0)
            GetImage((int)Images.MainUIWarpAImage).gameObject.SetActive(false);

        Managers.Game.OnFadeAction.Invoke(1f);

        Managers.UI.ShowStageNamePopup(1f);

        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
            Managers.Directing.Events.CoPlayTutorial_1();

        return true;
    }

    public void Refresh()
    {
        GetText((int)Texts.MainUIMapNameText).text = Managers.GetString(Managers.Data.StageInfoDic[Managers.Game.PlayerData.CurStageid].DungeonNameScriptID);
        GetText((int)Texts.PlayerLevelText).text = Managers.Game.PlayerData.Level.ToString();
        int level = Managers.Game.PlayerData.Level;
        Managers.Game.PlayerData.Level = Mathf.Max(level, 1);
        level = Mathf.Max(level, 1);
        Debug.Log($"{Managers.Game.PlayerData.CurExp} , {Managers.Data.PlayerDic[level + 1].NeedExp}");
        GetImage((int)Images.MainUIEXPGaugeImage).fillAmount = Managers.Game.PlayerData.CurExp / Managers.Data.PlayerDic[level + 1].NeedExp;
        float hpRatio = Managers.Game.PlayerData.CurHP / Managers.Game.PlayerData.MaxHP;
        //GetImage((int)Images.MainUIAuxiliaryHPGaugeImage).fillAmount = hpRatio;
        GameObject.Find("PlayerHPBarGauge").GetComponent<Image>().fillAmount = hpRatio;
        Managers.Game.KeyInventory.ShowKeySlot(Managers.Game.Player._keyInventory);
        SetPlayerInfo();
    }

    int _mask = (1 << (int)Define.Layer.Monster);

    private void Update()
    {
        if (!isOpenMenuPopup)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool raycastHit = Physics.Raycast(ray, out hit, 100.0f, _mask);
            Debug.DrawRay(Camera.main.transform.position, ray.direction * 100.0f, Color.red, 1.0f);

            if (raycastHit)
            {
                if (hit.collider.gameObject.layer == (int)Define.Layer.Monster)
                {
                    MonsterController monster = hit.collider.gameObject.GetComponent<MonsterController>();
                    int id = monster.id;
                    Debug.Log($"MonsterName : {Managers.Data.MonsterDic[id].Name}");
                    Debug.Log($"MonsterImage : {Managers.Data.MonsterDic[id].IdleAnimStr}");
                    Debug.Log($"MonsterImage : {Managers.Data.MonsterDic[id].IdleAnimStr}");

                    UI_MonsterInfo monsterInfo = Managers.UI.MakeSubItem<UI_MonsterInfo>(monster.transform);
                    monsterInfo.Position = Util.ScreenToWorldCood(Input.mousePosition);
                }
            }
        }

        #region for_test
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Managers.Game.PlayerData.CurExp += 10;
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Managers.Game.PlayerData.Attack += 10;
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Managers.Game.PlayerData.CurHP -= 10;
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Managers.Game.PlayerData.Attack -= 10;
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {
            switch (Managers.Game.PlayerData.MoveSpeed)
            {
                case 1f:
                    Managers.Game.PlayerData.MoveSpeed = 1.5f;
                    Managers.Game.Player.Speed = Managers.Game.PlayerData.MoveSpeed * 5;
                    break;
                case 1.5f:
                    Managers.Game.PlayerData.MoveSpeed = 2f;
                    Managers.Game.Player.Speed = Managers.Game.PlayerData.MoveSpeed * 5;
                    break;
                case 2f:
                    Managers.Game.PlayerData.MoveSpeed = 1f;
                    Managers.Game.Player.Speed = Managers.Game.PlayerData.MoveSpeed * 5;
                    break;
            }

            Debug.Log($"Managers.Game.CurPlayerData.MoveSpeed : {Managers.Game.PlayerData.MoveSpeed}");
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
        GetText((int)Texts.PlayerHPText).text = $"{Managers.Game.PlayerData.CurHP}";
        GetText((int)Texts.PlayerAttackText).text = $"{Managers.Game.PlayerData.Attack}";
        GetText((int)Texts.PlayerDefenseText).text = $"{Managers.Game.PlayerData.Defence}";
    }

    public void OnClickMainUIInventoryAImage()
    {
        if (GameObject.Find("UI_InvenPopup") == null)
            Managers.UI.ShowPopupUI<UI_InvenPopup>();
        else
            Managers.UI.ClosePopupUI();
    }

    public void OffUIInventory()
    {
        GetImage((int)Images.MainUIInventoryAImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(false);
    }

    public void OffUISword()
    {
        GetImage((int)Images.MainUISwordAImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(false);
    }

    public void OffUIWarp()
    {
        GetImage((int)Images.MainUIWarpAImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(false);
    }

    public void OffUI()
    {
        OffUIInventory();
        OffUISword();
        OffUIWarp();
    }

    public void OnUI()
    {
        GetImage((int)Images.MainUIOptionAImage).gameObject.SetActive(true);
        GetImage((int)Images.MainUIInventoryAImage).gameObject.SetActive(true);
        GetImage((int)Images.MainUISwordAImage).gameObject.SetActive(true);
        GetImage((int)Images.MainUIWarpAImage).gameObject.SetActive(true);
    }
}
