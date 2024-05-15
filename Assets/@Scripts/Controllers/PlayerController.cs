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

    Vector3 _interpolatePos = new Vector3(0.5f, 1.5f, Mathf.Sqrt(2)/2);
    Vector3 _cellPos;

    void Start()
    {
        Managers.Input.KeyAction -= OnKeyboard;
        Managers.Input.KeyAction += OnKeyboard;

        _duration = 1 / _speed;

        Vector3 startPoint = GameObject.Find("StartPoint").transform.position + _interpolatePos;
        transform.position = startPoint;
        _cellPos = startPoint;

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

        transform.DOMove(_cellPos, _duration).OnComplete(()=> _isMoving = false);
    }
    #endregion
}
