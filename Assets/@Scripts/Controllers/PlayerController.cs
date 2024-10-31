using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using static Define;
using Unity.Burst.CompilerServices;

public class PlayerController : MonoBehaviour
{
    const float adjustingDis = 0.003f;
    public GameObject _keyInventory;

    float _speed = 5.0f;
    public float Speed
    {
        get { return _speed; }
        set
        {
           _speed = Managers.Game.CurPlayerData.MoveSpeed * 5;
           _duration = 1 / _speed;
        }
    }

    public bool _isEquiptWeapon = true;
    public bool _isEquiptShield = true;

    PortalController _bossRoom;

    GameObject _weapon;
    GameObject _shield;

    float _duration;
    bool _isMoving = false;

    float _offset = Define.TILE_SIZE;
    Vector3 _interpolateRayPos = new Vector3(0f, Define.TILE_SIZE / 2f, 0f);
    public Vector3 _cellPos;

    Vector3 _nextCellPos;

    public MoveDir _moveDir = MoveDir.None;
    public PlayerState _state = PlayerState.IdleFront;
    public void SetState(PlayerState state)
    {
        _state = state;
    }

    private void Awake()
    {
        Managers.Game.Player = this;
    }

    void Start()
    {
        Managers.Input.KeyAction -= OnKeyboard;
        Managers.Input.KeyAction += OnKeyboard;
        Managers.Input.KeyAction -= Managers.Directing.Events.MeetKingSlime;
        Managers.Input.KeyAction += Managers.Directing.Events.MeetKingSlime;

        _duration = 1 / _speed;
        _keyInventory = GameObject.Find("KeyInventory");
        _weapon = GameObject.Find("WeaponSlot");
        _shield = GameObject.Find("ShieldSlot");
    }

    void OnKeyboard()
    {
        if (Managers.Game.OnBattle || Managers.Game.OnConversation || Managers.Game.OnLever
            || Managers.Game.OnFade || Managers.Game.OnDirect || Managers.Game.OnInteract)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _moveDir = MoveDir.Up;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            _moveDir = MoveDir.Down;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _moveDir = MoveDir.Left;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _moveDir = MoveDir.Right;
        }

        if(_moveDir != MoveDir.None && (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow)||
            Input.GetKey(KeyCode.LeftArrow)|| Input.GetKey(KeyCode.RightArrow)))
        {
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

        if (Managers.Game.OnBattle || Managers.Game.OnConversation || Managers.Game.OnLever
            || Managers.Game.OnFade || Managers.Game.OnDirect || Managers.Game.OnInteract)
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
        if (Managers.Game.CurPlayerData.CurSword == 0)
            _isEquiptWeapon = false;
        if (_isEquiptWeapon)
            _weapon.SetActive(true);
        else
            _weapon.SetActive(false);
    }

    void CheckShield()
    {
        if (Managers.Game.CurPlayerData.CurShield == 0)
            _isEquiptShield = false;
        if (_isEquiptShield)
            _shield.SetActive(true);
        else
            _shield.SetActive(false);
    }

    void PlayAnimation()
    {
        CheckWeapon();
        CheckShield();

        switch (_state)
        {
            case PlayerState.IdleBack:
                GetComponent<Animator>().speed = 1f;
                GetComponent<Animator>().Play("Player_Idle_B");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurSword].ImageName}_Idle_B");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurShield].ImageName}_Idle_B");

                _weapon.transform.localPosition = Vector3.forward * adjustingDis;
                _shield.transform.localPosition = Vector3.forward * adjustingDis;

                break;
            case PlayerState.IdleFront:
                GetComponent<Animator>().speed = 1f;
                GetComponent<Animator>().Play("Player_Idle_F");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurSword].ImageName}_Idle_F");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurShield].ImageName}_Idle_F");

                _weapon.transform.localPosition = Vector3.back * adjustingDis;
                _shield.transform.localPosition = Vector3.back * adjustingDis;
                break;
            case PlayerState.IdleLeft:
                GetComponent<Animator>().speed = Managers.Game.CurPlayerData.MoveSpeed;
                GetComponent<Animator>().Play("Player_Idle_L");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurSword].ImageName}_Idle_L");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurShield].ImageName}_Idle_L");

                _weapon.transform.localPosition = Vector3.forward * adjustingDis;
                _shield.transform.localPosition = Vector3.back * adjustingDis;
                break;
            case PlayerState.IdleRight:
                GetComponent<Animator>().speed = Managers.Game.CurPlayerData.MoveSpeed;
                GetComponent<Animator>().Play("Player_Idle_R");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurSword].ImageName}_Idle_R");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurShield].ImageName}_Idle_R");

                _weapon.transform.localPosition = Vector3.back * adjustingDis;
                _shield.transform.localPosition = Vector3.forward * adjustingDis;
                break;
            case PlayerState.Left:
                GetComponent<Animator>().speed = Managers.Game.CurPlayerData.MoveSpeed;
                GetComponent<Animator>().Play("Player_Run_L");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurSword].ImageName}_Run_L");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurShield].ImageName}_Run_L");

                _weapon.transform.localPosition = Vector3.forward * adjustingDis;
                _shield.transform.localPosition = Vector3.back * adjustingDis;
                break;
            case PlayerState.Right:
                GetComponent<Animator>().speed = Managers.Game.CurPlayerData.MoveSpeed;
                GetComponent<Animator>().Play("Player_Run_R");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurSword].ImageName}_Run_R");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurShield].ImageName}_Run_R");

                _weapon.transform.localPosition = Vector3.back * adjustingDis;
                _shield.transform.localPosition = Vector3.forward * adjustingDis;
                break;
            case PlayerState.Up:
                GetComponent<Animator>().speed = Managers.Game.CurPlayerData.MoveSpeed;
                GetComponent<Animator>().Play("Player_Run_B");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurSword].ImageName}_Run_B");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurShield].ImageName}_Run_B");

                _weapon.transform.localPosition = Vector3.forward * adjustingDis;
                _shield.transform.localPosition = Vector3.forward * adjustingDis;
                break;
            case PlayerState.Down:
                GetComponent<Animator>().speed = Managers.Game.CurPlayerData.MoveSpeed;
                GetComponent<Animator>().Play("Player_Run_F");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurSword].ImageName}_Run_F");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurShield].ImageName}_Run_F");

                _weapon.transform.localPosition = Vector3.back * adjustingDis;
                _shield.transform.localPosition = Vector3.back * adjustingDis;
                break;
            case PlayerState.BackStep:
                GetComponent<Animator>().speed = 1f;
                GetComponent<Animator>().Play("Player_BackStep");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurSword].ImageName}_Run_F");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurShield].ImageName}_Run_F");

                _weapon.transform.localPosition = Vector3.back * adjustingDis;
                _shield.transform.localPosition = Vector3.back * adjustingDis;
                break;
            case PlayerState.OnLever:
                GetComponent<Animator>().speed = 1f;
                GetComponent<Animator>().Play("Player_IronLever_B");
                _isEquiptShield = false;
                _isEquiptWeapon = false;
                break;
            case PlayerState.DrawSword:
                GetComponent<Animator>().speed = 1f;
                GetComponent<Animator>().Play("Player_SwordDraw_B");
                _isEquiptShield = false;
                _isEquiptWeapon = false;
                break;
            case PlayerState.ContractSword:
                GetComponent<Animator>().speed = 1f;
                GetComponent<Animator>().Play("Player_ContractSword_F");
                _isEquiptShield = false;
                _isEquiptWeapon = false;
                break;
        }
    }

    public void ResetWeaponAndShieldAnimation()
    {
        if (_isEquiptWeapon)
        {
            switch(_state)
            {
                case PlayerState.IdleFront:
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurSword].ImageName}_Idle_F", 0, 0.0f);
                    break;
                case PlayerState.IdleLeft:
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurSword].ImageName}_Idle_L", 0, 0.0f);
                    break;
                case PlayerState.IdleRight:
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurSword].ImageName}_Idle_R", 0, 0.0f);
                    break;
            }

        }
            
        if (_isEquiptShield)
        {
            switch (_state)
            {
                case PlayerState.IdleFront:
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurShield].ImageName}_Idle_F", 0, 0.0f);
                    break;
                case PlayerState.IdleLeft:
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurShield].ImageName}_Idle_L", 0, 0.0f);
                    break;
                case PlayerState.IdleRight:
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.CurPlayerData.CurShield].ImageName}_Idle_R", 0, 0.0f);
                    break;
            }
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
                _nextCellPos = Vector3.left * _offset;
                _state = PlayerState.Left;
                break;
            case MoveDir.Right:
                _nextCellPos = Vector3.right * _offset;
                _state = PlayerState.Right;
                break;
            case MoveDir.Back:
                _nextCellPos = Vector3.back * _offset;
                _state = PlayerState.BackStep;
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
        transform.DOMove(_cellPos, _duration).SetEase(Ease.Linear).OnComplete(()=> 
        {
            _isMoving = false;
        });
    }

    public void SetIdleState(MoveDir moveDir)
    {
        _isMoving = false;

        if (_state == PlayerState.OnLever)
            return;

        if (moveDir == MoveDir.Up)
            _state = PlayerState.IdleBack;
        else if (moveDir == MoveDir.Left)
            _state = PlayerState.IdleLeft;
        else if (moveDir == MoveDir.Right)
            _state = PlayerState.IdleRight;
        else
            _state = PlayerState.IdleFront;
    }
    #endregion

    void CheckInteract()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position + _interpolateRayPos, _nextCellPos, out hit, _offset, LayerMask.GetMask("InteractObjects"));

         if (hit.collider != null)
         {
            InteractObjectController interactObejct = hit.collider.gameObject.GetComponent<InteractObjectController>();
            Managers.Game.CurInteractObject = hit.collider.gameObject;
            interactObejct.Interact();
         }
    }

    bool CheckSomething()
    {
        bool somethingExist = false;
        //int layerMask = (1 << (int)Define.Layer.Wall) + (1 << (int)Define.Layer.CItem) + (1 << (int)Define.Layer.Door) + (1 << (int)Define.Layer.Portal)
            //+ (1 << (int)Define.Layer.EItem) + (1 << (int)Define.Layer.Lever) + (1 << (int)Define.Layer.Monster) + (1 << (int)Define.Layer.InteractObjects); 

        RaycastHit hit;
        Physics.Raycast(transform.position + _interpolateRayPos, _nextCellPos, out hit, _offset);

        if (hit.collider != null)
        {
            // Checking Wall
            if (hit.collider.gameObject.layer == (int)Define.Layer.Wall || hit.collider.gameObject.layer == (int)Define.Layer.InteractObjects)
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
                if (Managers.Game.OnInteract)
                    return true;
                if (Managers.Game.KeyInventory.TryUseKey(hit.collider.gameObject))
                {
                    Managers.Game.OnInteract = true;
                    SetIdleState(_moveDir);
                    somethingExist = true;

                    hit.collider.gameObject.GetComponentInChildren<Door>().CoDoorLockOpenAnim();
                    hit.collider.gameObject.GetComponentInChildren<Door>().CoOpenDoor(2.5f);
                    hit.collider.gameObject.GetComponentInChildren<Door>().FadeDoor().OnComplete(() =>
                    {
                        Managers.Game.OnInteract = false;
                        hit.collider.gameObject.SetActive(false);
                        somethingExist = false;
                    });
                }
                else if(!Managers.Game.KeyInventory.TryUseKey(hit.collider.gameObject))
                {
                    hit.collider.gameObject.GetComponentInChildren<Door>().CoDoorLockLockedAnim();
                    Managers.Game.OnInteract = true;
                    somethingExist = true;
                    InteractAnim().OnComplete(() =>
                    {
                        SetIdleState(_moveDir);
                        Managers.Game.OnInteract = false;
                    });
                }
            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.Portal)
            {
                somethingExist = false;
                hit.collider.gameObject.GetComponentInChildren<PortalController>().UsePortal();
            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.Lever)
            {
                somethingExist = true;

                Vector3 originPos = _cellPos;
                Vector3 movePos = new Vector3(hit.collider.transform.position.x, transform.position.y + 0.2f, hit.collider.transform.position.z - 0.1f);

                transform.DOMove(movePos, 0.2f).OnPlay(() =>
                {
                    _state = PlayerState.OnLever;
                    Managers.Game.OnLever = true;

                    hit.collider.gameObject.GetComponentInChildren<Lever>().Play(1.0f).OnComplete(() =>
                    {
                        _state = PlayerState.IdleFront;
                        hit.collider.gameObject.GetComponentInChildren<Lever>().SetActive();
                        hit.collider.gameObject.GetComponentInChildren<Lever>().Open();
                        _isEquiptShield = true;
                        _isEquiptWeapon = true;
                        transform.DOMove(originPos, 0.2f).OnComplete(()=>
                        { 
                            Managers.Game.OnLever = false;
                            _cellPos = originPos;
                            transform.position = _cellPos;
                            Managers.Game.SaveGame();
                        });
                    });
                });
            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.BossDoor)
            {
                if (Managers.Game.OnDirect)
                    return false;

                somethingExist = true;

                Managers.Game.BossRoom = hit.collider.gameObject.GetComponentInChildren<PortalController>().transform;
                Managers.UI.ShowPopupUI<UI_BossRoomCheckPopup>();
            }
        }

        Managers.Game.SaveGame();

        return somethingExist;
    }

    Sequence InteractAnim()
    {
        Vector3 interactPos = _cellPos;
        switch (_moveDir)
        {
            case MoveDir.Up:
                interactPos += Vector3.forward * _offset / 3;
                break;
            case MoveDir.Down:
                interactPos += Vector3.back * _offset / 3;
                break;
            case MoveDir.Left:
                interactPos += Vector3.left * _offset / 3;
                break;
            case MoveDir.Right:
                interactPos += Vector3.right * _offset / 3;
                break;
        }

        Sequence seq = DOTween.Sequence();

        seq.Append(gameObject.transform.DOMove(interactPos, 0.2f));
        seq.Append(gameObject.transform.DOMove(_cellPos, 0.2f));

        return seq;
    }
}
