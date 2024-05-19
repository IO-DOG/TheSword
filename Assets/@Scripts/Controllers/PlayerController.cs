using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using static Define;
using Unity.Burst.CompilerServices;

public class PlayerController : MonoBehaviour
{
    public Grid _grid;
    public GameObject _startPoint;
    public float _speed = 15.0f;

    float _duration;
    bool _isMoving = false;

    Vector3 _interpolateGridPos = new Vector3(0f, 0f, Mathf.Sqrt(2));
    Vector3 _interpolateRayPos = new Vector3(0f, -0.5f, 0.4f);
    Vector3 _cellPos;
    Vector3 _nextCellPos;

    void Start()
    {
        Managers.Input.KeyAction -= OnKeyboard;
        Managers.Input.KeyAction += OnKeyboard;

        _duration = 1 / _speed;

        transform.position = _startPoint.transform.position;
        _cellPos = _startPoint.transform.position;

        Managers.Game.Player = this; // 던전 돌 때 죽으면 연동하도록 하기 위함.
    }

    void OnKeyboard()
    {
        if (Managers.Game.OnBattle == true)
        {
            return;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            Moving(MoveDir.Up);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            Moving(MoveDir.Down);
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            Moving(MoveDir.Left);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            Moving(MoveDir.Right);
        }
    }

    #region Moving
    void Moving(MoveDir moveDir)
    {
        if (_isMoving)
            return;

        _isMoving = true;

        _nextCellPos = Vector3.zero;
        switch (moveDir) 
        {
            case MoveDir.Up:
                _nextCellPos = _interpolateGridPos;
                break;
            case MoveDir.Down:
                _nextCellPos = (-1) * _interpolateGridPos;
                break;
            case MoveDir.Left:
                _nextCellPos = Vector3Int.left;
                break;
            case MoveDir.Right:
                _nextCellPos = Vector3Int.right;
                break;
        }

        // Checking Forward
        // If Obstacles, Stop
        if (CheckSomething())
        {
            _isMoving = false;
            return;
        }

        // Move
        _cellPos += _nextCellPos;
        transform.DOMove(_cellPos, _duration).OnComplete(()=> _isMoving = false);
    }
    #endregion

    bool CheckSomething()
    {
        bool somethingExist = false;
        int layerMask = (1 << (int)Define.Layer.Wall) + (1 << (int)Define.Layer.Key) + (1 << (int)Define.Layer.Door);

        RaycastHit hit;
        Physics.Raycast(transform.position + _interpolateRayPos, _nextCellPos, out hit, 0.8f, layerMask);

        if(hit.collider != null)
        {
            // Checking Wall
            if (hit.collider.gameObject.layer == (int)Define.Layer.Wall)
            {
                somethingExist = true;
            }
            // Checking Key
            if (hit.collider.gameObject.layer == (int)Define.Layer.Key)
            {
                hit.collider.gameObject.GetComponent<Key>().PickUp();
            }
            //Checking Door
            if (hit.collider.gameObject.layer == (int)Define.Layer.Door)
            {
                //if(Managers.Game.Inventory.HasKey(hit.collider.gameObject.name))
                //{
                //    Managers.Game.Inventory.UseKey(hit.collider.gameObject);
                //}
                //else
                //{
                //    somethingExist = true;
                //}
            }
        }

        return somethingExist;
    }
}
