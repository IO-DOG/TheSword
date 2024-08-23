using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_MenuPopup : UI_Popup
{
    #region Enum
    enum Buttons
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
        BindButton(typeof(Buttons));
        BindText(typeof(Texts));
        #endregion

        Managers.Game.playerControllLock = true;

        GetButton((int)Buttons.SelectLanguageButton).gameObject.BindEvent(OnClickSelectLanguageButton);
        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePopupUI();
        }
    }



    void OnClickSelectLanguageButton()
    {
        Managers.UI.ShowPopupUI<UI_SelectLanguagePopup>();
    }
}
