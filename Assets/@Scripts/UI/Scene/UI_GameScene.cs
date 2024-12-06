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
        LetterBoxTop,
        LetterBoxBottom,
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


        Managers.Game.PlayerData.CurSword = Define.EQUIP_SOWRD_FIRST;
        Managers.Game.PlayerData.CurShield = 0;

        Managers.Game.Player._keyInventory = GetObject((int)GameObjects.KeyInventory);
        GetObject((int)GameObjects.GreenKey).SetActive(false);
        GetObject((int)GameObjects.YellowKey).SetActive(false);
        GetObject((int)GameObjects.RedKey).SetActive(false);

        #region Letter Box
        GetImage((int)Images.LetterBoxTop).GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Define.LETTER_BOX_HEIGHT);
        GetImage((int)Images.LetterBoxBottom).GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Define.LETTER_BOX_HEIGHT);
        GetImage((int)Images.LetterBoxTop).GetComponent<RectTransform>().position = Util.WorldToScreenCood(new Vector3(0f, Screen.height / 2 + Define.LETTER_BOX_HEIGHT, 0f));
        GetImage((int)Images.LetterBoxBottom).GetComponent<RectTransform>().position = Util.WorldToScreenCood(new Vector3(0f, -Screen.height / 2 - Define.LETTER_BOX_HEIGHT, 0f));
        GetImage((int)Images.LetterBoxTop).gameObject.SetActive(false);
        GetImage((int)Images.LetterBoxBottom).gameObject.SetActive(false);

        #endregion

        GetImage((int)Images.LetterBoxTop).gameObject.SetActive(false);
        GetImage((int)Images.LetterBoxBottom).gameObject.SetActive(false);


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

    int _mask = (1 << (int)Define.Layer.Monster | 1 << (int)Define.Layer.CItem);

    private void Update()
    {
        ShowCItemInfo();
        ShowMonsterInfo();

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
            if (monsters != null) monsters.gameObject.SetActive(false);
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

    void ShowMonsterInfo()
    {
        if (isOpenMenuPopup)
            return;

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

    void ShowCItemInfo()
    {
        if (isOpenMenuPopup)
            return;

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f, _mask);

        if (raycastHit)
        {
            Debug.Log(hit.collider.gameObject.layer);
            if (hit.collider.gameObject.layer == (int)Define.Layer.CItem)
            {
                ConsumableItem cItem = hit.collider.gameObject.GetComponent<ConsumableItem>();
                int id = cItem.id;
                Debug.Log($"cItem : {Managers.GetString(Managers.Data.ConsumableItemDic[id].ScriptNameId)}");

                UI_CItemInfo cItemInfo = Managers.UI.MakeSubItem<UI_CItemInfo>(cItem.transform);
                cItemInfo.Position = Util.ScreenToWorldCood(Input.mousePosition);
            }
        }
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

    public void StartLetterBox()
    {
        GetImage((int)Images.LetterBoxTop).gameObject.SetActive(true);
        GetImage((int)Images.LetterBoxBottom).gameObject.SetActive(true);
        GetImage((int)Images.LetterBoxTop).GetComponent<RectTransform>().DOMove(Util.WorldToScreenCood(new Vector3(0f, Screen.height / 2, 0f)), 1f);
        GetImage((int)Images.LetterBoxBottom).GetComponent<RectTransform>().DOMove(Util.WorldToScreenCood(new Vector3(0f, -Screen.height / 2, 0f)), 1f);
    }

    public void StopLetterBox()
    {
        Tween twe1 = GetImage((int)Images.LetterBoxTop).GetComponent<RectTransform>().DOMove(Util.WorldToScreenCood(new Vector3(0f, Screen.height / 2 + Define.LETTER_BOX_HEIGHT, 0f)), 1f);
        Tween twe2 = GetImage((int)Images.LetterBoxBottom).GetComponent<RectTransform>().DOMove(Util.WorldToScreenCood(new Vector3(0f, -Screen.height / 2 - Define.LETTER_BOX_HEIGHT, 0f)), 1f);

        Sequence seq = DOTween.Sequence();
        seq.Append(twe1);
        seq.Join(twe2).OnComplete(() =>
        {
            GetImage((int)Images.LetterBoxTop).gameObject.SetActive(false);
            GetImage((int)Images.LetterBoxBottom).gameObject.SetActive(false);
        });
    }
}
