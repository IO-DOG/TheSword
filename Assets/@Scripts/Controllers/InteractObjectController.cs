using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractObjectController : MonoBehaviour
{
    int _eventID = Define.EVENT_SWORD_FIRST;

    public void Interact()
    {
        UI_ConversationPopup popup = Managers.UI.ShowPopupUI<UI_ConversationPopup>();
        popup._eventID = _eventID;
    }
}
