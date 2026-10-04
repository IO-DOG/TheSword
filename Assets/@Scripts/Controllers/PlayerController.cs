using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

    // 매 프레임 GetComponent 하던 것들. 무기·방패 애니메이터는 Start 에서 슬롯을 찾은 뒤 잡는다.
    Animator _body;
    Animator _weaponAnim;
    Animator _shieldAnim;

    private void Awake()
    {
        Managers.Game.Player = this;
        _body = GetComponent<Animator>();
    }

    void Start()
    {
        Managers.Input.KeyAction -= OnKeyboard;
        Managers.Input.KeyAction += OnKeyboard;
        FightGate.Add(RefuseStrayFight, int.MinValue);

        transform.localScale = new Vector3(1f, 2f, 1f);

        // 세터가 MoveSpeed 를 다시 읽는다. 예전에는 필드 기본값 5 로 셈해서, 씬을 다시 올리면(이어하기·죽은 뒤)
        // 부츠를 신고도 1배로 걸었다 — EquipUtility.Apply 는 MoveSpeed 가 이미 맞으면 플레이어를 건드리지 않는다.
        Speed = 0f;

        _keyInventory = GameObject.Find("KeyInventory");
        _weapon = GameObject.Find("WeaponSlot");
        _shield = GameObject.Find("ShieldSlot");
        _back = GameObject.Find("BackSlot");
        _weaponAnim = _weapon.GetComponent<Animator>();
        _shieldAnim = _shield.GetComponent<Animator>();

        if (PlayerPrefs.GetInt("ISFIRST", 1) != 1)
        {
            _back.SetActive(false);
        }
    }

    // 씬을 다시 올리면(죽음·다시 시작) 새 플레이어가 구독한다. 부서진 쪽을 남겨 두면 키를 누를 때마다
    // 그쪽이 먼저 불려 MissingReference 로 뒤따르는 구독까지 끊겼다 — 다시 올린 뒤 첫 키가 먹지 않았다.
    void OnDestroy()
    {
        FightGate.Remove(RefuseStrayFight);
        // 플레이를 끄는 중이면 매니저가 먼저 부서져 있다. 그때 부르면 @Managers 를 새로 만들어
        // 다음 플레이의 첫 씬 전환이 죽었다(스모크 P1-2).
        if (Managers.IsAlive)
            Managers.Input.KeyAction -= OnKeyboard;
    }

    // 꺼졌다 켜진 애니메이터는 기본 상태로 돌아간다. 틀어 둔 것을 잊고 다시 튼다.
    void OnEnable()
    {
        _bodyPlayed = _weaponPlayed = _shieldPlayed = 0;
    }

    /// <summary>연출·대화·전투처럼 흐름이 캐릭터를 쥐고 있다. 걷던 방향도 버린다.</summary>
    static bool IsBusy()
    {
        GameManager g = Managers.Game;
        // 치명 수업이 전투를 붙들고 말하는 동안(StoryDirector.HoldsBattle)도 전투 중이다.
        return g.OnBattle || g.OnConversation || g.OnLever || g.OnFade || g.OnDirect || g.OnInteract
            || g.OnInputLock || g.IsPlayerDead || StoryDirector.HoldsBattle;
    }

    /// <summary>창이 떠서 잠깐 멈췄다 — 메뉴(시간이 멈춘다), 확인 창, 전투 직전 관문.
    /// 걷던 방향은 남겨 둔다. 창이 닫히면 걷던 쪽을 보고 선다.</summary>
    static bool IsHeld()
    {
        return Managers.UI.IsPaused || UI_ConfirmPopup.IsOpen || FightGate.Pending;
    }

    Stack<int> keyInputStack = new Stack<int>();

    void OnKeyboard()
    {
        if (IsBusy())
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

        // 창이 떠 있어도 누름·뗌은 위에서 쌓는다 — 걷지만 않는다. 예전에는 창 동안 통째로 건너뛰어
        // 창 전에 걷던 방향이 맨 위에 남았고, 메뉴에서 S 를 쥔 채 닫으면 S 가 아니라 오른쪽으로 걸었다.
        if (IsHeld())
            return;

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
        TickClickMove();   // 걸음을 냈으면 _isMoving 이 서 있어 아래에서 서는 그림으로 바꾸지 않는다

        if (IsBusy() || IsHeld())
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
        if (_weapon.activeSelf != _isEquiptWeapon)
            _weapon.SetActive(_isEquiptWeapon);
        if (_weapon.activeInHierarchy == false)
            _weaponPlayed = 0;   // 꺼지면 애니메이터가 처음으로 돌아간다 — 켜질 때 다시 튼다
    }

    void CheckShield()
    {
        if (Managers.Game.PlayerData.CurShield == Define.NOT_EQUIP)
            _isEquiptShield = false;
        if (_shield.activeSelf != _isEquiptShield)
            _shield.SetActive(_isEquiptShield);
        if (_shield.activeInHierarchy == false)
            _shieldPlayed = 0;
    }

    // 마지막으로 튼 상태. 예전에는 매 프레임 GetComponent 두세 번에 상태 이름을 문자열로 새로 짓고
    // Play 를 불렀다. 이제 상태·장비가 바뀔 때만 이름을 짓고 튼다. 0 은 "아직 안 틀었다".
    int _bodyPlayed;
    int _weaponPlayed;
    int _shieldPlayed;

    void PlayAnimation()
    {
        CheckWeapon();
        CheckShield();

        string body = null;
        string gear = null;         // 무기·방패 상태 이름의 뒤쪽 ("_Idle_B" …). null 이면 장비를 틀지 않는다
        bool followMove = false;    // 걷는 속도에 맞춰 돈다
        bool keepSpeed = false;     // 속도를 건드리지 않는다
        float weaponZ = 0f;         // 몸 앞(-)·뒤(+)
        float shieldZ = 0f;

        switch (_state)
        {
            case PlayerState.IdleBack: body = "Player_Idle_B"; gear = "_Idle_B"; weaponZ = 1f; shieldZ = 1f; break;
            case PlayerState.IdleFront: body = "Player_Idle_F"; gear = "_Idle_F"; weaponZ = -1f; shieldZ = -1f; break;
            case PlayerState.IdleLeft: body = "Player_Idle_L"; gear = "_Idle_L"; followMove = true; weaponZ = 1f; shieldZ = -1f; break;
            case PlayerState.IdleRight: body = "Player_Idle_R"; gear = "_Idle_R"; followMove = true; weaponZ = -1f; shieldZ = 1f; break;
            case PlayerState.Left: body = "Player_Run_L"; gear = "_Run_L"; followMove = true; weaponZ = 1f; shieldZ = -1f; break;
            case PlayerState.Right: body = "Player_Run_R"; gear = "_Run_R"; followMove = true; weaponZ = -1f; shieldZ = 1f; break;
            case PlayerState.Up: body = "Player_Run_B"; gear = "_Run_B"; followMove = true; weaponZ = 1f; shieldZ = 1f; break;
            case PlayerState.Down: body = "Player_Run_F"; gear = "_Run_F"; followMove = true; weaponZ = -1f; shieldZ = -1f; break;
            case PlayerState.BackStep: body = "Player_BackStep"; gear = "_Run_F"; weaponZ = -1f; shieldZ = -1f; break;
            case PlayerState.OnLever: body = "Player_IronLever_B"; _isEquiptShield = false; _isEquiptWeapon = false; break;
            case PlayerState.DrawSword: body = "Player_SwordDraw_B"; _isEquiptShield = false; _isEquiptWeapon = false; break;
            case PlayerState.ContractSword: body = "Player_ContractSword_F"; _isEquiptShield = false; _isEquiptWeapon = false; break;
            case PlayerState.Death: body = "Player_Death"; keepSpeed = true; break;
            case PlayerState.TutorialFirst_Ready: body = "Player_TutorialFirst_Ready"; keepSpeed = true; break;
        }
        if (body == null)
            return;

        // 두 번 눌러 빨리 걸을 때(클릭 이동)는 발도 그만큼 빨리 — 안 그러면 미끄러져 보인다. 칼·방패도 같은 빠르기로
        // 돈다: 클립 길이가 몸과 같아(0.52초) 같이 돌아야 맞는다. 예전에는 몸만 따라가서 부츠를 신으면 칼이 뒤처졌다.
        float speed = followMove ? Managers.Game.PlayerData.MoveSpeed * (_walking && _walkFast ? FastWalk : 1f) : 1f;
        if (keepSpeed == false)
            _body.speed = speed;

        int bodyKey = (int)_state + 1;
        if (_bodyPlayed != bodyKey)
        {
            _bodyPlayed = bodyKey;
            _body.Play(body);
        }

        if (gear == null)
            return;

        // 같은 상태라도 칼·방패를 바꾸면 다른 클립이다. 장비 id 를 같이 센다.
        if (_isEquiptWeapon)
        {
            _weaponAnim.speed = speed;
            int sword = Managers.Game.PlayerData.CurSword;
            int key = bodyKey << 16 | sword;
            if (_weaponPlayed != key)
            {
                _weaponPlayed = key;
                _weaponAnim.Play(Managers.Data.EquipDic[sword].ImageName + gear);
            }
        }
        if (_isEquiptShield)
        {
            _shieldAnim.speed = speed;
            int shield = Managers.Game.PlayerData.CurShield;
            int key = bodyKey << 16 | shield;
            if (_shieldPlayed != key)
            {
                _shieldPlayed = key;
                _shieldAnim.Play(Managers.Data.EquipDic[shield].ImageName + gear);
            }
        }

        _weapon.transform.localPosition = Vector3.forward * (adjustingDis * weaponZ);
        _shield.transform.localPosition = Vector3.forward * (adjustingDis * shieldZ);
    }

    public void ResetWeaponAndShieldAnimation()
    {
        if (_isEquiptWeapon)
        {
            switch (_state)
            {
                case PlayerState.IdleFront:
                    _weaponAnim.Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Idle_F", 0, 0.0f);
                    break;
                case PlayerState.IdleLeft:
                    _weaponAnim.Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Idle_L", 0, 0.0f);
                    break;
                case PlayerState.IdleRight:
                    _weaponAnim.Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName}_Idle_R", 0, 0.0f);
                    break;
            }

        }

        // 방패 상태를 무기 애니메이터에 틀고 있었다(없는 상태라 경고만 나고 방패는 맞춰지지 않았다).
        if (_isEquiptShield)
        {
            switch (_state)
            {
                case PlayerState.IdleFront:
                    _shieldAnim.Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Idle_F", 0, 0.0f);
                    break;
                case PlayerState.IdleLeft:
                    _shieldAnim.Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Idle_L", 0, 0.0f);
                    break;
                case PlayerState.IdleRight:
                    _shieldAnim.Play($"{Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName}_Idle_R", 0, 0.0f);
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
    /// <param name="durationScale">한 칸 걷는 시간의 배수. 두 번 눌러 걷는 클릭 이동만 줄인다 — 나머지(키보드·봇·연출)는 1.</param>
    public void Moving(Define.MoveDir moveDir, bool isDirecting, float durationScale = 1f)
    {
        // 흐름이 캐릭터를 쥐고 있으면 걷지도 싸우지도 않는다. 키 입력은 OnKeyboard 가 먼저 거르지만 봇은 여기를
        // 바로 부른다 — 예전에 치명 수업이 OnBattle 을 내린 프레임에 봇이 싸우던 몬스터를 다시 밀어 전투창이 둘 떴다.
        if (isDirecting == false && IsBusy())
            return;

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
        transform.DOMove(_cellPos, _duration * durationScale).SetEase(Ease.Linear).OnComplete(() =>
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
                    StartFight(mc);
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
                StartFight(mc);
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

    /// <summary>
    /// 몬스터와 부딪혔다. 전투는 관문(FightGate)을 거쳐 연다 — 보스 등장 연출이나 "이 싸움은 죽는다"
    /// 확인 창이 그 사이에 끼어든다. 관문이 처리하는 동안(Pending) 캐릭터는 움직이지 않는다.
    /// </summary>
    void StartFight(MonsterController mc)
    {
        // 관문이 창을 띄우는 동안 뛰는 그림으로 서 있지 않게 먼저 선다. (SetMonster 도 같은 일을 한다)
        SetIdleState(_moveDir);
        FightGate.Request(mc, mc.SetMonster);
    }

    #region 클릭 이동
    // 바닥을 누르면 그 칸까지, 몬스터·문·계단·아이템을 누르면 그 옆 칸까지 걸어가 한 번 부딪힌다(매직 타워의
    // "双击快速移动" — 스팀 덱은 오른쪽 트랙패드가 마우스다). 걸음은 키보드와 같은 Moving 한 번씩이라 줍기·트리거·
    // 문·계단 규칙이 그대로 돌고, 싸움은 FightGate 를 거친다(지는 싸움 확인이 그대로 뜬다).
    // 길은 봇과 같은 격자(PathMover)로 걸음마다 다시 찾는다 — 몬스터·문·계단·아이템 칸은 지나가지 않는다.
    // 두 번 누르면 빨리 걷는다. 아무 키·새 클릭·창·대화·전투·연출·입력 잠금이 멈춘다.
    const float DoubleClickTime = 0.35f;
    const float DoubleClickPx = 8f;     // 두 번 누름으로 치는 화면 거리(픽셀)
    const float FastWalk = 3f;          // 두 번 누르면 한 칸 걷는 시간을 이만큼 나눈다
    // 누른 칸 앞으로 몇 칸까지 선 것의 그림을 보나(Aim). 가장 큰 그림은 100층 보스(검은 태양, 프레임 86x71)를 덩치 2 로
    // 세운 것이라 카메라 50도에서 뒤로 5.8칸을 가린다(0.71 x 2 / sin 50). 그림이 더 커지면 같이 늘린다.
    const int FrontCells = 6;

    // 누르면 부딪히는 것. 벽은 없다 — 벽을 누르면 아무 일도 없다.
    static readonly int ClickMask = (1 << (int)Define.Layer.Monster) | (1 << (int)Define.Layer.CItem)
                                  | (1 << (int)Define.Layer.EItem) | (1 << (int)Define.Layer.Door)
                                  | (1 << (int)Define.Layer.Portal) | (1 << (int)Define.Layer.Lever)
                                  | (1 << (int)Define.Layer.InteractObjects) | (1 << (int)Define.Layer.BossDoor);

    // 문은 늘 벽으로 본다 — 지나가다 열쇠를 쓰면 안 된다.
    readonly PathMover _path = new PathMover { UseKeys = false };
    readonly List<Vector2Int> _walkTo = new List<Vector2Int>();   // 걸어 들어갈 칸 하나, 또는 부딪힐 것이 덮은 칸들
    readonly Collider[] _probe = new Collider[8];
    GameObject _walkMap;
    bool _walking;
    bool _walkBump;         // 누른 것이 물건이다 — 옆에 서서 부딪힌다
    bool _walkFromSouth;    // 보스문·상호작용 물체는 아래에서 밀어야 반응한다(isObstacled 의 방향 검사)
    bool _walkFast;
    bool _stepping;         // 클릭 이동의 중간 걸음 — 이때 난 싸움은 누른 것이 아니다
    Vector3 _clickPos;      // 지난 누름의 화면 자리와 시각
    float _clickAt = -1f;

    void TickClickMove()
    {
        // 봇이 도는 동안에는 손대지 않는다 — 떠도는 클릭 하나가 봇의 걸음을 가로채면 안 된다.
        if (GameEvents.IsAutoPlaying)
        {
            _walking = false;
            return;
        }

        // 키든 마우스 단추든 새로 누르면 걷던 것을 멈춘다. 맵을 누른 것이면 그리로 다시 간다.
        if (Input.anyKeyDown)
        {
            bool was = _walking;
            _walking = false;
            if (Input.GetMouseButtonDown(0))
                OnMapClick(was);
        }

        if (_walking)
            WalkStep();
    }

    /// <summary>클릭으로 걸어도 되는가. 흐름이 캐릭터를 쥐었거나(전투·대화·연출·문·계단·레버·입력 잠금), 창이 맵을
    /// 덮었거나(층·보스 이름과 인벤토리는 괜찮다 — UI_GameScene.CanShowTooltip), 키보드로 걷는 중이면 아니다.</summary>
    static bool CanClickWalk()
    {
        return IsBusy() == false && IsHeld() == false && UI_GameScene.CanShowTooltip()
               && Input.GetKey(KeyCode.W) == false && Input.GetKey(KeyCode.A) == false
               && Input.GetKey(KeyCode.S) == false && Input.GetKey(KeyCode.D) == false;
    }

    /// <param name="wasWalking">이 누름이 멈추기 전에 걷고 있었나.</param>
    void OnMapClick(bool wasWalking)
    {
        // 두 번 누름은 칸이 아니라 화면에서 잰다. 카메라가 감쇠 없이 플레이어를 따라가서, 같은 자리를 눌러도 두 번째는
        // 그새 걸은 만큼(0.6~1.3칸) 앞 칸에 닿는다 — 칸으로 재면 빨리 걷기는 거의 안 걸리고, 두 번째 누름이 그 앞의
        // 문·계단·물약·몬스터를 새 목표로 잡았다. 그래서 두 번째 누름은 목표를 고르지 않는다: 걷던 것을 빠르게 할 뿐이고,
        // 다 걸었으면(또는 첫 누름이 대사를 넘겼으면) 아무 일도 없다. 첫 누름에 이미 걷기 시작했으니 기다리지 않는다.
        Vector3 at = Input.mousePosition;
        bool twice = Time.unscaledTime - _clickAt <= DoubleClickTime
                     && (at - _clickPos).sqrMagnitude <= DoubleClickPx * DoubleClickPx;
        _clickPos = at;
        _clickAt = Time.unscaledTime;

        // 창 위를 눌렀으면 창의 일이다. 이번 프레임에 창이 닫혔으면 그 클릭은 창(마지막 대사)을 넘기는 데 쓰였다.
        if (CanClickWalk() == false || Managers.UI.ClosedThisFrame
            || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            return;
        if (twice)
        {
            if (wasWalking)
            {
                _walking = true;
                _walkFast = true;
            }
            return;
        }

        Camera cam = Camera.main;
        GameObject map = MapUnder(_cellPos);
        if (cam == null || map == null)
            return;

        Ray ray = cam.ScreenPointToRay(at);
        // 자기 그림을 눌렀다 — 서기만 한다(걷던 것은 위에서 멈췄다). 그림이 한 칸 반쯤 서 있어서 그대로 두면 광선이
        // 1~2칸 뒤 바닥에 닿아 그리로 걷거나 거기 선 것과 부딪혔다.
        if (OnSelf(ray))
            return;

        Vector2Int cell;
        if (ClickedCell(ray, map, out cell) == false)
            return;

        FloodFromHere(map);
        if (Aim(ray, map, cell) == false)
            return;
        _walkMap = map;
        _walkFast = false;
        _walking = true;
    }

    /// <summary>
    /// 광선이 플레이어 몸 그림을 지나는가. 그려진 곳만 잰다 — 스프라이트 bounds 는 128x128 칸 전체라(세로 늘림까지
    /// 1.28x2.56) 옆 두 칸·위 네 칸을 덮어서, 그 안의 바닥·몬스터·계단을 눌러도 "나를 눌렀다" 로 섰다. 촘촘한(Tight)
    /// 메시의 꼭짓점이 그림을 감싼다(ForecastOverlay.HeadOf 와 같은 셈). 서 있는 스프라이트는 두께가 0 이라 앞뒤로 조금 불린다.
    /// </summary>
    bool OnSelf(Ray ray)
    {
        SpriteRenderer body = GetComponent<SpriteRenderer>();
        Vector2[] drawn = body != null && body.enabled && body.sprite != null ? body.sprite.vertices : null;
        if (drawn == null || drawn.Length == 0)
            return false;
        Bounds b = new Bounds(transform.TransformPoint(drawn[0]), Vector3.zero);
        foreach (Vector2 v in drawn)
            b.Encapsulate(transform.TransformPoint(v));
        b.Expand(new Vector3(0f, 0f, 0.02f));
        return b.IntersectRay(ray);
    }

    /// <summary>누른 칸 — 발밑 높이의 바닥에서 잰다. 벽을 눌렀으면 그 벽 칸이다(벽 너머 바닥은 벽에 가려 안 보였다).</summary>
    bool ClickedCell(Ray ray, GameObject map, out Vector2Int cell)
    {
        cell = default;
        float enter;
        if (new Plane(Vector3.up, _cellPos).Raycast(ray, out enter) == false)
            return false;
        RaycastHit wall;
        Vector3 point = Physics.Raycast(ray, out wall, enter, PathMover.WallMask, QueryTriggerInteraction.Collide)
            ? wall.point + ray.direction * (PathMover.Tile * 0.1f)   // 겉면에서 살짝 안으로
            : ray.GetPoint(enter);
        cell = PathMover.Cell(map, point);
        return true;
    }

    /// <summary>
    /// 누른 것을 고른다. 카메라가 비스듬히 내려다봐서 서 있는 그림(몬스터·아이템·문)의 몸통을 누르면 광선은 그 뒤 칸
    /// 바닥에 닿는다 — 보통 몬스터는 몸통 절반 넘게, 보스는 여섯 칸 뒤까지. 그래서 누른 칸 앞(카메라 쪽)에 선 것부터,
    /// 광선이 그 그림을 지나면 그것을 누른 것이다. 아무것도 안 가렸으면 누른 칸 자체다: 걸어서 갈 수 있는 바닥이면
    /// 거기까지만 간다(싸움도 줍기도 없다), 무엇이 서 있으면 그것.
    /// </summary>
    bool Aim(Ray ray, GameObject map, Vector2Int cell)
    {
        _walkTo.Clear();
        for (int k = FrontCells; k >= 0; k--)
        {
            Vector2Int c = cell + new Vector2Int(0, -k);
            if (k == 0 && _path.Dist.ContainsKey(c))
            {
                _walkBump = false;
                _walkTo.Add(c);
                return true;
            }
            int n = Physics.OverlapBoxNonAlloc(_path.CellCenter(c), PathMover.ProbeHalf, _probe, Quaternion.identity,
                                               ClickMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                Collider col = _probe[i];
                // 광선이 그 그림을 지나야 누른 것이다 — 아니면 그 위나 옆으로 지나가 뒤를 누른 것이다. 누른 칸에서는 제 칸에
                // 선 것도 된다(그림 앞 바닥을 눌렀다). 옆 칸에서 넘어온 콜라이더는 안 된다: 물약 콜라이더는 북쪽으로 0.1
                // 밀려 있어(ConsumableItem.prefab) 윗칸 탐침에도 걸리는데, 그 빈 칸을 누른 것을 물약으로 잡아 마셔 버렸다.
                if (Seen(col, ray, c) == false && (k > 0 || PathMover.Cell(map, col.bounds.center) != c))
                    continue;

                _walkBump = true;
                _walkFromSouth = col.gameObject.layer == (int)Define.Layer.BossDoor
                                 || col.gameObject.layer == (int)Define.Layer.InteractObjects;
                // 그것이 덮은 칸 전부 — 콜라이더가 여러 칸을 덮는 보스가 있다(AutoPlayer.BumpWide).
                // Covers 가 _probe 를 다시 채우므로 이 고리는 여기서 끝낸다.
                for (int x = -2; x <= 2; x++)
                    for (int y = -2; y <= 2; y++)
                        if (Covers(col, c + new Vector2Int(x, y)))
                            _walkTo.Add(c + new Vector2Int(x, y));
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 광선이 그 칸에 선 것의 그림을 지나는가. 스프라이트는 촘촘한(Tight) 메시라 bounds 가 그려진 곳만 두른다 —
    /// SitOnFloor 가 그림 아래끝을 같은 값으로 잰다. 그래도 네 귀퉁이는 비어 있어서 그 칸 너비로 좁힌다: 머리 옆으로
    /// 보이는 뒤 칸 바닥을 누른 것을 몬스터로 잡으면 원치 않는 싸움이 난다. 몬스터 콜라이더는 16칸 높이라 쓰면 안 된다.
    /// 그림은 콜라이더와 같은 오브젝트나 그 밑, 아니면 바로 위에 있다(보스 프리팹은 콜라이더가 자식이다).
    /// 그래도 없으면(문·계단의 모델) 콜라이더로 잰다 — 제 칸에 제 모양대로 놓여 있다.
    /// </summary>
    bool Seen(Collider col, Ray ray, Vector2Int cell)
    {
        Renderer[] drawings = col.GetComponentsInChildren<Renderer>();
        if (drawings.Length == 0 && col.transform.parent != null)
            drawings = col.transform.parent.GetComponents<Renderer>();

        float x = _path.CellCenter(cell).x;
        bool drawn = false;
        foreach (Renderer r in drawings)
        {
            if (r.enabled == false || (r is SpriteRenderer == false && r is MeshRenderer == false))
                continue;
            drawn = true;
            Bounds b = r.bounds;
            float x0 = Mathf.Max(b.min.x, x - PathMover.Tile * 0.5f);
            float x1 = Mathf.Min(b.max.x, x + PathMover.Tile * 0.5f);
            if (x0 >= x1)
                continue;
            // 서 있는 스프라이트는 두께가 0 이다 — 앞뒤로 조금 불린다.
            b.SetMinMax(new Vector3(x0, b.min.y, b.min.z - 0.01f), new Vector3(x1, b.max.y, b.max.z + 0.01f));
            if (b.IntersectRay(ray))
                return true;
        }
        return drawn == false && col.bounds.IntersectRay(ray);
    }

    /// <summary>그 칸을 잴 때(PathMover 의 탐침) 이 콜라이더가 걸리는가.</summary>
    bool Covers(Collider col, Vector2Int cell)
    {
        int n = Physics.OverlapBoxNonAlloc(_path.CellCenter(cell), PathMover.ProbeHalf, _probe, Quaternion.identity,
                                           1 << col.gameObject.layer, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
            if (_probe[i] == col)
                return true;
        return false;
    }

    Vector2Int FloodFromHere(GameObject map)
    {
        _path.Begin(map, _cellPos.y + PathMover.Tile * 0.5f);   // 봇과 같은 키높이
        Vector2Int at = PathMover.Cell(map, _cellPos);
        _path.Flood(at);
        return at;
    }

    /// <summary>
    /// 서 있는 층의 맵. 게임이 "지금 몇 층" 으로 쓰는 맵이 플레이어를 품으면 그것, 아니면 가장 가까운 것 —
    /// 층들은 100 유닛씩 떨어져 놓이고 한 장은 7 유닛 남짓이다(AutoPlayer.ResolveMap 과 같은 셈).
    /// 보스방으로 넘어간 직후에는 CurStageid 의 맵 위가 아닐 수 있다.
    /// </summary>
    GameObject MapUnder(Vector3 p)
    {
        GameManager g = Managers.Game;
        GameObject here;
        Bounds b;
        if (g.Maps.TryGetValue(g.PlayerData.CurStageid, out here) && here != null
            && _path.TryWallBounds(here, out b) && PathMover.InsideXZ(b, p))
            return here;

        GameObject nearest = null;
        float best = float.MaxValue;
        foreach (GameObject map in g.Maps.Values)
        {
            if (map == null)
                continue;
            float d = (map.transform.position - p).sqrMagnitude;
            if (d < best)
            {
                best = d;
                nearest = map;
            }
        }
        return nearest;
    }

    /// <summary>한 칸. 걸음마다 길을 다시 찾는다(봇과 같다) — 문이 열리거나 무엇이 사라져도 그 판 그대로 간다.</summary>
    void WalkStep()
    {
        if (CanClickWalk() == false || _walkMap == null)
        {
            _walking = false;
            return;
        }
        if (_isMoving)
            return;

        Vector2Int at = FloodFromHere(_walkMap);
        Vector2Int goal = at;
        Vector2Int into = at;
        int best = int.MaxValue;
        int d;
        if (_walkBump == false)
        {
            if (_path.Dist.TryGetValue(_walkTo[0], out d))
            {
                best = d;
                goal = _walkTo[0];
            }
        }
        else
        {
            // 가장 가까운, 옆에 설 자리. 보스문·상호작용 물체는 바로 아래 칸만 된다.
            foreach (Vector2Int t in _walkTo)
            {
                foreach (Vector2Int side in PathMover.Dirs)
                {
                    if (_walkFromSouth && side != Vector2Int.down)
                        continue;
                    Vector2Int stand = t + side;
                    if (_walkTo.Contains(stand) || _path.Dist.TryGetValue(stand, out d) == false || d >= best)
                        continue;
                    best = d;
                    goal = stand;
                    into = t;
                }
            }
        }
        if (best == int.MaxValue)
        {
            _walking = false;   // 길이 끊겼다
            return;
        }

        bool bump = best == 0;
        if (bump && _walkBump == false)
        {
            _walking = false;   // 다 왔다
            return;
        }
        MoveDir dir = bump ? PathMover.ToDir(into - at) : _path.FirstStep(at, goal);
        if (dir == MoveDir.None)
        {
            _walking = false;
            return;
        }

        _moveDir = dir;
        _stepping = bump == false;
        Moving(dir, false, _walkFast ? 1f / FastWalk : 1f);
        _stepping = false;
        // 부딪혔으면 여기까지다. 중간 걸음이 막혔으면(발이 안 떨어졌다) 같은 곳을 다시 밀지 않는다.
        if (bump || _isMoving == false)
            _walking = false;
    }

    /// <summary>
    /// 클릭 이동의 중간 걸음이 몬스터를 건드렸다 — 누른 것은 그놈이 아니다. 싸우지 않고 멈춘다.
    /// 길은 몬스터 칸을 피하지만 부딪힘 검사(광선 1.3칸·칸 상자)가 탐침보다 넓어서, 콜라이더가 칸보다 큰
    /// 손수 만든 층의 몬스터는 옆을 지나다 걸릴 수 있다. 관문 맨 앞이라 이야기 연출·지는 싸움 확인보다 먼저 거른다.
    /// </summary>
    bool RefuseStrayFight(MonsterController monster, System.Action proceed)
    {
        if (_stepping == false)
            return false;
        _walking = false;
        FightGate.Cancel();
        return true;
    }
    #endregion

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
