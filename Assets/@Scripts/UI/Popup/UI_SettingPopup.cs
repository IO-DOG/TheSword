using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_SettingPopup : UI_Popup
{
    #region Enum
    enum GameObjects
    {
        SoundSlider,
        SoundToggle,
    }

    enum Buttons
    {
        SelectLanguageButton,
    }
    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region
        BindButton(typeof(Buttons));
        BindObject(typeof(GameObjects));
        #endregion

        Managers.Game.playerControllLock = true;

        GetObject((int)GameObjects.SoundToggle).gameObject.BindEvent(OnClickSoundToggle);
        GetButton((int)Buttons.SelectLanguageButton).gameObject.BindEvent(OnClickSelectLanguageButton);
        GetObject((int)GameObjects.SoundSlider).GetComponent<Slider>().value = PlayerPrefs.GetFloat("CURSOUND", 1);
        return true;
    }

    private void Update()
    {
        if (PlayerPrefs.GetFloat("CURSOUND", 1) != GetObject((int)GameObjects.SoundSlider).GetComponent<Slider>().value &&
            GetObject((int)GameObjects.SoundToggle).GetComponent<Toggle>().isOn == true)
        {
            PlayerPrefs.SetFloat("CURSOUND", GetObject((int)GameObjects.SoundSlider).GetComponent<Slider>().value);
            PlayerPrefs.SetFloat("SAVESOUND", GetObject((int)GameObjects.SoundSlider).GetComponent<Slider>().value);
            Managers.Sound.SetVolume(GetObject((int)GameObjects.SoundSlider).GetComponent<Slider>().value);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePopupUI();
        }
    }

    void OnClickSoundToggle()
    {
        if (GetObject((int)GameObjects.SoundToggle).GetComponent<Toggle>().isOn == true)
        {
            Managers.Sound.SetVolume(PlayerPrefs.GetFloat("SAVESOUND", 1));
        }
        else
        {
            Managers.Sound.SetVolume(0);
        }
    }

    void OnClickSelectLanguageButton()
    {
        Managers.UI.ShowPopupUI<UI_SelectLanguagePopup>();
    }
}
