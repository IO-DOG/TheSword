using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DirectingManager
{
    public Action PopupAction;
    public Events Events = new Events();

    public void PlayDirecting(int eventId)
    {
        switch (eventId)
        {
            case 1:
                Events.CoStartEvent_1();
                PopupAction += (()=> Managers.UI.ShowPopupUI<UI_MagicalSwordCheckPopup>());
                break;
        }
    }
}

public class Events
{
    #region EVENT_1
    public void CoStartEvent_1()
    {
        CoroutineManager.StartCoroutine(EVENT_1());
    }
    IEnumerator EVENT_1()
    {
        Managers.Game.OnDirect = true;
        Managers.Game.Player.SetState(Define.PlayerState.IdleUp);

        #region #1
        {
            GameObject go = Managers.Resource.Instantiate(Managers.Data.EventDic[Managers.Game.CurEventID].HeroEmoji, Managers.Game.Player.transform);
            go.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);
            yield return new WaitForSeconds(Managers.Data.EventDic[Managers.Game.CurEventID].Delay);
            Managers.Resource.Destroy(go);
            Managers.Game.CurEventID++;
        }
        #endregion
        #region #2
        {
            Managers.Game.CurInteractObject.layer = (int)Define.Layer.Default;
            float originalSpeed = Managers.Game.CurPlayerData.MoveSpeed;
            Managers.Game.Player.Speed = 1f;
            Managers.Game.Player.Moving(Define.MoveDir.Up);
            yield return new WaitForSeconds(1f);
            Managers.Game.Player.SetState(Define.PlayerState.DrawSword);
            yield return new WaitForSeconds(1f);
            Managers.Game.Player.Moving(Define.MoveDir.Down);
            yield return new WaitForSeconds(1f);
            Managers.Game.Player.SetState(Define.PlayerState.IdleUp);
            Managers.Game.Player.Speed = originalSpeed;
            yield return new WaitForSeconds(1f);
            GameObject go = Managers.Resource.Instantiate(Managers.Data.EventDic[Managers.Game.CurEventID].HeroEmoji, Managers.Game.Player.transform);
            go.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);
            yield return new WaitForSeconds(Managers.Data.EventDic[Managers.Game.CurEventID].Delay);
            Managers.Resource.Destroy(go);
            Managers.Game.CurEventID++;
        }
        #endregion
        #region #3
        {
            Managers.Game.CurInteractObject.layer = (int)Define.Layer.InteractObjects;
            GameObject go = Managers.Resource.Instantiate(Managers.Data.EventDic[Managers.Game.CurEventID].OtherEmoji, Managers.Game.CurInteractObject.transform);
            go.transform.localPosition = new Vector3(0.15f, -0.4f, -1.7f);
            go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            yield return new WaitForSeconds(Managers.Data.EventDic[Managers.Game.CurEventID].Delay);
            Managers.Resource.Destroy(go);
            Managers.Game.CurEventID++;
        }
        #endregion
        #region #4
        {
            GameObject go = Managers.Resource.Instantiate(Managers.Data.EventDic[Managers.Game.CurEventID].HeroEmoji, Managers.Game.Player.transform);
            go.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);
            yield return new WaitForSeconds(Managers.Data.EventDic[Managers.Game.CurEventID].Delay);
            Managers.Resource.Destroy(go);
            Managers.Game.CurEventID++;
        }
        #endregion
        Managers.Game.OnDirect = false;
        Managers.UI.ShowPopupUI<UI_ConversationPopup>();
    }
    #endregion

    #region Contract Sword
    public void CoStartContractSword()
    {
        CoroutineManager.StartCoroutine(ContractSword());
    }

    IEnumerator ContractSword()
    {
        Managers.Game.OnDirect = true;

        GameObject go1 = Managers.Resource.Instantiate("FX_ContractSwordEffect", Managers.Game.Player.transform);
        go1.transform.localPosition = Vector3.zero;
        go1.transform.localScale = new Vector3(0.15f, 0.15f, 0.075f);

        GameObject go2 = Managers.Resource.Instantiate("FX_PowerWave", Managers.Game.Player.transform);
        go2.transform.localPosition = Vector3.zero;
        go2.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);

        Managers.Game.Player.SetState(Define.PlayerState.ContractSword);

        yield return new WaitForSeconds(4f);

        Managers.Resource.Destroy(go1);
        Managers.Resource.Destroy(go2);

        Managers.Game.Player.SetState(Define.PlayerState.IdleDown);

        Managers.Game.OnDirect = false;

        Managers.Game.Player._moveDir = Define.MoveDir.Down;
        Managers.Game.Player._isEquiptWeapon = true;
        Managers.Game.Player._isEquiptShield = true;
    }

    #endregion
}
