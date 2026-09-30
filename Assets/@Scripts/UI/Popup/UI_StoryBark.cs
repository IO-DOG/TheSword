using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 바크: 화면 위쪽에 잠깐 뜨는 한 줄 (층 유형·되살아남·여분 열쇠·열쇠 없음). 움직임을 막지 않는다.
///
/// 팝업 스택에 올리지 않고 레이캐스터도 달지 않는다 — 입력을 받지 않고, Esc·메뉴·대사 넘기기의
/// "맨 위 창" 판정에도 끼지 않는다. 여럿이면 줄을 서서 차례로 뜬다. 전투·대화·연출·메뉴 동안은 숨고
/// 그동안 시간도 세지 않는다. 씬이 바뀌면 같이 사라진다.
/// </summary>
public class UI_StoryBark : MonoBehaviour
{
    const float Hold = 3.5f;
    const float FadeTime = 0.25f;

    static UI_StoryBark s_instance;
    readonly Queue<StoryLine> _queue = new Queue<StoryLine>();
    CanvasGroup _group;
    Image _portrait;
    TMP_Text _name;
    TMP_Text _text;
    float _shownFor = -1f;          // 음수면 지금 띄운 것이 없다

    public static void Show(StoryLine line)
    {
        if (line == null)
            return;
        if (s_instance == null)
        {
            GameObject go = new GameObject("@StoryBark", typeof(RectTransform));
            s_instance = go.AddComponent<UI_StoryBark>();
            s_instance.Build();
        }
        s_instance._queue.Enqueue(line);
    }

    /// <summary>떠 있는 것과 줄 선 것을 걷는다 (StoryDirector.AbortAll — 메뉴의 "이 층 다시"·"타이틀로" 앞).</summary>
    public static void Clear()
    {
        if (s_instance != null)
            Destroy(s_instance.gameObject);
        s_instance = null;
    }

    void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;      // 팝업(10~) 위, 토스트(500~) 아래
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        // 대화창 틀(700x148)을 같은 비율로 줄여 쓴다.
        RectTransform box = StoryUI.Child(transform, "Box");
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 1f);
        box.pivot = new Vector2(0.5f, 1f);
        box.anchoredPosition = new Vector2(0f, -30f);
        box.sizeDelta = new Vector2(1000f, 212f);
        Image frame = StoryUI.NewImage(box, "Frame", StoryUI.SpriteOf("UI_ConversationPopup", "EntireObject/CoversationBox"), Color.white);
        StoryUI.Stretch(frame.rectTransform);

        _portrait = StoryUI.NewImage(box, "Portrait", null, Color.white);
        RectTransform p = _portrait.rectTransform;
        p.anchorMin = p.anchorMax = p.pivot = new Vector2(0f, 0.5f);
        p.anchoredPosition = new Vector2(34f, 0f);
        p.sizeDelta = new Vector2(140f, 140f);
        _portrait.preserveAspect = true;

        _name = StoryUI.NewText(box, "Name", StoryUI.ProseFont, 36f, TextAlignmentOptions.TopLeft);
        RectTransform n = _name.rectTransform;
        n.anchorMin = n.anchorMax = n.pivot = new Vector2(0f, 1f);
        n.anchoredPosition = new Vector2(196f, -26f);
        n.sizeDelta = new Vector2(760f, 46f);
        _name.color = new Color(1f, 0.87f, 0.55f, 1f);

        _text = StoryUI.NewText(box, "Text", StoryUI.ProseFont, 44f, TextAlignmentOptions.MidlineLeft);
        RectTransform t = _text.rectTransform;
        t.anchorMin = Vector2.zero;
        t.anchorMax = Vector2.one;
        t.offsetMin = new Vector2(196f, 24f);
        t.offsetMax = new Vector2(-40f, -70f);
        _text.textWrappingMode = TextWrappingModes.Normal;
    }

    void Update()
    {
        GameManager g = Managers.Game;
        bool hidden = g == null || g.OnBattle || g.OnConversation || g.OnDirect || Managers.UI.IsPaused;
        if (_shownFor < 0f)
        {
            if (hidden || _queue.Count == 0)
                return;
            Present(_queue.Dequeue());
        }

        if (hidden == false)
            _shownFor += Time.unscaledDeltaTime;
        bool expired = _shownFor >= (StoryUI.Auto ? 1.5f : Hold);
        float target = hidden || expired ? 0f : 1f;
        _group.alpha = Mathf.MoveTowards(_group.alpha, target, Time.unscaledDeltaTime / FadeTime);
        if (expired && _group.alpha <= 0f)
            _shownFor = -1f;
    }

    void Present(StoryLine line)
    {
        StorySpeaker who = line.Who;
        string portrait = who.Portrait == StoryPortrait.Damian ? "Adventurer"
            : who.Portrait == StoryPortrait.Sword ? "MagicalSword" : null;
        _portrait.sprite = portrait != null ? Managers.Resource.Load<Sprite>(portrait) : null;
        _portrait.enabled = _portrait.sprite != null;
        _name.text = who.NameId != 0 ? Managers.GetString(who.NameId) : "";
        _text.text = Managers.GetString(line.ScriptId);
        _shownFor = 0f;
    }
}
