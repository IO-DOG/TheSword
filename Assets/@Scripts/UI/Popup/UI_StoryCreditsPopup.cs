using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 크레딧 (바이블 11절). 까만 화면에서 줄이 아래에서 위로 흘러가고, 마지막 줄이 가운데에 닿으면 멈춘다.
/// 결말 셋이 같이 쓴다. Esc·Enter·Space·클릭을 누르고 있으면 빨리 흐른다. 자동 플레이 중에는 더 빨리.
/// </summary>
public class UI_StoryCreditsPopup : UI_Popup
{
    const float Speed = 90f;        // 초당 올라가는 거리 (캔버스 단위, 1920x1080 기준)
    const float LineWidth = 1600f;

    Action _onDone;
    bool _finished;
    CanvasGroup _group;
    RectTransform _content;
    float _lastCenter;              // 맨 위에서 마지막 줄 가운데까지

    public static UI_StoryCreditsPopup Show(StoryScene scene, Action onDone)
    {
        if (scene == null || scene.Lines.Length == 0)
        {
            onDone?.Invoke();
            return null;
        }
        UI_StoryCreditsPopup popup = StoryUI.NewPopup<UI_StoryCreditsPopup>();
        popup._onDone = onDone;
        popup.Build(scene);
        popup.StartCoroutine(popup.CoRoll());
        return popup;
    }

    public override bool OnEscape() => true;       // Esc 는 누르고 있으면 빨라질 뿐 닫지 않는다

    void OnDestroy() => Done();

    void Done()
    {
        if (_finished)
            return;
        _finished = true;
        try { _onDone?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
    }

    void Build(StoryScene scene)
    {
        _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        Image black = StoryUI.NewImage(transform, "Black", null, Color.black);
        StoryUI.Stretch(black.rectTransform);
        black.raycastTarget = true;

        // 위끝이 화면 아래끝에 붙어 시작한다 (기준점: 화면 아래 가운데, 자기 위끝).
        _content = StoryUI.Child(transform, "Content");
        _content.anchorMin = _content.anchorMax = new Vector2(0.5f, 0f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.sizeDelta = new Vector2(LineWidth, 0f);

        float y = 0f;
        for (int i = 0; i < scene.Lines.Length; i++)
        {
            bool title = i == 0;
            TMP_Text text = StoryUI.NewText(_content, $"Line{i + 1:00}", title ? StoryUI.PixelFont : StoryUI.ProseFont,
                title ? 96f : 54f, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.text = Managers.GetString(scene.Lines[i].ScriptId);
            float height = text.GetPreferredValues(text.text, LineWidth, 0f).y;
            RectTransform rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(LineWidth, height);
            rt.anchoredPosition = new Vector2(0f, -y);
            _lastCenter = y + height * 0.5f;
            // 빈 줄을 데이터에 넣지 않는다 — 항목 사이를 띄운다 (제목 뒤는 더 넓게).
            y += height + (title ? 180f : 120f);
        }
    }

    static bool Held() =>
        Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)
        || Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);

    IEnumerator CoRoll()
    {
        yield return null;      // 캔버스가 화면 크기를 잡은 뒤
        yield return _group.DOFade(1f, StoryUI.Auto ? 0.1f : 1f).SetLink(gameObject).WaitForCompletion();

        float stop = ((RectTransform)transform).rect.height * 0.5f + _lastCenter;
        while (_content.anchoredPosition.y < stop)
        {
            float speed = Speed * (StoryUI.Auto ? 12f : Held() ? 5f : 1f);
            _content.anchoredPosition = new Vector2(0f, Mathf.Min(stop, _content.anchoredPosition.y + speed * Time.unscaledDeltaTime));
            yield return null;
        }

        // 마지막 인사는 가운데에 잠깐 둔다. 한 번 더 누르면 바로 넘어간다.
        float hold = StoryUI.Auto ? 0.3f : 3f;
        for (float t = 0f; t < hold; t += Time.unscaledDeltaTime)
        {
            if (t > 0.5f && StoryUI.NextPressed())
                break;
            yield return null;
        }
        yield return _group.DOFade(0f, StoryUI.Auto ? 0.1f : 1f).SetLink(gameObject).WaitForCompletion();
        Done();
        ClosePopupUI();
    }
}
