using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class UIManager
{
    int _order = 10;
    int _toastOrder = 500;

    Stack<UI_Popup> _popupStack = new Stack<UI_Popup>();
    Stack<UI_Toast> _toastStack = new Stack<UI_Toast>();
    UI_Scene _sceneUI = null;
    public UI_Scene SceneUI { get { return _sceneUI; } }

    public event Action<int> OnTimeScaleChanged;

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
        _popupStack.Push(popup);

        go.transform.SetParent(Root.transform);

        RefreshTimeScale();

        return popup;
    }

    public void ClosePopupUI(UI_Popup popup)
    {
        if (_popupStack.Count == 0)
            return;

        if (_popupStack.Peek() != popup)
        {
            Debug.Log("Close Popup Failed!");
            return;
        }
        //Managers.Sound.PlayPopupClose();
        ClosePopupUI();
    }

    public void ClosePopupUI()
    {
        if (_popupStack.Count == 0)
            return;

        UI_Popup popup = _popupStack.Pop();
        Managers.Resource.Destroy(popup.gameObject);
        popup = null;
        _order--;
        RefreshTimeScale();
    }

    public void CloseAllPopupUI()
    {
        while (_popupStack.Count > 0)
            ClosePopupUI();
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
        return _popupStack.Count;
    }

    public void Clear()
    {
        CloseAllPopupUI();
        Time.timeScale = 1;
        _sceneUI = null;
    }

    public void RefreshTimeScale()
    {
        //if (SceneManager.GetActiveScene().name != Define.Scene.GameScene.ToString())
        //{
        //    Time.timeScale = 1;
        //    return;
        //}

        //if (_popupStack.Count > 0 || IsActiveSoulShop == true)
        //    Time.timeScale = 0;
        //else
        //    Time.timeScale = 1;

        //DOTween.timeScale = 1;
        //OnTimeScaleChanged?.Invoke((int)Time.timeScale);
    }

    #region 임시
    bool _isActiveSoulShop = false;
    public bool IsActiveSoulShop
    {
        get { return _isActiveSoulShop; }
        set
        {
            _isActiveSoulShop = value;

            if (_isActiveSoulShop == true)
                Time.timeScale = 0;
            else
                Time.timeScale = 1;

            DOTween.timeScale = 1;
            OnTimeScaleChanged?.Invoke((int)Time.timeScale);
        }
    }

    #endregion
}
