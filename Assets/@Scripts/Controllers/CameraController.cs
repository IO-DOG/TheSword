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
    int _resolutionIndex = 0;

    //float _angle = 60f; // 원하는 x축 회전 각도
    public float _scaleMultiplier;
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
        _vCam = GetComponent<CinemachineVirtualCamera>();
        _vCam.Follow = Managers.Game.Player.transform;

        _transposer = _vCam.GetCinemachineComponent<CinemachineTransposer>();
        _transposer.m_FollowOffset = new Vector3(0f, 10f, -5f);
    }

    private void Update()
    {
        if (_isCombineMap)
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

    public void SetCameraTarget(GameObject target)
    {
        GetComponent<CinemachineVirtualCamera>().Follow = target.transform;
        GetComponent<CinemachineVirtualCamera>().LookAt = null;
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

    public void SetupCameraConfiner()
    {
        string curDungeonName = $"Dungeon_{Managers.Data.StageInfoDic[Managers.Game.PlayerData.CurStageid].DungeonID}";
        _bg = GameObject.Find(curDungeonName).transform.Find("Deco/BG").gameObject.GetComponent<SpriteRenderer>();

        if (confinerCollider != null)
            Managers.Resource.Destroy(confinerCollider);
        confinerCollider = new GameObject { name = "Confiner" };
        confinerCollider.transform.Rotate(Define.CAMERA_ANGLE, 0, 0);

        _collider = confinerCollider.AddComponent<BoxCollider>();
        _collider.size = new Vector3(_bg.bounds.size.x, _bg.bounds.size.z * Mathf.Sqrt(3) / 2, Define.CONFINER_HEIGHT);

        float offsetY = Mathf.Sin(Mathf.Deg2Rad * Define.CAMERA_ANGLE) * (_collider.size.z / 2);
        _collider.center = new Vector3(0, _collider.size.y, 0);
        confinerCollider.transform.position = new Vector3(_bg.bounds.min.x + _bg.bounds.size.x / 2 + Define.TILE_SIZE / 2, 0, _bg.bounds.min.z + _bg.bounds.center.z + -Define.TILE_SIZE / 2);

        // Cinemachine Confiner 설정
        _confiner = _vCam.GetComponent<CinemachineConfiner>();
        _confiner.m_BoundingVolume = _collider;
        _confiner.InvalidatePathCache();
    }

}
