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

    static Vector3 _minBounds;
    static Vector3 _maxBounds;

    CinemachineVirtualCamera vCam;

    float verExtent;
    float horzExtent;

    Vector3 _goOriginScale;
    Vector3 _playerOriginScale;

    private void Start()
    {
        if(this.transform.parent.tag == "MainCamera")
            Managers.Game.MainCamera = this.transform.parent.GetComponent<Camera>();
        if (this.transform.parent.tag == "RenderCamera")
            Managers.Game.RenderCamera = this.transform.parent.GetComponent<Camera>();

        vCam = GetComponent<CinemachineVirtualCamera>();
        vCam.Follow = Managers.Game.Player.transform;

        CinemachineTransposer transposer = vCam.GetCinemachineComponent<CinemachineTransposer>();
        transposer.m_FollowOffset = new Vector3(0f, 20f, -10f);
        this.transform.parent.eulerAngles = new Vector3(Define.CAMERA_ANGLE, 0f, 0f);

        SetCameraExtent();
    }

    private void LateUpdate()
    {
        CameraUpdate();
    }

    void CameraUpdate()
    {
        Vector3 pos = Managers.Game.Player.transform.position;

        if (pos.x != Mathf.Clamp(pos.x, _minBounds.x + horzExtent + Define.TILE_SIZE / 3, _maxBounds.x - horzExtent - Define.TILE_SIZE / 3) ||
            pos.z != Mathf.Clamp(pos.z, _minBounds.z + verExtent + Define.TILE_SIZE / 1.5f, _maxBounds.z - verExtent - Define.TILE_SIZE / 1.5f))
        {
            float clampedX = Mathf.Clamp(pos.x, _minBounds.x + horzExtent + Define.TILE_SIZE / 3, _maxBounds.x - horzExtent - Define.TILE_SIZE / 3);
            float clampedZ = Mathf.Clamp(pos.z, _minBounds.z + verExtent + Define.TILE_SIZE/ 1.5f, _maxBounds.z - verExtent - Define.TILE_SIZE / 1.5f);

            Vector3 curOffset = vCam.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
            Vector3 targetOffset = new Vector3(clampedX - pos.x, 20, clampedZ - pos.z - 12);

            vCam.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = targetOffset;
        }
    }

    public void SetCameraTarget(GameObject target)
    {
        GetComponent<CinemachineVirtualCamera>().Follow = target.transform;
        GetComponent<CinemachineVirtualCamera>().LookAt = null;
    }

    // 해상도 변경할때 이거 필요할수도
    void SetCameraExtent()
    {
        verExtent = Camera.main.orthographicSize;
        horzExtent = verExtent * Screen.width / Screen.height;
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

    public static void SetConfinerBounds()
    {
        Bounds combineBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool boundsInitialized = false;

        Transform curMapTiles = GameObject.Find("Dungeon_" + Managers.Data.StageInfoDic[Managers.Game.CurPlayerData.CurStageid].DungeonID).transform.Find("Tiles");

        foreach (Transform child in curMapTiles)
        {
            BoxCollider collider = child.GetComponent<BoxCollider>();
            if (collider != null)
            {
                if (!boundsInitialized)
                {
                    combineBounds = collider.bounds;
                    boundsInitialized = true;
                }
                else
                {
                    combineBounds.Encapsulate(collider.bounds);
                }
            }
        }

        _minBounds = combineBounds.min;
        _maxBounds = combineBounds.max;

        Debug.Log(_minBounds);
        Debug.Log(_maxBounds);
    }
}
