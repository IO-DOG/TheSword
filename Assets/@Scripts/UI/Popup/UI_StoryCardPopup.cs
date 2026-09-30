using DG.Tweening;
using Febucci.UI;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이야기 카드: 화면을 덮는 그림 한 장과 아래의 내레이션 (마을·81층·후일담). 챕터 카드(이름과 부제)도 이 창이 띄운다.
///
/// 프리팹이 없다. 글줄(타자기·소리째)은 인트로의 SceneText, 화살표·그림자는 대화창, 챕터 이름과 장식선은
/// 층 이름 팝업에서 설정째 빌린다 (StoryUI.Clone).
/// 세로로 긴 그림(Intro05)은 아래에서 위로 훑는다 (바이블 R6). 그 그림을 쓰는 마지막 줄은 훑기가 멈춘 뒤에 뜬다
/// — "팬이 멈춰 검은 해와 초승달이 한 화면에 들어올 때" 말한다.
/// Enter·Space·클릭으로 넘기고 Esc 는 삼킨다. 자동 플레이 중에는 스스로 넘어간다.
/// </summary>
public class UI_StoryCardPopup : UI_Popup
{
    const float FadeTime = 0.6f;
    const float PanSeconds = 9f;
    const float TitleHold = 2.4f;

    StoryScene _scene;
    Action<int> _onLine;
    Action _onDone;
    bool _keep;             // 끝나도 그림을 깔아 둔다 — 다음 대화의 배경 (Close 가 걷는다)
    bool _finished;         // onDone 을 불렀다
    bool _closing;

    CanvasGroup _group;
    Image _black;
    Image _back, _front;    // 그림 두 겹. 바꿀 때 앞 겹을 서서히 올린 뒤 뒤 겹으로 옮긴다
    Image _shade;
    TMP_Text _text;
    TextAnimator_TMP _animator;
    GameObject _arrow;
    Tween _pan;
    bool _waitingPan;
    bool _textShown;
    bool _allShown;
    bool _advance;
    int _textFrame;
    float _autoTimer;

    static float Quick(float seconds) => StoryUI.Auto ? Mathf.Min(seconds, 0.15f) : seconds;

    /// <summary>카드 장면 하나. onLine 은 줄이 뜰 때(1부터), onDone 은 다 넘겼을 때 한 번.
    /// keepOpen 이면 그림을 깐 채 onDone 을 부르고 남는다 — 다음 대화가 끝나면 Close 로 걷는다.</summary>
    public static UI_StoryCardPopup Show(StoryScene scene, Action<int> onLine, Action onDone, bool keepOpen)
    {
        if (scene == null || scene.Lines.Length == 0)
        {
            onDone?.Invoke();
            return null;
        }
        UI_StoryCardPopup popup = StoryUI.NewPopup<UI_StoryCardPopup>();
        popup._scene = scene;
        popup._onLine = onLine;
        popup._onDone = onDone;
        popup._keep = keepOpen;
        popup.Build();
        popup.StartCoroutine(popup.CoPlay());
        return popup;
    }

    /// <summary>챕터 카드 — 까만 화면에 챕터 이름, 장식선, 부제. 잠깐 두었다가(누르면 바로) 걷힌다.</summary>
    public static void ShowTitle(int titleId, int subtitleId, Action onDone)
    {
        UI_StoryCardPopup popup = StoryUI.NewPopup<UI_StoryCardPopup>();
        popup._onDone = onDone;
        popup.BuildTitle(titleId, subtitleId);
        popup.StartCoroutine(popup.CoTitle());
    }

    /// <summary>깔아 둔 카드를 걷는다.</summary>
    public void Close()
    {
        if (this == null || _closing)
            return;
        _closing = true;
        StopAllCoroutines();
        _pan?.Kill();
        _group.DOFade(0f, Quick(0.5f)).SetLink(gameObject).OnComplete(() =>
        {
            Done();
            ClosePopupUI();
        });
    }

    public override bool OnEscape() => true;

    void OnDestroy()
    {
        _pan?.Kill();
        Done();
    }

    void Done()
    {
        if (_finished)
            return;
        _finished = true;
        try { _onDone?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
    }

    #region 카드
    void Build()
    {
        _group = gameObject.AddComponent<CanvasGroup>();
        _black = StoryUI.NewImage(transform, "Black", null, new Color(0f, 0f, 0f, 0f));
        StoryUI.Stretch(_black.rectTransform);
        _black.raycastTarget = true;        // 뒤의 HUD·맵을 누르지 않게
        _back = Picture("PictureBack");
        _front = Picture("PictureFront");

        _shade = StoryUI.NewImage(transform, "Shade",
            StoryUI.SpriteOf("UI_ConversationPopup", "EntireObject/CoversationBox/Shadow"), Color.white);
        RectTransform shade = _shade.rectTransform;
        shade.anchorMin = new Vector2(0f, 0f);
        shade.anchorMax = new Vector2(1f, 0f);
        shade.pivot = new Vector2(0.5f, 0f);
        shade.sizeDelta = new Vector2(0f, 340f);
        shade.anchoredPosition = Vector2.zero;
        _shade.enabled = false;

        GameObject line = StoryUI.Clone("UI_IntroScene", "SceneText", transform);
        _text = line != null ? line.GetComponent<TMP_Text>()
            : StoryUI.NewText(transform, "SceneText", StoryUI.ProseFont, 52f, TextAlignmentOptions.Center);
        RectTransform rt = _text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 70f);
        rt.sizeDelta = new Vector2(1560f, 230f);
        _text.alignment = TextAlignmentOptions.Center;
        _text.textWrappingMode = TextWrappingModes.Normal;
        _text.text = "";
        _animator = _text.GetComponent<TextAnimator_TMP>();
        StoryUI.ApplyTextSpeed(_text);

        _arrow = StoryUI.Clone("UI_ConversationPopup", "EntireObject/CoversationBox/ConversationArrow", transform);
        if (_arrow != null)
        {
            RectTransform a = (RectTransform)_arrow.transform;
            a.anchorMin = a.anchorMax = a.pivot = new Vector2(1f, 0f);
            a.anchoredPosition = new Vector2(-90f, 60f);
            _arrow.SetActive(false);
        }
    }

    Image Picture(string name)
    {
        Image image = StoryUI.NewImage(transform, name, null, new Color(1f, 1f, 1f, 0f));
        StoryUI.Stretch(image.rectTransform);
        image.preserveAspect = true;
        return image;
    }

    static bool IsTall(Sprite s) => s != null && s.rect.height > s.rect.width * 1.3f;

    // 보통 그림은 화면에 맞춰 넣고(남는 곳은 검다), 세로로 긴 그림은 가로에 맞추고 아래끝을 화면 아래에 붙인다.
    void Place(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        RectTransform rt = image.rectTransform;
        if (IsTall(sprite))
        {
            float width = ((RectTransform)transform).rect.width;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(width, width * sprite.rect.height / sprite.rect.width);
            rt.anchoredPosition = Vector2.zero;
            image.preserveAspect = false;
        }
        else
        {
            StoryUI.Stretch(rt);
            image.preserveAspect = true;
        }
    }

    IEnumerator CoPlay()
    {
        yield return null;      // 캔버스가 화면 크기를 잡은 뒤에 그림을 놓는다
        yield return _black.DOFade(1f, Quick(0.5f)).SetLink(gameObject).WaitForCompletion();

        Sprite current = null;
        for (int i = 0; i < _scene.Lines.Length; i++)
        {
            StoryLine line = _scene.Lines[i];
            Sprite next = line.Image != null ? Managers.Resource.Load<Sprite>(line.Image) : current;
            if (next != null && next != current)
            {
                yield return CoPicture(next);
                current = next;
            }

            // 세로 그림을 쓰는 마지막 줄은 훑기가 멈춘 뒤에 뜬다 (누르면 훑기를 끝까지 당긴다).
            bool lastOnPan = IsTall(current) && (i + 1 == _scene.Lines.Length || _scene.Lines[i + 1].Image != null);
            if (lastOnPan && _pan != null && _pan.IsActive())
            {
                _waitingPan = true;
                yield return _pan.WaitForCompletion();
                _waitingPan = false;
            }

            ShowText(Managers.GetString(line.ScriptId));
            try { _onLine?.Invoke(i + 1); }
            catch (Exception e) { Debug.LogException(e); }
            _advance = false;
            while (_advance == false)
                yield return null;
        }

        _textShown = false;
        _text.text = "";
        _shade.enabled = false;
        if (_arrow != null)
            _arrow.SetActive(false);
        if (_keep)
        {
            Done();         // 그림은 남긴다 — 다음 대화가 이 위에 뜬다
            yield break;
        }
        yield return _group.DOFade(0f, Quick(0.5f)).SetLink(gameObject).WaitForCompletion();
        Done();
        ClosePopupUI();
    }

    IEnumerator CoPicture(Sprite sprite)
    {
        _pan?.Kill();
        _pan = null;
        Place(_front, sprite);
        _front.color = new Color(1f, 1f, 1f, 0f);
        yield return _front.DOFade(1f, Quick(FadeTime)).SetLink(gameObject).WaitForCompletion();
        Place(_back, sprite);
        _back.color = Color.white;
        _front.color = new Color(1f, 1f, 1f, 0f);
        if (IsTall(sprite))
        {
            float travel = _back.rectTransform.sizeDelta.y - ((RectTransform)transform).rect.height;
            _pan = _back.rectTransform.DOAnchorPosY(-Mathf.Max(0f, travel), StoryUI.Auto ? 1.2f : PanSeconds)
                .SetEase(Ease.InOutSine).SetLink(gameObject);
        }
    }

    void ShowText(string value)
    {
        _shade.enabled = true;
        _text.text = value;
        _textFrame = Time.frameCount;
        _textShown = true;
        _allShown = false;
        _autoTimer = 0f;
    }

    void Update()
    {
        if (_finished || _closing || _scene == null)
            return;
        bool pressed = Managers.UI.TopPopup == this && StoryUI.NextPressed();
        if (_waitingPan)
        {
            if (pressed && _pan != null)
                _pan.Complete();
            return;
        }
        if (_textShown == false)
            return;

        if (_animator == null || (_animator.allLettersShown && Time.frameCount > _textFrame + 1))
            _allShown = true;
        if (pressed)
        {
            if (_allShown == false)
            {
                _animator.SetVisibilityEntireText(true);
                _allShown = true;
            }
            else
            {
                _advance = true;
            }
        }
        if (_arrow != null)
            _arrow.SetActive(_allShown);

        if (StoryUI.Auto && _allShown)
        {
            _autoTimer += Time.unscaledDeltaTime;
            if (_autoTimer >= StoryUI.AutoDelay)
                _advance = true;
        }
    }
    #endregion

    #region 챕터 카드
    void BuildTitle(int titleId, int subtitleId)
    {
        _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        Image black = StoryUI.NewImage(transform, "Black", null, Color.black);
        StoryUI.Stretch(black.rectTransform);
        black.raycastTarget = true;

        GameObject titleGo = StoryUI.Clone("UI_StageNamePopup", "StageNameText", transform);
        TMP_Text title = titleGo != null ? titleGo.GetComponent<TMP_Text>()
            : StoryUI.NewText(transform, "StageNameText", StoryUI.ProseFont, 110f, TextAlignmentOptions.Center);
        RectTransform rt = title.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 60f);
        rt.sizeDelta = new Vector2(1600f, 180f);
        title.fontSize = 110f;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.text = Managers.GetString(titleId);

        // 층 이름 팝업의 장식선(시작·선·끝). 프리팹에서는 투명하게 두고 팝업이 서서히 올린다.
        GameObject deco = StoryUI.Clone("UI_StageNamePopup", "StageNameDeco", transform);
        if (deco != null)
        {
            ((RectTransform)deco.transform).anchoredPosition = new Vector2(0f, 10f);
            foreach (Image image in deco.GetComponentsInChildren<Image>(true))
                image.color = Color.white;
        }

        TMP_Text sub = StoryUI.NewText(transform, "Subtitle", StoryUI.ProseFont, 56f, TextAlignmentOptions.Center);
        sub.rectTransform.sizeDelta = new Vector2(1600f, 100f);
        sub.rectTransform.anchoredPosition = new Vector2(0f, -150f);
        sub.color = new Color(0.82f, 0.82f, 0.82f, 1f);
        sub.text = Managers.GetString(subtitleId);
    }

    IEnumerator CoTitle()
    {
        yield return _group.DOFade(1f, Quick(0.6f)).SetLink(gameObject).WaitForCompletion();
        float hold = StoryUI.Auto ? 0.4f : TitleHold;
        for (float t = 0f; t < hold; t += Time.unscaledDeltaTime)
        {
            if (t > 0.5f && Managers.UI.TopPopup == this && StoryUI.NextPressed())
                break;
            yield return null;
        }
        yield return _group.DOFade(0f, Quick(0.6f)).SetLink(gameObject).WaitForCompletion();
        Done();
        ClosePopupUI();
    }
    #endregion
}
