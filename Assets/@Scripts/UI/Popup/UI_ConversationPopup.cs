using Data;
using Febucci.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UI_ConversationPopup : UI_Popup
{
    public int _scriptCode;
    List<ScriptData> _scripts;
    int _currentIndex = 0;

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
        InitScript(_scriptCode);

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
        _scripts = Managers.Data.LoadScriptData(scriptCode);

        GetImage((int)Images.PlayerPortrait).gameObject.SetActive(false);
        GetImage((int)Images.OpponentPortrait).gameObject.SetActive(false);

        _currentIndex = 0; // 대화 인덱스 초기화
        ShowCurrentScript();
    }

    private void ShowCurrentScript()
    {
        if (_currentIndex < _scripts.Count)
        {
            string text = Managers.GetString(_scripts[_currentIndex].id);
            GetText((int)Texts.ConversationText).text = text;

            if (!string.IsNullOrEmpty(_scripts[_currentIndex].PlayerSprite))
            {
                GetImage((int)Images.PlayerPortrait).gameObject.SetActive(true);
                GetImage((int)Images.PlayerPortrait).sprite = Managers.Resource.Load<Sprite>(_scripts[_currentIndex].PlayerSprite);
            }
            else
            {
                GetImage((int)Images.PlayerPortrait).gameObject.SetActive(false);
            }

            if (!string.IsNullOrEmpty(_scripts[_currentIndex].OpponentSprite))
            {
                GetImage((int)Images.OpponentPortrait).gameObject.SetActive(true);
                GetImage((int)Images.OpponentPortrait).sprite = Managers.Resource.Load<Sprite>(_scripts[_currentIndex].OpponentSprite);
            }
            else
            {
                GetImage((int)Images.OpponentPortrait).gameObject.SetActive(false);
            }

            GetText((int)Texts.SpeakerText).text = _scripts[_currentIndex].Speaker;

            if (_scripts[_currentIndex].Speaker == "P" && !string.IsNullOrEmpty(_scripts[_currentIndex].PlayerSprite))
            {
                GetImage((int)Images.OpponentPortrait).color = Color.gray;
                GetImage((int)Images.PlayerPortrait).color = Color.white;
            }
            else if (_scripts[_currentIndex].Speaker == "O" && !string.IsNullOrEmpty(_scripts[_currentIndex].PlayerSprite))
            {
                GetImage((int)Images.PlayerPortrait).color = Color.gray;
                GetImage((int)Images.OpponentPortrait).color = Color.white;
            }
        }
    }

    public void ShowNextScript()
    {
        _currentIndex++;
        if (_currentIndex < _scripts.Count)
        {
            ShowCurrentScript();
        }
        else
        {
            Debug.Log("Conversation ended");
            Managers.Game.OnConversation = false;
            ClosePopupUI();
        }
    }
}
