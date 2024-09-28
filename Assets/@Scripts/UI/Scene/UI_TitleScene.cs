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
        MainTitle_Text,
        BlackBGImage
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

        GetImage((int)Images.BlackBGImage).gameObject.SetActive(false);
        Loading();

        //GetObject((int)Objects.Slider).GetComponent<Slider>().value = 0;
        GetObject((int)Objects.Slider).GetComponent<Slider>().gameObject.SetActive(false);
        // 테스트용
        GetButton((int)Buttons.NewGameButton).gameObject.BindEvent(() =>
        {
            if (isPreload)
                Managers.Scene.LoadScene(Define.Scene.GameScene, transform);
        });
        GetImage((int)Images.Buttons).gameObject.SetActive(false);
        GetButton((int)Buttons.NewGameButton).gameObject.SetActive(false);

        //GetButton((int)Buttons.GameSpeedButton).gameObject.BindEvent(() => { // 게임 속도 조절
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

    }

    void Loading()
    {
        GameObject.Find("MainTitle_BGAnim").GetComponent<Animator>().Play("WaitForOpening");

        Managers.Resource.LoadAllAsync<Object>("PreLoad", (key, count, totalCount) =>
        {
            GetObject((int)Objects.Slider).GetComponent<Slider>().value = (float)count / totalCount;
            if (count == totalCount)
            {
                isPreload = true;

                Managers.Data.Init();
                Managers.Game.Init();
                Managers.Sound.Init();
                Managers.Sound.Play(Define.Sound.Bgm, "MainTitle_BGM");
                Managers.Sound.SetVolume(PlayerPrefs.GetFloat("CURSOUND", 1));
                Managers.Sound.SetVolume(PlayerPrefs.GetFloat("CURBGMSOUND", 1));
                Managers.Sound.SetVolume(PlayerPrefs.GetFloat("CUREFFECTSOUND", 1));

                GameObject.Find("MainTitle_BGAnim").GetComponent<Animator>().Play("TitleOpeningAnimation");
                GetObject((int)Objects.Slider).gameObject.SetActive(false);
                GetButton((int)Buttons.NewGameButton).gameObject.SetActive(true);

                // cursor 시작
                GameObject.Find("@Cursor").GetOrAddComponent<CursorManager>().Init();
                // continueData로 플레이어 적용시키기. TODO
            }
        });
    }

    private void Update()
    {
        if (Input.anyKeyDown && GetText((int)Texts.PessAnyKeyText).gameObject.activeSelf && !Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.UpArrow) && !Input.GetKeyDown(KeyCode.DownArrow))
        {
            GetText((int)Texts.PessAnyKeyText).gameObject.SetActive(false);
            GetImage((int)Images.Buttons).gameObject.SetActive(true);
            ButtonsSetting();
            CheckFirstGame();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (buttonsIdx != (maxButtonCount - 1)) Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UImove");
            buttonsIdx++;
            buttonsIdx = Mathf.Min(buttonsIdx, maxButtonCount - 1);
            SetButtonColorAndButtonsText(buttonsIdx);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (buttonsIdx != 0) Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UImove");
            buttonsIdx--;
            buttonsIdx = Mathf.Max(buttonsIdx, 0);
            SetButtonColorAndButtonsText(buttonsIdx);
        }

        if (Input.GetKeyDown(KeyCode.Return) && !GetText((int)Texts.PessAnyKeyText).gameObject.activeSelf)
        {
            Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UIselect");

            switch (buttonsIdx)
            {
                case 0:
                    StartCoroutine(CoFadeOutImage());
                    //OnClickNewGameButton();
                    break;
                case 1:
                    OnClickLoadGameButton();
                    break;
                case 2:
                    OnClickSettingButton();
                    break;
                case 3:
                    OnClickExitButton();
                    break;
                default:
                    break;
            }
        }

        #region ForTest

        if (Input.GetKeyDown(KeyCode.F8))
        {
            Managers.Game.CurPlayerData.CurSword = 9;
            Managers.Game.CurPlayerData.CurShield = 0;
            Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword].Clear();
            //anagers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword].Add(9);
            Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword].Add(10);
            Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword].Add(11);
            Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword].Add(12);
            Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword].Add(13);
            Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword].Add(14);
            Managers.Game.CurPlayerData.CurSword = 10;
        }
        #endregion
    }

    IEnumerator CoFadeOutImage()
    {
        GetImage((int)Images.BlackBGImage).gameObject.SetActive(true);

        GetImage((int)Images.BlackBGImage).color = new Color(0, 0, 0, 0);

        float tick = 0;
        while (tick < 1)
        {
            GetImage((int)Images.BlackBGImage).color += new Color(0, 0, 0, +0.1f);
            tick += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.5f);

        OnClickNewGameButton();
    }

    void OnClickNewGameButton()
    {
        Debug.Log("Cllck OnClickNewGameButton");
        Managers.Game.DeleteGameData();
        Managers.Data.Init();

        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1) // 진짜 처음일 떄
        {
            // todo intro story scene
            SetPlayerprefs();
            Managers.Scene.LoadScene(Define.Scene.IntroScene);
        }
        else
        {
            // todo skip intro story scene
            Managers.Scene.LoadScene(Define.Scene.IntroScene);
        }
    }

    void OnClickLoadGameButton()
    {
        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
        {
            Debug.Log("Cllck OnClickLoadGameButton Nut Data is Null");
            Managers.Scene.LoadScene(Define.Scene.IntroScene);
        }
        else
        {
            Debug.Log("Cllck OnClickLoadGameButton");
            Managers.Scene.LoadScene(Define.Scene.TutorialScene);
        }
    }

    void OnClickSettingButton()
    {
        Debug.Log("Cllck OnClickSettingButton");
        Managers.UI.ShowPopupUI<UI_MenuPopup>();
    }

    void OnClickExitButton()
    {
        Debug.Log("Cllck OnClickExitButton");
        Application.Quit();
    }

    void CheckFirstGame()
    {
        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1) // 최초 실행 시
        {
            SetPlayerprefs();

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

        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1) // 최초 실행 시
        {
            GetText((int)Texts.NewGameText).text = "Game Start";
            SetPlayerprefs();
        }
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

    void SetPlayerprefs()
    {
        PlayerPrefs.SetInt("ISOPENINVENUI", 0);
        PlayerPrefs.SetInt("ISOPENWARPUI", 0);
        PlayerPrefs.SetInt("ISOPENCLASSUI", 0);
    }
}
