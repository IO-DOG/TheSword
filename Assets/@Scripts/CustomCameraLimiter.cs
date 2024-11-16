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
        if (stage != CinemachineCore.Stage.Body)
            return;

        // 카메라의 Virtual Camera 가져오기
        float orthographicSize = Camera.main.orthographicSize;
        float aspectRatio = Camera.main.aspect;
        float projectedHeight = orthographicSize * Mathf.Cos(Define.CAMERA_ANGLE * Mathf.Deg2Rad);
        // 카메라의 투영된 뷰 크기 계산
        float viewHeight = orthographicSize * 2f;             // 세로 크기
        float viewWidth = viewHeight * aspectRatio;           // 가로 크기

        Vector3 bgMin = bgBounds.min;
        Vector3 bgMax = bgBounds.max;

        Vector3 cameraPosition = state.RawPosition;

        // 카메라가 비추는 상단/하단 경계값 계산
        float cameraTop = cameraPosition.z + projectedHeight;
        float cameraBottom = cameraPosition.z - projectedHeight;

        // Y축 경계 제한
        if (cameraTop > bgMax.z) // 카메라가 BG 상단 바깥을 비출 경우
        {
            cameraPosition.z = bgMax.z - projectedHeight; // 상단 경계 내로 제한
        }
        else if (cameraBottom < bgMin.z) // 카메라가 BG 하단 바깥을 비출 경우
        {
            cameraPosition.z = bgMin.z + projectedHeight; // 하단 경계 내로 제한
        }
        state.RawPosition = cameraPosition;


        Debug.Log($"Camera Top: {cameraTop}, Camera Bottom: {cameraBottom}");
        Debug.Log($"BG Min Y: {bgMin.z}, BG Max Y: {bgMax.z}");
        Debug.Log($"cameraPosition: {cameraPosition.z}");
        Debug.Log($"projectedHeight: {projectedHeight}");
    }



    public void SetBG()
    {
        BG = Managers.Game.BG;
        bgBounds = Managers.Game.BG.bounds;
    }
}
