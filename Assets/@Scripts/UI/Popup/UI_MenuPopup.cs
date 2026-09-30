using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 메뉴. 게임에서는 Esc(또는 HUD 톱니)로, 타이틀에서는 "설정" 으로 연다.
///
/// 게임에서 열면 시간이 멈춘다(UIManager.RefreshTimeScale) — 전투 중에도 열 수 있다.
/// 게임에서는 계속 / 설정 / 언어 / 이 층 다시 시작 / 체크포인트 / 타이틀로 / 게임 종료,
/// 타이틀에서는 앞의 셋과 게임 종료만 있다. 게임에만 있는 줄은 프리팹을 고치지 않고
/// "게임 종료" 버튼을 복제해서 세운다 — 그래야 그림·글꼴·반짝임이 똑같다.
///
/// 키보드: W/S 또는 ↑/↓ 로 고르고 Enter/Space 로 누른다. Esc 는 한 칸 뒤로(체크포인트 목록 → 메뉴 → 닫기).
/// 마우스를 올리면 그 줄이 골라진다.
/// </summary>
public class UI_MenuPopup : UI_Popup
{
    #region Enum
    enum GameObjects
    {
        Buttons,
    }

    enum Images
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

    #region 문구 (Tools/ui_text_parts/ui.py)
    public const int MENU_RESTART_FLOOR = 180;
    public const int MENU_CHECKPOINTS = 181;
    public const int MENU_TO_TITLE = 182;
    public const int ASK_RESTART_FLOOR = 183;
    public const int ASK_TO_TITLE = 184;
    public const int ASK_QUIT = 185;
    public const int ASK_CHECKPOINT = 186;      // {0} 층
    public const int CHECKPOINT_ROW = 187;      // {0} 층, {1} 레벨, {2}/{3} HP
    public const int CHECKPOINT_NONE = 188;
    public const int CHECKPOINT_FAILED = 189;
    #endregion

    // 한 줄 = 버튼 그림 + 고른 표시(Choice) + 열어 둔 표시(Set) + 글자.
    class Row
    {
        public GameObject Button;
        public GameObject Choice;
        public GameObject Set;
        public TMP_Text Text;
        public int TextId = -1;     // 언어가 바뀌면 다시 칠한다. 체크포인트 줄은 -1
        public Action Press;
    }

    // 일곱 줄이 되면 프리팹 간격(100)으로는 화면 밖으로 넘친다.
    const float GameRowSpacing = 70f;
    const float ListRowScale = 1.5f;
    const float SlideTime = 0.3f;

    readonly List<Row> _main = new List<Row>();
    readonly List<Row> _listRows = new List<Row>();
    List<Row> _rows;                // 지금 키보드가 고르는 목록
    int _cursor;

    bool _inGame;
    bool _shifted;                  // 버튼이 왼쪽으로 비켜 있다 (설정·언어 창이나 체크포인트 목록)
    float _homeX;
    GameObject _list;               // 체크포인트 목록
    Row _setRow;                    // Set 이 켜진 줄
    Row _settingRow;
    Row _languageRow;
    Row _checkpointRow;             // 게임에서만 있다

    GameObject _keyInventory;
    GameObject _playerInfo;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindObject(typeof(GameObjects));
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        // Sound
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_SFX");

        _inGame = Managers.Game.GameScene != null;

        _settingRow = MainRow(Images.SettingButton, Images.SettingButtonChoice, Images.SettingButtonSet, Texts.SettingButtonText, Define.SETTING, OnClickSettingButton);
        _languageRow = MainRow(Images.SelectLanguageButton, Images.SelectLanguageButtonChoice, Images.SelectLanguageButtonSet, Texts.SelectLanguageButtonText, Define.LANGUAGE, OnClickSelectLanguageButton);
        Row quit = MainRow(Images.QuitGameButton, Images.QuitGameButtonChoice, Images.QuitGameButtonSet, Texts.QuitGameButtonText, Define.QUIT_GAME, OnClickQuitButton);
        _main.Add(MainRow(Images.ContinueButton, Images.ContinueButtonChoice, Images.ContinueButtonSet, Texts.ContinueButtonText, Define.CONTINUE, OnClickContinueGameButton));
        _main.Add(_settingRow);
        _main.Add(_languageRow);
        if (_inGame)
        {
            Transform buttons = GetObject((int)GameObjects.Buttons).transform;
            _checkpointRow = CloneRow(quit, "CheckpointButton", MENU_CHECKPOINTS, OnClickCheckpoints, buttons, 0f);
            Row[] extra =
            {
                CloneRow(quit, "RestartFloorButton", MENU_RESTART_FLOOR, OnClickRestartFloor, buttons, 0f),
                _checkpointRow,
                CloneRow(quit, "TitleButton", MENU_TO_TITLE, OnClickTitle, buttons, 0f),
            };
            foreach (Row row in extra)
            {
                row.Button.transform.SetSiblingIndex(quit.Button.transform.GetSiblingIndex());   // 게임 종료 바로 위
                _main.Add(row);
            }
            FitRows(_main.Count + 1);
        }
        _main.Add(quit);

        for (int i = 0; i < _main.Count; i++)
            BindRow(_main, i);
        Relabel();
        GameSettings.Changed += Relabel;

        HideHud();

        foreach (Row row in _main)
        {
            row.Choice.SetActive(false);
            row.Set.SetActive(false);
        }
        _rows = _main;
        Highlight(0);

        return true;
    }

    Row MainRow(Images button, Images choice, Images set, Texts text, int textId, Action press)
    {
        return new Row
        {
            Button = GetImage((int)button).gameObject,
            Choice = GetImage((int)choice).gameObject,
            Set = GetImage((int)set).gameObject,
            Text = GetText((int)text),
            TextId = textId,
            Press = press,
        };
    }

    // "게임 종료" 버튼을 통째로 복제한다. 글자는 줄마다 길이가 달라 칸에 맞춰 줄인다.
    // scale 이 0 이면 원본 크기 그대로. 이벤트 핸들러 컴포넌트가 같이 복제돼도 걸린 대리자는
    // 직렬화되지 않아 따라오지 않는다 — BindRow 가 새로 건다.
    Row CloneRow(Row source, string name, int textId, Action press, Transform parent, float scale)
    {
        GameObject go = Instantiate(source.Button, parent);
        go.name = name;
        go.SetActive(true);
        if (scale > 0f)
            go.transform.localScale = new Vector3(scale, scale, 1f);

        Row row = new Row
        {
            Button = go,
            Choice = go.transform.Find(source.Choice.name).gameObject,
            Set = go.transform.Find(source.Set.name).gameObject,
            Text = go.transform.Find(source.Text.name).GetComponent<TMP_Text>(),
            TextId = textId,
            Press = press,
        };
        row.Choice.name = name + "Choice";
        row.Set.name = name + "Set";
        row.Text.name = name + "Text";
        row.Choice.SetActive(false);
        row.Set.SetActive(false);
        row.Text.textWrappingMode = TextWrappingModes.NoWrap;
        row.Text.fontSizeMax = row.Text.fontSize;
        row.Text.fontSizeMin = 12f;
        row.Text.enableAutoSizing = true;
        return row;
    }

    // 간격을 줄이고, 줄 수가 달라진 만큼 위아래 가운데로 다시 맞춘다.
    // (프리팹의 네 줄·간격 100 에 이 식을 대면 원래 값 -200 에 가까운 -196 이 나온다)
    void FitRows(int count)
    {
        VerticalLayoutGroup layout = GetObject((int)GameObjects.Buttons).GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            return;

        float height = ((RectTransform)GetImage((int)Images.QuitGameButton).transform).rect.height;
        float box = ((RectTransform)layout.transform).rect.height;
        float firstCenter = (count - 1) * (height + GameRowSpacing) / 2f;
        layout.spacing = GameRowSpacing;
        layout.padding = new RectOffset(layout.padding.left, layout.padding.right,
            Mathf.RoundToInt(box / 2f - height / 2f - firstCenter), layout.padding.bottom);
    }

    void BindRow(List<Row> rows, int index)
    {
        Row row = rows[index];
        row.Button.BindEvent(() =>
        {
            if (_rows != rows)
                return;
            if (_cursor != index)
                Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_SFX");
            Highlight(index);
        }, type: Define.UIEvent.PointerEnter);
        row.Button.BindEvent(() => row.Choice.SetActive(false), type: Define.UIEvent.PointerExit);
        row.Button.BindEvent(() =>
        {
            if (_rows != rows)
                CloseSubPanels();   // 체크포인트 목록을 연 채 메뉴 줄을 눌렀다
            Highlight(index);
            row.Press();
        });
    }

    void Relabel()
    {
        foreach (Row row in _main)
        {
            if (row.TextId >= 0)
                row.Text.text = Managers.GetString(row.TextId);
        }
    }

    private void Update()
    {
        // 위에 다른 창(설정·언어·확인)이 떠 있으면 그 창의 차례다. 이번 프레임에 창이 닫혔으면
        // 그 Enter 는 이미 쓰였다 — 확인 창의 "아니오" 가 여기서 같은 질문을 다시 열었다.
        if (Managers.UI.TopPopup != this || Managers.UI.ClosedThisFrame)
            return;

        // 설정·언어 창이 제 손으로 닫혔다. 비켜 있던 버튼을 되돌린다.
        if (_shifted && _list == null)
            ShiftBack();

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            Move(-1);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            Move(1);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            _rows[_cursor].Press();
    }

    void Move(int delta)
    {
        int next = Mathf.Clamp(_cursor + delta, 0, _rows.Count - 1);
        if (next == _cursor)
            return;
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_SFX");
        Highlight(next);
    }

    void Highlight(int index)
    {
        _cursor = index;
        for (int i = 0; i < _rows.Count; i++)
            _rows[i].Choice.SetActive(i == index);
    }

    // 한 칸 뒤로. 곁창(설정·언어 창, 체크포인트 목록)이 열려 있으면 그것만 닫고 그 줄로 돌아오고,
    // 아니면 메뉴를 닫는다. 설정·언어 창이 Esc 를 쓰지 않으면 UIManager 가 여기로 넘겨준다.
    public override bool OnEscape()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Back_SFX");
        Row opener = _setRow;
        if (opener == null)
        {
            CloseMenu();
            return true;
        }
        CloseSubPanels();
        Highlight(_main.IndexOf(opener));
        return true;
    }

    #region 줄마다 하는 일
    public void OnClickContinueGameButton()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Click_SFX");
        CloseMenu();
    }

    void OnClickSettingButton()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Click_SFX");
        CloseSubPanels();
        MarkSet(_settingRow);
        ShiftLeft();
        UI_SettingPopup ui_SettingPopup = Managers.UI.ShowPopupUI<UI_SettingPopup>();
        ui_SettingPopup.menuPopup = this;
    }

    void OnClickSelectLanguageButton()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Click_SFX");
        CloseSubPanels();
        MarkSet(_languageRow);
        ShiftLeft();
        Managers.UI.ShowPopupUI<UI_SelectLanguagePopup>();
    }

    void OnClickRestartFloor()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Click_SFX");
        CloseSubPanels();
        UI_ConfirmPopup.AskDestructive(Managers.GetString(ASK_RESTART_FLOOR), () => Restart(null));
    }

    void OnClickCheckpoints()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Click_SFX");
        CloseSubPanels();

        List<SaveStore.CheckpointInfo> history = SaveStore.History(SaveStore.DirectoryPath);
        if (history.Count == 0)
        {
            UI_ConfirmPopup.Tell(Managers.GetString(CHECKPOINT_NONE));
            return;
        }
        OpenList(history);
    }

    void OnClickTitle()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Click_SFX");
        CloseSubPanels();
        UI_ConfirmPopup.AskDestructive(Managers.GetString(ASK_TO_TITLE), () =>
        {
            DropAbandonedBattle();
            Managers.Scene.LoadScene(Define.Scene.TitleScene);
        });
    }

    void OnClickQuitButton()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Click_SFX");
        CloseSubPanels();
        // 타이틀에서는 잃을 진행이 없다.
        if (_inGame == false)
        {
            Application.Quit();
            return;
        }
        UI_ConfirmPopup.AskDestructive(Managers.GetString(ASK_QUIT), Application.Quit);
    }

    /// <summary>체크포인트에서 다시 선다. file 이 null 이면 지금 층에 들어선 때(Checkpoint.json).</summary>
    void Restart(string file)
    {
        if (Managers.Game.RestartFromCheckpoint(file))
            DropAbandonedBattle();
        else
            UI_ConfirmPopup.Tell(Managers.GetString(CHECKPOINT_FAILED));
    }

    // 전투 한가운데서 씬을 다시 올리면 전투창은 씬과 함께 사라지는데, 전투 끝 알림(OnBattleAction)에
    // 걸어 둔 그 창의 BattleEnd 는 GameManager 에 남는다. 그러면 다음 전투가 끝날 때 부서진 창을
    // 먼저 부르다 예외로 멈춰, 새 전투창이 영영 닫히지 않았다. 씬을 떠나기로 정해진 뒤에만 걷는다.
    static void DropAbandonedBattle()
    {
        Managers.Game.OnBattleAction = null;
    }
    #endregion

    #region 체크포인트 목록
    void OpenList(List<SaveStore.CheckpointInfo> history)
    {
        MarkSet(_checkpointRow);
        ShiftLeft();

        _list = new GameObject("CheckpointList", typeof(RectTransform));
        _list.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)_list.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(100f, 100f);
        VerticalLayoutGroup layout = _list.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        layout.childScaleHeight = true;
        layout.spacing = 12f;
        // 목록 높이를 줄들에 맞춘다. 줄이 틀(100)보다 길면 레이아웃은 가운데 맞춤을 버리고 위끝부터 쌓아서,
        // 열 줄이 화면 가운데에서 시작해 아래로 넘쳤다(1080p 에서 일곱 줄만 보였다). 가운데를 기준으로 위아래로 편다.
        _list.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Row template = _main[_main.Count - 1];
        _listRows.Clear();
        foreach (SaveStore.CheckpointInfo info in history)
        {
            SaveStore.CheckpointInfo picked = info;
            Row row = CloneRow(template, $"Checkpoint_{info.Stage + 1:000}", -1, () => AskCheckpoint(picked), _list.transform, ListRowScale);
            row.Text.text = string.Format(Managers.GetString(CHECKPOINT_ROW), info.Stage + 1, info.Level, info.Hp, info.MaxHp)
                + "\n" + info.SavedAt.ToString("MM/dd HH:mm");
            _listRows.Add(row);
        }
        for (int i = 0; i < _listRows.Count; i++)
            BindRow(_listRows, i);

        // 설정 창이 들어오는 자리(화면 64.5%)로 오른쪽 밖에서 밀어 넣는다.
        _list.transform.position = new Vector3(Screen.width * 1.3f, Screen.height * 0.5f, 0f);
        _list.transform.DOMoveX(Screen.width * 0.645f, SlideTime).SetUpdate(true);

        _rows = _listRows;
        Highlight(0);
    }

    void AskCheckpoint(SaveStore.CheckpointInfo info)
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Click_SFX");
        UI_ConfirmPopup.AskDestructive(string.Format(Managers.GetString(ASK_CHECKPOINT), info.Stage + 1),
            () => Restart(info.File));
    }
    #endregion

    #region 버튼 자리
    void MarkSet(Row row)
    {
        if (_setRow != null)
            _setRow.Set.SetActive(false);
        _setRow = row;
        row.Set.SetActive(true);
    }

    // 설정·언어 창이나 체크포인트 목록이 들어올 자리를 비운다. 게임에서는 시간이 멈춰 있으니 트윈은 실시간으로 돈다.
    void ShiftLeft()
    {
        if (_shifted)
            return;
        // 제자리는 처음 비킬 때 한 번만 잰다. 레이아웃이 자리를 잡은 뒤(사람이 누른 뒤)다.
        if (_homeKnown == false)
        {
            _homeX = _main[0].Button.transform.position.x;
            _homeKnown = true;
        }
        _shifted = true;
        SlideRows(Screen.width * 0.365f);
    }
    bool _homeKnown;

    void ShiftBack()
    {
        if (_setRow != null)
            _setRow.Set.SetActive(false);
        _setRow = null;
        if (_shifted == false)
            return;
        _shifted = false;
        SlideRows(_homeX);
    }

    void SlideRows(float x)
    {
        Sequence seq = DOTween.Sequence().SetUpdate(true);
        for (int i = 0; i < _main.Count; i++)
        {
            _main[i].Button.transform.DOKill();   // 되돌아가던 중에 다시 비키면 두 트윈이 싸운다
            seq.Insert(0.1f + 0.02f * i, _main[i].Button.transform.DOMoveX(x, SlideTime));
        }
    }

    // 메뉴에서 연 곁창(설정·언어 창, 체크포인트 목록)을 닫고 버튼을 제자리로.
    // 곁창은 메뉴가 연 것이라 메뉴가 스택에서 걷는다 — 곁창의 닫는 길이 다시 메뉴를 부를 수 있어서다.
    void CloseSubPanels()
    {
        Managers.UI.ClosePopupUI(Managers.UI.FindPopup<UI_SettingPopup>());
        Managers.UI.ClosePopupUI(Managers.UI.FindPopup<UI_SelectLanguagePopup>());

        if (_list != null)
        {
            Destroy(_list);
            _list = null;
            _listRows.Clear();
            _rows = _main;
        }
        ShiftBack();
    }

    void CloseMenu()
    {
        CloseSubPanels();
        ClosePopupUI();
    }

    // 예전 이름. 쓰지 않는 옛 게임 씬(legacy_UI_GameScene)이 아직 부른다 — 메뉴를 닫고 HUD 를 되돌린다.
    public void OpenOtherUI() => CloseMenu();
    #endregion

    #region HUD
    // 메뉴가 떠 있는 동안 열쇠 칸과 능력치 줄을 가린다. 닫힐 때(어떤 길로든) OnDestroy 가 되돌린다.
    void HideHud()
    {
        _keyInventory = GameObject.Find("KeyInventory");
        if (_keyInventory != null)
            _keyInventory.SetActive(false);
        _playerInfo = GameObject.Find("PlayerInfo");
        if (_playerInfo != null)
            _playerInfo.SetActive(false);
    }

    void OnDestroy()
    {
        GameSettings.Changed -= Relabel;
        // 다시 시작·타이틀로 가면 씬이 내려가며 HUD 도 함께 파괴된다 — 그때 켜면 유니티가 오류를 찍는다.
        if (gameObject.scene.isLoaded == false)
            return;
        if (_keyInventory != null)
            _keyInventory.SetActive(true);
        if (_playerInfo != null)
            _playerInfo.SetActive(true);
    }
    #endregion
}
