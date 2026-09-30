using Data;
using Febucci.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대화창. 두 길이 있다.
///   이야기 — StoryDirector 가 ShowStory 로 연다. 화자 표(GeneratedStory.Speakers)를 따른다:
///            데미안은 왼쪽 초상화, 마검은 오른쪽, 보스는 오른쪽 자리에 제 전투 그림(색조 입힘),
///            촌장 같은 이는 이름만, 내레이션은 초상화도 이름도 없이 가운데. choice 장면은 마지막 줄 뒤에 선택지.
///   예전 대사 — CurEventID 를 정하고 ShowPopupUI 로 연다. EventData 를 Class 2 까지 넘긴다 (1~4층 연출).
/// Enter·Space·클릭으로 넘기고, Esc 는 삼킨다(대사를 끝까지 넘겨야 다음 연출로 이어진다).
/// 자동 플레이 중에는 스스로 넘기고, 선택지는 "놓지 않는다(hold)" 를 고른다.
/// </summary>
public class UI_ConversationPopup : UI_Popup
{
    bool _endFlag = false;
    bool _isAllTextShown = false;
    enum Texts
    {
        ConversationText,
        SpeakerText,
    }

    enum Images
    {
        LeftPortrait,
        RightPortrait,
        ConversationArrow,
    }

    enum GameObjects
    {
        LeftEmoji,
        RightEmoji,
        Speaker,
    }

    const float ChoiceGuard = 0.6f;         // 선택지가 뜬 뒤 이 시간 동안은 누르지 않는다 — 대사를 넘기던 Enter 연타가 고르지 않게
    const float BossPortraitSize = 900f;
    static readonly Color Dim = new Color(0.5f, 0.5f, 0.5f, 1f);

    // 이야기 대사. null 이면 예전 EventData 대사다.
    StoryScene _story;
    int _line = -1;
    Action<int> _onLine;
    Action<StoryChoice> _onDone;
    bool _finished;
    float _autoTimer;
    // 글을 바꾼 프레임. 타자기가 새 글을 받기 전 한두 프레임은 옛 글을 두고 "다 보였다" 고 답한다.
    int _textFrame;

    // 오른쪽 자리는 마검과 보스가 나눠 쓴다. 보스는 전투창의 애니메이션을 그대로 튼다.
    Animator _bossAnim;
    Color _rightColor = Color.white;

    // 선택지
    RectTransform[] _choiceBoxes;
    TMP_Text[] _choiceTexts;
    Image[] _choiceMarks;
    bool _choosing;
    int _choice = -1;
    float _choiceOpenedAt;

    /// <summary>이야기 장면 하나를 연다. onLine 은 줄이 뜰 때(1부터), onDone 은 닫힐 때 한 번 (고른 선택지, 없으면 null).
    /// 창을 못 띄워도 onDone 은 부른다 — 기다리는 연출이 굳지 않게.</summary>
    public static void ShowStory(StoryScene scene, Action<int> onLine, Action<StoryChoice> onDone)
    {
        if (scene == null || scene.Lines.Length == 0 || Managers.Resource.Load<GameObject>(nameof(UI_ConversationPopup)) == null)
        {
            onDone?.Invoke(null);
            return;
        }
        UI_ConversationPopup popup = Managers.UI.ShowPopupUI<UI_ConversationPopup>();
        popup._story = scene;
        popup._onLine = onLine;
        popup._onDone = onDone;
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        Managers.Game.OnConversation = true;
        _locked = true;
        InitScript();

        return true;
    }

    // Esc 로는 닫히지 않는다. 대사를 끝까지 넘겨야 다음 연출(마검 계약 창 등)로 이어진다.
    public override bool OnEscape() => true;

    // 이 창이 건 대화 잠금. 어떤 길로 사라지든 풀고 간다.
    bool _locked;

    void OnDestroy()
    {
        if (_locked && Managers.IsAlive)
            Managers.Game.OnConversation = false;
        // 끝까지 넘기기 전에 사라졌다(씬이 내려갔거나 누가 창을 걷었다). 기다리는 쪽이 굳지 않게 알린다.
        if (_story != null && _finished == false)
        {
            _finished = true;
            try { _onDone?.Invoke(null); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    private void Awake()
    {
        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindObject(typeof(GameObjects));
        #endregion

        StoryUI.ApplyTextSpeed(GetText((int)Texts.ConversationText));
    }

    private void Update()
    {
        if (_choosing)
        {
            UpdateChoice();
            return;
        }

        TextAnimator_TMP animator = GetText((int)Texts.ConversationText).GetComponent<TextAnimator_TMP>();
        bool top = Managers.UI.TopPopup == this;
        // 마우스 클릭으로도 넘긴다. 위에 다른 창이 떠 있으면 그 창을 누른 것이지 대사를 넘긴 게 아니다.
        bool next = top && StoryUI.NextPressed();
        if(!_isAllTextShown && next)
        {
            animator.SetVisibilityEntireText(true);
            _isAllTextShown = true;
        }
        else if(_isAllTextShown && next)
        {
            ShowNextScript();
            _isAllTextShown = false;
        }

        if(animator.allLettersShown && Time.frameCount > _textFrame + 1)
            _isAllTextShown = true;

        GetImage((int)Images.ConversationArrow).gameObject.SetActive(_isAllTextShown && _choosing == false);

        // 자동 플레이: 다 보이면 잠깐 뒤에 넘긴다.
        if (StoryUI.Auto && top && _isAllTextShown)
        {
            _autoTimer += Time.unscaledDeltaTime;
            if (_autoTimer >= StoryUI.AutoDelay)
            {
                _autoTimer = 0f;
                ShowNextScript();
                _isAllTextShown = false;
            }
        }
        else
        {
            _autoTimer = 0f;
        }
    }

    public void InitScript()
    {
        GetObject((int)GameObjects.LeftEmoji).SetActive(false);
        GetObject((int)GameObjects.RightEmoji).SetActive(false);
        GetImage((int)Images.LeftPortrait).gameObject.SetActive(false);
        GetImage((int)Images.RightPortrait).gameObject.SetActive(false);
        if (_story != null)
            ShowStoryLine(0);
        else
            ShowCurrentScript();
    }

    private void ShowCurrentScript()
    {
        if (!string.IsNullOrEmpty(Managers.Data.EventDic[Managers.Game.CurEventID].IllustLeft))
        {
            string[] speaker = Managers.Data.EventDic[Managers.Game.CurEventID].IllustLeft.Split('_');

            GetObject((int)GameObjects.RightEmoji).SetActive(false);
            if (speaker[2] == "Normal")
                GetObject((int)GameObjects.LeftEmoji).SetActive(false);
            else
                GetObject((int)GameObjects.LeftEmoji).SetActive(true);

            GetImage((int)Images.LeftPortrait).gameObject.SetActive(true);
            GetImage((int)Images.LeftPortrait).sprite = Managers.Resource.Load<Sprite>(speaker[1]);
            GetImage((int)Images.RightPortrait).color = Color.gray;
            GetImage((int)Images.LeftPortrait).color = Color.white;

            GetObject((int)GameObjects.LeftEmoji).GetComponent<Animator>().Play(Managers.Data.EventDic[Managers.Game.CurEventID].IllustLeft);

            GetText((int)Texts.SpeakerText).text = Managers.GetString(Define.PLAYER_DEFAULT_NAME);
        }

        if (!string.IsNullOrEmpty(Managers.Data.EventDic[Managers.Game.CurEventID].IllustRight))
        {
            string[] speaker = Managers.Data.EventDic[Managers.Game.CurEventID].IllustRight.Split('_');

            GetObject((int)GameObjects.LeftEmoji).SetActive(false);
            if (speaker[2] == "Normal")
                GetObject((int)GameObjects.RightEmoji).SetActive(false);
            else
                GetObject((int)GameObjects.RightEmoji).SetActive(true);

            GetImage((int)Images.RightPortrait).gameObject.SetActive(true);
            GetImage((int)Images.RightPortrait).sprite = Managers.Resource.Load<Sprite>(speaker[1]);
            GetImage((int)Images.RightPortrait).SetNativeSize();
            GetImage((int)Images.LeftPortrait).color = Color.gray;
            GetImage((int)Images.RightPortrait).color = Color.white;

            GetObject((int)GameObjects.RightEmoji).GetComponent<Animator>().Play(Managers.Data.EventDic[Managers.Game.CurEventID].IllustRight);

            GetText((int)Texts.SpeakerText).text = Managers.GetString(Define.SWORD_DEFAULT_NAME);
        }

        string text = Managers.GetString(Managers.Data.ScriptDic[Managers.Data.EventDic[Managers.Game.CurEventID].ScriptID].id);
        GetText((int)Texts.ConversationText).text = text;
        _textFrame = Time.frameCount;


        if (Managers.Data.EventDic[Managers.Game.CurEventID].Class == (int)Define.EventClass.End)
        {
            _endFlag = true;
            return;
        }

        Managers.Game.CurEventID++;
    }

    /// <summary>다음 줄. 마지막 줄 뒤에는 선택지를 띄우거나 닫는다. 자동 플레이 봇이 직접 부른다.</summary>
    public void ShowNextScript()
    {
        if (_story != null)
        {
            NextStoryLine();
            return;
        }

        if (_endFlag == true)
        {
            Debug.Log("Conversation ended");
            Managers.Game.OnConversation = false;
            _locked = false;
            ClosePopupUI();

            if(Managers.Directing.PopupAction != null)
            {
                Managers.Directing.PopupAction.Invoke();
                Managers.Directing.PopupAction = null;
            }
            else
                Managers.UI.ShowGameSceneUI();

            return;
        }

        ShowCurrentScript();
    }

    #region 이야기 대사
    void NextStoryLine()
    {
        if (_finished)
            return;
        if (_choosing)
        {
            if (StoryUI.Auto)
                Choose(AutoChoice());
            return;
        }
        if (_line + 1 < _story.Lines.Length)
        {
            ShowStoryLine(_line + 1);
            return;
        }
        if (_story.Choices != null && _story.Choices.Length > 0)
        {
            OpenChoices();
            return;
        }
        Finish(null);
    }

    void ShowStoryLine(int index)
    {
        _line = index;
        StoryLine line = _story.Lines[index];
        StorySpeaker who = line.Who;
        Image left = GetImage((int)Images.LeftPortrait);
        Image right = GetImage((int)Images.RightPortrait);
        GameObject leftEmoji = GetObject((int)GameObjects.LeftEmoji);
        GameObject rightEmoji = GetObject((int)GameObjects.RightEmoji);
        leftEmoji.SetActive(false);
        rightEmoji.SetActive(false);

        bool narration = who.NameId == 0;
        switch (who.Portrait)
        {
            case StoryPortrait.Damian:
                left.gameObject.SetActive(true);
                left.sprite = Managers.Resource.Load<Sprite>("Adventurer");
                PlayEmotion(leftEmoji, line.Emotion);
                break;
            case StoryPortrait.Sword:
                SetRightSword(right);
                PlayEmotion(rightEmoji, line.Emotion);
                break;
            case StoryPortrait.Boss:
                SetRightBoss(right, who.Chapter);
                break;
        }
        if (narration)
        {
            left.gameObject.SetActive(false);
            right.gameObject.SetActive(false);
        }
        left.color = who.Portrait == StoryPortrait.Damian ? Color.white : Dim;
        right.color = who.Portrait == StoryPortrait.Sword || who.Portrait == StoryPortrait.Boss ? _rightColor : _rightColor * Dim;

        GetObject((int)GameObjects.Speaker).SetActive(narration == false);
        if (narration == false)
            GetText((int)Texts.SpeakerText).text = Managers.GetString(who.NameId);

        TMP_Text text = GetText((int)Texts.ConversationText);
        text.horizontalAlignment = narration ? HorizontalAlignmentOptions.Center : HorizontalAlignmentOptions.Left;
        text.text = Managers.GetString(line.ScriptId);
        _textFrame = Time.frameCount;
        _isAllTextShown = false;

        try { _onLine?.Invoke(index + 1); }
        catch (Exception e) { Debug.LogException(e); }
    }

    // 감정 아이콘. Normal 이나 없는 감정은 아이콘을 띄우지 않는다 (story_gen 이 상태 이름을 확인해 둔다).
    static void PlayEmotion(GameObject emoji, string state)
    {
        if (string.IsNullOrEmpty(state))
            return;
        emoji.SetActive(true);
        Animator animator = emoji.GetComponent<Animator>();
        if (animator != null)
            animator.Play(state);
    }

    void SetRightSword(Image right)
    {
        if (_bossAnim != null)
            _bossAnim.enabled = false;
        right.gameObject.SetActive(true);
        right.sprite = Managers.Resource.Load<Sprite>("MagicalSword");
        right.preserveAspect = false;
        right.SetNativeSize();
        _rightColor = Color.white;
    }

    // 보스의 초상화 자리 = 그 보스의 전투 그림 (대기 애니메이션, 맵과 같은 색조).
    void SetRightBoss(Image right, int chapter)
    {
        int id = chapter >= 0 && chapter < GeneratedStory.BossIds.Length ? GeneratedStory.BossIds[chapter] : -1;
        RuntimeAnimatorController controller = Managers.Resource.Load<RuntimeAnimatorController>("UIMonsterAnimController");
        if (Managers.Data.MonsterDic.TryGetValue(id, out MonsterData data) == false || controller == null)
        {
            right.gameObject.SetActive(false);
            return;
        }
        right.gameObject.SetActive(true);
        if (_bossAnim == null)
        {
            _bossAnim = right.gameObject.GetOrAddComponent<Animator>();
            _bossAnim.runtimeAnimatorController = controller;
        }
        _bossAnim.enabled = true;
        _bossAnim.Play(data.IdleAnimStr);
        _bossAnim.Update(0f);       // Play 만으로는 이 프레임에 그림이 바뀌지 않는다
        right.preserveAspect = true;
        right.rectTransform.sizeDelta = new Vector2(BossPortraitSize, BossPortraitSize);
        _rightColor = MonsterTint.Of(id);
    }

    void Finish(StoryChoice choice)
    {
        if (_finished)
            return;
        _finished = true;
        Managers.Game.OnConversation = false;
        _locked = false;
        ClosePopupUI();
        try { _onDone?.Invoke(choice); }
        catch (Exception e) { Debug.LogException(e); }
    }
    #endregion

    #region 선택지
    // 마지막 대사를 둔 채 대화 상자 위에 둘을 쌓는다. 틀은 이름표 그림, 표시는 대화창 화살표를 빌린다.
    void OpenChoices()
    {
        _choosing = true;
        _choice = -1;
        _choiceOpenedAt = Time.unscaledTime;
        GetImage((int)Images.ConversationArrow).gameObject.SetActive(false);

        int count = _story.Choices.Length;
        Transform box = GetObject((int)GameObjects.Speaker).transform.parent;   // CoversationBox
        Image plate = GetObject((int)GameObjects.Speaker).GetComponent<Image>();
        Sprite frame = plate != null ? plate.sprite : null;
        Sprite arrow = GetImage((int)Images.ConversationArrow).sprite;
        TMP_FontAsset font = GetText((int)Texts.ConversationText).font;

        // 아무것도 골라져 있지 않아 Enter 만으로는 넘어가지 않는다 — 어떻게 고르는지 적어 둔다.
        TMP_Text hint = StoryUI.NewText(box, "ChoiceHint", font, 36f, TextAlignmentOptions.Center);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        hint.rectTransform.pivot = new Vector2(0.5f, 0f);
        hint.rectTransform.sizeDelta = new Vector2(1200f, 50f);
        hint.rectTransform.anchoredPosition = new Vector2(0f, 110f + count * 116f);
        hint.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        hint.text = Managers.GetString(StoryUI.ChoiceHint);

        _choiceBoxes = new RectTransform[count];
        _choiceTexts = new TMP_Text[count];
        _choiceMarks = new Image[count];
        for (int i = 0; i < count; i++)
        {
            int index = i;
            RectTransform rt = StoryUI.Child(box, $"Choice{i}");
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(860f, 100f);
            rt.anchoredPosition = new Vector2(0f, 110f + (count - 1 - i) * 116f);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = frame;
            image.raycastTarget = true;

            TMP_Text label = StoryUI.NewText(rt, "Label", font, 50f, TextAlignmentOptions.Center);
            StoryUI.Stretch(label.rectTransform);
            label.text = Managers.GetString(_story.Choices[i].ScriptId);

            Image mark = StoryUI.NewImage(rt, "Mark", arrow, Color.white);
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            mark.rectTransform.sizeDelta = new Vector2(46f, 46f);
            mark.rectTransform.anchoredPosition = new Vector2(52f, 0f);
            mark.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);     // 아래를 가리키던 화살표를 옆으로

            rt.gameObject.BindEvent(() => Select(index), type: Define.UIEvent.PointerEnter);
            rt.gameObject.BindEvent(() => { if (Time.unscaledTime - _choiceOpenedAt >= ChoiceGuard) Choose(index); });

            _choiceBoxes[i] = rt;
            _choiceTexts[i] = label;
            _choiceMarks[i] = mark;
        }
        PaintChoices();
    }

    void UpdateChoice()
    {
        if (StoryUI.Auto)
        {
            if (Time.unscaledTime - _choiceOpenedAt >= StoryUI.AutoDelay)
                Choose(AutoChoice());
            return;
        }
        if (Managers.UI.TopPopup != this || Time.unscaledTime - _choiceOpenedAt < ChoiceGuard)
            return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            Select(Mathf.Max(0, _choice - 1));
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            Select(Mathf.Min(_choiceBoxes.Length - 1, _choice + 1));
        else if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) && _choice >= 0)
            Choose(_choice);
    }

    // 자동 플레이는 "놓지 않는다" 를 고른다 — 새벽 결말까지 가는 길이다.
    int AutoChoice()
    {
        int hold = Array.FindIndex(_story.Choices, c => c.Ending == StoryEnding.Hold);
        return hold >= 0 ? hold : 0;
    }

    void Select(int index)
    {
        if (_choosing == false || index == _choice)
            return;
        _choice = index;
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Choice_SFX");
        PaintChoices();
    }

    // 처음에는 아무것도 골라져 있지 않다 — 되돌릴 수 없는 선택이라 방향키로 먼저 짚어야 Enter 가 먹는다.
    void PaintChoices()
    {
        for (int i = 0; i < _choiceBoxes.Length; i++)
        {
            bool on = i == _choice;
            _choiceTexts[i].color = on ? Color.white : new Color(0.62f, 0.62f, 0.62f, 1f);
            _choiceMarks[i].enabled = on;
            _choiceBoxes[i].GetComponent<Image>().color = on ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
        }
    }

    void Choose(int index)
    {
        if (_choosing == false || index < 0 || index >= _story.Choices.Length)
            return;
        _choosing = false;
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Ok_SFX");
        Finish(_story.Choices[index]);
    }
    #endregion
}
