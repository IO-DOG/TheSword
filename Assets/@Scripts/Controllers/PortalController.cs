using Cinemachine;
using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

public class PortalController : MonoBehaviour
{
    public enum Type
    {
        None,
        UpStairs,
        DownStairs,
        Boss,
    }

    public Type _portalType = Type.None;
    public int _mapId;

    public void UsePortal()
    {
        StartCoroutine(CoUsePortal());
    }

    IEnumerator CoUsePortal()
    {
        if (Managers.Game.OnFade || Managers.Game.OnInteract)
            yield break;
        Vector3 nextPos = Vector3.zero;

        if (_portalType == Type.UpStairs)
        {
            PortalController tartgetPortal = SearchPortal(_mapId + 1, Type.DownStairs);
            if (tartgetPortal == null)
                yield break;

            bool ch = true;
            // todo 최초 진입인지 확인
            if (Managers.Game.PlayerData.FirstEnterMapCheck[_mapId + 1] == false)
            {
                ch = false;
                Managers.Game.PlayerData.FirstEnterMapCheck[_mapId + 1] = true;

                if (_mapId + 1 != 2)
                {
                    yield return StartCoroutine(Managers.Game.GameScene.CoShowLoadingIllust());
                    Managers.Sound.FadeInBGM(1f);
                }
                else
                {
                    yield return StartCoroutine(Managers.Game.GameScene.CoShowMagicSwordAni());
                    Managers.Sound.FadeInBGM(2f);
                }
            }

            // 다음 챕터 처리
            // 플레이어의 스테이지 아이디가 해당 챕터의 마지막 아이디라면 
            if (Managers.Game.PlayerData.CurStageid + 1 > Managers.Game.GetChapterCount(Managers.Game.PlayerData.CurStageid).Value)
            {
                Managers.Game.GenerateMap(++Managers.Game.PlayerData.CurStageid);
                nextPos = Managers.Game.SpawnPoints[0].transform.position;
                PlayerPrefs.SetInt("ISMEETBOSS", 0);
            }
            else
            {
                //Managers.Game.PlayerData.CurStageid++;
                nextPos = tartgetPortal.transform.position;
            }

            if (ch)
                CoStartWait(nextPos);
            else
                LoadingAndWarp(nextPos);
            yield break;
        }
        else if (_portalType == Type.DownStairs)
        {
            PortalController tartgetPortal = SearchPortal(_mapId - 1, Type.UpStairs);
            if (tartgetPortal == null)
                yield break;

            nextPos = tartgetPortal.transform.position;
            //Managers.Game.PlayerData.CurStageid--;
            CoStartWait(nextPos);
        }
        else // 보스룸 입장
        {
            nextPos = Managers.Game.SpawnPoints[1].transform.position;
            PlayerPrefs.SetInt("ISMEETBOSS", 1);
            yield return StartCoroutine(Managers.Game.GameScene.CoShowLoadingIllust());
            Managers.Sound.FadeInBGM(1f);
            LoadingAndWarp(nextPos);
            //Managers.Game.PlayerData.CurStageid = Managers.Game.BossRoomId;
        }
        Debug.Log($"Setting player position to: {nextPos}");
        //Managers.Game.SaveGame();
    }

    PortalController SearchPortal(int targetmapId, Type targetType)
    {
        for (int i = 0; i < Managers.Game.Portals.Length; i++)
        {
            PortalController targetPortal = Managers.Game.Portals[i].GetComponent<PortalController>();
            if (targetPortal._mapId == targetmapId && targetPortal._portalType == targetType)
            {
                return targetPortal;
            }
        }

        Debug.Log("포탈 없음");
        Managers.Directing.Events.CoStartEndingScene();
        return null;
    }

    int SetStageID()
    {
        if (_portalType == Type.UpStairs)
        {
            return ++Managers.Game.PlayerData.CurStageid;
        }
        else if (_portalType == Type.DownStairs)
        {
            return --Managers.Game.PlayerData.CurStageid; ;
        }
        else
        {
            Managers.Game.PlayerData.CurStageid = Managers.Game.BossRoomId;
            return Managers.Game.PlayerData.CurStageid;
        }
    }

    void CoStartWait(Vector3 nextPos)
    {
        StartCoroutine(WaitAndWarp(nextPos));
    }
    IEnumerator WaitAndWarp(Vector3 nextPos)
    {
        Managers.Game.OnInteract = true;
        yield return new WaitForSeconds(0.2f);
        Managers.Game.Player.SetIdleState(Managers.Game.Player._moveDir);
        Managers.Game.OnFadeAction.Invoke(0.3f);
        int nextStageID = SetStageID();
        if (nextStageID == 2)
        {
            Managers.Game.OnStaticResolution = true;
        }
        else
        {
            Managers.Game.OnStaticResolution = false;
        }
        yield return new WaitForSeconds(0.03f);
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetupCameraConfiner();
        Debug.Log($"Setting player position to2: {nextPos}");

        Managers.Game.Player.transform.position = nextPos;
        Managers.Game.Player._cellPos = nextPos;
        //Managers.Game.SaveGame();

        Managers.Game.OnPortalAction.Invoke();
        Managers.Game.GameScene.Refresh();
        Managers.Game.OnInteract = false;

        Managers.UI.ShowStageNamePopup(1f);
    }

    void LoadingAndWarp(Vector3 nextPos)
    {
        //StartCoroutine(CoLoadingAndWarp(nextPos));

        Managers.Game.OnInteract = true;
        //yield return new WaitForSeconds(0.2f);
        Managers.Game.Player.SetIdleState(Managers.Game.Player._moveDir);
        Managers.Game.OnFadeAction.Invoke(0.3f);
        int nextStageID = SetStageID();
        if (nextStageID == 2)
        {
            Managers.Game.OnStaticResolution = true;
        }
        else
        {
            Managers.Game.OnStaticResolution = false;
        }
        //yield return new WaitForSeconds(0.03f);
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetupCameraConfiner();
        Debug.Log($"Setting player position to2: {nextPos}");

        Managers.Game.Player.transform.position = nextPos;
        Managers.Game.Player._cellPos = nextPos;
        //Managers.Game.SaveGame();

        Managers.Game.OnPortalAction.Invoke();
        Managers.Game.GameScene.Refresh();
        Managers.Game.OnInteract = false;

        Managers.UI.ShowStageNamePopup(1f);
    }
    IEnumerator CoLoadingAndWarp(Vector3 nextPos)
    {
        yield return null;
        Managers.Game.OnInteract = true;
        //yield return new WaitForSeconds(0.2f);
        Managers.Game.Player.SetIdleState(Managers.Game.Player._moveDir);
        Managers.Game.OnFadeAction.Invoke(0.3f);
        int nextStageID = SetStageID();
        if (nextStageID == 2)
        {
            Managers.Game.OnStaticResolution = true;
        }
        else
        {
            Managers.Game.OnStaticResolution = false;
        }
        //yield return new WaitForSeconds(0.03f);
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetupCameraConfiner();
        Debug.Log($"Setting player position to2: {nextPos}");

        Managers.Game.Player.transform.position = nextPos;
        Managers.Game.Player._cellPos = nextPos;
        //Managers.Game.SaveGame();

        Managers.Game.OnPortalAction.Invoke();
        Managers.Game.GameScene.Refresh();
        Managers.Game.OnInteract = false;

        Managers.UI.ShowStageNamePopup(1f);
    }

}
