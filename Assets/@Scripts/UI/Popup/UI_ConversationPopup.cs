using Data;
using Febucci.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UI_ConversationPopup : UI_Popup
{
    public int _eventID;
    bool _endFlag = false;

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

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        GetImage((int)Images.RightPortrait).gameObject.transform.localScale = new Vector3(-1, 1, 1);

        Managers.Game.OnConversation = true;
        InitScript(Managers.Data.EventDic[_eventID].ScriptID);

        return true;
    }

    private void Update()
    {
        if(!GetText((int)Texts.ConversationText).GetComponent<TextAnimator_TMP>().allLettersShown && Input.GetKeyDown(KeyCode.Return))
        {
            GetText((int)Texts.ConversationText).GetComponent<TextAnimator_TMP>().SetVisibilityEntireText(true);
        }
        else if(GetText((int)Texts.ConversationText).GetComponent<TextAnimator_TMP>().allLettersShown && Input.GetKeyDown(KeyCode.Return))
        {
            ShowNextScript();
        }

        if(GetText((int)Texts.ConversationText).GetComponent<TextAnimator_TMP>().allLettersShown)
            GetImage((int)Images.ConversationArrow).gameObject.SetActive(true);
        else
            GetImage((int)Images.ConversationArrow).gameObject.SetActive(false);
    }

    public void InitScript(int scriptCode)
    {
        GetImage((int)Images.LeftPortrait).gameObject.SetActive(false);
        GetImage((int)Images.RightPortrait).gameObject.SetActive(false);
        ShowCurrentScript();
    }

    private void ShowCurrentScript()
    {
        if (!string.IsNullOrEmpty(Managers.Data.EventDic[_eventID].IllustLeft))
        {
            GetImage((int)Images.LeftPortrait).gameObject.SetActive(true);
            GetImage((int)Images.LeftPortrait).sprite = Managers.Resource.Load<Sprite>(Managers.Data.EventDic[_eventID].IllustLeft);
            GetImage((int)Images.RightPortrait).color = Color.gray;
            GetImage((int)Images.LeftPortrait).color = Color.white;

            GetText((int)Texts.SpeakerText).text = "용사";
        }

        if (!string.IsNullOrEmpty(Managers.Data.EventDic[_eventID].IllustRight))
        {
            GetImage((int)Images.RightPortrait).gameObject.SetActive(true);
            GetImage((int)Images.RightPortrait).sprite = Managers.Resource.Load<Sprite>(Managers.Data.EventDic[_eventID].IllustRight);
            GetImage((int)Images.LeftPortrait).color = Color.gray;
            GetImage((int)Images.RightPortrait).color = Color.white;

            GetText((int)Texts.SpeakerText).text = "미지의 검";
        }

        string text = Managers.GetString(Managers.Data.ScriptDic[Managers.Data.EventDic[_eventID].ScriptID].id);
        GetText((int)Texts.ConversationText).text = text;


        if (Managers.Data.EventDic[_eventID].Class == (int)Define.EventClass.End)
        {
            _endFlag = true;
            return;
        }

        _eventID++;
    }

    public void ShowNextScript()
    {
        if (_endFlag == true)
        {
            Debug.Log("Conversation ended");
            Managers.Game.OnConversation = false;
            ClosePopupUI();
            return;
        }

        ShowCurrentScript();
    }
}
