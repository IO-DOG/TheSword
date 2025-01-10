using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{ 
    public enum Exposure
    {
        Default,
        White,
        Black,
    }

    public SpriteRenderer _bg;

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
        _vCam = GetComponent<CinemachineVirtualCamera>();
    }
    private void Start()
    {
        _scaleMultiplier = 2f;
        _vCam = GetComponent<CinemachineVirtualCamera>();
        //_vCam.Follow = Managers.Game.Player.transform;

        _transposer = _vCam.GetCinemachineComponent<CinemachineTransposer>();

        Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionX = _resolutionX[Managers.Game.ResolutionIdx];
        Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionY = _resolutionY[Managers.Game.ResolutionIdx];
        //_transposer.m_FollowOffset = new Vector3(0f, 10f, -5f);
    }

    private void Update()
    {
        if (Managers.Game.OnStaticResolution == true)
        {
            Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionX = _resolutionX[2];
            Managers.Game.MainCamera.GetComponent<PixelPerfectCamera>().refResolutionY = _resolutionY[2];
        }
        else if (Managers.Game.OnDirect == true) return;
        else if (Managers.UI.GetPopupCount() == 0 || (Managers.UI.StageNamePopup != null && Managers.UI.GetPopupCount() == 1))
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

    public static IEnumerator CoExposure(float time, Exposure exposure)
    {
        Volume postProcessingVolume = Managers.Game.MainCamera.GetComponent<Volume>();
        ColorAdjustments colorAdjustment;

        if (postProcessingVolume.profile.TryGet<ColorAdjustments>(out colorAdjustment))
        {
            float startExposure = colorAdjustment.postExposure.value;
            float targetExposure;

            // Select Exposure Type
            if (exposure == Exposure.Default)
                targetExposure = Define.POSTPROCESSING_DEFAULT_EXPOSURE;
            else if (exposure == Exposure.White)
                targetExposure = Define.POSTPROCESSING_WHITE_EXPOSURE;
            else
                targetExposure = Define.POSTPROCESSING_BLACK_EXPOSURE;

            float elapsed = 0f; // 경과 시간

            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                colorAdjustment.postExposure.value = Mathf.Lerp(startExposure, targetExposure, elapsed / time);
                yield return null;
            }

            colorAdjustment.postExposure.value = targetExposure;
        }
    }

    public static IEnumerator CoShakeCamera(float time, float force = 5f)
    {
        var cinmemachineCamera = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>();
        var transposer = cinmemachineCamera.GetCinemachineComponent<CinemachineTransposer>();
        var defalutFllowOffset = transposer.m_FollowOffset;
        var noise = cinmemachineCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        #region Shake Camera
        noise.m_NoiseProfile = Managers.Resource.Load<NoiseSettings>("6D Shake");
        noise.enabled = true;
        noise.m_AmplitudeGain = force;

        yield return new WaitForSeconds(time);
        #endregion

        #region To Slow Stop Shake
        float duration = 0f; // 감쇄 시간
        float elapsed = 0f;
        float initialAmplitude = noise.m_AmplitudeGain;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            noise.m_AmplitudeGain = Mathf.Lerp(initialAmplitude, 0f, elapsed / duration);
            yield return null;
        }

        noise.m_AmplitudeGain = 0f;
        noise.enabled = false;
        #endregion

        #region To Set Slowly Default Camera Offset
        // set camera default offset
        float offsetElapsed = 0f;
        Vector3 curFllowOffset = transposer.m_FollowOffset;

        while (offsetElapsed < duration)
        {
            offsetElapsed += Time.deltaTime;
            transposer.m_FollowOffset.x = Mathf.Lerp(curFllowOffset.x, defalutFllowOffset.x, offsetElapsed / duration);
            transposer.m_FollowOffset.y = Mathf.Lerp(curFllowOffset.y, defalutFllowOffset.y, offsetElapsed / duration);
            transposer.m_FollowOffset.z = Mathf.Lerp(curFllowOffset.z, defalutFllowOffset.z, offsetElapsed / duration);
            yield return null;
        }
        transposer.m_FollowOffset = defalutFllowOffset;
        #endregion
    }

    public void SetVCamOffset(Transform transform, float time)
    {
        if (transform == null)
        {
            Debug.Log("SetVCamOffset - transform is null!");
            return;
        }

        Vector3 diff = transform.position - Managers.Game.Player.transform.position;
        Vector3 offset = new Vector3(diff.x, Define.DEFALUT_CAMERA_OFFSET.y, -diff.z);
        StartCoVirtualCameraMove(offset, time);
    }

    Coroutine CoVirtualCameraMove;
    void StartCoVirtualCameraMove(Vector3 offset, float time)
    {
        if (CoVirtualCameraMove != null)
            CoroutineManager.StopCoroutine(CoVirtualCameraMove);
        CoVirtualCameraMove = CoroutineManager.StartCoroutine(VirtualCameraMove(offset, time));
    }

    private IEnumerator VirtualCameraMove(Vector3 offset, float time)
    {
        float elapsedTime = 0f;

        Vector3 initOffset = _transposer.m_FollowOffset;

        while (elapsedTime < time)
        {
            float t = Mathf.Clamp01(elapsedTime / time);
            _transposer.m_FollowOffset = Vector3.Lerp(initOffset, offset, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _transposer.m_FollowOffset = offset;
    }

    //float GetConfinerMinZ()
    //{
    //    var bounds = _vCam.GetComponent<CinemachineConfiner>().m_BoundingVolume.bounds;
    //    Debug.Log("minZ: " + (bounds.min.z - _transposer.m_FollowOffset.z));
    //    return bounds.min.z - _transposer.m_FollowOffset.z;
    //}
}
