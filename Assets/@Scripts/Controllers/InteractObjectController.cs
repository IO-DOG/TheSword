using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractObjectController : MonoBehaviour
{
    int _eventID = Define.EVENT_SWORD_FIRST;

    public void Interact()
    {
        Managers.Game.CurEventID = _eventID;

        Managers.Directing.PlayDirecting(Managers.Game.CurEventID);
    }
}
