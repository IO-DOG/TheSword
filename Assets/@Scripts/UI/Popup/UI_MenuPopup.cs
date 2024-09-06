using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UI_MenuPopup : UI_Popup
{
    #region Enum
    enum GameObjects
    {
        Buttons,
    }

    enum Images
    {
        ContinueButton,
        ContinueButtonChoice,
        ContinueButtonSet,
        SettingButton,
        SettingButtonChoice,
        SettingButtonSet,
        SelectLanguageButton,
        SelectLanguageButtonChoice,
        SelectLanguageButtonSet,
        QuitGameButton,
        QuitGameButtonChoice,
        QuitGameButtonSet,
    }

    enum Texts
    {
        ContinueButtonText,
        SettingButtonText,
        SelectLanguageButtonText,
        QuitGameButtonText,
    }
    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindObject(typeof(GameObjects));
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        Managers.Game.playerControllLock = true;

        GetText((int)Texts.ContinueButtonText).text = Managers.GetString(Define.CONTINUE);
        GetText((int)Texts.SettingButtonText).text = Managers.GetString(Define.SETTING);
        GetText((int)Texts.SelectLanguageButtonText).text = Managers.GetString(Define.LANGUAGE);
        GetText((int)Texts.QuitGameButtonText).text = Managers.GetString(Define.QUIT_GAME);
        
        GetImage((int)Images.ContinueButton).gameObject.BindEvent(OnClickContinueGameButton);
        GetImage((int)Images.SettingButton).gameObject.BindEvent(OnClickSettingButton);
        GetImage((int)Images.SelectLanguageButton).gameObject.BindEvent(OnClickSelectLanguageButton);
        GetImage((int)Images.QuitGameButton).gameObject.BindEvent(() => { Application.Quit(); });
        OnEnterExitImage();

        CloseOtherUI();
        Refresh();
        GetImage((int)Images.ContinueButtonChoice).gameObject.SetActive(true);

        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OpenOtherUI();
            //Managers.UI.ClosePopupUI();
        }
    }

    void Refresh()
    {
        GetImage((int)Images.ContinueButtonChoice).gameObject.SetActive(false);
        GetImage((int)Images.ContinueButtonSet).gameObject.SetActive(false);
        GetImage((int)Images.SettingButtonChoice).gameObject.SetActive(false);
        GetImage((int)Images.SettingButtonSet).gameObject.SetActive(false);
        GetImage((int)Images.SelectLanguageButtonChoice).gameObject.SetActive(false);
        GetImage((int)Images.SelectLanguageButtonSet).gameObject.SetActive(false);
        GetImage((int)Images.QuitGameButtonChoice).gameObject.SetActive(false);
        GetImage((int)Images.QuitGameButtonSet).gameObject.SetActive(false);

    }

    void OnEnterExitImage()
    {
        GetImage((int)Images.ContinueButton).gameObject.BindEvent(() => {
            Refresh(); GetImage((int)Images.ContinueButtonChoice).gameObject.SetActive(true); 
        }, null, Define.UIEvent.PointerEnter);

        GetImage((int)Images.SettingButton).gameObject.BindEvent(() => {
            Refresh(); GetImage((int)Images.SettingButtonChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);

        GetImage((int)Images.SelectLanguageButton).gameObject.BindEvent(() => {
            Refresh(); GetImage((int)Images.SelectLanguageButtonChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);

        GetImage((int)Images.QuitGameButton).gameObject.BindEvent(() => {
            Refresh(); GetImage((int)Images.QuitGameButtonChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);


        GetImage((int)Images.ContinueButton).gameObject.BindEvent(() => {
            GetImage((int)Images.ContinueButtonChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.SettingButton).gameObject.BindEvent(() => {
            GetImage((int)Images.SettingButtonChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.SelectLanguageButton).gameObject.BindEvent(() => {
            GetImage((int)Images.SelectLanguageButtonChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.QuitGameButton).gameObject.BindEvent(() => {
            GetImage((int)Images.QuitGameButtonChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
    }

    void OnClickContinueGameButton()
    {
        {
            GameObject go = GameObject.Find("UI_SelectLanguagePopup");
            if (go != null)
                Managers.UI.ClosePopupUI();
        }

        {
            GameObject go = GameObject.Find("UI_SettingPopup");
            if (go != null)
                Managers.UI.ClosePopupUI();
        }

        OpenOtherUI();
        //Managers.UI.ClosePopupUI();
    }

    void OnClickSettingButton()
    {
        Refresh();
        GetImage((int)Images.SettingButtonSet).gameObject.SetActive(true);

        {
            GameObject go = GameObject.Find("UI_SelectLanguagePopup");
            if (go != null)
                Managers.UI.ClosePopupUI();
        }

        {
            GameObject go = GameObject.Find("UI_SettingPopup");
            if (go != null)
                Managers.UI.ClosePopupUI();
        }

        ButtonsMoveToLeft();
        Managers.UI.ShowPopupUI<UI_SettingPopup>();
    }

    void OnClickSelectLanguageButton()
    {
        Refresh();
        GetImage((int)Images.SelectLanguageButtonSet).gameObject.SetActive(true);

        {
            GameObject go = GameObject.Find("UI_SelectLanguagePopup");
            if (go != null)
                Managers.UI.ClosePopupUI();
        }

        {
            GameObject go = GameObject.Find("UI_SettingPopup");
            if (go != null)
                Managers.UI.ClosePopupUI();
        }

        ButtonsMoveToLeft();
        Managers.UI.ShowPopupUI<UI_SelectLanguagePopup>();
    }

    void ButtonsMoveToLeft()
    {
        //GetObject((int)GameObjects.Buttons).gameObject.transform.DOMoveX(700, 0.3f);
        GetImage((int)Images.ContinueButton).gameObject.transform.DOMoveX(700, 0.3f);

        DG.Tweening.Sequence mySequence = DOTween.Sequence()
        .Insert(0.1f, GetImage((int)Images.ContinueButton).gameObject.transform.DOMoveX(700, 0.3f))
        .Insert(0.12f, GetImage((int)Images.SettingButton).gameObject.transform.DOMoveX(700, 0.3f))
        .Insert(0.14f, GetImage((int)Images.SelectLanguageButton).gameObject.transform.DOMoveX(700, 0.3f))
        .Insert(0.16f, GetImage((int)Images.QuitGameButton).gameObject.transform.DOMoveX(700, 0.3f));

    }

    public GameObject KeyInventory = null;
    public GameObject PlayerInfo = null;
    public GameObject UIImages = null;
    public GameObject PlayConversation = null;

    public void CloseOtherUI()
    {
        //UI_GameScene uI_GameScene = Managers.Game.GameScene;
        //Transform[] child = uI_GameScene.gameObject.GetComponentsInChildren<Transform>();
        //for (int i = 1; i < child.Length; i++)
        //{
        //    if (child[i].name == "MainUIOptionAImage" || child[i].name == "MainUIOptionBImage")
        //        continue;

        //    child[i].gameObject.SetActive(false);
        //    Debug.Log($"child name : {child[i].name}");
        //}
        GameObject keyInventory = GameObject.Find("KeyInventory");
        if (keyInventory != null)
        {
            KeyInventory = keyInventory;
            keyInventory.SetActive(false);
        }
        GameObject playerInfo = GameObject.Find("PlayerInfo");
        if (playerInfo != null)
        {
            PlayerInfo = playerInfo;
            playerInfo.SetActive(false);
        }
        GameObject uIImages = GameObject.Find("UIImages");
        if (uIImages != null)
        {
            UIImages = uIImages;
            uIImages.SetActive(false);
        }
        GameObject playConversation = GameObject.Find("PlayConversation");
        if (playConversation != null)
        {
            PlayConversation = playConversation;
            playConversation.SetActive(false);
        }
    }

    public void OpenOtherUI()
    {
        //UI_GameScene uI_GameScene = Managers.Game.GameScene;
        //Transform[] child = uI_GameScene.gameObject.GetComponentsInChildren<Transform>();
        //for (int i = 1; i < child.Length; i++)
        //{
        //    if (child[i].name == "MainUIOptionAImage" || child[i].name == "MainUIOptionBImage")
        //        continue;

        //    child[i].gameObject.SetActive(true);
        //}

        //Managers.Game.GameScene.Init();

        if (KeyInventory != null)
            KeyInventory.SetActive(true);
        if (PlayerInfo != null)
            PlayerInfo.SetActive(true);
        if (UIImages != null)
            UIImages.SetActive(true);
        if (PlayConversation != null)
            PlayConversation.SetActive(true);
        Managers.UI.ClosePopupUI();
    }
}
