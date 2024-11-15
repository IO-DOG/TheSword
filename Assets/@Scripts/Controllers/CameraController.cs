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
        //transposer.m_FollowOffset = new Vector3(0f, 10f, -5f);
        //SetCameraExtent();
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

    //private void LateUpdate()
    //{
    //   CameraUpdate();
    //}

    void CameraUpdate()
    {
        if (Managers.Game.OnMeetKingSlime)
            return;

        Vector3 pos = Managers.Game.Player.transform.position;

        SetCameraExtent();

        float clampedZ = Mathf.Clamp
            (
                pos.z,
                _spriteBottom + _projectedHeight,
                _spriteTop - _projectedHeight
            );

        Vector3 curOffset = _vCam.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
        Vector3 targetOffset = new Vector3(curOffset.x, 10f, clampedZ - 5f);

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

    public static void SetupCameraConfiner()
    {
        string curDungeonName = $"Dungeon_{Managers.Data.StageInfoDic[Managers.Game.PlayerData.CurStageid].DungeonID}";
        BG = GameObject.Find(curDungeonName).transform.Find("Deco/BG").GetComponent<SpriteRenderer>();

        // BG의 Bounds 가져오기
        Bounds spriteBounds = BG.bounds;

        // 카메라 각도 (60도) -> 라디안으로 변환
        float cameraAngle = 60f * Mathf.Deg2Rad;

        // 투영된 상단/하단/좌측/우측 좌표 계산
        Vector3[] projectedPoints = new Vector3[4];
        projectedPoints[0] = ProjectPoint(spriteBounds.min.x, spriteBounds.min.y, cameraAngle); // Bottom-left
        projectedPoints[1] = ProjectPoint(spriteBounds.max.x, spriteBounds.min.y, cameraAngle); // Bottom-right
        projectedPoints[2] = ProjectPoint(spriteBounds.max.x, spriteBounds.max.y, cameraAngle); // Top-right
        projectedPoints[3] = ProjectPoint(spriteBounds.min.x, spriteBounds.max.y, cameraAngle); // Top-left

        // Polygon Collider 2D 생성 및 설정
        GameObject confinerObject = new GameObject("CameraConfiner");
        PolygonCollider2D confinerCollider = confinerObject.AddComponent<PolygonCollider2D>();

        // Polygon Collider에 투영된 좌표 적용
        Vector2[] colliderPoints = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            colliderPoints[i] = new Vector2(projectedPoints[i].x, projectedPoints[i].z); // XZ 평면에 맞춤
        }
        confinerCollider.points = colliderPoints;

        // Cinemachine Confiner 설정
        CinemachineConfiner confiner = vCam.GetComponent<CinemachineConfiner>();
        if (confiner == null)
        {
            confiner = vCam.gameObject.AddComponent<CinemachineConfiner>();
        }
        confiner.m_BoundingShape2D = confinerCollider;
        confiner.m_Damping = 0; // 필요에 따라 댐핑 설정
        confiner.InvalidatePathCache();

        Debug.Log("Camera Confiner setup complete.");
    }


    public static Vector3 ProjectPoint(float x, float y, float angle)
    {
        // X, Y 좌표를 투영하여 XZ 평면으로 변환
        float projectedZ = y * Mathf.Cos(angle);
        return new Vector3(x, 0, projectedZ); // Z축에 투영된 값
    }
}
