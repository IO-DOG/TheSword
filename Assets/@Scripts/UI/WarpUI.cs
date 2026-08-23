using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 워프석 반지를 끼면 열리는 층 이동 창 (기획서 65쪽 "워프를 등록한 층에 한하여 자유롭게 이동").
///
/// 이 창에는 프리팹이 없다. MainUI_Warp_A/B 스프라이트만 있고 팝업은 만들어진 적이 없어서,
/// UIManager.ShowPopupUI 로는 띄울 수가 없다. 그래서 최소한의 UI 를 코드로 세운다 —
/// 기능이 먼저 돌아가야 프리팹을 어떻게 만들지도 정할 수 있다.
/// 미술이 들어오면 이 스크립트는 목록을 채우는 부분만 남기고 프리팹으로 옮기면 된다.
///
/// 여는 키는 Tab. 반지를 끼지 않았거나 전투/연출 중에는 열리지 않는다.
/// </summary>
public class WarpUI : MonoBehaviour
{
    public static KeyCode OpenKey = KeyCode.Tab;

    static WarpUI _instance;
    GameObject _panel;

    /// <summary>지금 살아 있는 워프 창. HUD 의 워프 버튼이 이걸 연다.</summary>
    public static WarpUI Instance { get { return _instance; } }

    public static void Spawn()
    {
        if (_instance != null)
            return;
        GameObject go = new GameObject("WarpUI");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<WarpUI>();
    }

    void Update()
    {
        if (Input.GetKeyDown(OpenKey) == false)
            return;

        if (_panel != null)
        {
            Close();
            return;
        }

        if (Managers.Game == null || Managers.Game.Player == null)
            return;
        if (EquipUtility.WarpUnlocked == false)
            return;
        if (Managers.Game.OnBattle || Managers.Game.OnFade || Managers.Game.OnDirect
            || Managers.Game.OnInteract || Managers.Game.OnConversation)
            return;

        Open();
    }

    public void Open()
    {
        List<int> stages = Managers.Game.WarpableStages();
        if (stages.Count == 0)
            return;

        // 창이 떠 있는 동안은 캐릭터가 움직이면 안 된다.
        Managers.Game.OnInputLock = true;

        _panel = new GameObject("WarpPanel", typeof(Canvas), typeof(GraphicRaycaster));
        Canvas canvas = _panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        Image bg = NewChild(_panel.transform, "BG").AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        Stretch(bg.rectTransform);

        GameObject list = NewChild(_panel.transform, "List");
        RectTransform listRt = list.AddComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0.5f, 0.5f);
        listRt.anchorMax = new Vector2(0.5f, 0.5f);
        listRt.pivot = new Vector2(0.5f, 0.5f);
        listRt.sizeDelta = new Vector2(420f, 460f);

        GridLayoutGroup grid = list.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(96f, 40f);
        grid.spacing = new Vector2(8f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperCenter;

        foreach (int stage in stages)
            AddButton(list.transform, stage);
    }

    void AddButton(Transform parent, int stageId)
    {
        GameObject go = NewChild(parent, $"Warp_{stageId}");
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.16f, 0.20f, 0.95f);

        Button button = go.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            Close();
            Managers.Game.WarpToStage(stageId);
        });

        GameObject labelGo = NewChild(go.transform, "Label");
        Text label = labelGo.AddComponent<Text>();
        label.text = $"{stageId + 1}층";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 18;
        Stretch(label.rectTransform);
    }

    public void Close()
    {
        if (_panel != null)
            Destroy(_panel);
        _panel = null;
        if (Managers.Game != null)
            Managers.Game.OnInputLock = false;
    }

    static GameObject NewChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
