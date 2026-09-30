using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GameManager;

public class UIManager
{
    const int PopupBaseOrder = 10;
    int _order = PopupBaseOrder;
    int _toastOrder = 500;

    // 떠 있는 팝업. 마지막이 맨 위다. (스택이던 것을 목록으로 바꿨다 — 맨 위가 아닌 창도 닫아야 한다)
    readonly List<UI_Popup> _popups = new List<UI_Popup>();
    Stack<UI_Toast> _toastStack = new Stack<UI_Toast>();
    UI_Scene _sceneUI = null;
    public UI_Scene SceneUI { get { return _sceneUI; } }

    public UI_StageNamePopup StageNamePopup;
    public UI_BossNamePopup BossNamePopup;
    public UI_GameScene UI_GameScene;

    /// <summary>게임 안에서 메뉴가 떠 있어 시간이 멈췄다. 플레이어는 이때 움직이지 않는다.</summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// 이번 프레임에 창이 닫혔다. 창을 닫은 Enter 가 같은 프레임에 그 밑(메뉴·타이틀)에서 한 번 더
    /// 먹히지 않게 쓴다 — 확인 창에서 "아니오" 를 누르면 밑의 메뉴가 같은 Enter 로 같은 질문을 다시 열었다.
    /// </summary>
    public bool ClosedThisFrame => _closedFrame == Time.frameCount;
    int _closedFrame = -1;

    public GameObject Root
    {
        get
        {
            GameObject root = GameObject.Find("@UI_Root");
            if (root == null)
                root = new GameObject { name = "@UI_Root" };
            return root;
        }
    }

    public void SetCanvas(GameObject go, bool sort = true, int sortOrder = 0, bool isToast = false)
    {
        Canvas canvas = Util.GetOrAddComponent<Canvas>(go);
        if (canvas == null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
        }

        CanvasScaler cs = go.GetOrAddComponent<CanvasScaler>();
        if (cs != null)
        {
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
        }

        go.GetOrAddComponent<GraphicRaycaster>();

        if (sort)
        {
            canvas.sortingOrder = _order;
            _order++;
        }
        else
        {
            canvas.sortingOrder = sortOrder;
        }

        if (isToast)
        {
            _toastOrder++;
            canvas.sortingOrder = _toastOrder;
        }

    }

    public T MakeWorldSpaceUI<T>(Transform parent = null, string name = null) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"{name}");
        if (parent != null)
            go.transform.SetParent(parent);

        Canvas canvas = go.GetOrAddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        return Util.GetOrAddComponent<T>(go);
    }

    public T MakeSubItem<T>(Transform parent = null, string name = null, bool pooling = true) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"{name}", parent, pooling);
        go.transform.SetParent(parent);
        return Util.GetOrAddComponent<T>(go);
    }

    public T ShowSceneUI<T>(string name = null) where T : UI_Scene
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"{name}");
        T sceneUI = Util.GetOrAddComponent<T>(go);
        _sceneUI = sceneUI;

        go.transform.SetParent(Root.transform);

        return sceneUI;
    }

    public T ShowPopupUI<T>(string name = null) where T : UI_Popup
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"{name}");
        T popup = Util.GetOrAddComponent<T>(go);
        PushPopup(popup);

        return popup;
    }

    /// <summary>
    /// 이미 만들어 둔 창을 스택에 올린다 — 프리팹 없이 코드로 세운 창이 쓴다.
    /// 정렬 순서는 창의 Init(UI_Popup.Init → SetCanvas)이 받으므로, 창은 base.Init 을 불러야 한다.
    /// </summary>
    public void PushPopup(UI_Popup popup)
    {
        if (popup == null || IndexOf(popup) >= 0)
            return;

        _popups.Add(popup);
        popup.transform.SetParent(Root.transform);
        RefreshTimeScale();
    }

    /// <summary>맨 위 창. 없으면 null.</summary>
    public UI_Popup TopPopup
    {
        get
        {
            Prune();
            return _popups.Count > 0 ? _popups[_popups.Count - 1] : null;
        }
    }

    /// <summary>떠 있는 창 중 그 종류의 가장 위의 것. 없으면 null.</summary>
    public T FindPopup<T>() where T : UI_Popup
    {
        Prune();
        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            if (_popups[i] is T found)
                return found;
        }
        return null;
    }

    /// <summary>
    /// Esc 를 맨 위 창에 묻는다. 씬 UI(UI_GameScene · UI_TitleScene)만 부른다 — Esc 를 받는 곳은 씬마다
    /// 하나다. 창이 처리했으면(닫았거나 삼켰으면) true.
    /// </summary>
    public bool EscapeTopPopup()
    {
        UI_Popup top = TopPopup;
        if (top == null)
            return false;
        if (top.OnEscape())
            return true;

        // 메뉴에서 연 곁창(설정·언어)이 Esc 를 쓰지 않으면 그 창을 연 메뉴가 받는다.
        UI_MenuPopup menu = FindPopup<UI_MenuPopup>();
        return menu != null && menu != top && menu.OnEscape();
    }

    public void ClosePopupUI(UI_Popup popup)
    {
        // 맨 위가 아니어도 닫는다. 예전에는 조용히 실패해서, 그 사이 다른 창이 올라온 층 이름 팝업이
        // 보이지 않는 채 스택에 남아 휠 줌을 막고 다음 Esc 를 먹었다.
        int index = IndexOf(popup);
        if (index >= 0)
            RemoveAt(index);
    }

    public void ClosePopupUI()
    {
        Prune();
        if (_popups.Count == 0)
            return;

        RemoveAt(_popups.Count - 1);
    }

    public void CloseAllPopupUI()
    {
        while (_popups.Count > 0)
            ClosePopupUI();
    }

    void RemoveAt(int index)
    {
        UI_Popup popup = _popups[index];
        _popups.RemoveAt(index);
        if (popup != null)
            Managers.Resource.Destroy(popup.gameObject);
        _closedFrame = Time.frameCount;
        Repack();
        RefreshTimeScale();
    }

    // 같은 창인가는 참조로 본다. UnityEngine.Object 의 == 는 부서진 것끼리를 같다고 한다.
    int IndexOf(UI_Popup popup)
    {
        for (int i = 0; i < _popups.Count; i++)
        {
            if (ReferenceEquals(_popups[i], popup))
                return i;
        }
        return -1;
    }

    // 남은 창의 정렬 순서를 아래부터 다시 매긴다. 가운데 창이 빠진 뒤 새로 뜨는 창이
    // 맨 위 창과 같은 순서를 받아 그 밑에 깔리지 않게 한다.
    void Repack()
    {
        for (int i = 0; i < _popups.Count; i++)
        {
            Canvas canvas = _popups[i] != null ? _popups[i].GetComponent<Canvas>() : null;
            if (canvas != null)
                canvas.sortingOrder = PopupBaseOrder + i;
        }
        _order = PopupBaseOrder + _popups.Count;
    }

    // 닫는 길을 거치지 않고 부서진 창(씬이 통째로 내려간 경우 등)을 걷는다.
    void Prune()
    {
        if (_popups.RemoveAll(p => p == null) == 0)
            return;
        Repack();
        RefreshTimeScale();
    }

    private void ClosePlayerHpBar()
    {
        GameObject UI_PlayerHPBar = GameObject.Find("UI_PlayerHPBar");
        if (UI_PlayerHPBar != null)
            UI_PlayerHPBar.GetComponent<Image>().color = new Color(1, 1, 1, 0);

        GameObject UI_PlayerHPBarGauge = GameObject.Find("PlayerHPBarGauge");
        if (UI_PlayerHPBarGauge != null)
            UI_PlayerHPBarGauge.GetComponent<Image>().color = new Color(1, 1, 1, 0);
    }
    private void ShowPlayerHpBar()
    {
        GameObject UI_PlayerHPBar = GameObject.Find("UI_PlayerHPBar");
        if (UI_PlayerHPBar != null)
            UI_PlayerHPBar.GetComponent<Image>().color = new Color(1, 1, 1, 1);

        GameObject UI_PlayerHPBarGauge = GameObject.Find("PlayerHPBarGauge");
        if (UI_PlayerHPBarGauge != null)
            UI_PlayerHPBarGauge.GetComponent<Image>().color = new Color(1, 1, 1, 1);
    }

    public Transform GetPlayerHpBar()
    {
        GameObject UI_PlayerHPBar = GameObject.Find("UI_PlayerHPBar");
        return UI_PlayerHPBar.transform;
    }

    public void CloseGameSceneUI()
    {
        CloseAllPopupUI();
        ClosePlayerHpBar();
        UI_GameScene.gameObject.SetActive(false);
    }

    public void ShowGameSceneUI()
    {
        UI_GameScene.gameObject.SetActive(true);
        ShowPlayerHpBar();
    }

    public UI_Toast ShowToast(string msg)
    {
        string name = typeof(UI_Toast).Name;
        GameObject go = Managers.Resource.Instantiate($"{name}", pooling: true);
        UI_Toast popup = Util.GetOrAddComponent<UI_Toast>(go);
        popup.SetInfo(msg);
        _toastStack.Push(popup);
        go.transform.SetParent(Root.transform);
        //CoroutineManager.StartCoroutine(CoCloseToastUI());
        return popup;
    }

    IEnumerator CoCloseToastUI()
    {
        yield return new WaitForSeconds(1f);
        CloseToastUI();
    }

    public void CloseToastUI()
    {
        if (_toastStack.Count == 0)
            return;

        UI_Toast toast = _toastStack.Pop();
        Managers.Resource.Destroy(toast.gameObject);
        toast = null;
        _toastOrder--;
    }

    public int GetPopupCount()
    {
        Prune();
        return _popups.Count;
    }

    public void Clear()
    {
        CloseAllPopupUI();
        // 씬을 떠날 때는 무엇이 떠 있었든 시간을 되돌린다.
        IsPaused = false;
        Time.timeScale = 1;
        DOTween.defaultTimeScaleIndependent = false;
        _sceneUI = null;
    }

    /// <summary>
    /// 게임 안에서 메뉴가 떠 있으면 시간을 멈춘다. 메뉴에서 연 확인 창·설정 창은 메뉴가 밑에 깔려 있으니
    /// 그동안에도 멈춘 채다. 전투는 FixedUpdate 로 걸음을 세고 연출은 WaitForSeconds 로 기다려서
    /// 둘 다 여기서 같이 멈춘다. 타이틀의 설정 메뉴는 멈출 것이 없어 건드리지 않는다.
    /// </summary>
    public void RefreshTimeScale()
    {
        bool pause = false;
        if (Managers.Game.GameScene != null)
        {
            for (int i = 0; i < _popups.Count && pause == false; i++)
                pause = (_popups[i] is UI_MenuPopup || _popups[i] is UI_MonsterManualPopup) && _popups[i] != null;
        }

        if (pause == IsPaused)
            return;

        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
        // 멈춘 동안 새로 만드는 트윈은 멈춘 시간과 상관없이 돈다. 메뉴에서 여는 설정·언어 창은
        // DOMoveX 로 밀려 들어오는데, 시간이 멈춰 있으면 화면 밖에 그대로 서 있었다.
        DOTween.defaultTimeScaleIndependent = pause;
    }

    public T SetBattleCard<T> (Transform parent, CreatureData creature) where T : UI_BaseCard
    {
        var card = MakeSubItem<T>(parent);
        card.SetData(creature);
        return card;
    }

    #region StageName Popup 처리
    Coroutine CoHideStageNamePopup;
    public void ShowStageNamePopup(float duration)
    {
        if (StageNamePopup != null)
        {
            StageNamePopup.SetStageName();
            if (CoHideStageNamePopup != null)
                CoroutineManager.StopCoroutine(CoHideStageNamePopup);
        }
        else
        {
            StageNamePopup = Managers.UI.ShowPopupUI<UI_StageNamePopup>();
            StageNamePopup.SetStageName();
        }

        if(StageNamePopup != null)
            CoHideStageNamePopup = CoroutineManager.StartCoroutine(StageNamePopup.HideStageNamePopup(duration));
    }


    #endregion

    #region BossNamePopup
    Coroutine CoHideBossNamePopup;
    public void ShowBossNamePopup(float duration)
    {
        if (BossNamePopup != null)
        {
            BossNamePopup.SetBossName();
            if (CoHideBossNamePopup != null)
                CoroutineManager.StopCoroutine(CoHideBossNamePopup);
        }
        else
        {
            BossNamePopup = Managers.UI.ShowPopupUI<UI_BossNamePopup>();
            BossNamePopup.SetBossName();
        }

        if (BossNamePopup != null)
            CoHideBossNamePopup = CoroutineManager.StartCoroutine(BossNamePopup.HideBossNamePopup(duration));
    }


    #endregion
}
