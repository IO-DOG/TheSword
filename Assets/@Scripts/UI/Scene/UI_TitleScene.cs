using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_TitleScene : UI_Scene
{
    #region Enum
    enum Images
    {
        Buttons,
    }

    enum Buttons
    {
        NewGameButton,
        LoadGameButton,
        SettingButton,
        //GameSpeedButton,
        ExitButton,
    }

    enum Texts
    {
        PessAnyKeyText,
        NewGameText,
        LoadGameText,
        SettingText,
        ExitText,
    }

    enum Objects
    {
        Slider,
    }
    #endregion

    bool isPreload = false;
    int buttonsIdx = 0;
    int maxButtonCount = 4;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindButton(typeof(Buttons));
        BindText(typeof(Texts));
        BindObject(typeof(Objects));
        #endregion

        GetObject((int)Objects.Slider).GetComponent<Slider>().value = 0;
        // 테스트용
        GetButton((int)Buttons.NewGameButton).gameObject.BindEvent(() =>
        {
            if (isPreload)
                Managers.Scene.LoadScene(Define.Scene.GameScene, transform);
        });
        GetImage((int)Images.Buttons).gameObject.SetActive(false);
        GetButton((int)Buttons.NewGameButton).gameObject.SetActive(false);
        //GetButton((int)Buttons.GameSpeedButton).gameObject.BindEvent(() => {
        //    if (Managers.Game.GameSpeed == 1)
        //        Managers.Game.GameSpeed = 2;
        //    else if (Managers.Game.GameSpeed == 2)
        //        Managers.Game.GameSpeed = 4;
        //    else
        //        Managers.Game.GameSpeed = 1;
        //});

        return true;
    }

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        Managers.Resource.LoadAllAsync<Object>("PreLoad", (key, count, totalCount) =>
        {
            GetObject((int)Objects.Slider).GetComponent<Slider>().value = (float)count / totalCount;
            if (count == totalCount)
            {
                isPreload = true;
                GetObject((int)Objects.Slider).gameObject.SetActive(false);
                GetButton((int)Buttons.NewGameButton).gameObject.SetActive(true);
                Managers.Data.Init();
                Managers.Game.Init();
                // continueData로 플레이어 적용시키기. TODO
            }
        });
    }

    private void Update()
    {
        if (Input.anyKeyDown && GetText((int)Texts.PessAnyKeyText).gameObject.activeSelf)
        {
            GetText((int)Texts.PessAnyKeyText).gameObject.SetActive(false);
            GetImage((int)Images.Buttons).gameObject.SetActive(true);
            ButtonsSetting();
            CheckFirstGame();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            buttonsIdx++;
            buttonsIdx = Mathf.Min(buttonsIdx, maxButtonCount - 1);
            SetButtonColorAndButtonsText(buttonsIdx);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            buttonsIdx--;
            buttonsIdx = Mathf.Max(buttonsIdx, 0);
            SetButtonColorAndButtonsText(buttonsIdx);
        }
    }

    void OnClickNewGameButton()
    {

    }

    void OnClickLoadGameButton()
    {

    }

    void OnClickSettingButton()
    {

    }

    void OnClickExitButton()
    {

    }

    void CheckFirstGame()
    {
        if (PlayerPrefs.GetInt("ISFIRST") == 1) // 최초 실행 시
        {
            GetText((int)Texts.NewGameText).text = "Game Start";
            buttonsIdx = 0;
            SetButtonColorAndButtonsText(buttonsIdx);
        }
        else
        {
            GetText((int)Texts.NewGameText).text = "New Game";
            buttonsIdx = 1;
            SetButtonColorAndButtonsText(buttonsIdx);
        }
    }

    void ButtonsSetting()
    {
        GetText((int)Texts.NewGameText).color = new Color(0.5f, 0.5f, 0.5f);
        GetText((int)Texts.LoadGameText).color = new Color(0.5f, 0.5f, 0.5f);
        GetText((int)Texts.SettingText).color = new Color(0.5f, 0.5f, 0.5f);
        GetText((int)Texts.ExitText).color = new Color(0.5f, 0.5f, 0.5f);

        if (PlayerPrefs.GetInt("ISFIRST") == 1) // 최초 실행 시
            GetText((int)Texts.NewGameText).text = "Game Start";
        else
            GetText((int)Texts.NewGameText).text = "New Game";
        GetText((int)Texts.LoadGameText).text = "Load Game";
        GetText((int)Texts.SettingText).text = "Setting";
        GetText((int)Texts.ExitText).text = "Exit";
    }

    void SetButtonColorAndButtonsText(int index)
    {
        List<TMP_Text> texts = new List<TMP_Text>()
        {
            GetText((int)Texts.NewGameText), GetText((int)Texts.LoadGameText),
            GetText((int)Texts.SettingText), GetText((int)Texts.ExitText)
        };

        ButtonsSetting();
        texts[index].color = new Color(1, 1, 1);
        string str = texts[index].text;
        texts[index].text = $"- {str} -";
    }
}
