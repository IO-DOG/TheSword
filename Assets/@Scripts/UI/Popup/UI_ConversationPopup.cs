using Data;
using Febucci.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ConversationPopup : UI_Popup
{
    public string _conversationName;
    List<Data.ConversationInfo> _conversationInfo;
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
        if(Input.GetKeyDown(KeyCode.Space))
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
        if (Managers.Data.ConversationDic.TryGetValue(conversationName, out Data.ConversationData data))
        {
            _conversationInfo = data.ConversationInfo;
            currentIndex = 0; // 대화 인덱스 초기화
            ShowCurrentConversation();
        }
    }

    private void ShowCurrentConversation()
    {
        if (currentIndex < _conversationInfo.Count)
        {
            Data.ConversationInfo info = _conversationInfo[currentIndex];
            string text = Managers.GetString(info.id);
            GetText((int)Texts.ConversationText).text = text;

            GetImage((int)Images.PlayerPortrait).sprite = Managers.Resource.Load<Sprite>(info.PlayerPortrait);
            GetImage((int)Images.OpponentPortrait).sprite = Managers.Resource.Load<Sprite>(info.OpponentPortrait);
            GetText((int)Texts.SpeakerText).text = info.Speaker;

            if (info.Speaker == "Player")
            {
                GetImage((int)Images.OpponentPortrait).color = Color.gray;
                GetImage((int)Images.PlayerPortrait).color = Color.white;
            }
            else
            {
                GetImage((int)Images.PlayerPortrait).color = Color.gray;
                GetImage((int)Images.OpponentPortrait).color = Color.white;
            }
        }
    }

    public void ShowNextConversation()
    {
        currentIndex++;
        if (currentIndex < _conversationInfo.Count)
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
