using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class legacy_UI_GameScene : UI_Scene
{
    #region Enum
    enum Buttons
    {
        //ToTitleButton,
        PlayConversation,
    }

    enum GameObjects
    {
        KeyInventory,
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
        MainUIInventoryAImage,
        MainUIInventoryBImage,
        MainUISwordAImage,
        MainUISwordBImage,
        MainUIWarpAImage,
        MainUIWarpBImage,
    }

    #endregion

    int _mask = (1 << (int)Define.Layer.Monster);

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindButton(typeof(Buttons));
        BindObject(typeof(GameObjects));
        BindText(typeof(Texts));
        BindImage(typeof(Images));
        #endregion

        Managers.Game.Player._keyInventory = GetObject((int)GameObjects.KeyInventory);

        //GetButton((int)Buttons.ToTitleButton).gameObject.BindEvent(() => Managers.Scene.LoadScene(Define.Scene.TitleScene));

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

        GetImage((int)Images.MainUIOptionBImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(false);

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
                Managers.UI.ShowPopupUI<UI_MenuPopup>();
            else
                go.GetComponent<UI_MenuPopup>().OpenOtherUI();
        });
        GetImage((int)Images.MainUIInventoryAImage).gameObject.BindEvent(OnClickMainUIInventoryAImage);

        GetButton((int)Buttons.PlayConversation).gameObject.BindEvent(() =>
        {
            if (!Managers.Game.OnBattle)
            {
                Managers.UI.ShowPopupUI<UI_ConversationPopup>();
                Managers.Game.CurEventID= Define.EVENT_SWORD_FIRST;
            }
        });

        Managers.Game.PlayerData.CurStageid = 0;
        Managers.Game.GenerateMap(Managers.Game.PlayerData.CurStageid);

        SetPlayerInfo();
        Refresh();

        if (PlayerPrefs.GetInt("ISOPENSWORD") == 0)
            GetImage((int)Images.MainUISwordAImage).gameObject.SetActive(false);
        if (PlayerPrefs.GetInt("ISOPENPORTAL") == 0)
            GetImage((int)Images.MainUIWarpAImage).gameObject.SetActive(false);

        FadeEffect(Define.FadeEvent.FadnIn, Define.FADE_DURATION);
        FadeEffect(Define.FadeEvent.CenterToRight, Define.FADE_DURATION);

        return true;
    }

    public void Refresh()
    {
        if (PlayerPrefs.GetInt("ISOPENINVENUI") == 1) // 인벤 활성화 x
            GetImage((int)Images.MainUIInventoryAImage).gameObject.SetActive(true);
        if (PlayerPrefs.GetInt("ISOPENWARPUI") == 1)
            GetImage((int)Images.MainUIWarpAImage).gameObject.SetActive(true);
        if (PlayerPrefs.GetInt("ISOPENCLASSUI") == 1)
            GetImage((int)Images.MainUISwordAImage).gameObject.SetActive(true);

        GetText((int)Texts.PlayerLevelText).text = Managers.Game.PlayerData.Level.ToString();
        int level = Managers.Game.PlayerData.Level;
        Debug.Log($"{Managers.Game.PlayerData.CurExp} , {Managers.Data.PlayerDic[level].NeedExp}");
        GetImage((int)Images.MainUIEXPGaugeImage).fillAmount = Managers.Game.PlayerData.CurExp / Managers.Data.PlayerDic[level].NeedExp;
        GetImage((int)Images.MainUIAuxiliaryHPGaugeImage).fillAmount = Managers.Game.PlayerData.CurHP / Managers.Game.PlayerData.MaxHP;
        Managers.Game.KeyInventory.ShowKeySlot(Managers.Game.Player._keyInventory);
        SetPlayerInfo();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
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

#if UNITY_EDITOR
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
            Managers.Game.SaveGame();
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Managers.Game.LoadGame();
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Managers.Game.PlayerData.Attack -= 10;
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            //Managers.Game.PlayerData.CurSword = 9;
            Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Clear();
            Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Add(9);
            Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Add(10);

            for (int i = 0; i < Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Count; i++)
            {
                Debug.Log(Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword][i] + "Inventory");
                Debug.Log(Managers.Game.PlayerData.CurSword + "CurPlayerSword");
            }

        }
        #endregion
#endif
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
