using Cinemachine;
using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class PortalController : MonoBehaviour
{
    public int _id;
    public int _mapId;

    public void UsePortal()
    {
        Vector3 nextPos = Vector3.zero;

        for(int i = 0; i < Managers.Game.Portals.Length; i++)
        {
            PortalController targetPortal = Managers.Game.Portals[i].GetComponent<PortalController>();
            if (_id == targetPortal._id && _mapId != targetPortal._mapId)
            {
                nextPos = targetPortal.gameObject.transform.position;
                CoStartWait(nextPos);

                return;
            }
        }

        if (_mapId == Managers.Game.BossRoomId)
            nextPos = Managers.Game.SpawnPoints[1].transform.position;
        else
            nextPos = Managers.Game.SpawnPoints[0].transform.position;
    }

    void CoStartWait(Vector3 nextPos)
    {
        StartCoroutine(WaitAndWarp(nextPos));
    }
    IEnumerator WaitAndWarp(Vector3 nextPos)
    {
        yield return new WaitForSeconds(0.2f);
        Managers.Game.Player.SetIdleState(Managers.Game.Player._moveDir);
        Managers.Game.OnDirect = true;
        Managers.Game.OnFadeAction.Invoke(0.3f);
        yield return new WaitForSeconds(0.03f);

        Managers.Game.Player.transform.position = nextPos;
        Managers.Game.Player._cellPos = nextPos;
        CameraController.SetConfinerBounds();

        int nextStageID = Managers.Game.PlayerData.CurStageid;

        if (nextStageID == 2)
        {
            CameraController._isCombineMap = true;
        }
        else
        {
            CameraController._isCombineMap = false;
        }
        Managers.Game.OnPortalAction.Invoke();
        Managers.Game.TutorialScene.Refresh();
        //Managers.Game.GameScene.Refresh();

        Managers.Game.OnDirect = false;
    }
}
