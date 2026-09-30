using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 워프석 반지를 끼면 열리는 층 이동 창 (기획서 65쪽 "워프를 등록한 층에 한하여 자유롭게 이동").
/// 다녀온 층(FirstEnterMapCheck)을 층 이름과 함께 늘어놓고, 누르면 그 층으로 간다.
/// 아직 안 연 금고가 남은 층에는 "금고" 를 단다 — 남는 열쇠를 들고 돌아갈 이유다.
///
/// 이 창에는 프리팹이 없어 코드로 세운다. 글꼴과 틀은 CodeUI 가 다른 창에서 빌려 와 같은 그림을 쓴다.
/// 팝업 스택 밖의 창이라 Esc 는 UI_GameScene 이 먼저 여기로 보낸다.
///
/// 여는 키는 Tab (또는 HUD 의 워프 단추). 반지를 끼지 않았거나, 메뉴·다른 창이 떠 있거나, 흐름이 캐릭터를
/// 쥐고 있으면(전투·연출·대화·문·계단·레버·입력 잠금·전투 직전 관문 — UI_GameScene.CanOpenPanel) 열리지 않는다.
/// </summary>
public class WarpUI : MonoBehaviour
{
    public static KeyCode OpenKey = KeyCode.Tab;

    const int Columns = 10;
    const float Gap = 8f;
    static readonly Vector2 Cell = new Vector2(150f, 72f);
    static readonly Color Gold = new Color32(240, 210, 138, 255);
    static readonly Color Soft = new Color32(174, 182, 200, 255);
    static readonly Color Idle = new Color(0.8f, 0.8f, 0.86f, 1f);

    static WarpUI _instance;
    GameObject _panel;
    bool _lockedInput;      // 입력 잠금을 이 창이 켰다. 켠 쪽만 끈다

    /// <summary>지금 살아 있는 워프 창. HUD 의 워프 버튼이 이걸 연다.</summary>
    public static WarpUI Instance { get { return _instance; } }

    public bool IsOpen => _panel != null;

    // 에디터는 도메인 리로드 없이 플레이를 되풀이한다 — 지난 플레이의 (부서진) 창과 바꿔 둔 키를 넘기지 않는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _instance = null;
        OpenKey = KeyCode.Tab;
    }

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
        // 창은 씬에 놓이므로 씬이 바뀌면 같이 사라진다. 잠금은 새 씬이 스스로 푼다 — 여기서 건드리지 않는다.
        if (_lockedInput && _panel == null)
            _lockedInput = false;

        if (Input.GetKeyDown(OpenKey) == false)
            return;
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (IsOpen || EquipUtility.WarpUnlocked == false || UI_GameScene.CanOpenPanel() == false)
            return;
        List<int> stages = Managers.Game.WarpableStages();
        if (stages.Count == 0)
            return;

        // 창이 떠 있는 동안은 캐릭터가 움직이면 안 된다.
        _lockedInput = Managers.Game.OnInputLock == false;
        Managers.Game.OnInputLock = true;
        Build(stages);
    }

    public void Close()
    {
        if (_panel != null)
            Destroy(_panel);
        _panel = null;
        if (_lockedInput && Managers.Game != null)
            Managers.Game.OnInputLock = false;
        _lockedInput = false;
    }

    void Build(List<int> stages)
    {
        // 다 세운 뒤에 켠다. 스케일러는 켜질 때 비율을 재므로 첫 프레임부터 맞는다.
        _panel = new GameObject("WarpPanel", typeof(RectTransform));
        _panel.SetActive(false);
        Canvas canvas = _panel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = _panel.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;     // 높이 1080 에 맞춘다
        _panel.AddComponent<GraphicRaycaster>();

        Image dim = CodeUI.NewImage(_panel.transform, "Dim", null, new Color(0f, 0f, 0f, 0.7f));
        CodeUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        dim.gameObject.BindEvent(Close);    // 창 밖을 누르면 닫는다

        int rows = (stages.Count + Columns - 1) / Columns;
        int columns = Mathf.Min(Columns, stages.Count);
        Vector2 grid = new Vector2(columns * Cell.x + (columns - 1) * Gap, rows * Cell.y + (rows - 1) * Gap);
        Vector2 size = grid + new Vector2(56f, 150f);

        Sprite frame = Frame();
        Image box = CodeUI.NewImage(_panel.transform, "Frame", frame, frame != null ? Color.white : new Color(0.07f, 0.08f, 0.12f, 0.97f), true);
        box.raycastTarget = true;
        CodeUI.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        // 좁은 화면(4:3)에서는 창째로 줄인다. 캔버스 높이는 늘 1080 이다.
        float width = 1080f * Screen.width / Mathf.Max(1, Screen.height);
        box.rectTransform.localScale = Vector3.one * Mathf.Min(1f, (width - 40f) / size.x, 1040f / size.y);

        TextMeshProUGUI title = CodeUI.NewText(box.transform, "Title", CodeUI.ProseFont, 40f, Gold, TextAlignmentOptions.Center);
        CodeUI.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(grid.x, 50f));
        title.text = Managers.GetString(ForecastUI.WarpTitle);

        TextMeshProUGUI help = CodeUI.NewText(box.transform, "Help", CodeUI.ProseFont, 22f, Soft, TextAlignmentOptions.Center);
        CodeUI.Place(help.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(grid.x, 30f));
        CodeUI.Fit(help, 14f).text = Managers.GetString(ForecastUI.WarpHelp);

        RectTransform list = CodeUI.NewRect(box.transform, "Floors");
        CodeUI.Place(list, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), grid);
        GridLayoutGroup layout = list.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = Cell;
        layout.spacing = new Vector2(Gap, Gap);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
        layout.childAlignment = TextAnchor.UpperLeft;

        foreach (int stage in stages)
            AddFloor(list, stage);
        _panel.SetActive(true);
    }

    // 한 칸 = 층 번호 + 그 층 이름 (+ 남은 금고). 마우스를 올리면 밝아진다.
    void AddFloor(Transform list, int stageId)
    {
        Sprite frame = Frame();
        Image cell = CodeUI.NewImage(list, $"Warp_{stageId}", frame, frame != null ? Idle : new Color(0.16f, 0.16f, 0.2f, 0.95f), true);
        cell.raycastTarget = true;
        Color idle = cell.color;
        cell.gameObject.BindEvent(() => cell.color = Color.white, type: Define.UIEvent.PointerEnter);
        cell.gameObject.BindEvent(() => cell.color = idle, type: Define.UIEvent.PointerExit);
        cell.gameObject.BindEvent(() =>
        {
            Close();
            Managers.Game.WarpToStage(stageId);
        });

        TextMeshProUGUI number = CodeUI.NewText(cell.transform, "Floor", CodeUI.NumberFont, 24f, Color.white);
        CodeUI.Place(number.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -6f), new Vector2(Cell.x - 24f, 28f));
        string floor = string.Format(Managers.GetString(ForecastUI.FloorN), stageId + 1);
        number.text = floor;

        // 이름은 조금만 줄이고 넘치면 말줄임(…)으로 자른다. 12 까지 줄이던 때는 긴 이름 하나(Inside the Forest of
        // Demons)만 이웃의 절반 크기가 됐고, 720p 에서 이름이 10px 남짓이었다. 글꼴(Silver)은 줄 높이 1.1em 이라 칸도 높인다.
        Data.StageInfoData info;
        TextMeshProUGUI name = CodeUI.NewText(cell.transform, "Name", CodeUI.ProseFont, 28f, Soft);
        CodeUI.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -36f), new Vector2(Cell.x - 24f, 34f));
        name.overflowMode = TextOverflowModes.Ellipsis;
        CodeUI.Fit(name, 22f).text = Managers.Data.StageInfoDic.TryGetValue(stageId, out info)
            ? WithoutFloor(Managers.GetString(info.DungeonNameScriptID), floor) : "";

        if (HasClosedVault(stageId))
        {
            TextMeshProUGUI vault = CodeUI.NewText(cell.transform, "Vault", CodeUI.ProseFont, 24f, Gold, TextAlignmentOptions.TopRight);
            CodeUI.Place(vault.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -6f), new Vector2(70f, 28f));
            CodeUI.Fit(vault, 12f).text = Managers.GetString(ForecastUI.Vault);
        }
    }

    // 층 이름에는 층 번호가 붙어 있다("이끼 낀 지하 묘소 5층") — 칸 위의 큰 번호와 겹쳐 "5층" 이 두 번 나왔고, 긴 이름을
    // 칸 폭에 넣느라 글자가 깨알이 됐다(960x540 에서 읽을 수 없었다). 끝의 번호만 뗀다. 중국어 이름은 "第5层" 이다.
    static string WithoutFloor(string name, string floor) =>
        name.EndsWith(floor, System.StringComparison.Ordinal) ? name.Substring(0, name.Length - floor.Length).TrimEnd(' ', '第') : name;

    /// <summary>
    /// 아직 안 연 금고(네 번째 문)가 남은 층인가. 큰길 문 셋은 위층 계단 앞을 차례로 막고 있어서, 위층에 한 번이라도
    /// 올라갔다면 셋은 다 열린 것이다 — 그 층에 아직 닫힌 문이 있으면 그것이 금고다(금고는 마지막 구역에만 있다,
    /// layout_gen.check_vault_safe). 위층에 아직 못 간 층은 큰길 문과 가를 수 없어 달지 않는다.
    /// 손수 만든 1~4층에는 금고가 없다.
    /// </summary>
    static bool HasClosedVault(int stageId)
    {
        List<bool> visited = Managers.Game.PlayerData.FirstEnterMapCheck;
        if (visited == null || stageId + 1 >= visited.Count || visited[stageId + 1] == false)
            return false;

        Data.StageInfoData info;
        Data.MapData map;
        if (Managers.Data.StageInfoDic.TryGetValue(stageId, out info) == false || MapBuilder.IsHandAuthored(info.DungeonID))
            return false;
        if (Managers.Data.MapDic.TryGetValue(stageId, out map) == false || map.Objects == null)
            return false;

        foreach (Data.ObjectData obj in map.Objects)
        {
            bool closed;
            if (obj.ObjectType == (int)Define.ObjectType.Door
                && Managers.Data.DoorActiveDic.TryGetValue(obj.Count, out closed) && closed)
                return true;
        }
        return false;
    }

    // 틀: 인벤토리의 능력치 칸 그림(9분할, 두 배 픽셀). 도감과 같은 그림이다.
    static Sprite Frame() => CodeUI.PrefabSprite("UI_InvenPopup", "Inventory_Popup32");
}
