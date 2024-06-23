using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using static Define;
using Unity.Burst.CompilerServices;

public class PlayerController : MonoBehaviour
{
    const float adjustingDis = 0.025f;
    public Grid _grid;
    public GameObject _startPoint;
    public GameObject _keyInventory;
    public float _speed = 10.0f;
    public bool _isEquiptWeapon = true;
    public bool _isEquiptShield = true;


    GameObject _weapon;
    GameObject _shield;
    string _weaponName = "Sword01";
    string _shieldName = "Shield00";

    float _duration;
    bool _isMoving = false;

    float _offset = Define.TILE_SIZE * 0.33f;
    Vector3 _interpolateRayPos = new Vector3(0f, -2f, 0f);
    Vector3 _cellPos;
    Vector3 _nextCellPos;

    PlayerState _state = PlayerState.IdleDown;

    void Start()
    {
        Managers.Input.KeyAction -= OnKeyboard;
        Managers.Input.KeyAction += OnKeyboard;

        _duration = 1 / _speed;

        transform.position = _startPoint.transform.position;
        _cellPos = _startPoint.transform.position;
        _keyInventory = GameObject.Find("KeyInventory");
        _weapon = GameObject.Find("WeaponSlot");
        _shield = GameObject.Find("ShieldSlot");

        Managers.Game.Player = this; // 던전 돌 때 죽으면 연동하도록 하기 위함.
    }

    void OnKeyboard()
    {
        if (Managers.Game.OnBattle == true || Managers.Game.OnConversation == true)
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

    private void Update()
    {
        PlayAnimation();
        CheckWeapon();
        CheckShield();

        if (Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow))
        {
            SetIdleState(MoveDir.Down);
        }
        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            SetIdleState(MoveDir.Up);
        }
    }

    void CheckWeapon()
    {
        if(_isEquiptWeapon)
            _weapon.SetActive(true);
        else
            _weapon.SetActive(false);
    }

    void CheckShield()
    {
        if(_isEquiptShield)
            _shield.SetActive(true);
        else
            _shield.SetActive(false);
    }

    void PlayAnimation()
    {
        switch(_state)
        {
            case PlayerState.IdleUp:
                GetComponent<Animator>().Play("Player_Idle_B");
                if (_weapon.activeSelf)
                    _weapon.GetComponent<Animator>().Play($"{_weaponName}_Idle_B");
                if (_shield.activeSelf)
                    _shield.GetComponent<Animator>().Play($"{_shieldName}_Idle_B");

                _weapon.transform.localPosition = Vector3.forward * adjustingDis;
                _shield.transform.localPosition = Vector3.forward * adjustingDis;

                break;
            case PlayerState.IdleDown:
                GetComponent<Animator>().Play("Player_Idle_F");
                if (_weapon.activeSelf)
                    _weapon.GetComponent<Animator>().Play($"{_weaponName}_Idle_F");
                if (_shield.activeSelf)
                    _shield.GetComponent<Animator>().Play($"{_shieldName}_Idle_F");

                _weapon.transform.localPosition = Vector3.back * adjustingDis;
                _shield.transform.localPosition = Vector3.back * adjustingDis;
                break;
            case PlayerState.Left:
                GetComponent<Animator>().Play("Player_Run_L");
                if (_weapon.activeSelf)
                    _weapon.GetComponent<Animator>().Play($"{_weaponName}_Run_L");
                if (_shield.activeSelf)
                    _shield.GetComponent<Animator>().Play($"{_shieldName}_Run_L");

                _weapon.transform.localPosition = Vector3.forward * adjustingDis;
                _shield.transform.localPosition = Vector3.back * adjustingDis;
                break;
            case PlayerState.Right:
                GetComponent<Animator>().Play("Player_Run_R");
                if (_weapon.activeSelf)
                    _weapon.GetComponent<Animator>().Play($"{_weaponName}_Run_R");
                if (_shield.activeSelf)
                    _shield.GetComponent<Animator>().Play($"{_shieldName}_Run_R");

                _weapon.transform.localPosition = Vector3.back * adjustingDis;
                _shield.transform.localPosition = Vector3.forward * adjustingDis;
                break;
            case PlayerState.Up:
                GetComponent<Animator>().Play("Player_Run_B");
                if (_weapon.activeSelf)
                    _weapon.GetComponent<Animator>().Play($"{_weaponName}_Run_B");
                if (_shield.activeSelf)
                    _shield.GetComponent<Animator>().Play($"{_shieldName}_Run_B");

                _weapon.transform.localPosition = Vector3.forward * adjustingDis;
                _shield.transform.localPosition = Vector3.forward * adjustingDis;
                break;
            case PlayerState.Down:
                GetComponent<Animator>().Play("Player_Run_F");
                if (_weapon.activeSelf)
                    _weapon.GetComponent<Animator>().Play($"{_weaponName}_Run_F");
                if (_shield.activeSelf)
                    _shield.GetComponent<Animator>().Play($"{_shieldName}_Run_F");

                _weapon.transform.localPosition = Vector3.back * adjustingDis;
                _shield.transform.localPosition = Vector3.back * adjustingDis;
                break;
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
                _nextCellPos = Vector3.forward * _offset;
                _state = PlayerState.Up;
                break;
            case MoveDir.Down:
                _nextCellPos = Vector3.back * _offset;
                _state = PlayerState.Down;
                break;
            case MoveDir.Left:
                _nextCellPos = Vector3.left  * _offset;
                _state = PlayerState.Left;
                break;
            case MoveDir.Right:
                _nextCellPos = Vector3.right * _offset;
                _state = PlayerState.Right;
                break;
        }

        // Checking Forward
        // If Obstacles, Stop
        if (CheckSomething())
        {
            _isMoving = false;;
            return;
        }
        // Move
        _cellPos += _nextCellPos;
        transform.DOMove(_cellPos, _duration).OnKill(()=> 
        { 
            _isMoving = false;
            SetIdleState(moveDir);
        });
    }

    void SetIdleState(MoveDir moveDir)
    {
        if (moveDir == MoveDir.Up)
            _state = PlayerState.IdleUp;
        else
            _state = PlayerState.IdleDown;
    }

    public void PlayAnimation(string name)
    {
        GetComponent<Animator>().Play(name);
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
            // Checking Item
            else if (hit.collider.gameObject.layer == (int)Define.Layer.EItem)
            {
                hit.collider.gameObject.GetComponent<Equip>().PickUp();
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
        }

        return somethingExist;
    }
}
