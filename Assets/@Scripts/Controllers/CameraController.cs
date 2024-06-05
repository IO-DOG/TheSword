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


    Vector3 _goOriginScale;
    Vector3 _playerOriginScale;

    //float Angle
    //{
    //    get { return _angle; }
    //    set
    //    {
    //        if (_angle != value)
    //        {
    //            _angle = value;
    //            AdjustCameraPitch(_angle);
    //        }
    //    }
    //}


    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.A))
    //        Angle--;
    //    if (Input.GetKeyDown(KeyCode.S))
    //        Angle++;
    //}

    public void AdjustCameraPitch(float angle, GameObject go)
    {
        CinemachineTransposer transposer = GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>();
        Vector3 offset = transposer.m_FollowOffset;
        offset.y = (-1) * Mathf.Tan(Mathf.Deg2Rad * angle) * offset.z;

        transposer.m_FollowOffset = offset;

        ChangeView(angle, go);
    }

    void ChangeView(float angle, GameObject go)
    {
        scaleMultiplier = 1 / Mathf.Cos(angle * Mathf.Deg2Rad);
        _playerOriginScale = Managers.Game.Player.transform.localScale;
        _goOriginScale = go.transform.localScale;

        if (go.GetComponent<PlayerController>() != null)
            go.transform.localScale = new Vector3(_playerOriginScale.x, _playerOriginScale.y * scaleMultiplier, _playerOriginScale.z * scaleMultiplier);
        else
            go.transform.localScale = new Vector3(_goOriginScale.x, _goOriginScale.y * scaleMultiplier, _goOriginScale.z);
    }
}
