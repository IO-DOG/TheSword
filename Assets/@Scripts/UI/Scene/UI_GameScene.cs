using Cinemachine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UI_GameScene : UI_Scene
{
    #region Enum
    //enum Buttons
    //{
    //    //ToTitleButton,
    //    PlayConversation,
    //}

    enum GameObjects
    {
        KeyInventory,
        GreenKey,
        YellowKey,
        RedKey,
    }

    enum Texts
    {
        //PlayerNameText,
        PlayerHPText,
        PlayerAttackText,
        PlayerDefenseText,
        PlayerLevelText,
        MainUIMapNameText,
    }

    // MainUIAuxiliaryHPGaugeImage 는 뺐다 — 쓰는 곳이 없는데(HP 는 PlayerHPBarGauge 가 그린다) 묶이지도 않아
    // 씬마다 "Failed to bind" 만 남겼다.
    enum Images
    {
        MainUIEXPGaugeImage,
        MainUIOptionAImage,
        MainUIOptionBImage,
        MainUIInventoryAImage,
        MainUIInventoryBImage,
        MainUISwordAImage,
        MainUISwordBImage,
        MainUIWarpAImage,
        MainUIWarpBImage,
        MainUIStatusHPImage,
        LoadingIllustImage,
    }

    #endregion

    /// <summary>메뉴가 떠 있다. 스택에서 읽는다 — 따로 적어 두던 플래그는 메뉴가 다른 길로 닫히면 켜진 채 남았다.</summary>
    public bool isOpenMenuPopup => Managers.UI.FindPopup<UI_MenuPopup>() != null;
    public bool isOpenInfoPopup = false;

    // 플레이 시간. 매 프레임 PlayerPrefs(윈도우에서는 레지스트리)에 쓰던 것을 모아 두었다가 몇 초에 한 번만 쓴다.
    // 체크포인트(SaveGame)는 PlayerPrefs 의 값을 읽으므로 최대 PlayTimeFlush 초 늦게 적힐 수 있다.
    const float PlayTimeFlush = 5f;
    float _playTime;
    float _playTimeFlushed;

    // 전투 예측(마검의 눈) — 계약 뒤에 M 도감·V 맵 위 숫자. 판마다 처음 한 번 그 두 키를 알려 준다
    // (새 게임이면 GameManager.DeleteGameData 가 지운다 — 다른 ISFIRST* 안내와 같다).
    public const string KeysHintPref = "HINT_FORECAST_KEYS";
    const float HintFade = 0.6f;
    bool _keysHintShown;
    TMP_Text _critText;             // 치명까지 N — 전투 사이에 이어지는 치명 횟수
    TMP_Text _guardText;            // 방패가 올라와 있다 — 다음 한 대를 막는다
    CanvasGroup _hint;
    TMP_Text _hintText;
    float _hintUntil;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        // 씬을 다시 올렸다(죽음·다시 시작). 전투 직전 관문이 중간에 끊겼으면 그대로 남아 있다.
        FightGate.Reset();
        _playTime = _playTimeFlushed = PlayerPrefs.GetFloat("PLAYTIME", 0);

        // 워프 창(Tab). 프리팹이 없어 코드로 세우는 창이라 여기서 한 번 붙여 둔다.
        WarpUI.Spawn();

        #region Bind
        //BindButton(typeof(Buttons));
        BindObject(typeof(GameObjects));
        BindText(typeof(Texts));
        BindImage(typeof(Images));
        #endregion

        Managers.UI.UI_GameScene = this;
        #region PointerEnter&PointerExit
        GetImage((int)Images.MainUIOptionAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIOptionBImage).gameObject.SetActive(true); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUIOptionAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIOptionBImage).gameObject.SetActive(false); }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.MainUIInventoryAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(true); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUIInventoryAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(false); }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.MainUISwordAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(true); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUISwordAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(false); }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.MainUIWarpAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(true); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUIWarpAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(false); }, null, Define.UIEvent.PointerExit);
        #endregion

        #region OffBImage
        GetImage((int)Images.MainUIOptionBImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(false);
        #endregion

        // 전투 예측(마검의 눈): 맵 위 숫자, 지는 싸움 앞의 확인, 도감(M·마검 단추), 치명·방패 표시.
        ForecastOverlay.Spawn(transform);
        FightGate.Add(FatalFightGuard.Ask, FatalFightGuard.Order);
        GetImage((int)Images.MainUISwordAImage).gameObject.BindEvent(UI_MonsterManualPopup.Toggle);
        BuildCombatState();
        _keysHintShown = PlayerPrefs.GetInt(KeysHintPref, 0) == 1;
        // 언어를 바꾸면 HUD 글자(층 이름·치명 표시)를 다시 칠한다.
        GameSettings.Changed += Refresh;

        Managers.Game.GenerateMap(Managers.Game.PlayerData.CurStageid);
        // 이동 속도·전투 배속은 낀 장비가 정한다(기준 1 에 부츠 배수). 예전에는 여기서 1 로 덮어쓰기만 해서
        // 부츠·목걸이 효과가 씬을 다시 올릴 때마다(이어하기·죽은 뒤) 사라졌다.
        EquipUtility.Apply();
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetupCameraConfiner();

        Managers.Sound.Play(Define.Sound.Effect, "MapTransition_SFX");

        //Managers.Game.PlayerData.CurSword = Define.EQUIP_SOWRD_FIRST;
        //Managers.Game.PlayerData.CurShield = 0;

        Managers.Game.Player._keyInventory = GetObject((int)GameObjects.KeyInventory);

        // 열쇠 칸은 프리팹에서 화면 가운데 기준(-429)으로 놓여 있어, 16:9 보다 좁은 화면(스팀 덱 1280x800)에서는
        // 왼쪽 밖으로 13px 나갔다. 왼쪽 가장자리 기준으로 옮긴다 — 기준 폭(1920)의 절반을 더하니 16:9 에서는 그대로다.
        RectTransform keySlots = GetObject((int)GameObjects.KeyInventory).transform as RectTransform;
        if (keySlots != null && keySlots.anchorMin.x == 0.5f)
        {
            keySlots.anchorMin = keySlots.anchorMax = new Vector2(0f, 0.5f);
            keySlots.anchoredPosition += new Vector2(960f, 0f);
        }

        if (PlayerPrefs.GetInt("ISOPENGREENKEY") == 0)
            GetObject((int)GameObjects.GreenKey).SetActive(false);
        if (PlayerPrefs.GetInt("ISOPENYELLOWKEY") == 0)
            GetObject((int)GameObjects.YellowKey).SetActive(false);
        if (PlayerPrefs.GetInt("ISOPENREDKEY") == 0)
            GetObject((int)GameObjects.RedKey).SetActive(false);

        // 인벤토리·워프·마검 단추는 Refresh 가 정한다(계약했는가, 반지를 꼈는가). 예전의 ISOPENWARPUI·ISOPENPORTAL·
        // ISOPENSWORD·ISOPENCLASSUI 는 1 로 켜 주는 곳이 없어서, 씬을 올릴 때마다 단추를 도로 숨겼다.

        GetImage((int)Images.MainUIOptionAImage).gameObject.BindEvent(() =>
        {
            UI_MenuPopup menu = Managers.UI.FindPopup<UI_MenuPopup>();
            if (menu != null)
                menu.OnClickContinueGameButton();
            else
                TryOpenMenu();
        });
        GetImage((int)Images.MainUIInventoryAImage).gameObject.BindEvent(OnClickMainUIInventoryAImage);

        // HUD 에 워프 버튼과 그림은 진작 있었는데 아무 데도 연결돼 있지 않았다.
        GetImage((int)Images.MainUIWarpAImage).gameObject.BindEvent(() =>
        {
            if (WarpUI.Instance != null)
                WarpUI.Instance.Open();
        });

        //GetButton((int)Buttons.PlayConversation).gameObject.BindEvent(() =>
        //{
        //    if (!Managers.Game.OnBattle)
        //    {
        //        Managers.UI.ShowPopupUI<UI_ConversationPopup>();
        //        Managers.Game.CurEventID = Define.EVENT_SWORD_FIRST;
        //    }
        //});

        Refresh();
        Data.MyVector3 loadPos = Managers.Game.PlayerData.CurPosition;

        // 새 게임은 CurPosition 이 (0, 1.5, 0) 이라 늘 스폰 포인트 쪽으로 온다.
        // 그런데 SpawnPoints 는 GenerateMap 이 태그로 긁어 담는 것이라 비어 있을 때가 있고,
        // 예전에는 여기서 IndexOutOfRange 가 나면서 Init 이 통째로 끊겼다 —
        // 플레이어는 원점에 남고 튜토리얼 연출까지 전부 건너뛰어졌다.
        // 실행마다 되기도 하고 안 되기도 한 것이 이것 때문이다.
        if (loadPos.X == 0 && loadPos.Z == 0)
        {
            if (Managers.Game.SpawnPoints != null && Managers.Game.SpawnPoints.Length > 0
                && Managers.Game.SpawnPoints[0] != null)
            {
                Managers.Game.Player.SetPlayerPosition(Managers.Game.SpawnPoints[0].position);
            }
            else
            {
                Debug.LogError("[GameScene] 스폰 포인트가 아직 없다 — 생길 때까지 기다렸다 옮긴다");
                StartCoroutine(CoPlaceAtSpawnWhenReady());
            }
        }
        else
        {
            Vector3 playerPos = new Vector3(loadPos.X, loadPos.Y, loadPos.Z);
            Managers.Game.Player.SetPlayerPosition(playerPos);
        }

        //#region Test
        //Managers.Game.Player.SetPlayerPosition(Managers.Game.SpawnPoints[1].position);
        //Managers.Game.PlayerData.CurStageid = 3;
        //Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetupCameraConfiner();
        //#endregion

        Managers.Game.OnFadeAction.Invoke(1f);


        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
            Managers.Directing.Events.CoPlayTutorial_1();
        else
        {
            Managers.UI.ShowStageNamePopup(1f);
            // 곡은 GenerateMap 이 챕터 곡·조로 이미 틀었다. 예전에는 여기서 Chapter0_BGM 을 조 1 로 다시 틀어
            // 이어하기·죽은 뒤마다 챕터 조를 덮었다. 소리 크기만 설정값으로 되돌린다.
            Managers.Sound.SetBGMVolume(SoundManager.ConfiguredBgmVolume);
        }

        RestoreMagicSwordKey();

        if (GameEvents.RespawnPending)
        {
            GameEvents.RespawnPending = false;
            GameEvents.RaiseRespawned();
        }
        return true;
    }

    /// <summary>
    /// 마검 계약 뒤 3층에 생기는 열쇠를 다시 세운다. 그 열쇠는 계약 연출이 켜는 것이라, 계약 뒤에
    /// 층을 다시 만들면(이어하기·죽은 뒤) 프리팹대로 꺼진 채 나온다. 보스를 만난 뒤에는 저장 지점이
    /// 그 너머라 세우지 않는다.
    ///
    /// 예전에는 이름으로 맵을 찾아 무조건 켰다. 챕터 1~4 에서는 그 맵이 없어 널참조로 Init 끝이
    /// 날아갔고(챕터 경계마다 ISMEETBOSS 가 0 이 된다), 이미 주운 열쇠도 다시 켜져서 불러올 때마다
    /// 열쇠가 하나씩 늘었다. 주웠는지는 계약 때 옮겨 둔 번호로 본다 — 프리팹의 13 은 2층 물약과 겹친다.
    /// </summary>
    void RestoreMagicSwordKey()
    {
        if (PlayerPrefs.GetInt("ISMEETSWORD") != 1 || PlayerPrefs.GetInt("ISMEETBOSS") != 0)
            return;

        Transform key = null;
        foreach (KeyValuePair<int, GameObject> pair in Managers.Game.Maps)
        {
            Data.StageInfoData info;
            if (pair.Value != null && Managers.Data.StageInfoDic.TryGetValue(pair.Key, out info) && info.DungeonID == "00_002")
            {
                key = pair.Value.transform.Find("Items/CItem13");
                break;
            }
        }
        if (key == null)
            return;   // 지금 챕터에는 그 층이 없다

        ConsumableItem item = key.GetComponent<ConsumableItem>();
        if (item != null)
            item._itemIndex_forActive = Define.MAGICAL_SWORD_KEY_INDEX;

        bool alive;
        if (Managers.Data.CItemActiveDic.TryGetValue(Define.MAGICAL_SWORD_KEY_INDEX, out alive) == false)
            Managers.Data.CItemActiveDic[Define.MAGICAL_SWORD_KEY_INDEX] = alive = true;   // 번호를 옮기기 전의 저장

        key.gameObject.SetActive(alive);
        if (alive == false)
            return;
        SpriteRenderer sr = key.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.enabled = true;
        BoxCollider col = key.GetComponent<BoxCollider>();
        if (col != null)
            col.enabled = true;
    }

    public void Refresh()
    {
        RefreshWarpButton();
        RefreshSwordButton();
        RefreshInventoryButton();
        GetText((int)Texts.MainUIMapNameText).text = Managers.GetString(Managers.Data.StageInfoDic[Managers.Game.PlayerData.CurStageid].DungeonNameScriptID);
        GetText((int)Texts.PlayerLevelText).text = Managers.Game.PlayerData.Level.ToString();
        int level = Managers.Game.PlayerData.Level;
        Managers.Game.PlayerData.Level = Mathf.Max(level, 1);
        level = Mathf.Max(level, 1);

        // 표의 끝 레벨에는 다음 레벨이 없다. 거기서 터지면 HUD 를 다시 칠했다는 알림까지 끊긴다.
        Data.PlayerData next;
        GetImage((int)Images.MainUIEXPGaugeImage).fillAmount = Managers.Data.PlayerDic.TryGetValue(level + 1, out next) && next.NeedExp > 0f
            ? Managers.Game.PlayerData.CurExp / next.NeedExp : 1f;
        float hpRatio = Managers.Game.PlayerData.CurHP / Managers.Game.PlayerData.MaxHP;
        GameObject hpGauge = GameObject.Find("PlayerHPBarGauge");   // 게임오버 연출 중에는 꺼져 있다
        if (hpGauge != null)
            hpGauge.GetComponent<Image>().fillAmount = hpRatio;
        Managers.Game.KeyInventory.ShowKeySlot(Managers.Game.Player._keyInventory);
        SetPlayerInfo();

        GameEvents.RaiseHudRefreshed();
    }

    int _mask = (1 << (int)Define.Layer.Monster | 1 << (int)Define.Layer.CItem);

    private void Update()
    {
        ShowInfo();

        // ESC 설정창
        OnClickESC();

        // M 도감 · V 맵 위 예측 숫자
        OnForecastKeys();

        // Timer
        StartTimer();

        // 치트 키는 에디터와 개발 빌드에서만. 출시 빌드에 F2(공격 +10000)가 그대로 살아 있었다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        #region for_test
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Managers.Game.PlayerData.CurExp += 10;
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Managers.Game.PlayerData.Attack += 10000;
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Managers.Game.PlayerData.CurHP -= 10000;
            Refresh();

        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Managers.Game.PlayerData.MaxHP += 10000;
            Refresh();
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {
            switch (Managers.Game.PlayerData.MoveSpeed)
            {
                case 1f:
                    Managers.Game.PlayerData.MoveSpeed = 1.5f;
                    Managers.Game.Player.Speed = Managers.Game.PlayerData.MoveSpeed * 5;
                    break;
                case 1.5f:
                    Managers.Game.PlayerData.MoveSpeed = 2f;
                    Managers.Game.Player.Speed = Managers.Game.PlayerData.MoveSpeed * 5;
                    break;
                case 2f:
                    Managers.Game.PlayerData.MoveSpeed = 1f;
                    Managers.Game.Player.Speed = Managers.Game.PlayerData.MoveSpeed * 5;
                    break;
            }

            Debug.Log($"Managers.Game.CurPlayerData.MoveSpeed : {Managers.Game.PlayerData.MoveSpeed}");
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            GameObject monsters = GameObject.Find("Monsters");
            if (monsters != null) monsters.gameObject.SetActive(false);
            GameObject pillars = GameObject.Find("Pillars");
            if (pillars != null) pillars.gameObject.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            if (Managers.Game.PlayerData.CurSword != Define.EQUIP_SOWRD_FIRST)
                Managers.Game.SwapEquip(Define.EQUIP_SOWRD_FIRST);
            else
                Managers.Game.SwapEquip(Define.EQUIP_SOWRD_FIRST + 1);

            Refresh();
            //Managers.Game.SaveGame();
        }
        if (Input.GetKeyDown(KeyCode.F8))
        {
            Debug.Log("move count : " + PlayerPrefs.GetInt("MOVECOUNT"));
            Debug.Log("death count : " + PlayerPrefs.GetInt("DEATHCOUNT"));
            Debug.Log("paly time : " + _playTime);
        }
        #endregion
#endif
    }

    /// <summary>
    /// 맵 위 툴팁(몬스터·아이템 정보)을 세워 둬도 되는가 — 맵 위 숫자와 같은 때에(ForecastOverlay.MapInView), 팝업 스택
    /// 밖의 워프 창이 맵을 가리지 않을 때. 툴팁은 이것이 거짓이 되면 스스로 닫힌다: 몬스터에 마우스를 올린 채 Esc 를
    /// 누르거나 싸우면 설명이 메뉴 단추 사이와 전투창 뒤에 남아 있었다. 덮개가 걷히면 ShowInfo 가 다시 띄운다.
    /// 인벤토리 위에서는 선다 — 오른쪽 판이라 맵 왼쪽이 보이고 그동안도 걷는다. 칼을 갈아 끼우며 비용을 견주는
    /// 자리인데 맵 위 숫자는 창 밑에서 숨으니, 그때 예측을 보는 곳은 툴팁뿐이다.
    /// ponytail: 판 위에서도 그 밑의 몬스터를 짚는다(커서도 돋보기가 된다) — 예전과 같다. 툴팁 캔버스(0)가 판(10~)
    /// 밑이라 판의 클릭은 막지 않는다. 거슬리면 판(Inventory_Frame) 안의 마우스를 뺀다.
    /// </summary>
    public static bool CanShowTooltip() =>
        ForecastOverlay.MapInView(besideInventory: true) && (WarpUI.Instance == null || WarpUI.Instance.IsOpen == false);

    void ShowInfo()
    {
        if (isOpenInfoPopup || CanShowTooltip() == false)
            return;

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool raycastHit = Physics.Raycast(ray, out hit, 100.0f, _mask);

        if (raycastHit)
        {
            if (hit.collider.gameObject.layer == (int)Define.Layer.Monster && Managers.Cursor._cursor == CursorType.Search)
            {
                // 콜라이더가 붙은 오브젝트에 컴포넌트가 없을 수 있다(생성된 층의
                // 몬스터가 그렇다). 문에서 겪은 것과 같은 문제라 같은 방식으로 찾는다.
                MonsterController monster = Util.Find<MonsterController>(hit.collider.gameObject);
                if (monster == null)
                    return;

                int id = monster.id;

                UI_MonsterInfo monsterInfo = Managers.UI.MakeSubItem<UI_MonsterInfo>(monster.transform);
                Vector3 monsterInfoPos = monsterInfo.gameObject.transform.position;

                isOpenInfoPopup = true;
                monsterInfo.Position = Util.ScreenToWorldCood(Input.mousePosition);

            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.CItem && Managers.Cursor._cursor == CursorType.Search)
            {
                ConsumableItem cItem = Util.Find<ConsumableItem>(hit.collider.gameObject);
                if (cItem == null)
                    return;

                UI_CItemInfo cItemInfo = Managers.UI.MakeSubItem<UI_CItemInfo>(cItem.transform);
                isOpenInfoPopup = true;
                cItemInfo.Position = Util.ScreenToWorldCood(Input.mousePosition);

            }
        }
    }

    /// <summary>
    /// 게임 화면의 Esc 는 여기 한 곳에서만 받는다. 맨 위 창에게 먼저 묻고(UI_Popup.OnEscape),
    /// 창이 쓰지 않으면 메뉴를 연다. 창을 대신 걷어내지 않는다 — 가이드·대화·보스방 확인·게임오버 창이
    /// 잠금 플래그를 켠 채 사라져 플레이어가 굳었던 것이 그 때문이다.
    /// </summary>
    void OnClickESC()
    {
        if (Input.GetKeyDown(KeyCode.Escape) == false)
            return;
        // 죽는 연출이 도는 중이다. 게임오버 창을 걷으면 되살리는 코루틴이 같이 죽는다. 그 위에 뜬 전투 안내
        // (첫 전투에서 죽었다)만은 다른 때처럼 Esc 로 닫는다 — 게임오버 창은 같은 프레임의 키를 받지 않는다.
        if (Managers.Game.IsPlayerDead)
        {
            if (Managers.UI.TopPopup is UI_GuidePopup)
                Managers.UI.EscapeTopPopup();
            return;
        }
        // 워프 창은 팝업 스택 밖의 창이다. 떠 있으면 그것부터 닫는다.
        if (WarpUI.Instance != null && WarpUI.Instance.IsOpen)
        {
            WarpUI.Instance.Close();
            return;
        }
        if (Managers.UI.EscapeTopPopup())
            return;
        TryOpenMenu();
    }

    /// <summary>
    /// 게임 화면 위에 새 창(도감·워프)을 열어도 되는가. 흐름이 캐릭터를 쥐고 있거나(전투·연출·대화·문·계단·
    /// 레버·입력 잠금·전투 직전 관문) 다른 창이 떠 있으면 안 된다 — 저절로 사라지는 층·보스 이름은 괜찮다.
    /// 메뉴(TryOpenMenu)와 달리 전투창 위에서는 열지 않는다.
    /// </summary>
    public static bool CanOpenPanel()
    {
        GameManager g = Managers.Game;
        if (g == null || g.Player == null || g.GameScene == null || g.IsPlayerDead || FightGate.Pending || Managers.UI.IsPaused)
            return false;
        if (g.OnBattle || g.OnDirect || g.OnConversation || g.OnFade || g.OnInteract || g.OnLever || g.OnInputLock)
            return false;
        UI_Popup top = Managers.UI.TopPopup;
        return top == null || top is UI_StageNamePopup || top is UI_BossNamePopup;
    }

    // M 은 도감을 열고 닫는다(UI_MonsterManualPopup 이 계약·열 수 있는가를 본다). V 는 맵 위 숫자를 끄고 켠다.
    // 계약 뒤 처음으로 손이 비면 두 키를 한 번 알려 준다 — 자동 플레이 중에는 알리지 않고 다음으로 미룬다.
    void OnForecastKeys()
    {
        if (Input.GetKeyDown(KeyCode.M))
            UI_MonsterManualPopup.Toggle();
        else if (Input.GetKeyDown(KeyCode.V) && Managers.Game.PlayerData.IsContractedSword && CanOpenPanel())
        {
            GameSettings.ShowForecast = !GameSettings.ShowForecast;
            ShowHint(Managers.GetString(GameSettings.ShowForecast ? ForecastUI.ForecastOn : ForecastUI.ForecastOff), 1.5f);
        }

        if (_keysHintShown == false && GameEvents.IsAutoPlaying == false
            && Managers.Game.PlayerData.IsContractedSword && CanOpenPanel() && Managers.UI.TopPopup == null)
        {
            _keysHintShown = true;
            PlayerPrefs.SetInt(KeysHintPref, 1);
            ShowHint(Managers.GetString(ForecastUI.HintKeys), 6f);
        }

        // 알림은 막지 않고 떠 있다가 스스로 옅어진다. 멈춘 시간(메뉴)에도 흐르게 실시간으로 잰다.
        if (_hint != null && _hint.gameObject.activeSelf)
        {
            float left = _hintUntil - Time.unscaledTime;
            _hint.alpha = Mathf.Clamp01(left / HintFade);
            if (left <= 0f)
                _hint.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 메뉴를 연다(시간이 멈춘다). 창이 떠 있으면 저절로 사라지는 창(전투창·층 이름·보스 이름) 위에서만
    /// 연다 — 가이드·대화·확인 창은 제 손으로 닫혀야 잠금이 풀린다. 연출·문·계단·레버처럼 흐름이
    /// 캐릭터를 쥐고 있을 때도 열지 않는다. 그 사이에 씬을 다시 올리면 도는 중이던 연출이 옛 씬을 만진다.
    /// </summary>
    public bool TryOpenMenu()
    {
        GameManager g = Managers.Game;
        if (isOpenMenuPopup || g.IsPlayerDead || FightGate.Pending)
            return false;
        if (g.OnDirect || g.OnConversation || g.OnFade || g.OnInteract || g.OnLever || g.OnInputLock)
            return false;

        UI_Popup top = Managers.UI.TopPopup;
        // 전투 중이면 전투창 위에서만. 전투창이 뜨기 전(화면을 찍는 한 프레임, 보스 등장 연출)에 열면
        // 전투창이 메뉴 위로 올라온다.
        if (g.OnBattle)
        {
            if ((top is UI_BattlePopup) == false)
                return false;
        }
        else if (top != null && (top is UI_StageNamePopup || top is UI_BossNamePopup) == false)
        {
            return false;
        }

        Managers.UI.ShowPopupUI<UI_MenuPopup>();
        return true;
    }

    // 창을 내리면(알트탭) 메뉴를 열어 멈춘다. runInBackground 라 그냥 두면 전투가 혼자 흘러갔다.
    // 에디터에서는 인스펙터를 누를 때마다 멈추면 일을 못 하고, 자동 플레이는 멈추면 안 된다.
    void OnApplicationFocus(bool focus)
    {
        // 앱을 끄는 중에도 포커스를 잃는다 — 매니저가 먼저 부서졌으면 건드리지 않는다.
        if (focus || Application.isEditor || GameEvents.IsAutoPlaying || Managers.IsAlive == false)
            return;
        TryOpenMenu();
    }

    /// <summary>
    /// �������� �÷��̾� ������ �����ϴ� �Լ�
    /// �÷��̾� ������ �߰��Ǹ� ���Լ� ���� �߰��Ǿ����.
    /// </summary>
    public void SetPlayerInfo()
    {
        //GetText((int)Texts.PlayerNameText).text = "PlayerName";
        GetText((int)Texts.PlayerHPText).text = $"{Managers.Game.PlayerData.CurHP} / {Managers.Game.PlayerData.MaxHP}";
        GetText((int)Texts.PlayerAttackText).text = $"{Managers.Game.PlayerData.Attack}";
        GetText((int)Texts.PlayerDefenseText).text = $"{Managers.Game.PlayerData.Defence}";

        float textWidth = GetText((int)Texts.PlayerHPText).preferredWidth;

        // 이미지의 RectTransform 가져오기
        RectTransform imageRect = GetImage((int)Images.MainUIStatusHPImage).GetComponent<RectTransform>();
        // 이미지의 가로 크기를 텍스트 너비 + 여백으로 설정
        imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth + 30f);

        RefreshCombatState();
    }

    /// <summary>
    /// 공격·방어 칸 위에 한 줄씩: "치명까지 N" 과 "방패 준비". 둘 다 전투가 끝나도 이어져서 다음 싸움의 값을
    /// 바꾸는데(BattleForecast), 예전에는 어디에도 보이지 않았다. 프리팹을 건드리지 않고 칸 옆에 글자만 세운다.
    /// </summary>
    void BuildCombatState()
    {
        _critText = CombatLabel(GetText((int)Texts.PlayerAttackText), "CritCountText");
        _guardText = CombatLabel(GetText((int)Texts.PlayerDefenseText), "GuardReadyText");
        _guardText.color = new Color32(140, 205, 255, 255);
    }

    // 능력치 칸(왼쪽 아래 기준, 2배 그림) 바로 위, 칸 폭 안에 맞춘다 — 옆 칸 위의 글자와 겹치지 않게.
    // 메뉴가 PlayerInfo 를 가리면 같이 가려진다.
    static TMP_Text CombatLabel(TMP_Text value, string name)
    {
        RectTransform box = (RectTransform)value.transform.parent;
        Vector2 size = new Vector2(box.rect.width * box.localScale.x, box.rect.height * box.localScale.y);
        TextMeshProUGUI label = CodeUI.NewText(box.parent, name, CodeUI.NumberFont, 20f, Color.white, TextAlignmentOptions.BottomLeft);
        label.fontSharedMaterial = CodeUI.Outlined(label.font, 0.25f);
        CodeUI.Place(label.rectTransform, Vector2.zero, Vector2.zero,
            box.anchoredPosition + new Vector2(2f, size.y + 2f), new Vector2(size.x, 28f));
        return CodeUI.Fit(label, 12f);
    }

    void RefreshCombatState()
    {
        if (_critText == null)
            return;

        // 둘 다 마검의 눈이 보는 값이다 — 계약 전에는 다른 예측(맵 위 숫자·도감)처럼 숨긴다. 치명 이야기도 계약 뒤의 장면이다.
        bool eye = Managers.Game.PlayerData.IsContractedSword;

        // 1 이면 다음 한 대가 치명타다. 암살(치명만 맞는다)·불사(치명이 아니면 20%)를 앞두고 보는 숫자다.
        int crit = eye ? ForecastUI.HitsToCrit() : 0;
        _critText.gameObject.SetActive(crit > 0);
        if (crit > 0)
        {
            _critText.text = string.Format(Managers.GetString(ForecastUI.CritIn), crit);
            _critText.color = crit == 1 ? new Color32(255, 216, 74, 255) : new Color32(230, 230, 236, 255);
        }

        bool guard = eye && Managers.Game.PlayerData.IsDefence;
        _guardText.gameObject.SetActive(guard);
        if (guard)
            _guardText.text = Managers.GetString(ForecastUI.GuardUp);
    }

    /// <summary>
    /// 막지 않는 알림 한 줄. 화면 위쪽 가운데에 잠깐 떠 있다가 옅어진다(OnForecastKeys 가 옅게 한다).
    /// 틀은 인벤토리 칸 그림을 빌린다 — 없으면 반투명 검정.
    /// </summary>
    void ShowHint(string message, float seconds)
    {
        if (_hint == null)
        {
            Sprite frame = CodeUI.PrefabSprite("UI_InvenPopup", "Inventory_Popup32");
            Image box = CodeUI.NewImage(transform, "ForecastHint", frame, frame != null ? Color.white : new Color(0f, 0f, 0f, 0.7f), true);
            CodeUI.Place(box.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(700f, 64f));
            _hint = box.gameObject.AddComponent<CanvasGroup>();
            _hint.blocksRaycasts = false;
            _hintText = CodeUI.Fit(CodeUI.NewText(box.transform, "Text", CodeUI.ProseFont, 30f, new Color32(240, 210, 138, 255),
                TextAlignmentOptions.Center), 16f);
            CodeUI.Stretch(_hintText.rectTransform);
        }

        _hintText.text = message;
        // 글 길이에 맞춰 틀을 줄인다 (자동 축소 전의 폭 기준).
        RectTransform rt = (RectTransform)_hint.transform;
        rt.sizeDelta = new Vector2(Mathf.Clamp(_hintText.GetPreferredValues(message).x + 64f, 240f, 1200f), rt.sizeDelta.y);
        _hint.alpha = 1f;
        _hint.gameObject.SetActive(true);
        _hint.transform.SetAsLastSibling();
        _hintUntil = Time.unscaledTime + seconds;
    }

    public void OnClickMainUIInventoryAImage()
    {
        // 열려 있으면 그 창을 닫는다. 예전에는 맨 위 창을 걷었다 — 인벤토리가 맨 위가 아니면 엉뚱한 창이 닫혔다.
        UI_InvenPopup inven = Managers.UI.FindPopup<UI_InvenPopup>();
        if (inven == null)
            Managers.UI.ShowPopupUI<UI_InvenPopup>();
        else
            inven.ClosePopupUI();
    }

    /// <summary>
    /// 스폰 포인트는 GenerateMap 이 태그로 긁어 담는다. 아직 안 담겼으면
    /// 담길 때까지 몇 초 기다렸다 옮긴다. 못 옮기면 플레이어가 던전 밖
    /// 원점에 선 채로 게임이 시작되지 않는다.
    /// </summary>
    System.Collections.IEnumerator CoPlaceAtSpawnWhenReady()
    {
        for (int i = 0; i < 300; i++)
        {
            if (Managers.Game.SpawnPoints != null && Managers.Game.SpawnPoints.Length > 0
                && Managers.Game.SpawnPoints[0] != null && Managers.Game.Player != null)
            {
                Managers.Game.Player.SetPlayerPosition(Managers.Game.SpawnPoints[0].position);
                Debug.Log("[GameScene] 스폰 포인트가 생겨 플레이어를 옮겼다");
                yield break;
            }
            yield return null;
        }
        Debug.LogError("[GameScene] 스폰 포인트를 끝내 못 찾았다");
    }

    public void OffUIInventory()
    {
        GetImage((int)Images.MainUIInventoryAImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(false);
    }

    public void OnUIInventory()
    {
        PlayerPrefs.SetInt("ISOPENINVENUI", 1);
        GetImage((int)Images.MainUIInventoryAImage).gameObject.SetActive(true);
        GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(false);
    }

    public void OffUISword()
    {
        GetImage((int)Images.MainUISwordAImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(false);
    }

    /// <summary>워프 버튼은 워프석 반지를 끼고 있을 때만 보인다.</summary>
    public void RefreshWarpButton()
    {
        bool on = EquipUtility.WarpUnlocked;
        GetImage((int)Images.MainUIWarpAImage).gameObject.SetActive(on);
        if (on == false)
            GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(false);
    }

    /// <summary>인벤토리 단추는 계약 뒤에만 보인다. 계약 여부는 체크포인트마다 PlayerData 에 실린다 — 예전의
    /// ISOPENINVENUI 는 계약 연출이 체크포인트를 쓴 "뒤" 에야 1 이 돼서, 계약 직후 죽거나 불러오면 0 이 되살아나
    /// 단추가 판 끝까지 사라졌다(단축키도 없어 능력치·장비를 다시 볼 길이 없었다).</summary>
    public void RefreshInventoryButton()
    {
        bool on = Managers.Game.PlayerData.IsContractedSword;
        GetImage((int)Images.MainUIInventoryAImage).gameObject.SetActive(on);
        if (on == false)
            GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(false);
    }

    /// <summary>마검 단추(몬스터 도감)는 계약 뒤에만 보인다 — 도감은 마검의 눈이다.</summary>
    public void RefreshSwordButton()
    {
        bool on = Managers.Game.PlayerData.IsContractedSword;
        GetImage((int)Images.MainUISwordAImage).gameObject.SetActive(on);
        if (on == false)
            GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(false);
    }

    public void OffUIWarp()
    {
        GetImage((int)Images.MainUIWarpAImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(false);
    }

    public void OffUI()
    {
        OffUIInventory();
        OffUISword();
        OffUIWarp();
    }

    public void OnUI()
    {
        GetImage((int)Images.MainUIOptionAImage).gameObject.SetActive(true);
        GetImage((int)Images.MainUIInventoryAImage).gameObject.SetActive(true);
        GetImage((int)Images.MainUISwordAImage).gameObject.SetActive(true);
        GetImage((int)Images.MainUIWarpAImage).gameObject.SetActive(true);
    }

    // 멈춘 동안(메뉴)은 deltaTime 이 0 이라 세지 않는다.
    public void StartTimer()
    {
        _playTime += Time.deltaTime;
        Managers.Game.PlayTime = _playTime;
        if (_playTime - _playTimeFlushed >= PlayTimeFlush)
            FlushPlayTime();
    }

    void FlushPlayTime()
    {
        _playTimeFlushed = _playTime;
        PlayerPrefs.SetFloat("PLAYTIME", _playTime);
    }

    // 씬을 떠나기 전에 아직 안 적은 몇 초를 적는다.
    void OnDestroy()
    {
        FlushPlayTime();
        GameSettings.Changed -= Refresh;
        FightGate.Remove(FatalFightGuard.Ask);
    }

    /// <summary>
    /// 층을 옮길 때의 삽화 (처음 가는 층·보스방). 보통은 1.2초이고 아무 키·클릭에 곧장 어두워진다 — 곡은 건드리지 않는다.
    /// 예전에는 층마다 4~5초였고, 그동안 곡·효과음을 0 으로 내렸다가 올려서 한 판에 음악이 96번 끊겼다.
    /// chapter(새 챕터에 들어설 때: 5·21·41·61·81층)만 예전 길이(2초 밝아짐, 1~2초, 1초 어두워짐)로 곡을 낮춘다 —
    /// 곡을 되올리는 것은 부른 쪽이다(PortalController 의 FadeInBGM).
    /// 도는 동안 입력 잠금(OnInputLock)을 쥔다. 층을 옮기는 표시(OnInteract)는 부른 쪽이 쥐고 있다 — 워프가 끼어들지 않게.
    /// </summary>
    public IEnumerator CoShowLoadingIllust(bool chapter = false)
    {
        yield return null;
        Managers.Game.OnInputLock = true;

        Image illust = GetImage((int)Images.LoadingIllustImage);
        illust.raycastTarget = true;        // 넘기려고 누른 클릭이 밑의 HUD 단추(인벤토리 등)에 닿지 않게. 끝나면 되돌린다
        illust.color = new Color(0, 0, 0, 1);
        illust.sprite = Managers.Resource.Load<Sprite>($"LoadingIllust{UnityEngine.Random.Range(1, 7)}");
        // 정수 Range(1, 2) 는 늘 1 이었다. 1~2초 사이로.
        float fadeIn = chapter ? 2f : 0.4f, hold = chapter ? UnityEngine.Random.Range(1f, 2f) : 0.4f, fadeOut = chapter ? 1f : 0.4f;

        for (float timer = 0f; timer < fadeIn + hold; timer += Time.deltaTime)
        {
            float t = Mathf.Clamp01(timer / fadeIn);
            illust.color = new Color(t, t, t, 1);
            if (chapter)
            {
                Managers.Sound.SetBGMVolume(SoundManager.ConfiguredBgmVolume - t);
                Managers.Sound.SetEffectVolume(PlayerPrefs.GetFloat("CUREFFECTSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1) - t);
            }
            else if (timer > 0f && Input.anyKeyDown)     // 계단을 밟은 그 프레임의 키는 치지 않는다
            {
                break;
            }
            yield return null;
        }

        // 지금 밝기에서 까맣게. 누르면 남은 밝기만큼 짧아진다.
        float from = illust.color.r;
        for (float timer = 0f; timer < fadeOut * from; timer += Time.deltaTime)
        {
            float t = from - timer / fadeOut;
            illust.color = new Color(t, t, t, 1);
            yield return null;
        }

        Managers.Game.OnInputLock = false;

        // 일러스트 끄기
        illust.color = new Color(1, 1, 1, 0);
        illust.raycastTarget = false;
    }

    /// <summary>
    /// 마검방 들어갈때 삽화 애니 연출
    /// </summary>
    /// <returns></returns>
    public IEnumerator CoShowMagicSwordAni()
    {
        yield return null;
        Managers.Game.OnInputLock = true;
        // 소리끄기
        //Managers.Sound.SetVolume(0);
        // 일러스트 표현
        if (Managers.Game.GameScene == null)
            yield return null;

        GetImage((int)Images.LoadingIllustImage).color = new Color(0, 0, 0, 1);
        GetImage((int)Images.LoadingIllustImage).sprite = Managers.Resource.Load<Sprite>($"ForestIllust");
        GameObject UI_LoadingIllustImage = Managers.Resource.Instantiate($"UI_LoadingIllustImage", gameObject.transform);
        UI_LoadingIllustImage.GetComponent<Image>().sprite = Managers.Resource.Load<Sprite>($"ForestIllust");

        float timer1 = 0f;
        while (timer1 < 2)
        {
            timer1 += Time.deltaTime;
            float t = Mathf.Clamp01(timer1 / 1);
            UI_LoadingIllustImage.GetComponent<Image>().color = new Color(t, t, t, 1);
            //// 소리끄기
            //Managers.Sound.SetVolume(0);
            Managers.Sound.SetBGMVolume(PlayerPrefs.GetFloat("CURBGMSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1) - t);
            Managers.Sound.SetEffectVolume(PlayerPrefs.GetFloat("CUREFFECTSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1) - t);

            yield return null;
        }

        GameObject go = Managers.Resource.Instantiate($"UI_LoadingIllustImage", gameObject.transform);
        go.GetComponent<Image>().sprite = Managers.Resource.Load<Sprite>($"ForestColorIllust");
        go.GetComponent<Image>().color = Color.white;

        float timer = 0f;
        float duration = 4f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            // imageA는 점점 투명해지고, imageB는 점점 불투명해짐
            SetImageAlpha(UI_LoadingIllustImage.GetComponent<Image>(), 1f - t);
            SetImageAlpha(go.GetComponent<Image>(), t);

            yield return null;
        }
        //Image image = Managers.Game.GameScene.ShowLoadingIllust(randValue);
        yield return new WaitForSeconds(1);

        timer = 0f;
        while (timer < duration / 2)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / (duration / 2));
            go.GetComponent<Image>().color = new Color(1 - t, 1 - t, 1 - t, 1);

            yield return null;
        }

        Managers.Game.OnInputLock = false;

        // 일러스트 끄기
        GetImage((int)Images.LoadingIllustImage).color = new Color(1, 1, 1, 0);
        Destroy(go);
        Destroy(UI_LoadingIllustImage);
    }

    /// <summary>
    /// Image의 색상 알파값을 설정하는 헬퍼 함수
    /// </summary>
    private void SetImageAlpha(Image img, float alpha)
    {
        if (img != null)
        {
            Color col = img.color;
            col.a = alpha;
            img.color = col;
        }
    }
}
