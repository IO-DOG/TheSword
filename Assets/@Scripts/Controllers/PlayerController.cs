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
    public GameObject _keyInventory;

    float _speed = 5.0f;
    public float Speed
    {
        get { return _speed; }
        set
        {
            if (_speed != value)
            {
                _speed = value;
                _duration = 1 / _speed;
            }
        }
    }

    public bool _isEquiptWeapon = true;
    public bool _isEquiptShield = true;


    GameObject _weapon;
    GameObject _shield;
    string _weaponName = "Sword01";
    string _shieldName = "Shield00";

    float _duration;
    public bool _isMoving = false;

    bool _isInteracted = false;

    float _offset = Define.TILE_SIZE * 0.33f;
    Vector3 _interpolateRayPos = new Vector3(0f, -2f, 0f);
    public Vector3 _cellPos;
    Vector3 _nextCellPos;

    MoveDir _moveDir = MoveDir.None;
    public PlayerState _state = PlayerState.IdleDown;
    public void SetState(PlayerState state)
    {
        _state = state;
    }

    void Start()
    {
        Managers.Input.KeyAction -= OnKeyboard;
        Managers.Input.KeyAction += OnKeyboard;

        _keyInventory = GameObject.Find("KeyInventory");
        _weapon = GameObject.Find("WeaponSlot");
        _shield = GameObject.Find("ShieldSlot");

        Managers.Game.Player = this; // ���� �� �� ������ �����ϵ��� �ϱ� ����.
    }

    void OnKeyboard()
    {
        if (Managers.Game.OnBattle || Managers.Game.OnConversation || Managers.Game.OnLever 
            || Managers.Game.OnFade || Managers.Game.OnDirect)
        {
            return;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            _moveDir = MoveDir.Up;
            Moving(_moveDir);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            _moveDir = MoveDir.Down;
            Moving(_moveDir);
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            _moveDir = MoveDir.Left;
            Moving(_moveDir);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            _moveDir = MoveDir.Right;
            Moving(_moveDir);
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            CheckInteract();
        }
    }

    private void Update()
    {
        PlayAnimation();
        CheckWeapon();
        CheckShield();

        if (Managers.Game.OnBattle || Managers.Game.OnConversation || Managers.Game.OnLever
            || Managers.Game.OnFade || Managers.Game.OnDirect)
        {
            return;
        }

        if (_isMoving == false && _moveDir != MoveDir.None)
        {
            SetIdleState(_moveDir);
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
        switch (_state)
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
            case PlayerState.OnLever:
                GetComponent<Animator>().Play("Player_IronLever_B");
                _isEquiptShield = false;
                _isEquiptWeapon = false;
                break;
        }
    }

    #region Moving
    public void Moving(Define.MoveDir moveDir)
    {
        if (_isMoving)
        {
            return;
        }

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
            _isMoving = false;
            return;
        }

        if (_isInteracted)
            return;

        // Move
        _cellPos += _nextCellPos;
        transform.DOMove(_cellPos, _duration).SetEase(Ease.Linear).OnComplete(()=> 
        {
            _isMoving = false;
        });
    }

    void SetIdleState(MoveDir moveDir)
    {
        _isMoving = false;

        if (_state == PlayerState.OnLever)
            return;

        if (moveDir == MoveDir.Up)
            _state = PlayerState.IdleUp;
        else
            _state = PlayerState.IdleDown;
    }
    #endregion

    void CheckInteract()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position + _interpolateRayPos, _nextCellPos, out hit, _offset);

        InteractObjectController interactObejct = hit.collider.gameObject.GetComponent<InteractObjectController>();

        if (interactObejct != null)
        {
            interactObejct.Interact();
        }
    }

    bool CheckSomething()
    {
        bool somethingExist = false;
        int layerMask = (1 << (int)Define.Layer.Wall) + (1 << (int)Define.Layer.CItem) + (1 << (int)Define.Layer.Door) + (1 << (int)Define.Layer.Portal)
            + (1 << (int)Define.Layer.EItem) + (1 << (int)Define.Layer.Lever) + (1 << (int)Define.Layer.Monster);

        RaycastHit hit;
        Physics.Raycast(transform.position + _interpolateRayPos, _nextCellPos, out hit, _offset, layerMask);

        if (hit.collider != null)
        {
            // Checking Wall
            if (hit.collider.gameObject.layer == (int)Define.Layer.Wall)
            {
                somethingExist = true;
            }
            //Checking Monster
            //else if (hit.collider.gameObject.layer == (int)Define.Layer.Monster)
            //{

            //}
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
                    _isInteracted = true;
                    InteractAnim().OnComplete(() =>
                    {
                        SetIdleState(_moveDir);
                        _isInteracted = false;
                    });
                }
            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.Portal)
            {
                somethingExist = true;
                hit.collider.gameObject.GetComponentInChildren<PortalController>().UsePortal();
                _cellPos = transform.position;
            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.Lever)
            {
                somethingExist = true;

                Vector3 originPos = _cellPos;
                Vector3 movePos = new Vector3(hit.collider.transform.position.x, transform.position.y + 0.2f, hit.collider.transform.position.z);

                transform.DOMove(movePos, 0.2f).OnComplete(() =>
                {
                    _state = PlayerState.OnLever;
                    Managers.Game.OnLever = true;

                    hit.collider.gameObject.GetComponentInChildren<Lever>().Play(1.0f).OnComplete(() =>
                    {
                        _state = PlayerState.IdleDown;
                        hit.collider.gameObject.GetComponentInChildren<Lever>().SetActiveLight();
                        hit.collider.gameObject.GetComponentInChildren<Lever>().Open();
                        _isEquiptShield = true;
                        _isEquiptWeapon = true;
                        transform.DOMove(originPos, 0.2f);
                        Managers.Game.OnLever = false;
                    });
                });
            }
        }

        return somethingExist;
    }

    Sequence InteractAnim()
    {
        Vector3 interactPos = _cellPos;
        switch (_moveDir)
        {
            case MoveDir.Up:
                interactPos += Vector3.forward * _offset / 2;
                break;
            case MoveDir.Down:
                interactPos += Vector3.back * _offset / 2;
                break;
            case MoveDir.Left:
                interactPos += Vector3.left * _offset / 2;
                break;
            case MoveDir.Right:
                interactPos += Vector3.right * _offset / 2;
                break;
        }

        Sequence seq = DOTween.Sequence();

        seq.Append(gameObject.transform.DOMove(interactPos, 0.2f));
        seq.Append(gameObject.transform.DOMove(_cellPos, 0.2f));

        return seq;
    }
}
