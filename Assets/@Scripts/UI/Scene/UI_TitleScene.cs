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
    bool _loading;
    bool _loadFailed;
    Define.ScriptType _language;

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
        if (Managers.Game.HasSave)
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
        if (_loading) return;
        _loading = true;
        // 재시도일 때만 실패 문구를 걷는다. 처음부터 끄면 PlayOneShot.Start 의
        // GameObject.Find 가 꺼진 것을 못 찾아, 칼 부딪히는 소리 이벤트가 널참조로 죽는다.
        if (_loadFailed) GetText((int)Texts.PessAnyKeyText).gameObject.SetActive(false);
        _loadFailed = false;
        isPreload = false;
        var slider = GetObject((int)Objects.Slider).GetComponent<Slider>();
        slider.gameObject.SetActive(true);
        slider.value = 0;
        GameObject.Find("MainTitle_BGAnim").GetComponent<Animator>().Play("WaitForOpening");
        Managers.Resource.LoadAllAsync<Object>("PreLoad", (key, count, total) => {
            if (this != null) slider.value = total > 0 ? (float)count / total : 0;
        }, error => {
            if (this == null) return;
            _loading = false;
            if (error != null) { LoadingFailed(error); return; }
            try
            {
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
                slider.gameObject.SetActive(false);
                GetButton((int)Buttons.NewGameButton).gameObject.SetActive(true);
                Managers.Cursor = GameObject.Find("@Cursor").GetOrAddComponent<CursorManager>();
                Managers.Cursor.Init();
                isPreload = true;
                _language = Managers.Game.ScriptType;
                // 켜는 것은 오프닝의 칼 부딪히는 순간(PlayOneShot)이다. 여기서 켜면 그 전에 넘길 수 있다.
                GetText((int)Texts.PessAnyKeyText).text = Managers.GetString(Define.TITLE_PRESS_KEY);
            }
            catch (System.Exception ex) { LoadingFailed(ex.ToString()); }
        });
    }

    void LoadingFailed(string detail)
    {
        Debug.LogError("[Title] " + detail);
        _loadFailed = true;
        GetObject((int)Objects.Slider).SetActive(false);
        var prompt = GetText((int)Texts.PessAnyKeyText);
        prompt.text = Managers.GetString(Define.TITLE_LOAD_FAILED);
        prompt.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (_lock) return;
        if (!isPreload)
        {
            if (_loadFailed && Input.GetKeyDown(KeyCode.Return)) Loading();
            if (_loadFailed && Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
            return;
        }
        if (_language != Managers.Game.ScriptType)
        {
            _language = Managers.Game.ScriptType;
            SetButtonColorAndButtonsText(buttonsIdx);
        }

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
                    if (Managers.Game.HasSave) // 최초가 아니면
                        OnClickLoadGameButton();
                    else
                        OnClickSettingButton();
                    break;
                case 2:
                    if (Managers.Game.HasSave) // 최초가 아니면
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
        if (_lock || !isPreload) yield break;
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
        SetPlayerInitSetting();
        Managers.Scene.LoadScene(Define.Scene.IntroScene);
    }

    void OnClickLoadGameButton()
    {
        Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UIselect");

        if (_isFirst)
        {
            Debug.Log("Cllck OnClickLoadGameButton Nut Data is Null");
            Managers.Scene.LoadScene(Define.Scene.IntroScene);
        }
        else
        {
            // 저장된 것을 <b>실제로 불러온 뒤에</b> 들어간다.
            //
            // 예전에는 씬만 바꿨다. 앱을 새로 켠 직후라면 GameManager.Init 이 이미
            // 불러 둬서 우연히 맞았지만, 같은 실행에서 새 게임을 한 번 누른 뒤라면
            // 그 초기화된 데이터가 그대로 딸려 들어갔다 — 이어하기인데 1층부터
            // 시작하고, 오브젝트 상태만 남아 먹은 아이템이 없는 맵을 돌게 된다.
            if (Managers.Game.LoadGame() == false)
            {
                Debug.LogWarning("[Title] 불러올 저장이 없다 — 새 게임으로 간다");
                StartCoroutine(CoOnClickNewGameButton());
                return;
            }

            Debug.Log($"[Title] 이어하기 — {Managers.Game.PlayerData.CurStageid + 1}층 Lv{Managers.Game.PlayerData.Level}");
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
        if (_isFirst) // 최초 실행 시
        {

            GetText((int)Texts.NewGameText).text = Managers.GetString(Define.TITLE_START);
            buttonsIdx = 0;
            SetButtonColorAndButtonsText(buttonsIdx);
        }
        else
        {
            GetText((int)Texts.NewGameText).text = Managers.GetString(Define.TITLE_NEW);
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

        if (_isFirst) // 최초 실행 시
        {
            GetText((int)Texts.NewGameText).text = Managers.GetString(Define.TITLE_START);
        }
        else
            GetText((int)Texts.NewGameText).text = Managers.GetString(Define.TITLE_NEW);
        GetText((int)Texts.LoadGameText).text = Managers.GetString(Define.TITLE_CONTINUE);
        GetText((int)Texts.SettingText).text = Managers.GetString(Define.SETTING);
        GetText((int)Texts.ExitText).text = Managers.GetString(Define.QUIT_GAME);
    }

    void SetButtonColorAndButtonsText(int index)
    {
        List<TMP_Text> texts = new List<TMP_Text>()
        {
            GetText((int)Texts.NewGameText), GetText((int)Texts.LoadGameText),
            GetText((int)Texts.SettingText), GetText((int)Texts.ExitText)
        };
        if (_isFirst)
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


