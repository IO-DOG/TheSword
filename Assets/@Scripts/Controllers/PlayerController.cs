using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using static Define;
using Unity.Burst.CompilerServices;
//using UnityEditor.Experimental.GraphView;

public class PlayerController : MonoBehaviour
{
    const float adjustingDis = 0.01f;
    public GameObject _keyInventory;

    float _speed = 5.0f;
    public float Speed
    {
        get { return _speed; }
        set
        {
            _speed = Managers.Game.PlayerData.MoveSpeed * 5;
            _duration = 1 / _speed;
        }
    }

    public bool _isEquiptWeapon = true;
    public bool _isEquiptShield = true;

    PortalController _bossRoom;

    public GameObject _weapon;
    public GameObject _shield;
    public GameObject _back;

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

        transform.localScale = new Vector3(1f, 2f, 1f);

        _duration = 1 / _speed;
        _keyInventory = GameObject.Find("KeyInventory");
        _weapon = GameObject.Find("WeaponSlot");
        _shield = GameObject.Find("ShieldSlot");
        _back = GameObject.Find("BackSlot");

        if (PlayerPrefs.GetInt("ISFIRST", 1) != 1)
        {
            _back.SetActive(false);
        }
    }

    Stack<int> keyInputStack = new Stack<int>();

    void OnKeyboard()
    {
        if (Managers.Game.OnBattle || Managers.Game.OnConversation || Managers.Game.OnLever
            || Managers.Game.OnFade || Managers.Game.OnDirect || Managers.Game.OnInteract || Managers.Game.OnInputLock)
        {
            _moveDir = MoveDir.None;
            return;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            keyInputStack.Push((int)MoveDir.Up);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            keyInputStack.Push((int)MoveDir.Right);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            keyInputStack.Push((int)MoveDir.Down);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            keyInputStack.Push((int)MoveDir.Left);
        }

        // 값을 임시로 저장할 스택 생성
        Stack<int> tempStack = new Stack<int>();

        if (Input.GetKeyUp(KeyCode.W))
        {
            while (keyInputStack.Count > 0)
            {
                int current = keyInputStack.Pop();
                if (current == (int)MoveDir.Up)
                    break;
                else
                    tempStack.Push(current);
            }
            while (tempStack.Count > 0)
                keyInputStack.Push(tempStack.Pop());
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            while (keyInputStack.Count > 0)
            {
                int current = keyInputStack.Pop();
                if (current == (int)MoveDir.Right)
                    break;
                else
                    tempStack.Push(current);
            }
            while (tempStack.Count > 0)
                keyInputStack.Push(tempStack.Pop());
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            while (keyInputStack.Count > 0)
            {
                int current = keyInputStack.Pop();
                if (current == (int)MoveDir.Down)
                    break;
                else
                    tempStack.Push(current);
            }
            while (tempStack.Count > 0)
                keyInputStack.Push(tempStack.Pop());
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            while (keyInputStack.Count > 0)
            {
                int current = keyInputStack.Pop();
                if (current == (int)MoveDir.Left)
                    break;
                else
                    tempStack.Push(current);
            }
            while (tempStack.Count > 0)
                keyInputStack.Push(tempStack.Pop());
        }

        int topKey = -1;
        keyInputStack.TryPeek(out topKey);
        if (Input.GetKey(KeyCode.W) && topKey == (int)MoveDir.Up)
        {
            _moveDir = MoveDir.Up;
        }
        if (Input.GetKey(KeyCode.D) && topKey == (int)MoveDir.Right)
        {
            _moveDir = MoveDir.Right;
        }
        if (Input.GetKey(KeyCode.S) && topKey == (int)MoveDir.Down)
        {
            _moveDir = MoveDir.Down;
        }
        if (Input.GetKey(KeyCode.A) && topKey == (int)MoveDir.Left)
        {
            _moveDir = MoveDir.Left;
        }

        if (_moveDir != MoveDir.None && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)))
        {
            Moving(_moveDir, false);
        }
    }

    private void Update()
    {
        PlayAnimation();

        if (Managers.Game.OnBattle || Managers.Game.OnConversation || Managers.Game.OnLever
            || Managers.Game.OnFade || Managers.Game.OnDirect || Managers.Game.OnInteract || Managers.Game.OnInputLock)
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
        if (Managers.Game.PlayerData.CurSword == Define.NOT_EQUIP)
            _isEquiptWeapon = false;
        if (_isEquiptWeapon)
            _weapon.SetActive(true);
        else
            _weapon.SetActive(false);
    }

    void CheckShield()
    {
        if (Managers.Game.PlayerData.CurShield == Define.NOT_EQUIP)
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
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Idle_B");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Idle_B");

                _weapon.transform.localPosition = Vector3.forward * adjustingDis;
                _shield.transform.localPosition = Vector3.forward * adjustingDis;

                break;
            case PlayerState.IdleFront:
                GetComponent<Animator>().speed = 1f;
                GetComponent<Animator>().Play("Player_Idle_F");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Idle_F");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Idle_F");

                _weapon.transform.localPosition = Vector3.back * adjustingDis;
                _shield.transform.localPosition = Vector3.back * adjustingDis;
                break;
            case PlayerState.IdleLeft:
                GetComponent<Animator>().speed = Managers.Game.PlayerData.MoveSpeed;
                GetComponent<Animator>().Play("Player_Idle_L");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Idle_L");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Idle_L");

                _weapon.transform.localPosition = Vector3.forward * adjustingDis;
                _shield.transform.localPosition = Vector3.back * adjustingDis;
                break;
            case PlayerState.IdleRight:
                GetComponent<Animator>().speed = Managers.Game.PlayerData.MoveSpeed;
                GetComponent<Animator>().Play("Player_Idle_R");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Idle_R");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Idle_R");

                _weapon.transform.localPosition = Vector3.back * adjustingDis;
                _shield.transform.localPosition = Vector3.forward * adjustingDis;
                break;
            case PlayerState.Left:
                GetComponent<Animator>().speed = Managers.Game.PlayerData.MoveSpeed;
                GetComponent<Animator>().Play("Player_Run_L");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Run_L");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Run_L");

                _weapon.transform.localPosition = Vector3.forward * adjustingDis;
                _shield.transform.localPosition = Vector3.back * adjustingDis;
                break;
            case PlayerState.Right:
                GetComponent<Animator>().speed = Managers.Game.PlayerData.MoveSpeed;
                GetComponent<Animator>().Play("Player_Run_R");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Run_R");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Run_R");

                _weapon.transform.localPosition = Vector3.back * adjustingDis;
                _shield.transform.localPosition = Vector3.forward * adjustingDis;
                break;
            case PlayerState.Up:
                GetComponent<Animator>().speed = Managers.Game.PlayerData.MoveSpeed;
                GetComponent<Animator>().Play("Player_Run_B");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Run_B");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Run_B");

                _weapon.transform.localPosition = Vector3.forward * adjustingDis;
                _shield.transform.localPosition = Vector3.forward * adjustingDis;
                break;
            case PlayerState.Down:
                GetComponent<Animator>().speed = Managers.Game.PlayerData.MoveSpeed;
                GetComponent<Animator>().Play("Player_Run_F");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Run_F");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Run_F");

                _weapon.transform.localPosition = Vector3.back * adjustingDis;
                _shield.transform.localPosition = Vector3.back * adjustingDis;
                break;
            case PlayerState.BackStep:
                GetComponent<Animator>().speed = 1f;
                GetComponent<Animator>().Play("Player_BackStep");
                if (_isEquiptWeapon)
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Run_F");
                if (_isEquiptShield)
                    _shield.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Run_F");

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
            case PlayerState.Death:
                GetComponent<Animator>().Play("Player_Death");
                break;
            case PlayerState.TutorialFirst_Ready:
                GetComponent<Animator>().Play("Player_TutorialFirst_Ready");
                break;
        }
    }

    public void ResetWeaponAndShieldAnimation()
    {
        if (_isEquiptWeapon)
        {
            switch (_state)
            {
                case PlayerState.IdleFront:
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Idle_F", 0, 0.0f);
                    break;
                case PlayerState.IdleLeft:
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Idle_L", 0, 0.0f);
                    break;
                case PlayerState.IdleRight:
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Idle_R", 0, 0.0f);
                    break;
            }

        }

        if (_isEquiptShield)
        {
            switch (_state)
            {
                case PlayerState.IdleFront:
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Idle_F", 0, 0.0f);
                    break;
                case PlayerState.IdleLeft:
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Idle_L", 0, 0.0f);
                    break;
                case PlayerState.IdleRight:
                    _weapon.GetComponent<Animator>().Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Idle_R", 0, 0.0f);
                    break;
            }
        }
    }

    public void SetPlayerPosition(Vector3 pos)
    {
        transform.position = pos;
        _cellPos = pos;
    }

    #region Moving
    public void Moving(Define.MoveDir moveDir, bool isDirecting)
    {
        if (_isMoving && !isDirecting)
        {
            return;
        }

        _isMoving = true;
        int moveCount = PlayerPrefs.GetInt("MOVECOUNT", 0);
        moveCount++;
        PlayerPrefs.SetInt("MOVECOUNT", moveCount);

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
        if (isObstacled())
        {
            _isMoving = false;
            return;
        }

        // Move
        _cellPos += _nextCellPos;
        transform.DOMove(_cellPos, _duration).SetEase(Ease.Linear).OnComplete(() =>
        {
            Managers.Sound.Play(Define.Sound.Effect, "HeroMove_SFX");

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

    float _lastHitLog;

    bool isObstacled()
    {
        bool somethingExist = false;

        RaycastHit hit;
        Physics.Raycast(transform.position + _interpolateRayPos, _nextCellPos, out hit, _offset * 1.3f);

        // 밀었는데 아무 일도 안 일어나는 경우가 있다. 그때 광선이 무엇을 맞혔는지
        // 남긴다 — 이 한 줄이 없으면 상대가 왜 반응하지 않는지 알 수가 없다.
        if (Time.time - _lastHitLog > 2f)
        {
            _lastHitLog = Time.time;
            if (hit.collider == null)
                Debug.Log($"[충돌] 광선 빈손 dir={_nextCellPos} len={_offset * 1.3f:0.00} " +
                          $"origin={transform.position + _interpolateRayPos}");
            else
                Debug.Log($"[충돌] {hit.collider.gameObject.name} L{hit.collider.gameObject.layer} " +
                          $"거리{hit.distance:0.00}");
        }

        if (hit.collider != null)
        {
            // Checking Wall
            if (hit.collider.gameObject.layer == (int)Define.Layer.Wall)
            {
                somethingExist = true;
            }
            //Checking Monster
            else if (hit.collider.gameObject.layer == (int)Define.Layer.Monster && !Managers.Game.OnBattle)
            {
                MonsterController mc = Util.Find<MonsterController>(hit.collider.gameObject);
                if (mc != null)
                    mc.SetMonster();
                somethingExist = true;
            }
            // Checking Item
            else if (hit.collider.gameObject.layer == (int)Define.Layer.CItem)
            {
                ConsumableItem citem = Util.Find<ConsumableItem>(hit.collider.gameObject);
                if (citem != null)
                    citem.PickUp();
            }
            // Checking Item
            else if (hit.collider.gameObject.layer == (int)Define.Layer.EItem)
            {
                Equip equip = Util.Find<Equip>(hit.collider.gameObject);
                if (equip != null)
                    equip.PickUp();
            }
            //Checking Door
            else if (hit.collider.gameObject.layer == (int)Define.Layer.Door)
            {
                if (Managers.Game.OnInteract)
                    return true;
                Door door = Door.Find(hit.collider.gameObject);
                if (door == null)
                    return false;

                if (Managers.Game.KeyInventory.TryUseKey(hit.collider.gameObject))
                {
                    Managers.Game.OnInteract = true;
                    SetIdleState(_moveDir);
                    somethingExist = true;
                    door.CoDoorLockOpenAnim();
                    door.CoOpenDoor(1f);
                    door.FadeDoor().OnComplete(() =>
                    {
                        hit.collider.gameObject.SetActive(false);
                    });
                }
                else
                {
                    Managers.Game.OnInteract = true;
                    door.CoDoorLockLockedAnim();
                    somethingExist = true;

                    InteractAnim().OnComplete(() =>
                    {
                        SetIdleState(_moveDir);
                        Managers.Game.OnInteract = false;
                    });
                }

                // 최초 문인지 확인
                if (PlayerPrefs.GetInt("ISFIRSTKEY") == 0)
                {
                    PlayerPrefs.SetInt("ISFIRSTKEY", 1);
                    UI_GuidePopup guidePopup = Managers.UI.ShowPopupUI<UI_GuidePopup>();
                    guidePopup.SetInfo(Define.GUIDE_KEY);
                }
            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.Portal && !Managers.Game.OnFade && !Managers.Game.OnInteract)
            {
                somethingExist = false;
                PortalController portal = Util.FindInTile<PortalController>(hit.collider.gameObject);
                if (portal != null)
                    portal.UsePortal();
            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.Lever)
            {
                somethingExist = true;

                Lever lever = Util.FindInTile<Lever>(hit.collider.gameObject);
                if (lever == null)
                    return true;

                Vector3 originPos = _cellPos;
                Vector3 movePos = new Vector3(hit.collider.transform.position.x, transform.position.y + 0.2f, hit.collider.transform.position.z - 0.1f);

                transform.DOMove(movePos, 0.2f).OnPlay(() =>
                {
                    _state = PlayerState.OnLever;
                    Managers.Game.OnLever = true;

                    StartCoroutine(Managers.Sound.CoPlay(Define.Sound.Effect, "Gimic_leverOn_SFX", 1, 0.3f));

                    lever.Play(1.0f).OnComplete(() =>
                    {

                        _state = PlayerState.IdleFront;
                        lever.SetActive();
                        lever.Open();
                        _isEquiptShield = true;
                        _isEquiptWeapon = true;
                        transform.DOMove(originPos, 0.2f).OnComplete(() =>
                        {
                            Managers.Game.OnLever = false;
                            _cellPos = originPos;
                            transform.position = _cellPos;
                            //Managers.Game.SaveGame();
                        });
                    });
                });

                // 최초 레버인지 확인
                if (PlayerPrefs.GetInt("ISFIRSTLEVER") == 0)
                {
                    PlayerPrefs.SetInt("ISFIRSTLEVER", 1);
                    UI_GuidePopup guidePopup = Managers.UI.ShowPopupUI<UI_GuidePopup>();
                    guidePopup.SetInfo(Define.GUIDE_LEVER);
                }
            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.BossDoor)
            {
                if (Managers.Game.OnDirect)
                    return false;

                somethingExist = true;

                Vector3 playerDir = (transform.position - hit.collider.transform.position).normalized;
                float dotProduct = Vector3.Dot(Vector3.back, playerDir);
                if (dotProduct > 0.7f)
                {
                    Managers.UI.ShowPopupUI<UI_BossRoomCheckPopup>();
                }
            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.InteractObjects)
            {
                somethingExist = true;
                if (GetTouchDirection(hit.collider.transform, Vector3.back) != TouchDir.None)
                {
                    InteractObjectController interactObejct = Util.FindInTile<InteractObjectController>(hit.collider.gameObject);
                    Managers.Game.CurInteractObject = hit.collider.gameObject;
                    if (interactObejct != null)
                        interactObejct.Interact();
                }
            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.BossEventTrigger)
            {
                if (GetTouchDirection(hit.collider.transform, Vector3.back) == TouchDir.Right)
                {
                    Moving(MoveDir.Left, true);
                }
                else if (GetTouchDirection(hit.collider.transform, Vector3.back) == TouchDir.Left)
                {
                    Moving(MoveDir.Right, true);
                }
                somethingExist = true;
                Managers.Resource.Destroy(hit.collider.gameObject);
                Managers.Directing.BossOnAppearAction?.Invoke();
            }
        }

        // 광선이 아무것도 못 맞혔는데 갈 칸에 몬스터가 서 있을 수 있다.
        // 광선은 플레이어 키높이로 얇게 나가서, 스프라이트를 띄워 놓은 보스처럼
        // 콜라이더가 위쪽에 있는 상대는 스치지도 못한다 —
        // 킹슬라임이 정확히 그래서, 밀어도 전투가 안 열리고 그대로 통과했다.
        // 갈 칸을 상자로 한 번 더 훑어 몬스터가 있으면 붙는다.
        if (somethingExist == false && !Managers.Game.OnBattle)
        {
            // 레이어로 거르지 않는다. 보스는 레이어가 어긋나 있을 수도 있어서,
            // MonsterController 가 붙어 있는가만 본다.
            // 콜라이더와 컨트롤러가 다른 오브젝트에 있는 경우도 있어 위아래로 찾는다.
            Vector3 nextCell = _cellPos + _nextCellPos;
            Collider[] found = Physics.OverlapBox(
                nextCell, Vector3.one * (Define.TILE_SIZE * 0.45f), Quaternion.identity,
                ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < found.Length; i++)
            {
                MonsterController mc = Util.Find<MonsterController>(found[i].gameObject);
                if (mc == null)
                    continue;
                Debug.Log($"{mc.gameObject.name} (광선 밖 — 칸 검사로 붙는다)");
                mc.SetMonster();
                somethingExist = true;
                break;
            }

            // 아이템도 같은 이유로 광선을 벗어날 수 있다. 몬스터가 떨군 보상이
            // 그랬다 — 밟고 지나가는데 줍히지 않아서, 치울 수 없는 장애물로 남아
            // 그 칸에서 진행이 끝났다. 콜라이더 높이에 기대지 말고 칸으로 잡는다.
            for (int i = 0; i < found.Length && somethingExist == false; i++)
            {
                Equip eq = Util.Find<Equip>(found[i].gameObject);
                if (eq != null && eq.gameObject.activeInHierarchy)
                {
                    eq.PickUp();
                    continue;
                }

                ConsumableItem ci = Util.Find<ConsumableItem>(found[i].gameObject);
                if (ci != null && ci.gameObject.activeInHierarchy)
                    ci.PickUp();
            }
        }

        //Managers.Game.SaveGame();

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

    public enum TouchDir
    {
        None,
        Right,
        Left,
        FaceToFace,
    }

    public TouchDir GetTouchDirection(Transform otherObject, Vector3 otherObjectDir)
    {
        // 플레이어와 otherObject 간의 방향 벡터 계산
        Vector3 playerDir = (transform.position - otherObject.position).normalized;

        // otherObject의 Local Space X축 (Right)
        Vector3 rightDir = otherObject.right;

        // Y축 평면으로 투영
        Vector3 flattenedPlayerDir = new Vector3(playerDir.x, 0, playerDir.z).normalized;
        Vector3 flattenedRightDir = new Vector3(rightDir.x, 0, rightDir.z).normalized;

        // 정면 충돌 여부 확인
        float dotProduct = Vector3.Dot(otherObjectDir, flattenedPlayerDir);
        if (dotProduct > 0.98f) // 정면 기준 충돌
        {
            return TouchDir.FaceToFace;
        }
        else if(dotProduct > 0.7f)
        {
            if (flattenedPlayerDir.x > 0) // 오른쪽
            {
                return TouchDir.Right;
            }
            else if (flattenedPlayerDir.x < 0) // 왼쪽
            {
                return TouchDir.Left;
            }
        }

        return TouchDir.None; // 정면 충돌 아님
    }
}
