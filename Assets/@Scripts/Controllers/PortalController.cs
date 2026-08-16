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
                // 여기서 CurStageid 를 올리면 안 된다. 아래 LoadingAndWarp 가
                // SetStageID() 로 한 번 더 올리기 때문에 챕터를 넘을 때마다
                // 층 번호가 두 칸씩 뛴다. 그러면 플레이어는 새 챕터 1층에 서 있는데
                // 게임은 한 층 위라고 여겨서, 카메라 경계와 맵 갱신이 엉뚱한 층을
                // 향하고 캐릭터가 다른 층 벽에 파묻혀 보인다.
                Managers.Game.GenerateMap(Managers.Game.PlayerData.CurStageid + 1);
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
