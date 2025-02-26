using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
    bool _lock = false;
    bool _isFirst = false;

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

        //GetObject((int)Objects.Slider).GetComponent<Slider>().value = 0;
        GetObject((int)Objects.Slider).GetComponent<Slider>().gameObject.SetActive(false);

        GetButton((int)Buttons.NewGameButton).gameObject.BindEvent(() => { buttonsIdx = 0; SetButtonColorAndButtonsText(buttonsIdx); StartCoroutine(CoOnClickNewGameButton()); });
        if (PlayerPrefs.GetInt("ISFIRST", 1) != 1)
        {
            GetButton((int)Buttons.LoadGameButton).gameObject.BindEvent(() => { buttonsIdx = 1; SetButtonColorAndButtonsText(buttonsIdx); OnClickLoadGameButton(); });
        }
        else
        {
            GetButton((int)Buttons.LoadGameButton).gameObject.SetActive(false);
            _isFirst = true;
            maxButtonCount = 3;
        }
        GetButton((int)Buttons.SettingButton).gameObject.BindEvent(() => { buttonsIdx = _isFirst ? 1 : 2; SetButtonColorAndButtonsText(buttonsIdx); OnClickSettingButton(); });
        GetButton((int)Buttons.ExitButton).gameObject.BindEvent(() => { buttonsIdx = _isFirst ? 2 : 3; SetButtonColorAndButtonsText(buttonsIdx); OnClickExitButton(); });

        GetImage((int)Images.Buttons).gameObject.SetActive(false);
        GetButton((int)Buttons.NewGameButton).gameObject.SetActive(false);

        Loading();

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
                if (!PlayerPrefs.HasKey("CURSOUND")) PlayerPrefs.SetFloat("CURSOUND", 1);
                if (!PlayerPrefs.HasKey("SAVESOUND")) PlayerPrefs.SetFloat("SAVESOUND", 1);
                if (!PlayerPrefs.HasKey("CURBGMSOUND")) PlayerPrefs.SetFloat("CURBGMSOUND", 1);
                if (!PlayerPrefs.HasKey("CUREFFECTSOUND")) PlayerPrefs.SetFloat("CUREFFECTSOUND", 1);
                Managers.Sound.SetBGMVolume(PlayerPrefs.GetFloat("CURBGMSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1));
                Managers.Sound.SetEffectVolume(PlayerPrefs.GetFloat("CUREFFECTSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1));

                GameObject.Find("MainTitle_BGAnim").GetComponent<Animator>().Play("TitleOpeningAnimation");
                GetObject((int)Objects.Slider).gameObject.SetActive(false);
                GetButton((int)Buttons.NewGameButton).gameObject.SetActive(true);

                // cursor 시작
                Managers.Cursor = GameObject.Find("@Cursor").GetOrAddComponent<CursorManager>();
                Managers.Cursor.Init();
                // continueData로 플레이어 적용시키기. TODO
            }
        });
    }

    private void Update()
    {
        if (_lock)
            return;

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            if (buttonsIdx != (maxButtonCount - 1)) Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UImove");
            buttonsIdx++;
            buttonsIdx = Mathf.Min(buttonsIdx, maxButtonCount - 1);
            SetButtonColorAndButtonsText(buttonsIdx);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            if (buttonsIdx != 0) Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UImove");
            buttonsIdx--;
            buttonsIdx = Mathf.Max(buttonsIdx, 0);
            SetButtonColorAndButtonsText(buttonsIdx);
        }

        if (Input.GetKeyDown(KeyCode.Return) && !GetText((int)Texts.PessAnyKeyText).gameObject.activeSelf)
        {
            switch (buttonsIdx)
            {
                case 0:
                    StartCoroutine(CoOnClickNewGameButton());
                    //OnClickNewGameButton();
                    break;
                case 1:
                    if (PlayerPrefs.GetInt("ISFIRST", 1) != 1) // 최초가 아니면
                        OnClickLoadGameButton();
                    else
                        OnClickSettingButton();
                    break;
                case 2:
                    if (PlayerPrefs.GetInt("ISFIRST", 1) != 1) // 최초가 아니면
                        OnClickSettingButton();
                    else
                        OnClickExitButton();
                    break;
                case 3:
                    OnClickExitButton();
                    break;
                default:
                    break;
            }
        }

        if (isPreload && Input.anyKeyDown && GetText((int)Texts.PessAnyKeyText).gameObject.activeSelf /*&& !Input.GetKeyDown(KeyCode.Return)*/ && !Input.GetKeyDown(KeyCode.UpArrow) && !Input.GetKeyDown(KeyCode.DownArrow))
        {
            GetText((int)Texts.PessAnyKeyText).gameObject.SetActive(false);
            GetImage((int)Images.Buttons).gameObject.SetActive(true);
            ButtonsSetting();
            CheckFirstGame();
        }

        #region ForTest

        if (Input.GetKeyDown(KeyCode.F8))
        {
            Managers.Game.PlayerData.CurSword = 9;
            Managers.Game.PlayerData.CurShield = 0;
            Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Clear();
            //anagers.GamerPlayerData.Inventory[(int)Define.Types.Sword].Add(9);
            Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Add(10);
            Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Add(11);
            Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Add(12);
            Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Add(13);
            Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Add(14);
            Managers.Game.PlayerData.CurSword = 10;
        }
        #endregion
    }

    IEnumerator CoOnClickNewGameButton()
    {
        _lock = true;

        Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UIselect");

        GetImage((int)Images.BlackBGImage).gameObject.SetActive(true);
        GetImage((int)Images.BlackBGImage).color = new Color(1, 1, 1, 0);
        StartCoroutine(Util.CoFade(GetImage((int)Images.BlackBGImage), 3));
        Managers.Sound.FadeAndStopBGM(3f);
        yield return new WaitForSeconds(3f);

        //test
        Managers.Game.PlayerData.Ability = (int)Define.Trait.None;
        Debug.Log("Cllck OnClickNewGameButton");
        Managers.Game.DeleteGameData();
        Managers.Data.Init();
        SetPlayerInitSetting();
        Managers.Scene.LoadScene(Define.Scene.IntroScene);
    }

    void OnClickLoadGameButton()
    {
        Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UIselect");

        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
        {
            Debug.Log("Cllck OnClickLoadGameButton Nut Data is Null");
            Managers.Scene.LoadScene(Define.Scene.IntroScene);
        }
        else
        {
            Debug.Log("Cllck OnClickLoadGameButton");
            Managers.Scene.LoadScene(Define.Scene.GameScene);
        }
    }

    void OnClickSettingButton()
    {
        Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UIselect");

        Debug.Log("Cllck OnClickSettingButton");
        Managers.UI.ShowPopupUI<UI_MenuPopup>();
    }

    void OnClickExitButton()
    {
        Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UIselect");

        Debug.Log("Cllck OnClickExitButton");
        Application.Quit();
    }

    void CheckFirstGame()
    {
        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1) // 최초 실행 시
        {
            SetPlayerInitSetting();

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
            SetPlayerInitSetting();
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
        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
        {
            texts.Remove(GetText((int)Texts.LoadGameText));
        }

        ButtonsSetting();
        texts[index].color = new Color(1, 1, 1);
        string str = texts[index].text;
        texts[index].text = $"- {str} -";
    }

    void SetPlayerInitSetting()
    {
        PlayerPrefs.SetInt("ISOPENINVENUI", 0);
        PlayerPrefs.SetInt("ISOPENWARPUI", 0);
        PlayerPrefs.SetInt("ISOPENCLASSUI", 0);
        Managers.Game.PlayerData.CurSword = Define.NOT_EQUIP;
        Managers.Game.SwapEquip(Define.EQUIP_SOWRD_FIRST);
        //Managers.Game.PlayerData.CurSword = Define.EQUIP_SOWRD_FIRST;
        Managers.Game.PlayerData.CurShield = Define.NOT_EQUIP;
        Managers.Game.PlayerData.CurNecklace = Define.NOT_EQUIP;
        Managers.Game.PlayerData.CurRing = Define.NOT_EQUIP;
        Managers.Game.PlayerData.CurShoes = Define.NOT_EQUIP;
        Managers.Game.PlayerData.CurBook = Define.NOT_EQUIP;
    }
}


