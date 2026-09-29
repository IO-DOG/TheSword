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

        // 목록이 비어 있으면 아래에서 인덱싱하다 터지고 계단이 죽는다.
        Managers.Game.PlayerData.EnsureLists();

        Vector3 nextPos = Vector3.zero;

        if (_portalType == Type.UpStairs)
        {
            PortalController tartgetPortal = SearchPortal(_mapId + 1, Type.DownStairs);

            // 다음 층 자체가 없으면 그때가 진짜 끝이다.
            int nextStage = Managers.Game.PlayerData.CurStageid + 1;
            if (Managers.Data.StageInfoDic.ContainsKey(nextStage) == false)
            {
                Managers.Directing.Events.CoStartEndingScene();
                yield break;
            }

            // 챕터가 바뀌는 자리에서는 다음 챕터 맵이 아직 없어 포탈을 못 찾는다.
            // 그때는 아래에서 GenerateMap 으로 새로 만들고 스폰 지점으로 간다.
            bool chapterEdge =
                nextStage > Managers.Game.GetChapterCount(Managers.Game.PlayerData.CurStageid).Value;
            if (tartgetPortal == null && chapterEdge == false)
                yield break;

            // 처음 오는 층인가는 표시하기 전에 잰다 (GameEvents.FloorEntered 가 받는다).
            bool firstVisit = Managers.Game.PlayerData.FirstEnterMapCheck[_mapId + 1] == false;
            if (firstVisit)
            {
                Managers.Game.PlayerData.FirstEnterMapCheck[_mapId + 1] = true;
                // 삽화가 도는 몇 초도 층을 옮기는 중이다. 표시가 LoadingAndWarp 에만 있어서 그 사이
                // 워프(Tab·HUD)가 끼어들었고, 그 GenerateMap 이 이 계단을 부수면서 삽화 코루틴까지
                // 같이 죽어 화면이 가려진 채 남았다. 푸는 것은 LoadingAndWarp 가 한다.
                Managers.Game.OnInteract = true;

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

            if (chapterEdge)
            {
                // 다음 챕터 맵은 새로 짓는데, 그 GenerateMap 이 이 계단까지 부순다.
                // 예전에는 다녀온 챕터면 부순 뒤에 이 계단 위에서 WaitAndWarp 를 시작해서,
                // 한 프레임 뒤 계단과 함께 코루틴이 사라지고 OnInteract 가 켜진 채 멈췄다.
                // 기다릴 것(계단 칸으로 내딛는 한 발)은 부수기 전에 기다리고, 부순 뒤로는
                // 한 번에 끝낸다.
                //
                // 여기서 CurStageid 를 올리면 안 된다. LoadingAndWarp 의 SetStageID() 가 올린다 —
                // 두 번 올리면 챕터를 넘을 때마다 층 번호가 두 칸씩 뛴다.
                if (firstVisit == false)
                {
                    Managers.Game.OnInteract = true;
                    yield return new WaitForSeconds(0.2f);
                }
                Managers.Game.GenerateMap(nextStage);
                Transform arrival = Managers.Game.ArrivalPoint(nextStage);
                LoadingAndWarp(arrival != null ? arrival.position : Managers.Game.Player.transform.position, firstVisit);
                yield break;
            }

            nextPos = tartgetPortal.transform.position;
            if (firstVisit)
                LoadingAndWarp(nextPos, true);
            else
                CoStartWait(nextPos, false);
            yield break;
        }
        else if (_portalType == Type.DownStairs)
        {
            PortalController tartgetPortal = SearchPortal(_mapId - 1, Type.UpStairs);
            if (tartgetPortal == null)
                yield break;

            nextPos = tartgetPortal.transform.position;
            CoStartWait(nextPos, false);
        }
        else // 보스룸 입장
        {
            nextPos = Managers.Game.SpawnPoints[1].transform.position;
            PlayerPrefs.SetInt("ISMEETBOSS", 1);
            // 보스방은 계단이 아니라 이 문으로만 들어와서 "처음 밟은 층" 표시가 빠져 있었다.
            bool firstVisit = Managers.Game.PlayerData.FirstEnterMapCheck[Managers.Game.BossRoomId] == false;
            Managers.Game.PlayerData.FirstEnterMapCheck[Managers.Game.BossRoomId] = true;
            Managers.Game.OnInteract = true;   // 삽화 동안에도 옮기는 중이다 (위의 계단과 같다)
            yield return StartCoroutine(Managers.Game.GameScene.CoShowLoadingIllust());
            Managers.Sound.FadeInBGM(1f);
            LoadingAndWarp(nextPos, firstVisit);
        }
        Debug.Log($"Setting player position to: {nextPos}");
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

        // 챕터 경계에서는 다음 챕터의 맵이 아직 없어서 못 찾는 게 정상이다.
        // 예전에는 여기서 엔딩을 띄워, 20층 보스를 잡고 계단을 밟으면
        // 21층으로 가는 대신 게임이 끝나 버렸다.
        // 진짜 끝인지 아닌지는 부르는 쪽이 판단한다.
        Debug.LogWarning($"[포탈] {targetmapId} 층의 {targetType} 를 못 찾았다");
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

    void CoStartWait(Vector3 nextPos, bool firstVisit)
    {
        StartCoroutine(WaitAndWarp(nextPos, firstVisit));
    }
    IEnumerator WaitAndWarp(Vector3 nextPos, bool firstVisit)
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

        Managers.Game.OnPortalAction.Invoke();
        Managers.Game.GameScene.Refresh();
        Managers.Game.OnInteract = false;

        // 층이 확정된 뒤다. 층 이름 팝업보다 먼저 — 듣는 쪽이 창을 띄우면 이름 팝업이 그 위에
        // 올라와야 제 차례에 스스로 닫힌다 (UIManager 는 맨 위 창만 닫는다).
        Managers.Game.EnterFloor(firstVisit);
        Managers.UI.ShowStageNamePopup(1f);
    }

    /// <summary>기다리지 않고 바로 옮긴다. 이미 기다린 뒤(로딩 삽화·계단을 부수기 전)에 부른다.
    /// 코루틴이 아니라서 GenerateMap 이 이 계단을 부순 프레임에 불러도 끝까지 돈다.</summary>
    void LoadingAndWarp(Vector3 nextPos, bool firstVisit)
    {
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

        Managers.Game.OnPortalAction.Invoke();
        Managers.Game.GameScene.Refresh();
        Managers.Game.OnInteract = false;

        Managers.Game.EnterFloor(firstVisit);
        Managers.UI.ShowStageNamePopup(1f);
    }
}
