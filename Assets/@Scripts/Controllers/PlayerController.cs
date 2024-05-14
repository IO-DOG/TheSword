using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class PlayerController : MonoBehaviour
{
    public Grid _grid;
    public float _speed = 15.0f;

    Vector3Int _cellPos = Vector3Int.zero;
    public MoveDir _dir = MoveDir.None;
    bool _isMoving = false;
    Vector3 _adjustPos = new Vector3(0.5f, 3f, 24.5f);
    //float toAddDist = 1.6f;
    void Start()
    {
        GameObject startPoint = GameObject.Find("StartPoint");
        Vector3 pos = _grid.CellToWorld(_cellPos) + _adjustPos;
        transform.position = pos;
        if (startPoint != null)
        {
            transform.position = startPoint.transform.position;
            _cellPos = new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        }
        Managers.Game.Player = this; // 던전 돌 때 죽으면 연동하도록 하기 위함.
    }

    void LateUpdate()
    {
        GetKeyUpdate();
        UpdatePosition();
        UpdateIsMoving();
    }

    void GetKeyUpdate()
    {
        if (Managers.Game.OnBattle == true)
        {
            _dir = MoveDir.None;
            return;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            _dir = MoveDir.Up;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            _dir = MoveDir.Down;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            _dir = MoveDir.Left;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            _dir = MoveDir.Right;
        }
        else
        {
            _dir = MoveDir.None;
        }
    }

    void UpdatePosition()
    {
        if (_isMoving == false)
            return;

        Vector3 destPos = _grid.CellToWorld(_cellPos) + _adjustPos;
        Vector3 moveDir = destPos - transform.position;

        // 도착 여부 체크
        float dist = moveDir.magnitude;
        if (dist < _speed * Time.deltaTime)
        {
            transform.position = destPos;
            _isMoving = false;
        }
        else
        {
            transform.position += moveDir.normalized * _speed * Time.deltaTime;
            _isMoving = true;
        }
    }

    void UpdateIsMoving()
    {
        if (_isMoving != false)
            return;
        // 레이 체크

        switch (_dir)
        {
            case MoveDir.Up:
                _cellPos += Vector3Int.up;
                _isMoving = true;
                break;
            case MoveDir.Down:
                _cellPos += Vector3Int.down;
                _isMoving = true;
                break;
            case MoveDir.Left:
                _cellPos += Vector3Int.left;
                _isMoving = true;
                break;
            case MoveDir.Right:
                _cellPos += Vector3Int.right;
                _isMoving = true;
                break;
        }
    }

    void CheckRay()
    {

    }

}
