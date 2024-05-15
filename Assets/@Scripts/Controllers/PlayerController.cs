using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using static Define;

public class PlayerController : MonoBehaviour
{
    public Grid _grid;
    public float _speed = 15.0f;
    float _duration;
    bool _isMoving = false;

    Vector3 _interpolatePos = new Vector3(0.5f, 0, 0.5f);
    Vector3Int _cellPos = Vector3Int.zero;

    void Start()
    {
        Managers.Input.KeyAction -= OnKeyboard;
        Managers.Input.KeyAction += OnKeyboard;

        GameObject startPoint = GameObject.Find("StartPoint");
        Vector3 pos = _grid.CellToWorld(_cellPos);
        _duration = 1 / _speed;
        transform.position = pos;
        if (startPoint != null)
        {
            transform.position = startPoint.transform.position;
            _cellPos = new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        }
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
    void Moving(MoveDir dir)
    {
        if (_isMoving)
            return;

        _isMoving = true;
        switch (dir) 
        {
            case MoveDir.Up:
                _cellPos += Vector3Int.forward;
                break;
            case MoveDir.Down:
                _cellPos += Vector3Int.back;
                break;
            case MoveDir.Left:
                _cellPos += Vector3Int.left;
                break;
            case MoveDir.Right:
                _cellPos += Vector3Int.right;
                break;
        }

        transform.DOMove(_cellPos + _interpolatePos, _duration).OnComplete(()=> _isMoving = false);
    }
    #endregion
}
