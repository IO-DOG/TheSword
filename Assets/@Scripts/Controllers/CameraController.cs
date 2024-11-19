using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    public SpriteRenderer _bg;

    public static bool _isCombineMap = false;

    // 픽셀 퍼펙트 카메라 해상도
    int[] _resolutionX = { 960, 640, 384, 320 };
    int[] _resolutionY = { 540, 360, 256, 80 };

    //float _angle = 60f; // 원하는 x축 회전 각도
    [HideInInspector]
    public float _scaleMultiplier = 2f;
    float _scrollSpeed = 10f;

    GameObject confinerCollider;
    CinemachineTransposer _transposer;
    CinemachineVirtualCamera _vCam;
    CinemachineConfiner _confiner;
    BoxCollider _collider;

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
        _scaleMultiplier = 2f;
        _vCam = GetComponent<CinemachineVirtualCamera>();
        _vCam.Follow = Managers.Game.Player.transform;

        _transposer = _vCam.GetCinemachineComponent<CinemachineTransposer>();
        _transposer.m_FollowOffset = new Vector3(0f, 10f, -5f);
    }

    private void Update()
    {
        if (_isCombineMap == true)
        {
            Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionX = _resolutionX[2];
            Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionY = _resolutionY[2];
        }
        else if (Managers.UI.GetPopupCount() == 0)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel") * _scrollSpeed * Time.deltaTime;

            Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionX = _resolutionX[Managers.Game.ResolutionIdx];
            Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionY = _resolutionY[Managers.Game.ResolutionIdx];

            if (scroll > 0 && Managers.Game.ResolutionIdx < _resolutionX.Length - 1)
            {
                Managers.Game.ResolutionIdx++;
                Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionX = _resolutionX[Managers.Game.ResolutionIdx];
                Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionY = _resolutionY[Managers.Game.ResolutionIdx];
            }
            else if (scroll < 0 && 0 < Managers.Game.ResolutionIdx)
            {
                Managers.Game.ResolutionIdx--;
                Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionX = _resolutionX[Managers.Game.ResolutionIdx];
                Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionY = _resolutionY[Managers.Game.ResolutionIdx];
            }
        }
    }

    public void SetCameraTarget(GameObject target)
    {
        GetComponent<CinemachineVirtualCamera>().Follow = target.transform;
        GetComponent<CinemachineVirtualCamera>().LookAt = null;
    }

    //public void ChangeView(float angle, GameObject go)
    //{
    //    _scaleMultiplier = 1 / Mathf.Cos(angle * Mathf.Deg2Rad);
    //    _playerOriginScale = Managers.Game.Player.transform.localScale;
    //    _goOriginScale = go.transform.localScale;

    //    if (go.GetComponent<PlayerController>() != null)
    //        go.transform.localScale = new Vector3(_playerOriginScale.x, _playerOriginScale.y * _scaleMultiplier, _playerOriginScale.z * _scaleMultiplier);
    //    else
    //        go.transform.localScale = new Vector3(_goOriginScale.x, _goOriginScale.y * _scaleMultiplier, _goOriginScale.z);
    //}

    public void SetupCameraConfiner()
    {
        string curDungeonName = $"Dungeon_{Managers.Data.StageInfoDic[Managers.Game.PlayerData.CurStageid].DungeonID}";
        _bg = GameObject.Find(curDungeonName).transform.Find("Decos/BG").gameObject.GetComponent<SpriteRenderer>();

        if (confinerCollider != null)
            Managers.Resource.Destroy(confinerCollider);
        confinerCollider = new GameObject { name = "Confiner" };
        confinerCollider.transform.Rotate(Define.CAMERA_ANGLE, 0, 0);

        _collider = confinerCollider.AddComponent<BoxCollider>();
        _collider.size = new Vector3(_bg.bounds.size.x, _bg.bounds.size.z * Mathf.Sqrt(3) / 2, Define.CONFINER_HEIGHT);
        _collider.center = new Vector3(0, _collider.size.y, 0);
        confinerCollider.transform.position = new Vector3(_bg.bounds.min.x + _bg.bounds.size.x / 2 + Define.TILE_SIZE / 2, 0, _bg.bounds.min.z + _bg.bounds.center.z + -Define.TILE_SIZE / 2);

        // Cinemachine Confiner 설정
        _confiner = _vCam.GetComponent<CinemachineConfiner>();
        _confiner.m_BoundingVolume = _collider;
        _confiner.InvalidatePathCache();
    }
}
