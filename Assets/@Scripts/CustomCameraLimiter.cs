using UnityEngine;
using Cinemachine;
using System.Drawing;

public class CustomCameraLimiter : CinemachineExtension
{
    public SpriteRenderer BG; // 제한할 BG 스프라이트
    public float cameraAngle = 60f; // 카메라 기울기 (도 단위)

    CinemachineTransposer Transposer;
    private Bounds bgBounds;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Body)
        {
            // Cinemachine Transposer 가져오기
            var transposer = vcam.GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>();
            if (transposer == null)
                return;

            // 제한된 Offset 적용
            ConstrainCameraPosition(ref transposer);
        }
    }

    private void ConstrainCameraPosition(ref CinemachineTransposer transposer)
    {
        // 카메라의 Virtual Camera 가져오기
        CinemachineVirtualCamera virtualCamera = transposer.GetComponentInParent<CinemachineVirtualCamera>();
        float orthographicSize = Camera.main.orthographicSize;
        float aspectRatio = Camera.main.aspect;

        // 카메라의 투영된 뷰 크기 계산
        float viewHeight = orthographicSize * 2f;             // 세로 크기
        float viewWidth = viewHeight * aspectRatio;           // 가로 크기

        // BG 스프라이트의 크기와 위치 가져오기
        Vector3 bgSize = bgBounds.size;
        Vector3 bgCenter = bgBounds.center;

        // Z축 제한값 계산 (스프라이트 크기를 기준으로)
        float minZ = bgCenter.z - (bgSize.y / 2f) + (viewHeight / 2f) - 2f; // 하단 경계
        float maxZ = bgCenter.z + (bgSize.y / 2f) - (viewHeight / 2f); // 상단 경계
        float dynamicOffsetZ = orthographicSize / Mathf.Tan(Define.CAMERA_ANGLE * Mathf.Deg2Rad);
        // Follow Offset 제한
        transposer.m_FollowOffset.z = minZ;
        transposer.m_FollowOffset.y = 10f;


        // 디버깅
        Debug.Log($"minZ Size: {minZ}");
        Debug.Log($"maxZ Size: {maxZ}");
    }


    public void SetBG()
    {
        BG = Managers.Game.BG;
        bgBounds = Managers.Game.BG.bounds;
    }
}
