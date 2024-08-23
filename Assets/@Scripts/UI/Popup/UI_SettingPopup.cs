using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_SettingPopup : UI_Popup
{
    #region Enums
    enum GameObjects
    {
        SoundSlider,
        //SoundToggle,
    }
    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindObject(typeof(GameObjects));
        #endregion

        //GetObject((int)GameObjects.SoundToggle).gameObject.BindEvent(OnClickSoundToggle);
        GetObject((int)GameObjects.SoundSlider).GetComponent<Slider>().value = PlayerPrefs.GetFloat("CURSOUND", 1);

        return true;
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerPrefs.GetFloat("CURSOUND", 1) != GetObject((int)GameObjects.SoundSlider).GetComponent<Slider>().value /*&& GetObject((int)GameObjects.SoundToggle).GetComponent<Toggle>().isOn == true*/)
        {
            PlayerPrefs.SetFloat("CURSOUND", GetObject((int)GameObjects.SoundSlider).GetComponent<Slider>().value);
            PlayerPrefs.SetFloat("SAVESOUND", GetObject((int)GameObjects.SoundSlider).GetComponent<Slider>().value);
            Managers.Sound.SetVolume(GetObject((int)GameObjects.SoundSlider).GetComponent<Slider>().value);
        }
    }

    //void OnClickSoundToggle()
    //{
    //    if (GetObject((int)GameObjects.SoundToggle).GetComponent<Toggle>().isOn == true)
    //    {
    //        Managers.Sound.SetVolume(PlayerPrefs.GetFloat("SAVESOUND", 1));
    //    }
    //    else
    //    {
    //        Managers.Sound.SetVolume(0);
    //    }
    //}
}
