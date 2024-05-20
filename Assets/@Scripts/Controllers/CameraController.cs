using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // ToDo Object y position adjusting
    float _angle = 60f; // 원하는 x축 회전 각도

    GameObject _parent;
    GameObject _player;
    GameObject _monsters;

    Vector3 _tileOriginScale;
    Vector3 _playerOriginScale;
    Vector3 _monsterOriginScale;

    float Angle
    {
        get { return _angle; }
        set
        {
            if (_angle != value)
            {
                _angle = value;
                AdjustCameraPitch(_angle);
            }
        }
    }

    void Start()
    {
        _parent = GameObject.Find("Parent");
        _player = GameObject.Find("Player");
        _monsters = GameObject.Find("Monsters");

        _tileOriginScale = _parent.transform.GetChild(0).localScale;
        _playerOriginScale = _player.transform.localScale;
        _monsterOriginScale = _monsters.transform.GetChild(0).localScale;

        if (GetComponent<CinemachineVirtualCamera>() != null)
        {
            AdjustCameraPitch(_angle);
        }
    }

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.A))
    //        Angle--;
    //    if (Input.GetKeyDown(KeyCode.S))
    //        Angle++;
    //}

    void AdjustCameraPitch(float angle)
    {
        CinemachineTransposer transposer = GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>();
        Vector3 offset = transposer.m_FollowOffset;
        offset.y = (-1) * Mathf.Tan(Mathf.Deg2Rad * angle) * offset.z;

        transposer.m_FollowOffset = offset;

        ChangeView(angle);
    }

    void ChangeView(float angle)
    {
        float scaleMultiplier = 1 / Mathf.Cos(angle * Mathf.Deg2Rad);

        //for (int i = 0; i < _parent.transform.childCount; i++)
        //{
        //    Transform child = _parent.transform.GetChild(i);
        //    child.transform.localScale = new Vector3(_tileOriginScale.x, _tileOriginScale.y, _tileOriginScale.z * scaleMultiplier);
        //}

        for (int i = 0; i< _monsters.transform.childCount; i++)
        {
            Transform child = _monsters.transform.GetChild(i);
            child.transform.localScale = new Vector3(_monsterOriginScale.x, _monsterOriginScale.y * scaleMultiplier, _monsterOriginScale.z * scaleMultiplier);
        }

        _player.transform.localScale = new Vector3(_playerOriginScale.x, _playerOriginScale.y * scaleMultiplier, _playerOriginScale.z * scaleMultiplier);
    }
}
