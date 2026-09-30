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
///            촌장 같은 이는 이름만, 내레이션은 이름도 없이 가운데 — 둘 다 곁의 초상화를 어둡게 세운다.
///            choice 장면은 마지막 줄 뒤에 선택지.
///   예전 대사 — CurEventID 를 정하고 ShowPopupUI 로 연다. EventData 를 Class 2 까지 넘긴다 (1~4층 연출).
/// Enter·Space·클릭으로 넘기고, Esc 는 삼킨다(대사를 끝까지 넘겨야 다음 연출로 이어진다).
/// Tab 을 누르고 있으면 끝까지 넘긴다(StoryDirector.WatchSkip → StoryUI.Skipping) — 줄마다 넘기는 것과 같은 길이라
/// 연출 신호가 다 친다. 선택지에서는 선다.
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
    const float BossBodyHeight = 460f;      // 보스 초상화에서 그려진 몸의 키 (캔버스 단위)
    const float BossBodyWidth = 620f;       // 넓은 놈(검은 태양)은 이 폭에서 멈춘다
    const float BossFromRight = 480f;       // 화면 오른끝에서 몸 한가운데까지 — 대화 상자 오른끝 안쪽에 선다
    const float BossDimFloor = 0.75f;       // 보스가 곁에 설 때(남이 말할 때)의 가장 어두운 밝기
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
    Color _rightDim = Dim;          // 오른쪽이 듣고 있을 때

    // 선택지
    RectTransform[] _choiceBoxes;
    TMP_Text[] _choiceTexts;
    Image[] _choiceMarks;
    bool _choosing;
    int _choice = -1;
    float _choiceOpenedAt;

    /// <summary>선택지를 띄우고 고르기를 기다린다 — 건너뛸 수 없다.</summary>
    public bool Choosing => _choosing;

    /// <summary>이야기 장면 하나를 연다. onLine 은 줄이 뜰 때(1부터), onDone 은 닫힐 때 한 번 (고른 선택지, 없으면 null).
    /// 창을 못 띄워도 onDone 은 부른다 — 기다리는 연출이 굳지 않게. 그때는 null 을 돌려준다.</summary>
    public static UI_ConversationPopup ShowStory(StoryScene scene, Action<int> onLine, Action<StoryChoice> onDone)
    {
        if (scene == null || scene.Lines.Length == 0 || Managers.Resource.Load<GameObject>(nameof(UI_ConversationPopup)) == null)
        {
            onDone?.Invoke(null);
            return null;
        }
        UI_ConversationPopup popup = Managers.UI.ShowPopupUI<UI_ConversationPopup>();
        popup._story = scene;
        popup._onLine = onLine;
        popup._onDone = onDone;
        return popup;
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
        StoryUI.EndSkip();      // 건너뛰던 창이 어떤 길로 사라지든, 다음 창까지 넘기지 않는다
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

        // 이름표는 한 줄. 긴 이름(Shieldbearer of the Aqueduct)은 두 줄로 꺾여 대화 상자 테두리에 걸렸다 — 넘치면 줄여 넣는다.
        // 자동 크기는 높이에도 맞추는데 프리팹 칸(높이 50)은 글자보다 낮아서 모든 이름이 최소 크기가 됐다 — 글자 높이만큼
        // 늘린다 (가운데 정렬이라 글 자리는 그대로다).
        TMP_Text speaker = GetText((int)Texts.SpeakerText);
        speaker.textWrappingMode = TextWrappingModes.NoWrap;
        speaker.fontSizeMax = speaker.fontSize;
        speaker.fontSizeMin = 30f;
        speaker.enableAutoSizing = true;
        speaker.rectTransform.sizeDelta += new Vector2(0f, speaker.fontSize);
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

    // 이름표는 줄마다 말하는 쪽(IllustLeft = 데미안, IllustRight = 마검)에서 정한다. EventData 1~27 은 줄마다 한쪽만 채운다.
    // 초상화를 먼저 켠다 — 감정 아이콘이 그 자식이라, 꺼진 채 튼 감정은 버려지고 켜질 때 기본 상태(AHA)가 떴다.
    private void ShowCurrentScript()
    {
        if (!string.IsNullOrEmpty(Managers.Data.EventDic[Managers.Game.CurEventID].IllustLeft))
        {
            string[] speaker = Managers.Data.EventDic[Managers.Game.CurEventID].IllustLeft.Split('_');

            GetObject((int)GameObjects.RightEmoji).SetActive(false);
            GetImage((int)Images.LeftPortrait).gameObject.SetActive(true);
            PlayEmotion(GetObject((int)GameObjects.LeftEmoji),
                speaker[2] == "Normal" ? null : Managers.Data.EventDic[Managers.Game.CurEventID].IllustLeft);

            GetImage((int)Images.LeftPortrait).sprite = Managers.Resource.Load<Sprite>(speaker[1]);
            GetImage((int)Images.RightPortrait).color = Color.gray;
            GetImage((int)Images.LeftPortrait).color = Color.white;

            GetText((int)Texts.SpeakerText).text = Managers.GetString(Define.PLAYER_DEFAULT_NAME);
        }

        if (!string.IsNullOrEmpty(Managers.Data.EventDic[Managers.Game.CurEventID].IllustRight))
        {
            string[] speaker = Managers.Data.EventDic[Managers.Game.CurEventID].IllustRight.Split('_');

            GetObject((int)GameObjects.LeftEmoji).SetActive(false);
            GetImage((int)Images.RightPortrait).gameObject.SetActive(true);
            PlayEmotion(GetObject((int)GameObjects.RightEmoji),
                speaker[2] == "Normal" ? null : Managers.Data.EventDic[Managers.Game.CurEventID].IllustRight);

            GetImage((int)Images.RightPortrait).sprite = Managers.Resource.Load<Sprite>(speaker[1]);
            GetImage((int)Images.RightPortrait).SetNativeSize();
            GetImage((int)Images.LeftPortrait).color = Color.gray;
            GetImage((int)Images.RightPortrait).color = Color.white;

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

    /// <summary>넘기기 한 번 — 사람의 Enter 와 같다. 줄이 다 안 보였으면 먼저 다 보이고, 다 보였으면 다음 줄.
    /// 마지막 줄 뒤에는 선택지를 띄우거나 닫는다. 자동 플레이 봇이 직접 부른다.</summary>
    public void ShowNextScript()
    {
        // 첫 줄은 Init(Start)이 띄운다. 그 전에 불리면 첫 줄이 두 번 돈다(연출 신호도 두 번).
        if (_init == false)
            return;
        // 봇은 방금 바뀐 줄도 곧장 넘겼다. 이름표·초상화는 바로 바뀌는데 타자기 글은 한두 프레임 늦어서, 마검 이름표가
        // 데미안의 "뭐야?!" 위에 찍힌 프레임이 녹화에 남았다. 사람처럼 한 번은 다 보이게 한다.
        if (_isAllTextShown == false && _choosing == false)
        {
            GetText((int)Texts.ConversationText).GetComponent<TextAnimator_TMP>().SetVisibilityEntireText(true);
            _isAllTextShown = true;
            return;
        }
        _isAllTextShown = false;
        _autoTimer = 0f;
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
            StoryUI.EndSkip();      // 이어 여는 다음 대사 창(PopupAction)까지 넘기지 않는다
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
                SetLeftDamian(left);
                PlayEmotion(leftEmoji, line.Emotion);
                break;
            case StoryPortrait.Sword:
                SetRightSword(right);
                PlayEmotion(rightEmoji, line.Emotion);
                break;
            case StoryPortrait.Boss:
                SetRightBoss(right, who.Chapter);
                break;
            default:
                // 초상화가 없는 화자(촌장·내레이션): 곁의 둘을 늘 어둡게 세운다. 예전에는 앞 줄에 누가 떴느냐에 따라
                // 비어 있거나 한쪽만 어두웠다. 오른쪽에 보스가 서 있으면 그대로 둔다.
                SetLeftDamian(left);
                if (right.gameObject.activeSelf == false)
                    SetRightSword(right);
                break;
        }
        left.color = who.Portrait == StoryPortrait.Damian ? Color.white : Dim;
        right.color = who.Portrait == StoryPortrait.Sword || who.Portrait == StoryPortrait.Boss ? _rightColor : _rightDim;

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

    // 감정 아이콘. null 이면(Normal — 마검에는 Normal 상태 자체가 없다) 끈다. 켠 뒤에 튼다 — 꺼진 애니메이터에 Play 를
    // 부르면 "Game object with animator is inactive" 경고만 찍히고 버려진다. 아이콘은 초상화의 자식이라 부르는 쪽이
    // 초상화를 먼저 켠다(activeSelf 로 보면 부모가 꺼진 것을 못 본다). 이야기의 상태 이름은
    // story_gen 이 Emoji.controller 와 대조해 둔다.
    static void PlayEmotion(GameObject emoji, string state)
    {
        Animator animator = emoji.GetComponent<Animator>();
        emoji.SetActive(string.IsNullOrEmpty(state) == false && animator != null);
        if (emoji.activeInHierarchy)
            animator.Play(state);
    }

    static void SetLeftDamian(Image left)
    {
        left.gameObject.SetActive(true);
        left.sprite = Managers.Resource.Load<Sprite>("Adventurer");
    }

    void SetRightSword(Image right)
    {
        if (_bossAnim != null)
            _bossAnim.enabled = false;
        right.gameObject.SetActive(true);
        right.sprite = Managers.Resource.Load<Sprite>("MagicalSword");
        right.preserveAspect = false;
        right.SetNativeSize();
        RectTransform rt = right.rectTransform;     // 프리팹 자리로 (보스가 옮겨 둔다)
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        _rightColor = Color.white;
        _rightDim = Dim;
    }

    // 보스의 초상화 자리 = 그 보스의 전투 그림 (대기 애니메이션, 맵과 같은 색조).
    // 몹 그림은 86px 칸의 아래쪽에 작게 서 있다(칸 높이의 35~80%). 칸을 한 크기로 키웠더니 작은 놈은 머리만
    // 대화 상자 위로 내밀고, 검은 태양은 화면을 넘었다 — 첫 장의 그려진 부분(촘촘한 메시)으로 배율을 정하고
    // 그 발밑을 대화 상자 위에 세운다. 배율은 정수로 — 도트가 고르게.
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
        // 색조는 색만 입히고 밝기는 빼지 않는다. ForBoss 가 0.8 을 곱해서 원래 검은 놈(잿빛 파수꾼)은 어두운 바닥에 묻혔고,
        // 데미안 차례에 반으로 더 어두워지면 거의 사라졌다. 곁에 설 때도 BossDimFloor 밑으로는 내리지 않는다.
        _rightColor = AtLeast(MonsterTint.Of(id), 1f);
        _rightColor.a = 1f;         // ForBoss 는 알파까지 0.8 을 곱한다 — 초상화 너머로 맵이 비쳤다
        _rightDim = AtLeast(_rightColor * Dim, BossDimFloor);

        Sprite s = right.sprite;
        if (s == null)
            return;
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        foreach (Vector2 v in s.vertices)
        {
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);
        }
        min = min * s.pixelsPerUnit + s.pivot;      // 칸 왼쪽 아래 기준 픽셀
        max = max * s.pixelsPerUnit + s.pivot;
        Vector2 body = max - min;
        float k = Mathf.Max(1f, Mathf.Floor(Mathf.Min(BossBodyHeight / body.y, BossBodyWidth / body.x)));
        RectTransform rt = right.rectTransform;
        RectTransform box = (RectTransform)GetObject((int)GameObjects.Speaker).transform.parent;   // CoversationBox
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2((min.x + max.x) * 0.5f / s.rect.width, min.y / s.rect.height);    // 몸의 발밑 가운데
        rt.anchoredPosition = new Vector2(-BossFromRight, box.rect.height);
        rt.sizeDelta = s.rect.size * k / rt.localScale.y;
    }

    // 가장 밝은 채널을 floor 까지 끌어올린다. 색의 비율과 알파는 그대로.
    static Color AtLeast(Color c, float floor)
    {
        float max = c.maxColorComponent;
        if (max >= floor || max <= 0f)
            return c;
        float k = floor / max;
        return new Color(c.r * k, c.g * k, c.b * k, c.a);
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
    // 마지막 대사를 둔 채 대화 상자 위에 둘을 쌓는다. 틀은 대화 상자 그림, 표시는 대화창 화살표를 빌린다.
    // 이름표 그림을 틀로 썼더니 오른쪽이 투명하게 빠져서, 가운데 놓은 글의 뒷반이 맵 위에 떠 읽히지 않았다.
    void OpenChoices()
    {
        StoryUI.EndSkip();      // 건너뛰기는 선택지 앞에서 선다 — 결말의 선택은 건너뛰지 못한다
        _choosing = true;
        _choice = -1;
        _choiceOpenedAt = Time.unscaledTime;
        GetImage((int)Images.ConversationArrow).gameObject.SetActive(false);
        // 마검의 감정 풍선이 첫 선택지의 오른쪽 위 모서리를 덮었다. 고르는 동안은 걷는다(고른 뒤에는 창이 닫힌다).
        GetObject((int)GameObjects.LeftEmoji).SetActive(false);
        GetObject((int)GameObjects.RightEmoji).SetActive(false);

        int count = _story.Choices.Length;
        Transform box = GetObject((int)GameObjects.Speaker).transform.parent;   // CoversationBox
        Sprite frame = box.GetComponent<Image>().sprite;
        Sprite arrow = GetImage((int)Images.ConversationArrow).sprite;
        TMP_FontAsset font = GetText((int)Texts.ConversationText).font;

        // 아무것도 골라져 있지 않아 Enter 만으로는 넘어가지 않는다 — 어떻게 고르는지 대화 상자 오른쪽 아래(넘기기 화살표
        // 자리)에 적는다. 선택지 위 맵에 두었더니 밝은 바닥에서는 읽히지 않았다.
        TMP_Text hint = StoryUI.NewText(box, "ChoiceHint", font, 36f, TextAlignmentOptions.BottomRight);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(1f, 0f);
        hint.rectTransform.sizeDelta = new Vector2(1200f, 50f);
        hint.rectTransform.anchoredPosition = new Vector2(-60f, 26f);
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
            rt.sizeDelta = new Vector2(860f, 110f);
            rt.anchoredPosition = new Vector2(0f, 110f + (count - 1 - i) * 126f);
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
