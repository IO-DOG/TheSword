using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using static Define;

public class PlayerController : MonoBehaviour
{
    public Grid _grid;
    public GameObject _startPoint;
    public float _speed = 15.0f;

    float _duration;
    bool _isMoving = false;

    Vector3 _interpolateGridPos = new Vector3(0.5f, 1.5f, Mathf.Sqrt(2)/2);
    Vector3 _interpolateRayPos = new Vector3(0f, -0.5f, 0f);
    Vector3 _cellPos;

    void Start()
    {
        Managers.Input.KeyAction -= OnKeyboard;
        Managers.Input.KeyAction += OnKeyboard;

        _duration = 1 / _speed;

        transform.position = _startPoint.transform.position + _interpolateGridPos;
        _cellPos = _startPoint.transform.position + _interpolateGridPos;

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

        Vector3 nextCellPosition = Vector3.zero;
        switch (moveDir) 
        {
            case MoveDir.Up:
                nextCellPosition = Vector3Int.forward;
                break;
            case MoveDir.Down:
                nextCellPosition = Vector3Int.back;
                break;
            case MoveDir.Left:
                nextCellPosition = Vector3Int.left;
                break;
            case MoveDir.Right:
                nextCellPosition = Vector3Int.right;
                break;
        }

        // Checking Wall
        if (Physics.Raycast(transform.position + _interpolateRayPos, nextCellPosition, 1f, 1 << (int)Layer.Wall))
        {
            _isMoving = false;
            return;
        }

        // Move
        _cellPos += nextCellPosition;
        transform.DOMove(_cellPos, _duration).OnComplete(()=> _isMoving = false);
    }
    #endregion
}
