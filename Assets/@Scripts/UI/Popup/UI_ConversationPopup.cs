using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ConversationPopup : UI_Popup
{
    const string FADE_COLOR = "495057";

    private void Start()
    {
        Managers.Game.OnConversation = true;
    }
    private void Update()
    {
        if(Input.anyKeyDown)
        {
            Managers.Game.OnConversation = false;
            ClosePopupUI();
        }
    }
}
