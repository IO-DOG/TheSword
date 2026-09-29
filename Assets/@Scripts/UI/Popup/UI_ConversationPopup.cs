using Data;
using Febucci.UI;
using Febucci.UI.Examples;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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
        if (_locked)
            Managers.Game.OnConversation = false;
    }

    private void Awake()
    {
        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindObject(typeof(GameObjects));
        #endregion

        GetText((int)Texts.ConversationText).GetComponent<TAnimSoundWriter>().source = Managers.Sound.GetAudioSource(Define.Sound.Effect);
    }

    private void Update()
    {
        // 마우스 클릭으로도 넘긴다. 위에 다른 창이 떠 있으면 그 창을 누른 것이지 대사를 넘긴 게 아니다.
        bool next = Managers.UI.TopPopup == this
            && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0));
        if(!_isAllTextShown && next)
        {
            GetText((int)Texts.ConversationText).GetComponent<TextAnimator_TMP>().SetVisibilityEntireText(true);
            _isAllTextShown = true;
        }
        else if(_isAllTextShown && next)
        {
            ShowNextScript();
            _isAllTextShown = false;
        }

        if(GetText((int)Texts.ConversationText).GetComponent<TextAnimator_TMP>().allLettersShown)
            _isAllTextShown = true;

        if (_isAllTextShown)
            GetImage((int)Images.ConversationArrow).gameObject.SetActive(true);
        else
            GetImage((int)Images.ConversationArrow).gameObject.SetActive(false);
    }

    public void InitScript()
    {
        GetObject((int)GameObjects.LeftEmoji).SetActive(false);
        GetObject((int)GameObjects.RightEmoji).SetActive(false);
        GetImage((int)Images.LeftPortrait).gameObject.SetActive(false);
        GetImage((int)Images.RightPortrait).gameObject.SetActive(false);
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


        if (Managers.Data.EventDic[Managers.Game.CurEventID].Class == (int)Define.EventClass.End)
        {
            _endFlag = true;
            return;
        }

        Managers.Game.CurEventID++;
    }

    public void ShowNextScript()
    {
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
}
