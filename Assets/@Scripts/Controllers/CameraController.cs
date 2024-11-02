using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    public static bool _isCombineMap = false;

    // 픽셀 퍼펙트 카메라 해상도
    int[] _resolutionX = { 960, 640, 384, 320 };
    int[] _resolutionY = { 540, 360, 256, 80 };
    int _resolutionIndex = 0;

    //// ToDo Object y position adjusting
    //float _angle = 60f; // 원하는 x축 회전 각도
    public float _scaleMultiplier;
    float _scrollSpeed = 10f;

    public static Vector3 _minBounds;
    public static Vector3 _maxBounds;

    CinemachineVirtualCamera _vCam;

    float _verExtent;
    float _horzExtent;

    Vector3 _goOriginScale;
    Vector3 _playerOriginScale;

    private void Awake()
    {
        Managers.Game.MainCamera = this.transform.parent.GetComponent<Camera>();
    }
    private void Start()
    {
        _vCam = GetComponent<CinemachineVirtualCamera>();
        _vCam.Follow = Managers.Game.Player.transform;

        CinemachineTransposer transposer = _vCam.GetCinemachineComponent<CinemachineTransposer>();
        transposer.m_FollowOffset = new Vector3(0f, 10f, -5f);
        this.transform.parent.eulerAngles = new Vector3(Define.CAMERA_ANGLE, 0f, 0f);

        SetCameraExtent();
    }

    private void Update()
    {
        if(_isCombineMap)
        {
            Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionX = _resolutionX[2];
            Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionY = _resolutionY[2];
        }
        else if (Managers.UI.GetPopupCount() == 0)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel") * _scrollSpeed * Time.deltaTime;

            if (scroll > 0 && _resolutionIndex < _resolutionX.Length - 1)
            {
                _resolutionIndex++;
                Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionX = _resolutionX[_resolutionIndex];
                Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionY = _resolutionY[_resolutionIndex];
            }
            else if (scroll < 0 && 0 < _resolutionIndex)
            {
                _resolutionIndex--;
                Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionX = _resolutionX[_resolutionIndex];
                Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionY = _resolutionY[_resolutionIndex];
            }
        }
    }

    private void LateUpdate()
    {
       CameraUpdate();
    }

    void CameraUpdate()
    {
        if (Managers.Game.OnMeetKingSlime)
            return;

        Vector3 pos = Managers.Game.Player.transform.position;

        SetCameraExtent();
        float clampedZ = Mathf.Clamp(pos.z, _minBounds.z + _verExtent + Define.TILE_SIZE * 2.5f, _maxBounds.z - _verExtent - Define.TILE_SIZE * 3f);

        Vector3 curOffset = _vCam.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
        Vector3 targetOffset = new Vector3(curOffset.x, 10f, clampedZ - pos.z - (5.5f * (_resolutionIndex / 5 + 1)));

        _vCam.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = targetOffset;   
    }

    public void SetCameraTarget(GameObject target)
    {
        GetComponent<CinemachineVirtualCamera>().Follow = target.transform;
        GetComponent<CinemachineVirtualCamera>().LookAt = null;
    }

    // 해상도 변경할때 이거 필요할수도
    void SetCameraExtent()
    {
        _verExtent = Camera.main.orthographicSize;
        _horzExtent = _verExtent * Screen.width / Screen.height;
    }

    public void ChangeView(float angle, GameObject go)
    {
        _scaleMultiplier = 1 / Mathf.Cos(angle * Mathf.Deg2Rad);
        _playerOriginScale = Managers.Game.Player.transform.localScale;
        _goOriginScale = go.transform.localScale;

        if (go.GetComponent<PlayerController>() != null)
            go.transform.localScale = new Vector3(_playerOriginScale.x, _playerOriginScale.y * _scaleMultiplier, _playerOriginScale.z * _scaleMultiplier);
        else
            go.transform.localScale = new Vector3(_goOriginScale.x, _goOriginScale.y * _scaleMultiplier, _goOriginScale.z);
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
    }
}
