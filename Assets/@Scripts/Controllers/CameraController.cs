using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class CameraController : MonoBehaviour
{
    // ToDo Object y position adjusting
    float _angle = 60f; // 원하는 x축 회전 각도
    public float scaleMultiplier;

    GameObject _parent;
    GameObject _player;
    GameObject _monsters;
    GameObject _items;

    Vector3 _tileOriginScale;
    Vector3 _playerOriginScale;
    Vector3 _monsterOriginScale;
    Vector3 _itemOriginScale;

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
        _items = GameObject.Find ("Items");

        _tileOriginScale = _parent.transform.GetChild(0).localScale;
        _playerOriginScale = _player.transform.localScale;
        _monsterOriginScale = _monsters.transform.GetChild(0).localScale;
        _itemOriginScale = _items.transform.GetChild(0).localScale;

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
        scaleMultiplier = 1 / Mathf.Cos(angle * Mathf.Deg2Rad);

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

        for (int i = 0; i < _items.transform.childCount; i++)
        {
            Transform child = _items.transform.GetChild(i);
            child.transform.localScale = new Vector3(_itemOriginScale.x, _itemOriginScale.y * scaleMultiplier, _itemOriginScale.z * scaleMultiplier);
        }

        _player.transform.localScale = new Vector3(_playerOriginScale.x, _playerOriginScale.y * scaleMultiplier, _playerOriginScale.z * scaleMultiplier);
    }
}
