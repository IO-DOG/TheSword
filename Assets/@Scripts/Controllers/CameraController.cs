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

    static float x1, x2, y1, y2;

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

        CinemachineTransposer transposer = GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>();
        transposer.m_FollowOffset = new Vector3(0f, 2f, 0f);
        this.transform.parent.eulerAngles = new Vector3(Define.CAMERA_ANGLE, 0f, 0f);

        SetCameraExtent();
    }

    private void LateUpdate()
    {
        Vector3 pos = transform.parent.position;

        Debug.Log("Before" + transform.parent.position);

        if(pos.x != Mathf.Clamp(pos.x, _minBounds.x + horzExtent, _maxBounds.x - horzExtent) || pos.z != Mathf.Clamp(pos.z, _minBounds.z + verExtent, _maxBounds.z - verExtent))
        {
            pos.x = Mathf.Clamp(pos.x, _minBounds.x + horzExtent, _maxBounds.x - horzExtent);
            pos.z = Mathf.Clamp(pos.z, _minBounds.z + verExtent, _maxBounds.z - verExtent);

            transform.parent.position = pos;

            Debug.Log("After" + transform.parent.position);
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

        x1 = _minBounds.x;
        x2 = _maxBounds.x;
        y1 = _minBounds.z;
        y2 = _maxBounds.z;

        Debug.Log(_minBounds);
        Debug.Log(_maxBounds);
    }
}
