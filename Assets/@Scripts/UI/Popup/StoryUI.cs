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

    /// <summary>자동 플레이에서 한 줄을 다 보인 뒤 넘기기까지 (초).</summary>
    public const float AutoDelay = 0.35f;

    /// <summary>넘기기 입력 — Enter·Space·왼쪽 클릭.</summary>
    public static bool NextPressed() =>
        Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
        || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);

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
