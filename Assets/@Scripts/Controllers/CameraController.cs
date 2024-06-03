using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    Vector3 _goOriginScale;
    Vector3 _playerOriginScale;
    Vector3 _monsterOriginScale;
    Vector3 _itemOriginScale;

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

    void Start()
    {
        //_parent = GameObject.Find("Map");
        //_monsters = GameObject.Find("Monsters");
        //_items = GameObject.Find ("Items");

        //_monsterOriginScale = _monsters.transform.GetChild(0).localScale;
        //_itemOriginScale = _items.transform.GetChild(0).localScale;

        //if (GetComponent<CinemachineVirtualCamera>() != null)
        //{
        //    AdjustCameraPitch(_angle);
        //}
    }

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
