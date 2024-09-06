using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_SettingPopup : UI_Popup
{
    #region Enums
    enum GameObjects
    {
        TotalSoundSlider,
        BGMSoundSlider,
        EffectSoundSlider,
        //SoundToggle,
    }

    enum Texts
    {
        SoundClassText,
        TotalSoundText,
        BGMSoundText,
        EffectSoundText,
        TotalSoundClassText,
        BGMSoundClassText,
        EffectSoundClassText,
        ScreenClassText,
        FullScreenText,
        WindowScreenText,
        FullWindowScreenText,
    }

    enum Images
    {
        BackgroundImage,
        FullScreenCheckBox,
        FullScreenChoice,
        FullScreenPick,
        WindowScreenCheckBox,
        WindowScreenChoice,
        WindowScreenPick,
        FullWindowScreenCheckBox,
        FullWindowScreenChoice,
        FullWindowScreenPick,
    }
    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindObject(typeof(GameObjects));
        BindText(typeof(Texts));
        BindImage(typeof(Images));
        #endregion

        GetText((int)Texts.SoundClassText).text = Managers.GetString(Define.SOUND);
        GetText((int)Texts.TotalSoundClassText).text = Managers.GetString(Define.TOTAL_SOUND);
        GetText((int)Texts.BGMSoundClassText).text = Managers.GetString(Define.BGM);
        GetText((int)Texts.EffectSoundClassText).text = Managers.GetString(Define.EFFECT);
        GetText((int)Texts.ScreenClassText).text = Managers.GetString(Define.SCREEN);
        GetText((int)Texts.FullScreenText).text = Managers.GetString(Define.FULL_SCREEN);
        GetText((int)Texts.WindowScreenText).text = Managers.GetString(Define.WINDOW_SCREEN);
        GetText((int)Texts.FullWindowScreenText).text = Managers.GetString(Define.FULL_WINDOW_SCREEN);

        //GetImage((int)Images.BackgroundImage).gameObject.transform.localScale = new Vector3(0, 0, 0);
        GetImage((int)Images.BackgroundImage).gameObject.transform.DOMoveX(1240, 0.2f);
        //GetImage((int)Images.BackgroundImage).gameObject.transform.DOScale(2, 0.2f);

        //GetObject((int)GameObjects.SoundToggle).gameObject.BindEvent(OnClickSoundToggle);
        GetObject((int)GameObjects.TotalSoundSlider).GetComponent<Slider>().value = PlayerPrefs.GetFloat("CURSOUND", 1);
        GetObject((int)GameObjects.BGMSoundSlider).GetComponent<Slider>().value = PlayerPrefs.GetFloat("CURBGMSOUND", 1);
        GetObject((int)GameObjects.EffectSoundSlider).GetComponent<Slider>().value = PlayerPrefs.GetFloat("CUREFFECTSOUND", 1);

        GetImage((int)Images.FullScreenCheckBox).gameObject.BindEvent(OnClickFullScreenCheckBox);
        GetImage((int)Images.WindowScreenCheckBox).gameObject.BindEvent(OnClickWindowScreenCheckBox);
        GetImage((int)Images.FullWindowScreenCheckBox).gameObject.BindEvent(OnClickFullWindowScreenCheckBox);

        OnEnterExitImage();

        Refresh();
        return true;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSoundText();

        if (PlayerPrefs.GetFloat("CURSOUND", 1) != GetObject((int)GameObjects.TotalSoundSlider).GetComponent<Slider>().value /*&& GetObject((int)GameObjects.SoundToggle).GetComponent<Toggle>().isOn == true*/)
        {
            PlayerPrefs.SetFloat("CURSOUND", GetObject((int)GameObjects.TotalSoundSlider).GetComponent<Slider>().value);
            PlayerPrefs.SetFloat("SAVESOUND", GetObject((int)GameObjects.TotalSoundSlider).GetComponent<Slider>().value);
            Managers.Sound.SetVolume(GetObject((int)GameObjects.TotalSoundSlider).GetComponent<Slider>().value);
        }

        if (PlayerPrefs.GetFloat("CURBGMSOUND", 1) != GetObject((int)GameObjects.BGMSoundSlider).GetComponent<Slider>().value /*&& GetObject((int)GameObjects.SoundToggle).GetComponent<Toggle>().isOn == true*/)
        {
            PlayerPrefs.SetFloat("CURBGMSOUND", GetObject((int)GameObjects.BGMSoundSlider).GetComponent<Slider>().value);
            PlayerPrefs.SetFloat("SAVEBGMSOUND", GetObject((int)GameObjects.BGMSoundSlider).GetComponent<Slider>().value);
            Managers.Sound.SetVolume(GetObject((int)GameObjects.BGMSoundSlider).GetComponent<Slider>().value);
        }

        if (PlayerPrefs.GetFloat("CUREFFECTSOUND", 1) != GetObject((int)GameObjects.EffectSoundSlider).GetComponent<Slider>().value /*&& GetObject((int)GameObjects.SoundToggle).GetComponent<Toggle>().isOn == true*/)
        {
            PlayerPrefs.SetFloat("CUREFFECTSOUND", GetObject((int)GameObjects.EffectSoundSlider).GetComponent<Slider>().value);
            PlayerPrefs.SetFloat("SAVEEFFECTSOUND", GetObject((int)GameObjects.EffectSoundSlider).GetComponent<Slider>().value);
            Managers.Sound.SetVolume(GetObject((int)GameObjects.EffectSoundSlider).GetComponent<Slider>().value);
        }
    }

    void Refresh()
    {
        GetImage((int)Images.FullScreenChoice).gameObject.SetActive(false);
        GetImage((int)Images.FullScreenPick).gameObject.SetActive(false);
        GetImage((int)Images.WindowScreenChoice).gameObject.SetActive(false);
        GetImage((int)Images.WindowScreenPick).gameObject.SetActive(false);
        GetImage((int)Images.FullWindowScreenChoice).gameObject.SetActive(false);
        GetImage((int)Images.FullWindowScreenPick).gameObject.SetActive(false);

        TurnOnCurScreenModeImage();
    }

    void OnEnterExitImage()
    {
        GetImage((int)Images.FullScreenCheckBox).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.FullScreenChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.WindowScreenCheckBox).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.WindowScreenChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.FullWindowScreenCheckBox).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.FullWindowScreenChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);

        GetImage((int)Images.FullScreenCheckBox).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.FullScreenChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.WindowScreenCheckBox).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.WindowScreenChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.FullWindowScreenCheckBox).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.FullWindowScreenChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
    }

    void UpdateSoundText()
    {
        GetText((int)Texts.TotalSoundText).text = $"{(int)(GetObject((int)GameObjects.TotalSoundSlider).GetComponent<Slider>().value * 100)}%";
        GetText((int)Texts.BGMSoundText).text = $"{(int)(GetObject((int)GameObjects.BGMSoundSlider).GetComponent<Slider>().value * 100)}%";
        GetText((int)Texts.EffectSoundText).text = $"{(int)(GetObject((int)GameObjects.EffectSoundSlider).GetComponent<Slider>().value * 100)}%";
    }

    void TurnOnCurScreenModeImage()
    {
        Define.ScreenType curScreenType = Managers.Game.ScreenType;

        switch (curScreenType)
        {
            case Define.ScreenType.Full:
                GetImage((int)Images.FullScreenPick).gameObject.SetActive(true);
                break;
            case Define.ScreenType.Window:
                GetImage((int)Images.WindowScreenPick).gameObject.SetActive(true);
                break;
            case Define.ScreenType.FullWindow:
                GetImage((int)Images.FullWindowScreenPick).gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }

    void OnClickFullScreenCheckBox()
    {
        Managers.Game.ScreenType = Define.ScreenType.Full;

        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);

        Refresh();
    }

    void OnClickWindowScreenCheckBox()
    {
        Managers.Game.ScreenType = Define.ScreenType.Window;

        Screen.SetResolution(960, 540, FullScreenMode.Windowed);

        Refresh();
    }

    void OnClickFullWindowScreenCheckBox()
    {
        Managers.Game.ScreenType = Define.ScreenType.FullWindow;

        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);

        Refresh();
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
