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
    // 본 결말 수 "결말 {0}/{1}" (Tools/ui_text_parts/records.py)
    public const int ENDINGS_SEEN = 440;
    // 새 게임의 규칙 창(UI_ModePopup)과 규칙마다 최고 점수 (Tools/ui_text_parts/mode.py, 530~559)
    public const int MODE_TITLE = 530;
    public const int MODE_NORMAL = 531;
    public const int MODE_NORMAL_DESC = 532;
    public const int MODE_TOWER = 533;
    public const int MODE_TOWER_DESC = 534;
    public const int MODE_CONTROLS = 535;
    public const int BEST_SCORE = 536;      // "{0} 최고 {1}" — 규칙 이름, 점수(낮을수록 좋다)
    public const int MODE_CLEARED = 537;    // 탑의 법으로 결말까지 갔다

    bool isPreload = false;
    int buttonsIdx = 0;
    int maxButtonCount = 4;
    bool _lock = false;
    bool _isFirst = false;
    bool _loading;
    bool _loadFailed;
    int _loadRun;               // 지금 기다리는 불러오기 — 다시 부른 뒤 늦게 온 앞 요청의 콜백은 버린다
    float _loadAt;              // 그 불러오기를 시작한 때 (실시간)
    const float StuckSeconds = 10f;
    Define.ScriptType _language;
    CanvasGroup _buttons;       // 타이틀 메뉴 글자 묶음. 창이 떠 있는 동안 가린다
    CanvasGroup _logo;          // 로고. 마찬가지
    Image _notice;              // 이어하기 실패 알림 — 메뉴 자리의 어두운 띠(ShowNotice)
    TMP_Text _noticeText;
    TMP_Text _records;          // 본 결말 수·규칙마다 최고 점수 (Records) — 메뉴 띠 오른쪽 끝
    // 새 게임의 규칙. 규칙 창이 정하고, CoOnClickNewGameButton 이 판을 새로 세운 뒤에 적는다. 봇은 창을 거치지 않고
    // 코루틴을 곧장 부르므로 보통이다 — 탑의 법으로 돌리려면 부르기 전에 이 칸을 채운다(AutoPlayer 는 리플렉션을 쓴다).
    GameMode _mode = GameMode.Normal;

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
    /// 불러오기 실패 문구도 이 칸을 쓴다 — 한 줄로는 화면 밖으로 잘려서, 넓게 두고 넘치면 접고 줄인다.
    /// (이어하기 실패는 제 칸을 쓴다 — ShowNotice)
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
        _logo = GetImage((int)Images.MainTitle_Text).gameObject.GetOrAddComponent<CanvasGroup>();
        TMP_Text first = GetText((int)Texts.NewGameText);
        foreach (Texts t in new[] { Texts.NewGameText, Texts.LoadGameText, Texts.SettingText, Texts.ExitText })
        {
            TMP_Text label = GetText((int)t);
            label.fontSharedMaterial = CodeUI.Outlined(label.font, 0.3f);
            label.fontSize = first.fontSize;
        }

        // 본 결말 수와 규칙마다 최고 점수(ButtonsSetting). 띠의 자식이라 메뉴와 함께 켜지고 창이 뜨면 같이 가려진다
        // (_buttons.alpha). 세로 줄 배치에는 끼지 않고 띠 오른쪽 끝, 칼날 빛에서 먼 어두운 잎 위에 선다 — 가운데 아래
        // (칼끝 풀빛)는 화면에서 가장 밝다. 세 줄까지 띠 높이(250) 안에 든다. 넘치면 글자를 줄인다.
        _records = CodeUI.Fit(CodeUI.NewText(_buttons.transform, "Records", first.font, 36f, new Color(0.8f, 0.8f, 0.8f), TextAlignmentOptions.Right), 20f);
        _records.fontSharedMaterial = CodeUI.Outlined(first.font, 0.3f);
        _records.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        CodeUI.Place(_records.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-60f, 0f), new Vector2(560f, 240f));
        _records.gameObject.SetActive(false);
    }

    private void Start()
    {

    }

    void Loading()
    {
        if (_loading) return;
        _loading = true;
        int run = ++_loadRun;
        _loadAt = Time.realtimeSinceStartup;
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
            if (this != null && run == _loadRun) slider.value = total > 0 ? (float)count / total : 0;
        }, error => {
            if (this == null || run != _loadRun) return;
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
            // 도메인 리로드를 끈 에디터 플레이에서 첫 "PreLoad" 목록 요청의 답이 영영 안 올 때가 있다(0% 에서 멈춘다 — 어드레서블이
            // 플레이 사이에 다시 서며 그 요청을 버리는 것으로 보인다). 다시 부르면 6~10초에 끝난다. 하나도 못 받은 채 오래면 다시 부른다.
            // 빌드에서는 본 적이 없다. 에셋이 하나라도 오면(막대가 움직이면) 걸리지 않고, 늦게 온 앞 요청은 _loadRun 이 버린다.
            if (_loading && Time.realtimeSinceStartup - _loadAt > StuckSeconds
                && GetObject((int)Objects.Slider).GetComponent<Slider>().value <= 0f)
            {
                Debug.LogWarning("[Title] 불러오기가 0% 에서 멈췄다 — 다시 부른다");
                _loading = false;
                Loading();
            }
            if (_loadFailed && Input.GetKeyDown(KeyCode.Return)) Loading();
            if (_loadFailed && Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
            return;
        }
        // 창이 떠 있는 동안 타이틀 메뉴 글자를 가린다. 설정 메뉴에는 어둡게 덮는 판이 없어서, 그 단추 사이로
        // "- 이어하기 -" 가 비쳐 보였다. 켜고 끄기(SetActive)는 "아무 키나" 가 맡으니 투명도만 만진다.
        // 로고도 가린다 — 설정 판(1.3배, 화면 높이의 83%)이 로고 오른쪽 절반을 덮고 메뉴 첫 단추가 로고 아랫단에
        // 걸렸다. 판을 로고 밑으로 옮길 자리는 없다(판을 도로 줄여야 한다).
        _buttons.alpha = _logo.alpha = Managers.UI.GetPopupCount() > 0 ? 0f : 1f;

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

        // 방향키·Enter 는 메뉴가 서 있을 때만 받는다. 불러오기가 끝나고 칼이 부딪혀 "아무 키나" 가 뜨기까지(1초 남짓)는
        // 메뉴도 문구도 없어 Waiting 이 거짓이라, Enter 가 보이지 않는 "새 게임" 을 눌렀다 — 저장이 없으면 곧장 시작했고
        // 있으면 오프닝 위에 확인 창이 떴다.
        bool menu = GetImage((int)Images.Buttons).gameObject.activeSelf && !Waiting;

        if (menu && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))
        {
            if (buttonsIdx != (maxButtonCount - 1)) Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UImove");
            buttonsIdx++;
            buttonsIdx = Mathf.Min(buttonsIdx, maxButtonCount - 1);
            SetButtonColorAndButtonsText(buttonsIdx);
        }
        if (menu && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)))
        {
            if (buttonsIdx != 0) Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UImove");
            buttonsIdx--;
            buttonsIdx = Mathf.Max(buttonsIdx, 0);
            SetButtonColorAndButtonsText(buttonsIdx);
        }

        if (menu && Input.GetKeyDown(KeyCode.Return))
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

        if (isPreload && Input.anyKeyDown && Waiting /*&& !Input.GetKeyDown(KeyCode.Return)*/ && !Input.GetKeyDown(KeyCode.UpArrow) && !Input.GetKeyDown(KeyCode.DownArrow))
        {
            GetText((int)Texts.PessAnyKeyText).gameObject.SetActive(false);
            if (_notice != null)
                _notice.gameObject.SetActive(false);
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

    // 새 게임은 먼저 규칙(보통·탑의 법)을 묻는다 — Esc 는 타이틀로 물러난다. 탑의 법 표가 없으면(잘못 구운 빌드)
    // 물을 것이 없어 보통으로 간다. (자동 플레이는 CoOnClickNewGameButton 을 곧장 부른다 — 보통)
    void OnClickNewGameButton()
    {
        if (_lock || !isPreload)
            return;
        if (Managers.Data.HasMonsterTable(GameMode.Tower) == false)
        {
            OnPickMode(GameMode.Normal);
            return;
        }
        Managers.Sound.Play(Define.Sound.Effect, "MainTitle_UIselect");
        UI_ModePopup.Ask(OnPickMode);
    }

    // 규칙을 골랐다. 새 게임은 저장을 지운다 — 저장이 있으면 마지막에 묻는다. 처음 골라 둔 쪽은 "아니오".
    void OnPickMode(GameMode mode)
    {
        _mode = mode;
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
        Managers.Game.SetMode(GameEvents.IsAutoPlaying ? AutoPlayer.StartMode : _mode);   // 판을 새로 세운 뒤(Clear 가 보통으로 되돌린 뒤)에 적는다 — 몬스터 표도 같이 바뀐다
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
                ShowNotice(Managers.GetString(Define.TITLE_SAVE_FAILED));
                return;
            }

            Debug.Log($"[Title] 이어하기 — {Managers.Game.PlayerData.CurStageid + 1}층 Lv{Managers.Game.PlayerData.Level}");
            Managers.Scene.LoadScene(Define.Scene.GameScene);
        }
    }

    /// <summary>
    /// 이어하기를 거절한 까닭. 예전에는 "아무 키나" 칸을 빌려 그 깜빡임(색 0.08~0.96)을 같이 탔고, 칼끝 풀빛(화면에서
    /// 가장 밝은 곳) 위에 섰다 — 영어는 "return." 만 둘째 줄로 떨어졌다. 메뉴가 서던 어두운 띠(Buttons 와 같은 자리·색)에
    /// 가만히 세우고, 줄 길이가 고르게 접히게 폭을 정한다. 아무 키나 누르면 걷히고 메뉴로 돌아간다(Update).
    /// </summary>
    void ShowNotice(string message)
    {
        const float MaxWidth = 1100f, Pad = 24f;
        if (_notice == null)
        {
            Image band = GetImage((int)Images.Buttons);
            _notice = CodeUI.NewImage(transform, "Notice", null, band.color);
            _notice.rectTransform.anchorMin = band.rectTransform.anchorMin;
            _notice.rectTransform.anchorMax = band.rectTransform.anchorMax;
            _notice.rectTransform.anchoredPosition = band.rectTransform.anchoredPosition;
            TMP_FontAsset font = GetText((int)Texts.PessAnyKeyText).font;
            _noticeText = CodeUI.NewText(_notice.transform, "Text", font, 52f, Color.white, TextAlignmentOptions.Center);
            _noticeText.fontSharedMaterial = CodeUI.Outlined(font, 0.4f);
            _noticeText.textWrappingMode = TextWrappingModes.Normal;
        }

        // 한 줄 폭을 줄 수로 나눈다. TMP 는 앞줄부터 꽉 채워 끝줄에 낱말 하나만 남기곤 한다. 여유(글자 두 개)는
        // 줄 끝에 걸린 낱말이 한 줄을 더 만들지 않게.
        float line = _noticeText.GetPreferredValues(message).x;
        float width = Mathf.Min(MaxWidth, line / Mathf.Max(1f, Mathf.Ceil(line / MaxWidth)) + _noticeText.fontSize * 2f);
        float height = _noticeText.GetPreferredValues(message, width, 0f).y;
        _noticeText.text = message;
        CodeUI.Place(_noticeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
        _notice.rectTransform.sizeDelta = new Vector2(0f, height + Pad * 2f);
        _notice.gameObject.SetActive(true);
    }

    // "아무 키나" 나 이어하기 실패 알림이 떠 있다 — 아무 키나 누르면 걷히고 메뉴가 나온다.
    bool Waiting => GetText((int)Texts.PessAnyKeyText).gameObject.activeSelf || (_notice != null && _notice.gameObject.activeSelf);

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

        // 판을 넘어 남는 기록: 본 결말 수, 규칙마다 최고 점수(낮을수록 좋다, 없으면 그 줄을 뺀다), 탑의 법을 끝까지 갔다는 표시.
        // 하나라도 있어야 뜬다 — 처음 켠 사람에게 "0/3" 은 숙제일 뿐이다. 언어가 바뀌어도 여기로 온다.
        List<string> lines = new List<string>();
        int seen = Records.EndingsSeen;
        if (seen > 0)
            lines.Add(string.Format(Managers.GetString(ENDINGS_SEEN), seen, Records.EndingCount));
        foreach (GameMode mode in new[] { GameMode.Normal, GameMode.Tower })
        {
            int best = Records.BestScore(mode);
            bool cleared = mode == GameMode.Tower && Records.ModeCleared(mode);
            if (best <= 0 && cleared == false)
                continue;
            string name = Managers.GetString(mode == GameMode.Tower ? MODE_TOWER : MODE_NORMAL);
            string line = best > 0 ? string.Format(Managers.GetString(BEST_SCORE), name, best) : name;
            lines.Add(cleared ? $"{line}  <color=#F0D28A>{Managers.GetString(MODE_CLEARED)}</color>" : line);
        }
        _records.gameObject.SetActive(lines.Count > 0);
        _records.text = string.Join("\n", lines);
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


