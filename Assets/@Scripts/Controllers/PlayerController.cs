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

    Vector3 _interpolateGridPos = new Vector3(0f, 0f, 1f);
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
        int layerMask = (1 << (int)Define.Layer.Wall) + (1 << (int)Define.Layer.Item) + (1 << (int)Define.Layer.Door);

        RaycastHit hit;
        Physics.Raycast(transform.position + _interpolateRayPos, _nextCellPos, out hit, 1f, layerMask);

        if(hit.collider != null)
        {
            // Checking Wall
            if (hit.collider.gameObject.layer == (int)Define.Layer.Wall)
            {
                somethingExist = true;
            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.Item)
            {
                if (hit.collider.gameObject.GetComponent<ConsumableItem>()._consumableItemIndex < ConsumableItem.NUM_OF_KEYS)
                    hit.collider.gameObject.GetComponent<ConsumableItem>().PickUp();
            }
            //Checking Door
            else if (hit.collider.gameObject.layer == (int)Define.Layer.Door)
            {
                if (Managers.Game.Inventory.TryUseKey(hit.collider.gameObject))
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
        }

        return somethingExist;
    }
}
