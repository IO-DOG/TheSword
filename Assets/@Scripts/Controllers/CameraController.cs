using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Progress;

public class CameraController : MonoBehaviour
{
    //// ToDo Object y position adjusting
    //float _angle = 60f; // 원하는 x축 회전 각도
    public float scaleMultiplier;

    Vector3 _goOriginScale;
    Vector3 _playerOriginScale;

    private void Start()
    {
        if(this.transform.parent.tag == "MainCamera")
            Managers.Game.MainCamera = this.transform.parent.GetComponent<Camera>();
        if (this.transform.parent.tag == "RenderCamera")
            Managers.Game.RenderCamera = this.transform.parent.GetComponent<Camera>();

        CinemachineTransposer transposer = GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>();
        transposer.m_FollowOffset = new Vector3(0f, 2f, 0f);
        this.transform.parent.eulerAngles = new Vector3(Define.CAMERA_ANGLE, 0f, 0f);
    }

    public void SetCameraConfiner()
    {
        Managers.Game.MainCamera.GetComponentInChildren<CinemachineVirtualCamera>().GetComponent<CinemachineConfiner>().m_BoundingVolume
            = GameObject.Find("Dungeon_" + Managers.Data.StageInfoDic[Managers.Game.CurPlayerData.CurStageid].DungeonID).GetComponent<BoxCollider>();

        Managers.Game.RenderCamera.GetComponentInChildren<CinemachineVirtualCamera>().GetComponent<CinemachineConfiner>().m_BoundingVolume
    = GameObject.Find("Dungeon_" + Managers.Data.StageInfoDic[Managers.Game.CurPlayerData.CurStageid].DungeonID).GetComponent<BoxCollider>();
    }

    public void SetCameraTarget(GameObject target)
    {
        GetComponent<CinemachineVirtualCamera>().Follow = target.transform;
        GetComponent<CinemachineVirtualCamera>().LookAt = null;
    }

    public void ChangeView(float angle, GameObject go)
    {
        scaleMultiplier = 1 / Mathf.Cos(angle * Mathf.Deg2Rad);
        _playerOriginScale = Managers.Game.Player.transform.localScale;
        _goOriginScale = go.transform.localScale;

        if (go.GetComponent<PlayerController>() != null)
            go.transform.localScale = new Vector3(_playerOriginScale.x, _playerOriginScale.y * scaleMultiplier, _playerOriginScale.z * scaleMultiplier);
        else
            go.transform.localScale = new Vector3(_goOriginScale.x, _goOriginScale.y * scaleMultiplier, _goOriginScale.z);
    }
}
