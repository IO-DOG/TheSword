using Data;
using Febucci.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ConversationPopup : UI_Popup
{
    public string _conversationName;
    List<Data.ScriptInfo> _scriptInfo;
    int currentIndex = 0;

    enum Texts
    {
        ConversationText,
        SpeakerText,
    }

    enum Images
    {
        PlayerPortrait,
        OpponentPortrait,
        ConversationArrow,
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        GetImage((int)Images.PlayerPortrait).gameObject.transform.localScale = new Vector3(-1, 1, 1);

        Managers.Game.OnConversation = true;
        InitConversation(_conversationName);

        return true;
    }

    private void Update()
    {
        if(!GetText((int)Texts.ConversationText).GetComponent<TextAnimator_TMP>().allLettersShown && Input.GetKeyDown(KeyCode.Space))
        {
            GetText((int)Texts.ConversationText).GetComponent<TextAnimator_TMP>().SetVisibilityEntireText(true);
        }
        else if(GetText((int)Texts.ConversationText).GetComponent<TextAnimator_TMP>().allLettersShown && Input.GetKeyDown(KeyCode.Space))
        {
            ShowNextConversation();
        }

        if(GetText((int)Texts.ConversationText).GetComponent<TextAnimator_TMP>().allLettersShown)
            GetImage((int)Images.ConversationArrow).gameObject.SetActive(true);
        else
            GetImage((int)Images.ConversationArrow).gameObject.SetActive(false);
    }

    public void InitConversation(string conversationName)
    {
        if (Managers.Data.ScriptDic.TryGetValue(conversationName, out Data.ScriptData data))
        {
            GetImage((int)Images.PlayerPortrait).gameObject.SetActive(false);
            GetImage((int)Images.OpponentPortrait).gameObject.SetActive(false);

            _scriptInfo = data.ScriptInfo;
            currentIndex = 0; // 대화 인덱스 초기화
            ShowCurrentConversation();
        }
    }

    private void ShowCurrentConversation()
    {
        if (currentIndex < _scriptInfo.Count)
        {
            Data.ScriptInfo info = _scriptInfo[currentIndex];
            string text = Managers.GetString(info.id);
            GetText((int)Texts.ConversationText).text = text;

            if (!string.IsNullOrEmpty(info.PlayerSprite))
            {
                GetImage((int)Images.PlayerPortrait).gameObject.SetActive(true);
                GetImage((int)Images.PlayerPortrait).sprite = Managers.Resource.Load<Sprite>(info.PlayerSprite);
            }
            else
            {
                GetImage((int)Images.PlayerPortrait).gameObject.SetActive(false);
            }

            if (!string.IsNullOrEmpty(info.OpponentSprite))
            {
                GetImage((int)Images.OpponentPortrait).gameObject.SetActive(true);
                GetImage((int)Images.OpponentPortrait).sprite = Managers.Resource.Load<Sprite>(info.OpponentSprite);
            }
            else
            {
                GetImage((int)Images.OpponentPortrait).gameObject.SetActive(false);
            }

            GetText((int)Texts.SpeakerText).text = info.Speaker;

            if (info.Speaker == "P" && !string.IsNullOrEmpty(info.PlayerSprite))
            {
                GetImage((int)Images.OpponentPortrait).color = Color.gray;
                GetImage((int)Images.PlayerPortrait).color = Color.white;
            }
            else if (info.Speaker == "O" && !string.IsNullOrEmpty(info.PlayerSprite))
            {
                GetImage((int)Images.PlayerPortrait).color = Color.gray;
                GetImage((int)Images.OpponentPortrait).color = Color.white;
            }
        }
    }

    public void ShowNextConversation()
    {
        currentIndex++;
        if (currentIndex < _scriptInfo.Count)
        {
            ShowCurrentConversation();
        }
        else
        {
            Debug.Log("Conversation ended");
            Managers.Game.OnConversation = false;
            ClosePopupUI();
        }
    }
}
