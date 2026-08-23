using Cinemachine;
using Data;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
//using UnityEditor.Scripting;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using static Define;
using static UnityEngine.EventSystems.EventTrigger;

public class GameManager
{
    public bool OnBattle = false;
    public bool OnConversation = false;
    public bool OnLever = false;
    public bool OnFade = false;
    public bool OnDirect = false;
    public bool OnStaticResolution = false;
    public bool OnInteract = false;
    public bool OnMeetKingSlime = false;
    public bool OnInputLock = false;
    public bool IsPlayerDead = false;

    public int ResolutionIdx = 1;
    public int CurEventID;
    public string CurChapter;
    public int TotalKillSplitSlime = 0;

    public GameObject CurInteractObject;
    public Light DirectionalLight;

    public int BossRoomId;

    public PlayerController Player; // ������ ������ ����
    public MonsterController Monster; // ������ ������ ����

    public CurPlayerData PlayerData = new CurPlayerData();
    public List<CurMonsterData> MonsterData = new List<CurMonsterData>(10);

    public PortalController[] Portals;
    public Transform[] SpawnPoints;

    public CurConsumableItemData ConsumableItemData = new CurConsumableItemData(); // Current Consumable Item Data
    public KeyInventory KeyInventory = new KeyInventory(); //Inventory

    public Action<float> OnFadeAction;

    public Action OnBattleAction;

    public Action OnKingSlimeDeadAction;
    public Action OnGuardianEffectAction;
    public Action OnPortalAction;

    public Texture2D _screenShot = null;
    public Sprite _screenShot2 = null;

    public Camera MainCamera;
    public GameObject ParentMap;
    public Dictionary<int, GameObject> Maps = new Dictionary<int, GameObject>();
    public GameObject DropItems;
    public GameObject Lights;

    #region CurCreatureData
    public bool playerControllLock = false;

    public class CreatureData
    {
        [JsonIgnore]
        public Action OnDataRefreshAction;
        [JsonIgnore]
        public Action OnDefenceAction;
        [JsonIgnore]
        public Action OnHitAction;
        [JsonIgnore]
        public Action OnDeadAction;

        public CreatureClass.ITrait Trait { get; set; }
        public int Ability { get; set; }
        public string Name { get; set; }
        public float MaxHP { get; set; }
        public float CurHP { get; set; }
        public float Attack { get; set; }
        public float Defence { get; set; }
        public float AttackSpeed { get; set; }
        public float DefenceSpeed { get; set; }
        public float Critical { get; set; }
        public float CriticalAttack { get; set; }
        public bool IsDefence { get; set; }
        public bool IsCritical { get; set; }
        public string IdleAnimStr { get; set; }
        public string AttackAnimStr { get; set; }
        public string BattleParticleAttack { get; set; }
        public string BattleParticleHit { get; set; }
    }

    public class CurPlayerData : CreatureData
    {
        public int Level { get; set; } = 1; // Lv
        public float curExp;
        public float CurExp
        {
            get
            {
                return curExp;
            }
            set
            {
                curExp = value;

                float needExp = Managers.Data.PlayerDic[Level + 1].NeedExp;
                Debug.Log($"CurExp : {CurExp}");
                Debug.Log($"NeedExp : {Managers.Data.PlayerDic[Level + 1].NeedExp}");

                if (curExp >= needExp)
                {
                    curExp = curExp - needExp;
                    Level++;
                    Debug.Log("Level UP!!");
                    Managers.Resource.Instantiate("LevelUp", Managers.Game.Player.transform);
                    LevelUp();
                }
            }
        }
        //public float MaxHP { get; set; }
        //public float CurHP { get; set; }
        //public float Attack { get; set; }
        //public float Defence { get; set; }
        //public float AttackSpeed { get; set; }
        //public float DefenceSpeed { get; set; }
        //public float Critical { get; set; }
        //public float CriticalAttack { get; set; }
        public float MoveSpeed { get; set; }
        //public bool IsDefence { get; set; }
        //public Dictionary<int, int> Inventory = new Dictionary<int, int>();
        public List<List<int>> Inventory = new List<List<int>>();
        public List<int> KeyInventory = new List<int>();
        public int CurSword { get; set; }
        public int CurShield { get; set; }
        public int CurNecklace { get; set; }
        public int CurRing { get; set; }
        public int CurShoes { get; set; }
        public int CurBook { get; set; }
        public MyVector3 CurPosition { get; set; }
        public int CurStageid { get; set; }
        public bool IsContractedSword { get; set; }
        //public bool HasGetEquip { get; set; } // 인벤 UI 개방용
        //public bool HasGetWarp { get; set; } // 워프 UI 개방용
        //public bool HasGetClass { get; set; } // 특성을 얻었는지 -> 특성 UI 개방용
        public List<bool> FirstEnterMapCheck = new List<bool>();

        public void Clear()
        {
            int level = 1;
            Managers.Game.PlayerData.Level = Managers.Data.PlayerDic[level].id;
            Managers.Game.PlayerData.CurExp = 0;
            Managers.Game.PlayerData.MaxHP = Managers.Data.PlayerDic[level].MaxHP;
            Managers.Game.PlayerData.CurHP = Managers.Data.PlayerDic[level].MaxHP;
            Managers.Game.PlayerData.Attack = Managers.Data.PlayerDic[level].Attack;
            Managers.Game.PlayerData.Defence = Managers.Data.PlayerDic[level].Defence;
            Managers.Game.PlayerData.AttackSpeed = Managers.Data.PlayerDic[level].AttackSpeed;
            Managers.Game.PlayerData.DefenceSpeed = Managers.Data.PlayerDic[level].DefenceSpeed;
            Managers.Game.PlayerData.Critical = Managers.Data.PlayerDic[level].Critical;
            Managers.Game.PlayerData.CriticalAttack = Managers.Data.PlayerDic[level].CriticalAttack;
            Managers.Game.PlayerData.MoveSpeed = Managers.Data.PlayerDic[level].MoveSpeed;
            Managers.Game.PlayerData.IsDefence = false;
            Managers.Game.PlayerData.CurStageid = 0;
            Managers.Game.PlayerData.CurPosition = new MyVector3() { X = 0, Y = 1.5f, Z = 0 };
            Managers.Game.PlayerData.IsContractedSword = false;

            EnsureLists();
        }

        /// <summary>
        /// 새 게임에서 비어 있으면 안 되는 목록들을 채운다.
        /// 예전에는 LoadGame 의 "세이브 없음" 분기 안에서만 채워서, 새 게임 경로에
        /// 따라 빈 채로 남았다. 그러면
        ///   - FirstEnterMapCheck[_mapId+1] 이 터져서 계단이 죽고
        ///   - KeyInventory._keys 가 터져서 GameScene 초기화가 통째로 끊긴다.
        /// 실행마다 되기도 하고 안 되기도 한 원인이었다.
        /// </summary>
        public void EnsureLists()
        {
            if (FirstEnterMapCheck == null)
                FirstEnterMapCheck = new List<bool>();
            while (FirstEnterMapCheck.Count < 110)
                FirstEnterMapCheck.Add(false);

            if (Inventory == null)
                Inventory = new List<List<int>>();
            while (Inventory.Count < 10)
                Inventory.Add(new List<int>());

            if (Managers.Game.KeyInventory != null)
                Managers.Game.KeyInventory.EnsureKeys();
        }
    }

    public class CurMonsterData : CreatureData
    {
        public int id { get; set; }
        public int Chapter { get; set; }
        //public string Class { get; set; }
        //public string Name { get; set; }
        public int Feature { get; set; }
        public string Image { get; set; }
        //public float MaxHP { get; set; }
        //public float CurHP { get; set; }
        //public float Attack { get; set; }
        //public float Defence { get; set; }
        //public float AttackSpeed { get; set; }
        //public float DefenceSpeed { get; set; }
        //public float Critical { get; set; }
        //public float CriticalAttack { get; set; }
        public float RewardExp { get; set; }
        public int RewardItem { get; set; }
        //public string IdleAnimStr { get; set; }
        //public string AttackAnimStr { get; set; }
        //public string BattleParticleAttack { get; set; }
        //public string BattleParticleHit { get; set; }
        public int MonsterNameId { get; set; }
        public int MonsterDescId { get; set; }
        //public bool IsDefence { get; set; }
        public int IsActiveIndex { get; set; }
        public int DamagedCount { get; set; }
    }
    #endregion

    #region CurConsumableItemData
    public class CurConsumableItemData
    {
        public int id { get; set; }
        public float Heal { get; set; }
        public float AttackUp { get; set; }
        public float DefenceUp { get; set; }
        public float HPUp { get; set; }
        public string Img { get; set; }
        public string PrefabName { get; set; }
        public string Shadow { get; set; }
        public int ScriptNameId { get; set; }
        public int ScriptDescriptionId { get; set; }
        public int IsActiveIndex { get; set; }
    }

    #endregion

    #region InGame
    public int GameSpeed = 1;
    public UI_GameScene GameScene = null;
    public int AttackCount { get; set; }
    public float PlayTime = 0f;
    public float DefenceCoolTime = 0f;
    //public bool[] firstEnterMapCheck = new bool[1001];

    public static void LevelUp()
    {
        Managers.Game.PlayerData.MaxHP += Managers.Data.PlayerDic[Managers.Game.PlayerData.Level].MaxHP;
        Managers.Game.PlayerData.CurHP += Managers.Data.PlayerDic[Managers.Game.PlayerData.Level].MaxHP;
        Managers.Game.PlayerData.Attack += Managers.Data.PlayerDic[Managers.Game.PlayerData.Level].Attack;
        Managers.Game.PlayerData.Defence += Managers.Data.PlayerDic[Managers.Game.PlayerData.Level].Defence;
        Managers.Game.PlayerData.AttackSpeed += Managers.Data.PlayerDic[Managers.Game.PlayerData.Level].AttackSpeed;
        Managers.Game.PlayerData.DefenceSpeed += Managers.Data.PlayerDic[Managers.Game.PlayerData.Level].DefenceSpeed;
        Managers.Game.PlayerData.Critical += Managers.Data.PlayerDic[Managers.Game.PlayerData.Level].Critical;
        Managers.Game.PlayerData.CriticalAttack += Managers.Data.PlayerDic[Managers.Game.PlayerData.Level].CriticalAttack;
        Managers.Game.PlayerData.MoveSpeed += Managers.Data.PlayerDic[Managers.Game.PlayerData.Level].MoveSpeed;
    }

    public void SwapEquip(int curIdx, int idx)
    {
        if (curIdx == 0)
        {
            Managers.Game.PlayerData.Attack += Managers.Data.EquipDic[curIdx].ATK;
            Managers.Game.PlayerData.Defence += Managers.Data.EquipDic[curIdx].DEF;
            Managers.Game.PlayerData.MaxHP += Managers.Data.EquipDic[curIdx].HP;
            Managers.Game.PlayerData.AttackSpeed += Managers.Data.EquipDic[curIdx].ASPD;
            Managers.Game.PlayerData.DefenceSpeed += Managers.Data.EquipDic[curIdx].DSPD;
            Managers.Game.PlayerData.Critical += Managers.Data.EquipDic[curIdx].CRI;
            Managers.Game.PlayerData.CriticalAttack += Managers.Data.EquipDic[curIdx].CRIATK;
            Managers.Game.PlayerData.MoveSpeed += Managers.Data.EquipDic[curIdx].MSPD;
            return;
        }
        else
        {
            Managers.Game.PlayerData.Attack -= Managers.Data.EquipDic[curIdx].ATK;
            Managers.Game.PlayerData.Defence -= Managers.Data.EquipDic[curIdx].DEF;
            Managers.Game.PlayerData.MaxHP -= Managers.Data.EquipDic[curIdx].HP;
            Managers.Game.PlayerData.AttackSpeed -= Managers.Data.EquipDic[curIdx].ASPD;
            Managers.Game.PlayerData.DefenceSpeed -= Managers.Data.EquipDic[curIdx].DSPD;
            Managers.Game.PlayerData.Critical -= Managers.Data.EquipDic[curIdx].CRI;
            Managers.Game.PlayerData.CriticalAttack -= Managers.Data.EquipDic[curIdx].CRIATK;
            Managers.Game.PlayerData.MoveSpeed -= Managers.Data.EquipDic[curIdx].MSPD;

            Managers.Game.PlayerData.Attack += Managers.Data.EquipDic[idx].ATK;
            Managers.Game.PlayerData.Defence += Managers.Data.EquipDic[idx].DEF;
            Managers.Game.PlayerData.MaxHP += Managers.Data.EquipDic[idx].HP;
            Managers.Game.PlayerData.AttackSpeed += Managers.Data.EquipDic[idx].ASPD;
            Managers.Game.PlayerData.DefenceSpeed += Managers.Data.EquipDic[idx].DSPD;
            Managers.Game.PlayerData.Critical += Managers.Data.EquipDic[idx].CRI;
            Managers.Game.PlayerData.CriticalAttack += Managers.Data.EquipDic[idx].CRIATK;
            Managers.Game.PlayerData.MoveSpeed += Managers.Data.EquipDic[idx].MSPD;
        }

        Managers.Game.GameScene.Refresh();
    }

    #region Map 생성
    public KeyValuePair<int, int> GetChapterCount(int mapId)
    {
        CurChapter = Managers.Data.StageInfoDic[mapId].DungeonID.Substring(0, 2);

        var chapterMaps = Managers.Data.StageInfoDic
            .Where(entry => entry.Value.DungeonID.Substring(0, 2) == CurChapter) // 챕터 필터링
            .Select(entry => entry.Key);                 // 맵 ID 추출

        int startMapId = chapterMaps.Min();
        int endMapId = chapterMaps.Max();

        KeyValuePair<int, int> entireChapter = new KeyValuePair<int, int>(startMapId, endMapId);
        return entireChapter;
    }

    public void GenerateMap(int mapId)
    {
        if (ParentMap != null)
            Managers.Resource.Destroy(ParentMap);
        Maps.Clear();

        int count = 0;
        KeyValuePair<int, int> mapStartAndEnd = GetChapterCount(mapId);

        ParentMap = new GameObject(name: "Maps");

        for (int i = mapStartAndEnd.Key; i <= mapStartAndEnd.Value; i++)
        {
            // 1~4층은 손수 만든 프리팹을 그대로 쓴다.
            // 튜토리얼 / 마검 계약 / 킹슬라임 연출이 DirectingManager 에서 그 층의
            // 특정 오브젝트 이름("Items/CItem13", "SpawnKingSlime" …)을 직접 찾기 때문에,
            // 생성된 층으로 갈아끼우면 인트로가 NullReference 로 통째로 깨진다.
            // 나머지 96개 층은 CSV(MapData)로 조립한다.
            string dungeonId = Managers.Data.StageInfoDic[i].DungeonID;
            string mapKey = $"Dungeon_{dungeonId}";

            GameObject map = MapBuilder.IsHandAuthored(dungeonId)
                ? Managers.Resource.Instantiate(mapKey, ParentMap.transform)
                : MapBuilder.Build(i, ParentMap.transform);

            if (map == null)  // 프리팹이 없으면 조립으로, 데이터가 없으면 프리팹으로 폴백
            {
                map = MapBuilder.IsHandAuthored(dungeonId)
                    ? MapBuilder.Build(i, ParentMap.transform)
                    : Managers.Resource.Instantiate(mapKey, ParentMap.transform);
            }

            if (map == null)
            {
                Debug.LogError($"맵 생성 실패 : {mapKey}");
                continue;
            }

            map.transform.position = new Vector3(count * 100, 0f, 0f);
            Maps.Add(i, map);
            RefreshMap(i);
            count++;
        }
        DropItems = new GameObject(name: "DropItems");
        DropItems.transform.parent = ParentMap.transform;
        Portals = ParentMap.GetComponentsInChildren<PortalController>();
        SpawnPoints = ParentMap.GetComponentsInChildren<Transform>().Where(child => child.CompareTag("SpawnPoint")).ToArray();
        // 보스방은 "현재 챕터" 안에서 찾는다 (전역에서 찾으면 항상 첫 챕터 보스가 잡힌다)
        BossRoomId = Managers.Data.StageInfoDic
                    .Where(pair => pair.Key >= mapStartAndEnd.Key && pair.Key <= mapStartAndEnd.Value
                                   && pair.Value.Type == Define.DungeonType.Boss)
                    .Select(pair => pair.Key).DefaultIfEmpty(mapStartAndEnd.Value).First();

        if (Managers.Game.PlayerData.CurStageid == 2)
        {
            Managers.Game.OnStaticResolution = true;
        }
        //MainCamera.GetComponentInChildren<CustomCameraLimiter>().SetBG();

        RefreshBossGates();

        // 챕터별 분위기: 시간대(해의 각도·세기·색) + 안개 + 파티클.
        // 기획서 118~123쪽이 테마를 그 두 가지로 정의한다 — 색만 바꾸면 같은 곳에
        // 필터를 씌운 것으로 보이고, 그림자 방향과 안개가 같이 바뀌어야 다른 장소가 된다.
        int chapterIndex = MapBuilder.GetChapter(mapId);
        ChapterTheme.Apply(chapterIndex, DirectionalLight);

        PlayChapterBGM(mapId);

        string effectKey = $"Effects_{CurChapter}";
        if (Managers.Resource.Load<GameObject>(effectKey) != null)
            Managers.Resource.Instantiate(effectKey, ParentMap.transform);
        else if (Managers.Resource.Load<GameObject>("Effects_00") != null)
            Managers.Resource.Instantiate("Effects_00", ParentMap.transform);
    }

    /// <summary>
    /// 보스 층의 위층 계단은 그 층 보스를 잡기 전까지 잠근다 — "순서" 설계의 핵심 관문.
    ///
    /// 건드리는 것은 생성된 보스 층의 UpStairs 뿐이다:
    ///   - 보스방 입구(id 16)는 절대 끄지 않는다. 껐다가 다시 켜주는 곳이 없어서
    ///     킹슬라임 보스방에 영영 못 들어가고 3층에서 진행이 막혔다.
    ///   - 손수 만든 1~4층은 기존 연출(BossOnDeadAction -> Unlock4Floor)이 처리한다.
    ///     그 프리팹의 보스는 _monsterIndex_forActive 가 구워져 있지 않아 여기서 판정할 수 없다.
    /// </summary>
    public void RefreshBossGates()
    {
        foreach (KeyValuePair<int, GameObject> pair in Maps)
        {
            Data.StageInfoData info;
            if (Managers.Data.StageInfoDic.TryGetValue(pair.Key, out info) == false)
                continue;
            if (info.Type != Define.DungeonType.Boss || MapBuilder.IsHandAuthored(info.DungeonID))
                continue;

            bool bossAlive = IsBossAlive(pair.Value);

            foreach (PortalController portal in pair.Value.GetComponentsInChildren<PortalController>(true))
            {
                if (portal._portalType != PortalController.Type.UpStairs)
                    continue;
                if (portal.transform.parent != null)
                    portal.transform.parent.gameObject.SetActive(bossAlive == false);
            }
        }
    }

    static bool IsBossAlive(GameObject map)
    {
        foreach (MonsterController mc in map.GetComponentsInChildren<MonsterController>(true))
        {
            if (mc.CompareTag("Boss") == false)
                continue;
            bool alive;
            if (Managers.Data.MonsterActiveDic.TryGetValue(mc._monsterIndex_forActive, out alive) == false)
                return true;   // 모르면 잠가 둔다
            return alive;
        }
        return false;          // 보스가 없는 층은 잠그지 않는다
    }

    /// <summary>
    /// 챕터 BGM. StageInfoData 의 BGM 열을 쓴다.
    ///
    /// 지금 실재하는 BGM 은 챕터 0 것뿐이라 대부분 폴백으로 떨어진다.
    /// 챕터 음악을 새로 넣으면 StageInfoData 의 BGM 값(BGM_100 …)에 맞춰
    /// 어드레서블만 추가하면 이 코드가 그대로 집어간다.
    /// </summary>
    void PlayChapterBGM(int mapId)
    {
        Data.StageInfoData info;
        if (Managers.Data.StageInfoDic.TryGetValue(mapId, out info) == false)
            return;

        string key = info.BGM;
        if (string.IsNullOrEmpty(key) || Managers.Resource.Load<AudioClip>(key) == null)
            key = "Chapter0_BGM";

        Managers.Sound.FadeAndPlayBGM(key, 2f);
    }

    public MonsterController GetBoss()
    {
        // Maps 는 "절대 스테이지 ID" 로 키가 잡혀 있다.
        // 예전처럼 챕터 시작을 빼서 상대 인덱스로 찾으면 챕터 1 이후에서 예외가 난다.
        GameObject bossMap;
        if (Maps.TryGetValue(BossRoomId, out bossMap) == false || bossMap == null)
            return null;

        MonsterController[] monsters = bossMap.GetComponentsInChildren<MonsterController>(true);
        for (int i = 0; i < monsters.Length; i++)
            if (monsters[i].CompareTag("Boss") || monsters[i] is BossMonsterController)
                return monsters[i];

        return monsters.Length > 0 ? monsters[0] : null;
    }

    public void RefreshMap(int mapId)
    {
        foreach (Transform child in Maps[mapId].transform.Find("Monsters"))
        {
            if (child.TryGetComponent(out MonsterController monster)
                && Managers.Data.MonsterActiveDic[monster._monsterIndex_forActive] == false)
            {
                monster.gameObject.SetActive(false);
            }
        }

        foreach (Transform child in Maps[mapId].transform.Find("Items"))
        {
            if (child.TryGetComponent(out ConsumableItem cItem)
                && Managers.Data.CItemActiveDic[cItem._itemIndex_forActive] == false)
            {
                cItem.gameObject.SetActive(false);
            }

            if (child.TryGetComponent(out Equip eItem)
                && Managers.Data.EItemActiveDic[eItem._itemIndex_forActive] == false)
            {
                eItem.gameObject.SetActive(false);
            }
        }

        foreach (Transform child in Maps[mapId].transform.Find("Doors"))
        {
            Door door = child.GetComponentInChildren<Door>();
            if (door != null && Managers.Data.DoorActiveDic[door._doorIndex_forActive] == false)
            {
                door.gameObject.SetActive(false);
            }

        }

        foreach (Transform child in Maps[mapId].transform.Find("Pillars"))
        {
            Pillar pillar = child.GetComponentInChildren<Pillar>();
            if (pillar != null && Managers.Data.PillarActiveDic[pillar._pillarIndex_forActive] == false)
            {
                pillar.SetInActive();
            }
        }

        foreach (Transform child in Maps[mapId].transform.Find("Levers"))
        {
            Lever lever = child.GetComponentInChildren<Lever>();
            if (lever != null && Managers.Data.LeverActiveDic[lever._leverIndex_forActive] == false)
            {
                lever.Play(0f);
                lever.SetActive();
            }
        }
    }

    #endregion
    public void SwapEquip(int idx)
    {
        int type = Managers.Data.EquipDic[idx].Type;
        int curIdx = 1;
        switch (type)
        {
            case 1:
                curIdx = Managers.Game.PlayerData.CurSword;
                Managers.Game.PlayerData.CurSword = idx;
                break;
            case 2:
                curIdx = Managers.Game.PlayerData.CurShield;
                Managers.Game.PlayerData.CurShield = idx;
                break;
            // Define.Types 와 어긋나 있었다. 3 은 목걸이인데 반지 칸에 넣고 있어서
            // 목걸이 칸은 영원히 비어 있었고(인벤토리는 CurNecklace 를 그린다),
            // 반지와 책은 분기가 비어 있어 주워도 장착이 되지 않았다.
            case 3:
                curIdx = Managers.Game.PlayerData.CurNecklace;
                Managers.Game.PlayerData.CurNecklace = idx;
                break;
            case 4:
                curIdx = Managers.Game.PlayerData.CurRing;
                Managers.Game.PlayerData.CurRing = idx;
                break;
            case 5:
                curIdx = Managers.Game.PlayerData.CurShoes;
                Managers.Game.PlayerData.CurShoes = idx;
                break;
            case 6:
                curIdx = Managers.Game.PlayerData.CurBook;
                Managers.Game.PlayerData.CurBook = idx;
                break;
            default:
                break;
        }

        if (curIdx == 0)
        {
            Managers.Game.PlayerData.Attack += Managers.Data.EquipDic[idx].ATK;
            Managers.Game.PlayerData.Defence += Managers.Data.EquipDic[idx].DEF;
            Managers.Game.PlayerData.MaxHP += Managers.Data.EquipDic[idx].HP;
            Managers.Game.PlayerData.AttackSpeed += Managers.Data.EquipDic[idx].ASPD;
            Managers.Game.PlayerData.DefenceSpeed += Managers.Data.EquipDic[idx].DSPD;
            Managers.Game.PlayerData.Critical += Managers.Data.EquipDic[idx].CRI;
            Managers.Game.PlayerData.CriticalAttack += Managers.Data.EquipDic[idx].CRIATK;
            Managers.Game.PlayerData.MoveSpeed += Managers.Data.EquipDic[idx].MSPD;
            return;
        }
        else
        {
            Managers.Game.PlayerData.Attack -= Managers.Data.EquipDic[curIdx].ATK;
            Managers.Game.PlayerData.Defence -= Managers.Data.EquipDic[curIdx].DEF;
            Managers.Game.PlayerData.MaxHP -= Managers.Data.EquipDic[curIdx].HP;
            Managers.Game.PlayerData.AttackSpeed -= Managers.Data.EquipDic[curIdx].ASPD;
            Managers.Game.PlayerData.DefenceSpeed -= Managers.Data.EquipDic[curIdx].DSPD;
            Managers.Game.PlayerData.Critical -= Managers.Data.EquipDic[curIdx].CRI;
            Managers.Game.PlayerData.CriticalAttack -= Managers.Data.EquipDic[curIdx].CRIATK;
            Managers.Game.PlayerData.MoveSpeed -= Managers.Data.EquipDic[curIdx].MSPD;

            Managers.Game.PlayerData.Attack += Managers.Data.EquipDic[idx].ATK;
            Managers.Game.PlayerData.Defence += Managers.Data.EquipDic[idx].DEF;
            Managers.Game.PlayerData.MaxHP += Managers.Data.EquipDic[idx].HP;
            Managers.Game.PlayerData.AttackSpeed += Managers.Data.EquipDic[idx].ASPD;
            Managers.Game.PlayerData.DefenceSpeed += Managers.Data.EquipDic[idx].DSPD;
            Managers.Game.PlayerData.Critical += Managers.Data.EquipDic[idx].CRI;
            Managers.Game.PlayerData.CriticalAttack += Managers.Data.EquipDic[idx].CRIATK;
            Managers.Game.PlayerData.MoveSpeed += Managers.Data.EquipDic[idx].MSPD;
        }

        // 착용한 것이 바뀌었으니 유틸(이동 속도·전투 배속)을 다시 계산한다.
        EquipUtility.Apply();

        if (Managers.Game.GameScene != null)
            Managers.Game.GameScene.Refresh();
    }

    /// <summary>이미 다녀온 층으로 곧장 이동한다 (기획서 65쪽의 워프).
    ///
    /// 등록부를 따로 두지 않는다 — FirstEnterMapCheck 가 이미 "처음 밟은 층" 을
    /// 기록하고 있어서 그게 곧 다녀온 층 목록이다.
    /// 계단으로 오르내리는 것과 같은 절차를 밟는다: 맵을 만들고, 스폰 지점에 세우고,
    /// 카메라 경계를 다시 잡는다. 하나라도 빠지면 플레이어가 다른 층 벽에 파묻힌다.</summary>
    public bool WarpToStage(int stageId)
    {
        if (EquipUtility.WarpUnlocked == false)
            return false;
        if (CanWarpTo(stageId) == false)
            return false;
        if (OnBattle || OnFade || OnDirect || OnInteract)
            return false;

        GenerateMap(stageId);

        Vector3 pos = Player.transform.position;
        if (SpawnPoints != null && SpawnPoints.Length > 0 && SpawnPoints[0] != null)
            pos = SpawnPoints[0].transform.position;

        if (MainCamera != null)
        {
            CameraController cam = MainCamera.GetComponentInChildren<CameraController>();
            if (cam != null)
                cam.SetupCameraConfiner();
        }

        Player.transform.position = pos;
        Player._cellPos = pos;
        PlayerData.CurStageid = stageId;

        if (OnPortalAction != null)
            OnPortalAction.Invoke();
        if (GameScene != null)
            GameScene.Refresh();
        return true;
    }

    /// <summary>그 층으로 워프할 수 있는가. 다녀온 층이어야 한다.</summary>
    public bool CanWarpTo(int stageId)
    {
        if (stageId == PlayerData.CurStageid)
            return false;
        if (Managers.Data.StageInfoDic.ContainsKey(stageId) == false)
            return false;

        List<bool> visited = PlayerData.FirstEnterMapCheck;
        return visited != null && stageId >= 0 && stageId < visited.Count && visited[stageId];
    }

    /// <summary>다녀온 층 목록. 워프 UI 가 이걸 그린다.</summary>
    public List<int> WarpableStages()
    {
        List<int> found = new List<int>();
        List<bool> visited = PlayerData.FirstEnterMapCheck;
        if (visited == null)
            return found;

        for (int i = 0; i < visited.Count; i++)
        {
            if (visited[i] && i != PlayerData.CurStageid && Managers.Data.StageInfoDic.ContainsKey(i))
                found.Add(i);
        }
        return found;
    }

    /// <summary>주운 장비를 지금 낀 것과 견줘 더 나으면 갈아입는다.
    ///
    /// 예전에는 주우면 무조건 장착했다. 그래서 더 나쁜 것을 밟기만 해도 손해였고,
    /// 실제로 스탯이 0 인 자리표 장비를 주워 무기가 바뀌는 바람에 1층에서 게임이
    /// 끝난 적이 있다. 줍는 것 자체는 이득이어야 한다 — 갈아입을지는 판단이다.</summary>
    public bool EquipIfBetter(int idx)
    {
        Data.EquipData incoming;
        if (Managers.Data.EquipDic.TryGetValue(idx, out incoming) == false)
            return false;

        int cur = CurrentOfType(incoming.Type);
        if (cur == idx)
            return false;

        if (cur > 0 && EquipScore(idx) <= EquipScore(cur))
            return false;   // 지금 낀 것이 더 낫거나 같다. 인벤토리에 넣어만 둔다.

        SwapEquip(idx);
        return true;
    }

    /// <summary>그 부위에 지금 낀 장비 id.</summary>
    public int CurrentOfType(int type)
    {
        switch (type)
        {
            case (int)Define.Types.Sword: return PlayerData.CurSword;
            case (int)Define.Types.Shield: return PlayerData.CurShield;
            case (int)Define.Types.Necklace: return PlayerData.CurNecklace;
            case (int)Define.Types.Ring: return PlayerData.CurRing;
            case (int)Define.Types.Shoes: return PlayerData.CurShoes;
            case (int)Define.Types.Book: return PlayerData.CurBook;
            default: return 0;
        }
    }

    /// <summary>장비의 좋고 나쁨. 스탯 합에, 유틸 장비는 어빌리티 등급을 얹는다.</summary>
    public static float EquipScore(int idx)
    {
        Data.EquipData eq;
        if (idx <= 0 || Managers.Data.EquipDic.TryGetValue(idx, out eq) == false)
            return -1f;

        float score = eq.ATK + eq.DEF + eq.HP * 0.2f + (eq.ASPD + eq.DSPD) * 10f
                      + eq.CRI + eq.CRIATK * 0.1f + eq.MSPD * 10f;

        // 부츠·목걸이는 스탯이 0 이고 어빌리티 등급이 곧 성능이다.
        if (eq.AbilityId > 0)
            score += eq.AbilityId;
        return score;
    }

    #endregion

    #region Save&Load

    string _path;

    public void SaveGame()
    {
        if (Managers.Game.Player == null)
        {
            Managers.Game.PlayerData.CurPosition = new Data.MyVector3
            {
                X = 0,
                Y = 0,
                Z = 0,
            };
        }
        else
        {
            Managers.Game.PlayerData.CurPosition = new Data.MyVector3
            {
                X = Managers.Game.Player.transform.position.x,
                Y = Managers.Game.Player.transform.position.y,
                Z = Managers.Game.Player.transform.position.z,
            };
        }

        string jsonStr = JsonConvert.SerializeObject(PlayerData, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        });
        File.WriteAllText(_path, jsonStr);

        Managers.Data.UpdateActiveDic();
    }

    public bool LoadGame()
    {
        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
        {
            string path = Application.persistentDataPath + "/SaveData.json";
            if (File.Exists(path))
                File.Delete(path);


            Managers.Game.PlayerData.Clear();

            KeyInventory.InitKeyInventory();

            for (int i = 0; i < 10; ++i)
            {
                Managers.Game.PlayerData.Inventory.Add(new List<int>());
            }
            Managers.Game.PlayerData.FirstEnterMapCheck = new List<bool>(new bool[110]);
            // 오픈하면 1로 변경해야함.
            PlayerPrefs.SetInt("ISOPENSWORD", 0);
            PlayerPrefs.SetInt("ISOPENPORTAL", 0);
            PlayTime = PlayerPrefs.GetFloat("PLAYTIME", 0);

            return false;
        }

        if (File.Exists(_path) == false)
        {
            Debug.Log("�÷��̾� ������ �ε� ����");
            return false;
        }

        string fileStr = File.ReadAllText(_path);
        CurPlayerData data = JsonConvert.DeserializeObject<CurPlayerData>(fileStr, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        });

        if (data != null)
        {
            PlayerData = data;

            PlayTime = PlayerPrefs.GetFloat("PLAYTIME", 0);
            Managers.Data.LoadActiveDic();
            Debug.Log("Complete Loading Data.");
        }

        KeyInventory.InitKeyInventory();

        return true;
    }

    #endregion

    #region ForData
    public Define.ScriptType ScriptType = Define.ScriptType.None;
    public Define.ScreenType ScreenType = Define.ScreenType.None;

    public void DeleteGameData()
    {
        //PlayerPrefs.DeleteAll();
        // ISFIRST를 지워야하나? 진짜 최초는 아닌데
        PlayerPrefs.DeleteKey("ISFIRST");
        PlayerPrefs.DeleteKey("ISFIRSTBATTLE");
        PlayerPrefs.DeleteKey("ISFIRSTLEVER");
        PlayerPrefs.DeleteKey("ISFIRSTRECOVERY");
        PlayerPrefs.DeleteKey("ISFIRSTKEY");
        // 여기까지 찐으로 처음만 표시해야할거같은데

        PlayerPrefs.DeleteKey("ISOPENSWORD");
        PlayerPrefs.DeleteKey("ISOPENPORTAL");
        PlayerPrefs.DeleteKey("ISOPENINVENUI");
        PlayerPrefs.DeleteKey("ISOPENWARPUI");
        PlayerPrefs.DeleteKey("ISOPENCLASSUI");
        PlayerPrefs.DeleteKey("ISMEETSWORD"); // 마검 만났는지
        PlayerPrefs.DeleteKey("ISMEETBOSS"); // 해당 스테이지 보스 만났는지
        // Key Slot ---------------
        PlayerPrefs.DeleteKey("ISOPENGREENKEY");
        PlayerPrefs.DeleteKey("ISOPENYELLOWKEY");
        PlayerPrefs.DeleteKey("ISOPENREDKEY");
        // ------------------------
        PlayerPrefs.DeleteKey("DEATHCOUNT");
        PlayerPrefs.DeleteKey("MOVECOUNT");
        PlayerPrefs.DeleteKey("PLAYTIME");

        Managers.Data.ResetActiveDic();
        //ParseMapData();
        Managers.Game.PlayerData.Clear();
        Managers.Game.PlayerData.Inventory.Clear();
        for (int i = 0; i < 10; ++i)
        {
            Managers.Game.PlayerData.Inventory.Add(new List<int>());
        }

        Debug.Log("Complete DeleteGameData");
    }

    #endregion

    public void Init()
    {
        _path = Application.persistentDataPath + "/SaveData.json";

        if (LoadGame())
            return;

        //SaveGame();
    }
}
