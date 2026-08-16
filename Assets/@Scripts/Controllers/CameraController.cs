using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using static CameraController;

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
    public static CinemachineTransposer _transposer;
    public static CinemachineVirtualCamera _vCam;
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

    /// <summary>
    /// 연출이 끝났으면 카메라는 반드시 플레이어를 본다.
    ///
    /// 연출들이 Follow 를 보스나 기둥으로 돌려놓고 마지막에 되돌리는데,
    /// 중간에 코루틴이 죽으면 그대로 남는다. 그러면 보스를 잡고 다음 층으로
    /// 넘어가도 카메라가 플레이어를 따라가지 않고 엉뚱한 데 멈춰 있다.
    /// </summary>
    void RestoreFollowToPlayer()
    {
        if (Managers.Game.OnDirect || Managers.Game.OnBattle)
            return;
        if (_vCam == null || Managers.Game.Player == null)
            return;

        Transform player = Managers.Game.Player.transform;
        if (_vCam.Follow != player)
        {
            _vCam.Follow = player;
            _vCam.LookAt = null;
        }
    }

    private void Update()
    {
        RestoreFollowToPlayer();

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
        // 여기서 터지면 부르는 쪽(포탈 워프)이 통째로 죽어서 플레이어가 새 맵으로
        // 옮겨지지 않는다. 챕터가 바뀌는 20->21층 전환이 정확히 그래서 멈췄다.
        // 못 찾으면 쓰던 경계를 그대로 두고 넘어간다 — 카메라가 조금 어긋날 뿐
        // 게임은 진행된다.
        Data.StageInfoData info;
        if (Managers.Data.StageInfoDic.TryGetValue(Managers.Game.PlayerData.CurStageid, out info) == false)
        {
            Debug.LogWarning($"[Camera] {Managers.Game.PlayerData.CurStageid} 층 정보가 없어 경계를 그대로 둔다");
            return;
        }

        string curDungeonName = $"Dungeon_{info.DungeonID}";
        GameObject dungeon = GameObject.Find(curDungeonName);
        if (dungeon == null)
        {
            Debug.LogWarning($"[Camera] {curDungeonName} 을 못 찾아 경계를 그대로 둔다");
            return;
        }

        // 손으로 만든 층은 Decos/BG(바닥 그림)가 범위를 알려 준다.
        // 생성한 층에는 그게 없어서, 예전에는 여기서 멈추고 이전 층의 경계를
        // 그대로 썼다 — 그러면 카메라가 옛 층 범위에 갇혀 플레이어를 못 따라간다.
        // 없으면 벽으로 범위를 잰다. 벽은 어느 층에나 있다.
        Transform bgTr = dungeon.transform.Find("Decos/BG");
        SpriteRenderer bgSr = bgTr != null ? bgTr.GetComponent<SpriteRenderer>() : null;

        Bounds area;
        if (bgSr != null)
        {
            _bg = bgSr;
            area = bgSr.bounds;
        }
        else if (TryWallBounds(dungeon, out area) == false)
        {
            Debug.LogWarning($"[Camera] {curDungeonName} 의 범위를 잴 수 없어 경계를 그대로 둔다");
            return;
        }

        if (confinerCollider != null)
            Managers.Resource.Destroy(confinerCollider);
        confinerCollider = new GameObject { name = "Confiner" };
        confinerCollider.transform.Rotate(Define.CAMERA_ANGLE, 0, 0);

        _collider = confinerCollider.AddComponent<BoxCollider>();
        _collider.size = new Vector3(area.size.x, area.size.z * Mathf.Sqrt(3) / 2, Define.CONFINER_HEIGHT);
        _collider.center = new Vector3(0, _collider.size.y, -10);

        confinerCollider.transform.position = new Vector3(
            area.min.x + area.size.x / 2 + Define.TILE_SIZE / 2,
            0,
            area.min.z + area.center.z + -Define.TILE_SIZE / 2);

        // Cinemachine Confiner 설정
        _confiner = _vCam.GetComponent<CinemachineConfiner>();
        _confiner.InvalidatePathCache();
        _confiner.m_BoundingVolume = _collider;
    }

    /// <summary>이 층이 벽으로 두르는 범위. 바닥 그림이 없는 생성 층에서 쓴다.</summary>
    static bool TryWallBounds(GameObject map, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        foreach (Collider c in map.GetComponentsInChildren<Collider>(false))
        {
            if (c.gameObject.layer != (int)Define.Layer.Wall)
                continue;
            if (found == false)
            {
                bounds = c.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(c.bounds);
            }
        }
        return found;
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

    public static IEnumerator WhiteBang(float time)
    {
        Volume postProcessingVolume = Managers.Game.MainCamera.GetComponent<Volume>();
        ColorAdjustments colorAdjustment;
        if (postProcessingVolume.profile.TryGet<ColorAdjustments>(out colorAdjustment))
        {
            colorAdjustment.postExposure.value = Define.POSTPROCESSING_WHITE_EXPOSURE;
        }
        yield return new WaitForSeconds(time);
        if (postProcessingVolume.profile.TryGet<ColorAdjustments>(out colorAdjustment))
        {
            colorAdjustment.postExposure.value = Define.POSTPROCESSING_DEFAULT_EXPOSURE;
        }
    }

    public static IEnumerator CoShakeCamera(float time, float force = 1f)
    {
        float noiseOffsetX = Random.Range(0f, 80f); // X축 노이즈 시작점
        float noiseOffsetY = Random.Range(0f, 80f); // Y축 노이즈 시작점
        yield return new WaitForSeconds(0.1f);
        Vector3 originalOffset = _transposer.m_FollowOffset;
        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            elapsedTime += Time.deltaTime;
            Vector3 shakeOffset = new Vector3(
             (Mathf.PerlinNoise(noiseOffsetX, elapsedTime * 8f) - 0.5f) * force * 2f,
             0,//(Mathf.PerlinNoise(noiseOffsetY, elapsedTime * 6.5f) - 0.5f) * force * 2f,
             (Mathf.PerlinNoise(noiseOffsetY, elapsedTime * 8f) - 0.5f) * force * 2f
         );
            _transposer.m_FollowOffset = originalOffset + shakeOffset;
            yield return null;
        }

        Vector3 curOffset = _transposer.m_FollowOffset;
        float resetOffsetTime = 0f;
        float resetDuration = 0.2f;
        while (resetOffsetTime < resetDuration)
        {
            resetOffsetTime += Time.deltaTime;
            _transposer.m_FollowOffset = Vector3.Lerp(curOffset, originalOffset, resetOffsetTime / resetDuration);
            yield return null;
        }

        _transposer.m_FollowOffset = originalOffset;
    }

    Coroutine CoVirtualCameraMove;
    public void StartCoVirtualCameraMove(Vector3 originalOffset, Vector3 targetOffset, float time)
    {
        if (CoVirtualCameraMove != null)
            CoroutineManager.StopCoroutine(CoVirtualCameraMove);
        CoVirtualCameraMove = CoroutineManager.StartCoroutine(VirtualCameraMove(originalOffset, targetOffset, time));
    }

    private IEnumerator VirtualCameraMove(Vector3 originalOffset, Vector3 targetOffset, float time)
    {
        float elapsedTime = 0f;

        Vector3 initOffset = originalOffset;

        while (elapsedTime < time)
        {
            float t = Mathf.Clamp01(elapsedTime / time);
            _transposer.m_FollowOffset = Vector3.Lerp(initOffset, targetOffset, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _transposer.m_FollowOffset = targetOffset;
    }


}
