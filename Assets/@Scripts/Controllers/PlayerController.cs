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
    public GameObject _keyInventory;
    public float _speed = 15.0f;

    float _duration;
    bool _isMoving = false;

    float _offset = Define.TILE_SIZE * 0.33f;
    Vector3 _interpolateRayPos = new Vector3(0f, -0.5f, 0f);
    Vector3 _cellPos;
    Vector3 _nextCellPos;

    void Start()
    {
        Managers.Input.KeyAction -= OnKeyboard;
        Managers.Input.KeyAction += OnKeyboard;

        _duration = 1 / _speed;

        transform.position = _startPoint.transform.position;
        _cellPos = _startPoint.transform.position;
        _keyInventory = GameObject.Find("KeyInventory");

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

        //Debug.DrawRay(transform.position + _interpolateRayPos, _nextCellPos * _offset, Color.red);
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
                _nextCellPos = Vector3.forward * _offset;
                break;
            case MoveDir.Down:
                _nextCellPos = Vector3.back * _offset;
                break;
            case MoveDir.Left:
                _nextCellPos = Vector3.left  * _offset;
                break;
            case MoveDir.Right:
                _nextCellPos = Vector3.right * _offset;
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
        int layerMask = (1 << (int)Define.Layer.Wall) + (1 << (int)Define.Layer.CItem) + (1 << (int)Define.Layer.Door) + (1 << (int)Define.Layer.Portal)
            + (1 << (int)Define.Layer.EItem);

        RaycastHit hit;
        Physics.Raycast(transform.position + _interpolateRayPos, _nextCellPos, out hit, _offset, layerMask);

        if(hit.collider != null)
        {
            // Checking Wall
            if (hit.collider.gameObject.layer == (int)Define.Layer.Wall)
            {
                somethingExist = true;
            }
            // Checking Item
            else if (hit.collider.gameObject.layer == (int)Define.Layer.CItem)
            {
                hit.collider.gameObject.GetComponent<ConsumableItem>().PickUp();
            }
            //Checking Door
            else if (hit.collider.gameObject.layer == (int)Define.Layer.Door)
            {
                if (Managers.Game.KeyInventory.TryUseKey(hit.collider.gameObject))
                {
                    somethingExist = true;

                    hit.collider.gameObject.GetComponentInChildren<Door>().CoDoorLockAnim();
                    hit.collider.gameObject.GetComponentInChildren<Door>().CoOpenDoor(2.5f);
                    hit.collider.gameObject.GetComponentInChildren<Door>().FadeDoor().OnComplete(() =>
                    {
                        hit.collider.gameObject.SetActive(false);
                        somethingExist = false;
                    });
                }
                else
                {
                    somethingExist = true;
                }
            }
            else if(hit.collider.gameObject.layer == (int)Define.Layer.Portal)
            {
                somethingExist = true;
                hit.collider.gameObject.GetComponentInChildren<PortalController>().Stairs();
                _cellPos = transform.position;
            }
            else if(hit.collider.gameObject.layer == (int)Define.Layer.EItem)
            {
                Managers.UI.ShowPopupUI<UI_DialogPopup>();
            }
        }

        return somethingExist;
    }
}
