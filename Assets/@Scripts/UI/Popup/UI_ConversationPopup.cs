using Data;
using Febucci.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UI_ConversationPopup : UI_Popup
{
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

    enum GameObjects
    {
        LeftEmoji,
        RightEmoji,
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindObject(typeof(GameObjects));
        #endregion

        GetImage((int)Images.RightPortrait).gameObject.transform.localScale = new Vector3(-1, 1, 1);

        Managers.Game.OnConversation = true;
        InitScript();

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
            ClosePopupUI();

            if(Managers.Directing.PopupAction != null)
            {
                Managers.Directing.PopupAction.Invoke();
                Managers.Directing.PopupAction = null;
            }

            return;
        }

        ShowCurrentScript();
    }
}
