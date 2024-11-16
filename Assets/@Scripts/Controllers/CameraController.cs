using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    public static SpriteRenderer BG; // BG 스프라이트
    public static CinemachineVirtualCamera vCam;


    public static bool _isCombineMap = false;

    // 픽셀 퍼펙트 카메라 해상도
    int[] _resolutionX = { 960, 640, 384, 320 };
    int[] _resolutionY = { 540, 360, 256, 80 };
    int _resolutionIndex = 0;

    //// ToDo Object y position adjusting
    //float _angle = 60f; // 원하는 x축 회전 각도
    public float _scaleMultiplier;
    float _scrollSpeed = 10f;

    //public static Vector3 _minBounds;
    //public static Vector3 _maxBounds;

    public static float _spriteTop;
    public static float _spriteBottom;
    public static float _spriteLeft;
    public static float _spriteRight;
    public static float _projectedHeight;
    CinemachineTransposer transposer;
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

        transposer = _vCam.GetCinemachineComponent<CinemachineTransposer>();
        transposer.m_FollowOffset = new Vector3(0f, 10f, -5f);
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

    private void LateUpdate()
    {
        CameraUpdate();
    }

    void CameraUpdate()
    {
        if (Managers.Game.OnMeetKingSlime)
            return;

        //ConstrainFollowOffset(_vCam, Managers.Game.BG.bounds);
    }

    private void ConstrainFollowOffset(CinemachineVirtualCamera virtualCamera, Bounds bgBounds)
    {
        // CinemachineTransposer 가져오기
        var transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        Vector3 followOffset = transposer.m_FollowOffset;

        // 카메라의 오쏘그래픽 사이즈 및 각도
        float orthographicSize = Camera.main.orthographicSize;
        float cameraAngle = 60f; // 카메라 기울기
        float projectedHeight = orthographicSize * Mathf.Cos(cameraAngle * Mathf.Deg2Rad);

        // 배경 경계
        float bgMinY = bgBounds.min.z;
        float bgMaxY = bgBounds.max.z;

        // 카메라 경계 계산
        float cameraTop = followOffset.z + projectedHeight;
        float cameraBottom = followOffset.z - projectedHeight;

        // Follow Offset 제한
        if (cameraTop > bgMaxY)
        {
            followOffset.z = bgMaxY - projectedHeight;
        }
        else if (cameraBottom < bgMinY)
        {
            followOffset.z = bgMinY + projectedHeight;
        }

        // Follow Offset 업데이트
        transposer.m_FollowOffset = followOffset;

        // 디버깅
        Debug.Log($"Follow Offset Z: {followOffset.z}");
        Debug.Log($"Projected Height: {projectedHeight}");
        Debug.Log($"BG Min Y: {bgMinY}, BG Max Y: {bgMaxY}");
    }
    private void AdjustFollowOffsetBasedOnResolution()
    {
        // 현재 카메라의 Orthographic Size 가져오기
        float orthographicSize = Camera.main.orthographicSize;

        // 기본 FollowOffset.z 값 (기준값)
        float baseOffsetZ = -5f;

        // 해상도 또는 Orthographic Size에 따라 Z 오프셋 계산
        // orthographicSize가 작아질수록 Z축 오프셋을 줄임
        float dynamicOffsetZ = baseOffsetZ * 1.8f / orthographicSize;

        // FollowOffset 업데이트
        Vector3 followOffset = transposer.m_FollowOffset;
        followOffset.z = dynamicOffsetZ;
        transposer.m_FollowOffset = followOffset;

        // 디버깅
        Debug.Log($"Adjusted FollowOffset Z: {followOffset.z}");
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
        string curDungeonName = $"Dungeon_{Managers.Data.StageInfoDic[Managers.Game.PlayerData.CurStageid].DungeonID}";
        SpriteRenderer BG = GameObject.Find(curDungeonName).transform.Find("Deco/BG").GetComponent<SpriteRenderer>();

        float cameraAngle = Define.CAMERA_ANGLE * Mathf.Deg2Rad;
        float orthograpicSize = Camera.main.orthographicSize;

        _projectedHeight = orthograpicSize * Mathf.Cos(cameraAngle);

        // 스프라이트의 최상단, 최하단 Y좌표 투영 계산
        _spriteTop = BG.bounds.min.x;
        _spriteBottom = BG.bounds.max.x;
        _spriteLeft = BG.bounds.max.y * Mathf.Cos(Define.CAMERA_ANGLE * Mathf.Deg2Rad);
        _spriteRight = BG.bounds.min.y * Mathf.Cos(Define.CAMERA_ANGLE * Mathf.Deg2Rad);
    }

    public void SetupCameraConfiner()
    {
        string curDungeonName = $"Dungeon_{Managers.Data.StageInfoDic[Managers.Game.PlayerData.CurStageid].DungeonID}";
        BG = GameObject.Find(curDungeonName).transform.Find("Deco/BG").gameObject.GetComponent<SpriteRenderer>();
        Debug.Log(BG.name);
        GameObject confinerCollider = new GameObject { name = "Confiner" };
        confinerCollider.transform.Rotate(Define.CAMERA_ANGLE, 0, 0);
        BoxCollider collider = confinerCollider.AddComponent<BoxCollider>();
        //collider.center = new Vector3(BG.bounds.center.x, -5f, BG.bounds.center.z);
        collider.size = new Vector3(BG.bounds.size.x, BG.bounds.size.z * Mathf.Sqrt(3) / 2, Define.CONFINER_HEIGHT);
        float offsetY = Mathf.Sin(Mathf.Deg2Rad * Define.CAMERA_ANGLE) * (collider.size.z / 2);
        collider.center = new Vector3(0, collider.size.y, 0);
        confinerCollider.transform.position = new Vector3(BG.bounds.min.x + BG.bounds.size.x / 2 + Define.TILE_SIZE / 2, 0, BG.bounds.min.z + BG.bounds.center.z + -Define.TILE_SIZE / 2);
        // Cinemachine Confiner 설정
        CinemachineConfiner confiner = _vCam.GetComponent<CinemachineConfiner>();
        confiner.m_BoundingVolume = collider;
        confiner.m_Damping = 0; // 필요에 따라 댐핑 설정
        confiner.InvalidatePathCache();

        Debug.Log("Camera Confiner setup complete.");
    }

}
