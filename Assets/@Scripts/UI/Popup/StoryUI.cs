using Febucci.UI.Core;
using Febucci.UI.Examples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이야기 화면(대화창·카드·크레딧·바크)이 같이 쓰는 것.
///
/// 새 그림은 그리지 않는다. 글꼴·틀·화살표는 이미 있는 프리팹에서 빌린다 — 두 글꼴(Silver SDF,
/// DNFBitBitv2 SDF)과 대화창 그림(TalkUI_*)은 어드레서블이 아니라 프리팹 안에만 있어서, 빌드에서는
/// 프리팹을 거쳐야만 손에 닿는다.
/// </summary>
public static class StoryUI
{
    /// <summary>자동 플레이(완주 녹화 봇) 중이다. 이야기 화면은 기다리지 않고 스스로 넘어간다.</summary>
    public static bool Auto => GameEvents.IsAutoPlaying;

    /// <summary>UI 문구 (Tools/ui_text_parts/story_ui.py, 300~349). 대사는 ScriptData 300000~ (story_gen).</summary>
    public const int ChoiceHint = 300;

    /// <summary>이야기 흐름 문구 (Tools/ui_text_parts/story_flow.py, 380~409).</summary>
    public const int Instinct = 380;        // 계약 전, 지는 싸움 앞의 감 (FatalFightGuard)
    public const int SkipHoldHint = 381;    // Tab 길게: 건너뛰기
    public const int SkipSeenHint = 382;    // Tab: 건너뛰기 (본 장면)

    /// <summary>자동 플레이에서 한 줄을 다 보인 뒤 넘기기까지 (초).</summary>
    public const float AutoDelay = 0.35f;

    /// <summary>건너뛰기 키. 컨트롤러는 Steam Input 이 LB 를 Tab 으로 준다 — 전투 건너뛰기와 같은 키다.</summary>
    public const KeyCode SkipKey = KeyCode.Tab;
    /// <summary>이만큼(실시간 초) 누르고 있으면 지금 장면을 끝까지 넘긴다.</summary>
    public const float SkipHold = 0.6f;

    /// <summary>
    /// 지금 장면을 끝까지 넘기는 중이다 (StoryDirector.WatchSkip 이 켜고, 장면이 끝나면·선택지가 뜨면 끈다).
    /// 그동안 넘기기 입력을 매 프레임 누른 것으로 친다 — 대화창·카드가 제 길(줄마다 연출 신호, 그림 바꾸기, 카드 깔아 두기)로
    /// 끝까지 가므로, 사람이 빨리 넘긴 것과 같은 상태로 끝난다. 선택지는 이 입력을 보지 않는다.
    /// </summary>
    public static bool Skipping { get; private set; }
    public static void BeginSkip() => Skipping = true;
    public static void EndSkip() => Skipping = false;

    /// <summary>넘기기 입력 — Enter·Space·왼쪽 클릭 (건너뛰는 중이면 늘).</summary>
    public static bool NextPressed() =>
        Skipping || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
        || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);

    static CanvasGroup s_hint;
    static TMP_Text s_hintText;
    static RectTransform s_hintFill;
    static int s_hintId;
    static Define.ScriptType s_hintLang;

    // 에디터는 도메인 리로드 없이 플레이를 되풀이한다 — 지난 플레이의 건너뛰기와 (부서진) 안내를 넘기지 않는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Skipping = false;
        s_hint = null;
        s_hintText = null;
        s_hintFill = null;
        s_hintId = 0;
    }

    /// <summary>
    /// 오른쪽 위 구석의 건너뛰기 안내와 누른 만큼 차는 막대 (StoryDirector 가 매 프레임 부른다). 지난 판들에서 본 장면은
    /// 한 번 누르면 넘어가니 막대가 없다. 팝업 스택 밖의 캔버스라 "맨 위 창" 판정·Esc 에 끼지 않는다.
    /// </summary>
    public static void SkipHint(bool show, bool seen, float fill)
    {
        if (show == false)
        {
            if (s_hint != null)
                s_hint.gameObject.SetActive(false);
            return;
        }
        if (s_hint == null)
            BuildSkipHint();
        int id = seen ? SkipSeenHint : SkipHoldHint;
        if (id != s_hintId || s_hintLang != GameSettings.Language)     // 매 프레임 찾지 않는다
        {
            s_hintId = id;
            s_hintLang = GameSettings.Language;
            s_hintText.text = Managers.GetString(id);
        }
        s_hintFill.parent.gameObject.SetActive(seen == false);
        s_hintFill.localScale = new Vector3(Mathf.Clamp01(fill), 1f, 1f);
        s_hint.alpha = fill > 0f ? 1f : 0.75f;
        s_hint.gameObject.SetActive(true);
    }

    static void BuildSkipHint()
    {
        GameObject go = new GameObject("@StorySkipHint", typeof(RectTransform));
        Object.DontDestroyOnLoad(go);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;      // 이야기 창(10~)·바크(400) 위, 토스트(500~) 아래
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        s_hint = go.AddComponent<CanvasGroup>();
        s_hint.interactable = false;
        s_hint.blocksRaycasts = false;

        Image plate = CodeUI.NewImage(go.transform, "Plate", null, new Color(0f, 0f, 0f, 0.55f));
        CodeUI.Place(plate.rectTransform, Vector2.one, Vector2.one, new Vector2(-32f, -28f), new Vector2(420f, 58f));
        s_hintText = CodeUI.Fit(CodeUI.NewText(plate.transform, "Text", ProseFont, 32f, new Color(0.9f, 0.9f, 0.9f, 1f),
            TextAlignmentOptions.Center), 18f);
        CodeUI.Stretch(s_hintText.rectTransform).offsetMin = new Vector2(12f, 8f);
        s_hintText.rectTransform.offsetMax = new Vector2(-12f, 0f);

        Image track = CodeUI.NewImage(plate.transform, "Track", null, new Color(1f, 1f, 1f, 0.2f));
        CodeUI.Place(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(380f, 4f));
        Image fill = CodeUI.NewImage(track.transform, "Fill", null, new Color(0.94f, 0.82f, 0.54f, 1f));
        s_hintFill = CodeUI.Stretch(fill.rectTransform);
        s_hintFill.pivot = new Vector2(0f, 0.5f);      // 왼쪽부터 찬다 (localScale.x)
    }

    /// <summary>글자 속도를 설정(GameSettings.TextSpeed: 0 느림 1 보통 2 빠름 3 한 번에)에 맞춘다.
    /// 타자기는 새 글을 받을 때마다 속도를 1 로 되돌리게 구워져 있어서 그것부터 끈다.</summary>
    public static void ApplyTextSpeed(TMP_Text text)
    {
        TypewriterCore writer = text != null ? text.GetComponent<TypewriterCore>() : null;
        if (writer == null)
            return;
        int speed = Auto ? 3 : GameSettings.TextSpeed;
        writer.useTypeWriter = speed < 3;
        writer.resetTypingSpeedAtStartup = false;
        writer.SetTypewriterSpeed(speed == 0 ? 0.55f : speed == 2 ? 2.2f : 1f);

        TAnimSoundWriter sound = text.GetComponent<TAnimSoundWriter>();
        if (sound != null && Managers.Sound != null)
            sound.source = Managers.Sound.GetAudioSource(Define.Sound.Effect);
    }

    static TMP_FontAsset s_prose, s_pixel;

    /// <summary>글 (Silver SDF) — 대화창의 글꼴.</summary>
    public static TMP_FontAsset ProseFont => s_prose != null ? s_prose : (s_prose = FontOf("UI_ConversationPopup", "Silver"));

    /// <summary>숫자·제목 (DNFBitBitv2 SDF) — HUD 의 글꼴. 못 찾으면 Silver.</summary>
    public static TMP_FontAsset PixelFont => s_pixel != null ? s_pixel : (s_pixel = FontOf("UI_GameScene", "DNF") ?? ProseFont);

    static TMP_FontAsset FontOf(string prefab, string contains)
    {
        GameObject go = Managers.Resource.Load<GameObject>(prefab);
        if (go == null)
            return null;
        foreach (TMP_Text t in go.GetComponentsInChildren<TMP_Text>(true))
            if (t.font != null && t.font.name.Contains(contains))
                return t.font;
        return null;
    }

    /// <summary>프리팹 속 자식 하나를 설정째 복제한다 (타자기·소리·애니메이터까지). 없으면 null.</summary>
    public static GameObject Clone(string prefab, string path, Transform parent)
    {
        GameObject root = Managers.Resource.Load<GameObject>(prefab);
        Transform src = root != null ? root.transform.Find(path) : null;
        if (src == null)
            return null;
        // 꺼진 자리에서 복제해 Awake 를 미룬다. 타자 소리(TAnimSoundWriter)는 Awake 에서 소리통이 비었으면
        // 경고를 찍고 영영 울리지 않는다 — 프리팹에는 비어 있고, 원래 창은 제 Awake 에서 채워 준다.
        GameObject holder = new GameObject("@StoryClone");
        holder.SetActive(false);
        GameObject copy = Object.Instantiate(src.gameObject, holder.transform, false);
        copy.name = src.name;
        copy.SetActive(true);
        TAnimSoundWriter sound = copy.GetComponent<TAnimSoundWriter>();
        if (sound != null && Managers.Sound != null)
            sound.source = Managers.Sound.GetAudioSource(Define.Sound.Effect);
        copy.transform.SetParent(parent, false);
        Object.Destroy(holder);
        return copy;
    }

    /// <summary>프리팹 속 Image 의 그림.</summary>
    public static Sprite SpriteOf(string prefab, string path)
    {
        GameObject root = Managers.Resource.Load<GameObject>(prefab);
        Transform t = root != null ? root.transform.Find(path) : null;
        Image image = t != null ? t.GetComponent<Image>() : null;
        return image != null ? image.sprite : null;
    }

    /// <summary>프리팹 없이 코드로 세우는 창. 팝업 스택에 올리고 캔버스를 프리팹 창들과 같은 1920x1080 에 맞춘다.</summary>
    public static T NewPopup<T>() where T : UI_Popup
    {
        GameObject go = new GameObject(typeof(T).Name, typeof(RectTransform));
        T popup = go.AddComponent<T>();
        Managers.UI.PushPopup(popup);
        popup.Init();
        // SetCanvas 는 기준 해상도만 넣는다. 새 CanvasScaler 의 맞춤 비율은 0(가로)이라 그대로 두면 모든 것이
        // 1.8배로 커진다 — 프리팹 창들처럼 가로세로 반씩 맞춘다.
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        if (scaler != null)
            scaler.matchWidthOrHeight = 0.5f;
        return popup;
    }

    public static RectTransform Child(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static Image NewImage(Transform parent, string name, Sprite sprite, Color color)
    {
        Image image = Child(parent, name).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI NewText(Transform parent, string name, TMP_FontAsset font, float size, TextAlignmentOptions align)
    {
        TextMeshProUGUI text = Child(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.alignment = align;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
