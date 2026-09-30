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

    // 새 게임이 저장을 지운다는 확인 (Tools/ui_text_parts/ui.py)
    public const int ASK_NEW_GAME = 190;

    bool isPreload = false;
    int buttonsIdx = 0;
    int maxButtonCount = 4;
    bool _lock = false;
    bool _isFirst = false;
    bool _loading;
    bool _loadFailed;
    Define.ScriptType _language;
    CanvasGroup _buttons;       // 타이틀 메뉴 글자 묶음. 창이 떠 있는 동안 가린다

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
        PlacePrompt();
        StyleButtons();

        //GetObject((int)Objects.Slider).GetComponent<Slider>().value = 0;
        GetObject((int)Objects.Slider).GetComponent<Slider>().gameObject.SetActive(false);

        GetButton((int)Buttons.NewGameButton).gameObject.BindEvent(() => { buttonsIdx = 0; SetButtonColorAndButtonsText(buttonsIdx); OnClickNewGameButton(); });
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

    /// <summary>
    /// "아무 키나 누르세요" 는 프리팹에서 화면 가운데 조금 아래(-200)라 빛나는 칼날 위에 얹혀 칼이 글자를 갈랐다.
    /// 칼끝 아래 화면 아래쪽으로 내리고 검은 테두리를 둘러 풀빛 위에서도 읽히게 한다. 깜빡임(애니메이터)은 색만
    /// 만지고 켜기(PlayOneShot)는 SetActive 만 하니 자리·재질은 그대로 남는다.
    /// 불러오기·이어하기 실패 문구도 이 칸을 쓴다 — 한 줄로는 화면 밖으로 잘려서, 넓게 두고 넘치면 접고 줄인다.
    /// </summary>
    void PlacePrompt()
    {
        TMP_Text prompt = GetText((int)Texts.PessAnyKeyText);
        RectTransform rt = prompt.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 130f);    // 캔버스 높이 1080 에서 칼끝(아래에서 약 210) 밑
        rt.sizeDelta = new Vector2(1700f, 160f);
        prompt.fontSharedMaterial = CodeUI.Outlined(prompt.font, 0.4f);   // 글꼴 여백(5)이 얇아 0.4 라야 2px 남짓
        prompt.textWrappingMode = TextWrappingModes.Normal;
        prompt.fontSizeMax = prompt.fontSize;
        prompt.fontSizeMin = 36f;
        prompt.enableAutoSizing = true;
    }

    /// <summary>
    /// 메뉴 글자는 빛나는 칼날 한가운데에 선다. 안 고른 줄(회색)이 칼날 빛에 묻혀 읽히지 않았다 — 같은 테두리를 두른다.
    /// "설정" 만 프리팹에서 작게(35.7) 잡혀 있어 한 줄만 작아 보였다. 첫 줄 크기로 맞춘다.
    /// </summary>
    void StyleButtons()
    {
        _buttons = GetImage((int)Images.Buttons).gameObject.GetOrAddComponent<CanvasGroup>();
        TMP_Text first = GetText((int)Texts.NewGameText);
        foreach (Texts t in new[] { Texts.NewGameText, Texts.LoadGameText, Texts.SettingText, Texts.ExitText })
        {
            TMP_Text label = GetText((int)t);
            label.fontSharedMaterial = CodeUI.Outlined(label.font, 0.3f);
            label.fontSize = first.fontSize;
        }
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
        // 창이 떠 있는 동안 타이틀 메뉴 글자를 가린다. 설정 메뉴에는 어둡게 덮는 판이 없어서, 그 단추 사이로
        // "- 이어하기 -" 가 비쳐 보였다. 켜고 끄기(SetActive)는 "아무 키나" 가 맡으니 투명도만 만진다.
        _buttons.alpha = Managers.UI.GetPopupCount() > 0 ? 0f : 1f;

        // 창(설정 메뉴·확인 창)이 떠 있으면 타이틀 키는 쉰다 — 설정 창 밑에서 Enter 가 "새 게임" 을 눌러
        // 저장을 지운 적이 있다. 창을 닫은 그 Enter 도 같은 프레임에 여기서 다시 먹히면 안 된다.
        // Esc 는 맨 위 창에 넘긴다(메뉴·확인 창이 제 닫는 길로 닫힌다).
        if (Managers.UI.GetPopupCount() > 0 || Managers.UI.ClosedThisFrame)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Managers.UI.EscapeTopPopup();
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
                    OnClickNewGameButton();
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
            // 이 Enter 가 아래 "아무 키나" 에 한 번 더 먹히면, 방금 띄운 이어하기 실패 문구가 곧바로 닫힌다.
            return;
        }

        if (isPreload && Input.anyKeyDown && GetText((int)Texts.PessAnyKeyText).gameObject.activeSelf /*&& !Input.GetKeyDown(KeyCode.Return)*/ && !Input.GetKeyDown(KeyCode.UpArrow) && !Input.GetKeyDown(KeyCode.DownArrow))
        {
            GetText((int)Texts.PessAnyKeyText).gameObject.SetActive(false);
            GetImage((int)Images.Buttons).gameObject.SetActive(true);
            ButtonsSetting();
            CheckFirstGame();
        }

        // 치트 키는 에디터와 개발 빌드에서만.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
#endif
    }

    // 새 게임은 저장을 지운다. 저장이 있으면 먼저 묻는다 — 처음 골라 둔 쪽은 "아니오".
    // (자동 플레이는 CoOnClickNewGameButton 을 곧장 부른다)
    void OnClickNewGameButton()
    {
        if (_lock || !isPreload)
            return;
        if (Managers.Game.HasSave == false)
        {
            StartCoroutine(CoOnClickNewGameButton());
            return;
        }
        Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UIselect");
        UI_ConfirmPopup.AskDestructive(Managers.GetString(ASK_NEW_GAME), () => StartCoroutine(CoOnClickNewGameButton()));
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
                // 저장은 지우지 않는다. 예전에는 여기서 새 게임으로 넘어가 DeleteGameData 가 저장을 지웠다 —
                // 층 데이터를 다시 뽑기만 해도(내용 패치) 해시가 달라져 모두의 저장이 그렇게 사라졌다.
                // 알리고 타이틀에 머문다. 아무 키나 누르면 버튼으로 돌아간다.
                Debug.LogWarning($"[Title] 이어하기 실패 — 저장은 그대로 둔다: {Managers.Game.LastSaveError}");
                GetImage((int)Images.Buttons).gameObject.SetActive(false);
                TMP_Text notice = GetText((int)Texts.PessAnyKeyText);
                notice.text = Managers.GetString(Define.TITLE_SAVE_FAILED);
                notice.gameObject.SetActive(true);
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


