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

                // 한 번에 여러 레벨이 오를 수 있다 (킹 슬라임 400 = Lv11~12 에서 두 레벨 남짓).
                // 예전에는 if 하나라 한 레벨만 오르고 나머지가 다음 전투로 밀렸다 —
                // generate_content 의 while 과 어긋났다. 표의 끝(Lv115)에서는 멈춘다.
                bool leveled = false;
                Data.PlayerData next;
                while (Managers.Data.PlayerDic.TryGetValue(Level + 1, out next) && curExp >= next.NeedExp)
                {
                    curExp -= next.NeedExp;
                    Level++;
                    LevelUp();
                    leveled = true;
                    GameEvents.RaiseLevelUp(Level);
                }

                // 불기둥·소리는 맵 위에서 — 전투 중이면 전투창이 닫힌 뒤에 튼다(ObjectManager.ShowLevelUp).
                if (leveled)
                    Managers.Object.ShowLevelUp();
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
        {
            CollectDrops();   // 워프·챕터 경계: 떨군 보상이 맵과 함께 부서지기 전에
            Managers.Resource.Destroy(ParentMap);
        }
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
            BindKingSlime(map);
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
    /// 건드리는 것은 보스 층의 UpStairs 뿐이다:
    ///   - 보스방 입구(id 16)는 절대 끄지 않는다. 껐다가 다시 켜주는 곳이 없어서
    ///     킹슬라임 보스방에 영영 못 들어가고 3층에서 진행이 막혔다.
    ///   - 손수 만든 4층(킹 슬라임)도 여기서 잠근다. 예전에는 1~4층을 건너뛰었는데, 그 층 계단을 끄던 옛 줄
    ///     (GenerateMap 의 "Portals 마지막 끄기")이 챕터 확장 때 이 함수로 바뀌면서 4층을 끄는 곳이 사라졌다 —
    ///     킹 슬라임을 옆으로 돌아 5층으로 올라갈 수 있었다. 지금은 킹 슬라임(BindKingSlime)도 분열 슬라임
    ///     (SpawnSplitSlime)도 활성 번호를 받으니 IsBossAlive 가 판정한다.
    /// </summary>
    public void RefreshBossGates()
    {
        foreach (KeyValuePair<int, GameObject> pair in Maps)
        {
            Data.StageInfoData info;
            if (Managers.Data.StageInfoDic.TryGetValue(pair.Key, out info) == false)
                continue;
            if (info.Type != Define.DungeonType.Boss)
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

    /// <summary>킹 슬라임만 쓰는 활성 번호. 맵 데이터의 카운터(수백 번대)와 마검 열쇠(9000)를 피한다.</summary>
    const int KingSlimeActiveIndex = 9001;

    /// <summary>분열 슬라임 셋(몬스터 9002~9004)과 노랑이 품은 물약(아이템 9001)의 활성 번호.
    /// 킹 슬라임이 죽을 때 연출이 새로 세우는 것이라(DirectingManager.SpawnSplitSlime/SpawnSplitPotion) 맵 데이터 번호가 없다.
    /// 프리팹의 0 번을 그대로 쓰면 잡거나 마실 때 1층 첫 몬스터·첫 아이템이 없어진 것으로 저장된다.</summary>
    public const int SplitSlimeActiveIndex = 9002;
    public const int SplitPotionActiveIndex = 9001;

    /// <summary>
    /// 잡은 몬스터가 떨군 장비(DropItems, 활성 번호 -1)를 지금 줍는다. 떨군 것은 체크포인트에 없는데 잡은 것은 남아서,
    /// 줍지 않고 층을 떠난 뒤 워프·죽음·불러오기를 거치면 보스는 죽은 채 보상만 영영 사라졌다(킹 슬라임의 모래시계
    /// 목걸이, 20층의 워프석 반지). 층을 떠날 때(체크포인트를 쓸 때)와 맵을 부수기 전에 부른다 — 줍는 길은 발로 밟을 때와 같다.
    /// 씬을 다시 올린 뒤에는 옛 맵과 함께 이미 부서져(DropItems 가 유니티 null) 아무것도 안 한다: 불러온 판에 옛 판의 것을 섞지 않는다.
    /// </summary>
    void CollectDrops()
    {
        if (DropItems == null || GameScene == null)
            return;
        foreach (Equip drop in DropItems.GetComponentsInChildren<Equip>())   // 주운 것은 꺼져 있어 안 잡힌다
            drop.PickUp();
    }

    /// <summary>
    /// 손수 만든 4층의 킹 슬라임이 죽은 것을 기억하게 한다.
    ///
    /// 그 프리팹의 킹 슬라임은 활성 번호가 구워져 있지 않아 0 번(1층 첫 몬스터)을 나눠 쓰고,
    /// "BossMonsters" 밑에 있어 RefreshMap 이 보지도 않는다. 그래서 불러올 때마다 되살아났다.
    /// 층마다 체크포인트를 남기는 지금은 5층 이후에 죽고 4층으로 내려가기만 해도 등장 연출과
    /// 보스전이 처음부터 다시 열렸다 (경험치·보상도 다시).
    /// 마검 열쇠(MAGICAL_SWORD_KEY_INDEX)처럼 안 쓰는 번호를 따로 주고, 잡았으면 보스와 함께
    /// 등장 연출을 여는 발판도 치운다 — 연출 끝(AfterMeetKingSlime)이 보스를 도로 켜기 때문이다.
    /// </summary>
    static void BindKingSlime(GameObject map)
    {
        KingSlimeController king = map.GetComponentInChildren<KingSlimeController>(true);
        if (king == null)
            return;

        king._monsterIndex_forActive = KingSlimeActiveIndex;
        if (Managers.Data.MonsterActiveDic.ContainsKey(KingSlimeActiveIndex) == false)
            Managers.Data.MonsterActiveDic[KingSlimeActiveIndex] = true;
        if (Managers.Data.MonsterActiveDic[KingSlimeActiveIndex])
            return;

        king.gameObject.SetActive(false);
        foreach (Transform t in map.GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.layer == (int)Define.Layer.BossEventTrigger)
                t.gameObject.SetActive(false);
        }
    }

    /// <summary>보스가 하나라도 살아 있는가. 4층은 킹 슬라임(Boss 태그)에 더해, 그가 쓰러지면 연출이 세우는 분열 슬라임
    /// 셋(태그 없는 BossMonsterController)까지 다 잡아야 열린다. 분열 슬라임은 맵 데이터에 없어 불러온 판에는 다시 서지
    /// 않는다 — 그때는 없으니 킹 슬라임만 보고 연다(못 잡을 상대 때문에 갇히지 않는다).</summary>
    static bool IsBossAlive(GameObject map)
    {
        foreach (MonsterController mc in map.GetComponentsInChildren<MonsterController>(true))
        {
            if (mc.CompareTag("Boss") == false && (mc is BossMonsterController) == false)
                continue;
            bool alive;
            if (Managers.Data.MonsterActiveDic.TryGetValue(mc._monsterIndex_forActive, out alive) == false || alive)
                return true;   // 모르면 잠가 둔다
        }
        return false;          // 보스가 없는(다 잡은) 층은 잠그지 않는다
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

        Managers.Sound.FadeAndPlayBGM(key, 2f, ChapterTheme.Get(MapBuilder.GetChapter(mapId)).BgmPitch);
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

        // 빈 칸(0)이면 뺄 것이 없을 뿐, 나머지는 똑같이 간다. 예전에는 여기서 return 해서
        // 그 부위의 첫 장비(첫 부츠·목걸이)가 아래 EquipUtility.Apply 를 못 거쳐 효과가 없었다.
        if (curIdx != 0)
        {
            Managers.Game.PlayerData.Attack -= Managers.Data.EquipDic[curIdx].ATK;
            Managers.Game.PlayerData.Defence -= Managers.Data.EquipDic[curIdx].DEF;
            Managers.Game.PlayerData.MaxHP -= Managers.Data.EquipDic[curIdx].HP;
            Managers.Game.PlayerData.AttackSpeed -= Managers.Data.EquipDic[curIdx].ASPD;
            Managers.Game.PlayerData.DefenceSpeed -= Managers.Data.EquipDic[curIdx].DSPD;
            Managers.Game.PlayerData.Critical -= Managers.Data.EquipDic[curIdx].CRI;
            Managers.Game.PlayerData.CriticalAttack -= Managers.Data.EquipDic[curIdx].CRIATK;
            Managers.Game.PlayerData.MoveSpeed -= Managers.Data.EquipDic[curIdx].MSPD;
        }

        Managers.Game.PlayerData.Attack += Managers.Data.EquipDic[idx].ATK;
        Managers.Game.PlayerData.Defence += Managers.Data.EquipDic[idx].DEF;
        Managers.Game.PlayerData.MaxHP += Managers.Data.EquipDic[idx].HP;
        Managers.Game.PlayerData.AttackSpeed += Managers.Data.EquipDic[idx].ASPD;
        Managers.Game.PlayerData.DefenceSpeed += Managers.Data.EquipDic[idx].DSPD;
        Managers.Game.PlayerData.Critical += Managers.Data.EquipDic[idx].CRI;
        Managers.Game.PlayerData.CriticalAttack += Managers.Data.EquipDic[idx].CRIATK;
        Managers.Game.PlayerData.MoveSpeed += Managers.Data.EquipDic[idx].MSPD;

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

        // 층 번호부터 옮긴다. 카메라 경계·몬스터 배율·계단의 층 계산이 전부 CurStageid 를 읽는다.
        // 예전에는 맨 끝에 옮겨서 경계가 옛 층으로 잡히고, 아래의 스폰 지점도 챕터 첫 층 것이라
        // 몸은 챕터 첫 층에 서고 게임은 고른 층이라고 여겼다 — 그 차이가 계단마다 이어졌다.
        PlayerData.CurStageid = stageId;
        OnStaticResolution = stageId == 2;
        GenerateMap(stageId);

        Transform arrival = ArrivalPoint(stageId);
        Vector3 pos = arrival != null ? arrival.position : Player.transform.position;
        Player.transform.position = pos;
        Player._cellPos = pos;

        if (MainCamera != null)
        {
            CameraController cam = MainCamera.GetComponentInChildren<CameraController>();
            if (cam != null)
                cam.SetupCameraConfiner();
        }

        if (OnPortalAction != null)
            OnPortalAction.Invoke();
        if (GameScene != null)
            GameScene.Refresh();
        EnterFloor(false);
        return true;
    }

    /// <summary>
    /// 그 층에 내려설 자리. 그 층 안의 스폰 지점, 없으면(손수 만든 2·3층) 내려가는 계단,
    /// 그것도 없으면 아무 계단. SpawnPoints 는 챕터 전체를 긁은 것이라 [0] 은 늘 챕터 첫 층이다.
    /// </summary>
    public Transform ArrivalPoint(int stageId)
    {
        GameObject map;
        if (Maps.TryGetValue(stageId, out map) == false || map == null)
            return null;

        foreach (Transform t in map.GetComponentsInChildren<Transform>(true))
        {
            if (t.CompareTag("SpawnPoint"))
                return t;
        }

        PortalController any = null;
        foreach (PortalController p in map.GetComponentsInChildren<PortalController>(true))
        {
            if (p._portalType == PortalController.Type.DownStairs)
                return p.transform;
            if (any == null && p._portalType != PortalController.Type.None)
                any = p;
        }
        return any != null ? any.transform : null;
    }

    /// <summary>
    /// 층에 들어섰다 — 계단·워프가 다 끝나 층 번호와 자리가 확정된 뒤에 한 번 부른다.
    /// 체크포인트를 쓰고 그 층의 사본을 남긴 다음 알린다 (GameEvents.FloorEntered).
    /// 연출 도중에는 쓰지 않는다: 반쯤 진행된 연출의 상태가 체크포인트에 굳으면 안 된다.
    /// </summary>
    public void EnterFloor(bool firstVisit)
    {
        if (OnDirect == false)
        {
            SaveGame();
            if (LastSaveError == null)
            {
                try { SaveStore.WriteHistory(SaveStore.DirectoryPath, PlayerData.CurStageid); }
                catch (Exception ex) when (SaveStore.IsSaveError(ex))
                {
                    Debug.LogWarning($"[Save] Floor copy was not written: {ex.Message}");
                }
            }
        }
        GameEvents.RaiseFloorEntered(PlayerData.CurStageid, firstVisit);
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
        // 잡고 두고 온 보상은 여기서 줍는다 — 안 그러면 체크포인트에 "잡았다" 만 남고 보상은 빠진다.
        CollectDrops();

        // 칸 자리(_cellPos)를 적는다. transform 은 레버를 당기는 동안 레버 쪽으로 반 칸 떠 있고
        // 걷는 도중에는 칸 사이에 있다 — 그 자리를 적으면 이어하기 때 칸에서 어긋나 선다.
        Vector3 pos = Managers.Game.Player != null ? Managers.Game.Player._cellPos : Vector3.zero;
        Managers.Game.PlayerData.CurPosition = new Data.MyVector3 { X = pos.x, Y = pos.y, Z = pos.z };

        try
        {
            var snapshot = new SaveStore.Snapshot {
                Version = SaveStore.Version, ContentHash = CurrentContentHash(), Player = PlayerData,
                Active = Managers.Data.CaptureActive(), Progress = CaptureProgress(),
                PlayTime = Mathf.Max(PlayTime, PlayerPrefs.GetFloat("PLAYTIME", 0)),
                AttackCount = AttackCount, DefenceCoolTime = DefenceCoolTime
            };
            SaveStore.Write(SaveStore.DirectoryPath, snapshot);
            LastSaveError = null;
        }
        catch (Exception ex) when (SaveStore.IsSaveError(ex))
        {
            LastSaveError = ex.Message;
            Debug.LogError($"[Save] Checkpoint was not written: {ex.Message}");
        }
    }

    public string LastSaveError { get; private set; }
    public bool HasSave => SaveStore.Exists(SaveStore.DirectoryPath) ||
        File.Exists(Path.Combine(SaveStore.DirectoryPath, "SaveData.json"));

    // MapData 는 5MB 가 넘는다. 층마다 저장하면서 매번 해시하면 계단마다 끊긴다. 실행 중엔 안 바뀐다.
    string _contentHash;
    string CurrentContentHash() =>
        _contentHash ??= SaveStore.Hash(Managers.Resource.Load<TextAsset>("MapData").text);

    Dictionary<string, int> CaptureProgress() => SaveStore.ProgressKeys.ToDictionary(
        key => key, key => PlayerPrefs.GetInt(key, key == "ISFIRST" ? 1 : 0));

    void ValidateCheckpoint(SaveStore.Snapshot snapshot)
    {
        // Level+1 을 요구하면 표의 끝(Lv115)에서 저장한 체크포인트가 영영 안 읽힌다.
        if (snapshot.ContentHash != CurrentContentHash())
            throw new InvalidDataException("This checkpoint belongs to a different dungeon layout.");
        if (!Managers.Data.StageInfoDic.ContainsKey(snapshot.Player.CurStageid) ||
            !Managers.Data.PlayerDic.ContainsKey(snapshot.Player.Level))
            throw new InvalidDataException("Checkpoint stage or level is unavailable.");
    }

    /// <summary>체크포인트를 불러 지금 판에 입힌다. file 이 null 이면 Checkpoint.json(없으면 .bak),
    /// 아니면 SaveStore.History 가 준 층별 사본. 읽지 못하면 아무것도 바꾸지 않고 false.</summary>
    public bool LoadGame(string file = null)
    {
        LastSaveError = null;
        SaveStore.Snapshot snapshot;
        string error;
        bool read = file == null
            ? SaveStore.TryRead(SaveStore.DirectoryPath, out snapshot, out error, ValidateCheckpoint)
            : SaveStore.TryReadFile(file, out snapshot, out error, ValidateCheckpoint);
        if (!read)
        {
            // Never mix an incomplete new checkpoint with old object files.
            if (file != null || SaveStore.Exists(SaveStore.DirectoryPath)) { LastSaveError = error; return false; }
            string legacy = Path.Combine(SaveStore.DirectoryPath, "SaveData.json");
            if (!File.Exists(legacy)) return false;
            try
            {
                string legacyMap = Path.Combine(SaveStore.DirectoryPath, "MapData.json");
                if (!File.Exists(legacyMap) || !Newtonsoft.Json.Linq.JToken.DeepEquals(
                    Newtonsoft.Json.Linq.JToken.Parse(File.ReadAllText(legacyMap)),
                    Newtonsoft.Json.Linq.JToken.Parse(Managers.Resource.Load<TextAsset>("MapData").text)))
                    throw new InvalidDataException("Legacy checkpoint uses a different dungeon layout.");
                snapshot = new SaveStore.Snapshot {
                    Version = SaveStore.Version, ContentHash = CurrentContentHash(),
                    Player = JsonConvert.DeserializeObject<CurPlayerData>(File.ReadAllText(legacy), SaveStore.Settings),
                    Active = Managers.Data.ReadLegacyActive(SaveStore.DirectoryPath),
                    Progress = CaptureProgress(), PlayTime = PlayerPrefs.GetFloat("PLAYTIME", 0)
                };
                SaveStore.Validate(snapshot);
                ValidateCheckpoint(snapshot);
                SaveStore.Write(SaveStore.DirectoryPath, snapshot);
            }
            catch (Exception ex) when (SaveStore.IsSaveError(ex))
            {
                LastSaveError = ex.Message;
                Debug.LogWarning($"[Save] Cannot restore checkpoint: {ex.Message}");
                return false;
            }
        }
        PlayerData = snapshot.Player;
        Managers.Data.ApplyActive(snapshot.Active);
        foreach (var key in SaveStore.ProgressKeys)
        {
            int saved = snapshot.Progress.TryGetValue(key, out int value) ? value : 0;
            // 죽은 횟수·걸음 수는 기록이다. 체크포인트로 돌아가도 줄지 않는다 — 예전에는 죽어서
            // 불러올 때마다 저장 시점 값으로 돌아가 죽은 횟수가 늘지 않았다.
            bool tally = key == "DEATHCOUNT" || key == "MOVECOUNT";
            PlayerPrefs.SetInt(key, tally ? Mathf.Max(saved, PlayerPrefs.GetInt(key, 0)) : saved);
        }
        PlayTime = Mathf.Max(snapshot.PlayTime, PlayerPrefs.GetFloat("PLAYTIME", 0));
        PlayerPrefs.SetFloat("PLAYTIME", PlayTime);
        AttackCount = snapshot.AttackCount;
        DefenceCoolTime = snapshot.DefenceCoolTime;
        KeyInventory.InitKeyInventory();
        ResetTransientState();
        // 배속은 낀 목걸이가 정한다. 불러온 판에 없는 목걸이의 배속이 남지 않게 되돌린 뒤 다시 건다.
        GameSpeed = 1;
        EquipUtility.Apply();

        if (file != null)
        {
            // 고른 사본이 이제 지금의 체크포인트다. 여기서 죽거나 이어하기를 눌러도 이 자리로 온다.
            snapshot.Progress = CaptureProgress();
            snapshot.PlayTime = PlayTime;
            try { SaveStore.Write(SaveStore.DirectoryPath, snapshot); }
            catch (Exception ex) when (SaveStore.IsSaveError(ex))
            {
                LastSaveError = ex.Message;
                Debug.LogError($"[Save] Checkpoint was not written: {ex.Message}");
            }
        }
        return true;
    }

    /// <summary>
    /// 체크포인트에서 다시 선다 (게임오버, "이 층 다시" 같은 것). file 은 LoadGame 과 같다.
    /// 게임오버가 하던 그대로 GameScene 을 다시 올린다. 읽지 못하면 아무것도 바꾸지 않고 false.
    /// </summary>
    public bool RestartFromCheckpoint(string file = null)
    {
        if (LoadGame(file) == false)
            return false;

        // 불러온 상태로 옛 맵이 한 프레임 비치지 않게 가린다.
        if (ParentMap != null)
            ParentMap.SetActive(false);
        if (GameScene != null)
            GameScene.gameObject.SetActive(false);
        Managers.Scene.LoadScene(Define.Scene.GameScene);
        return true;
    }

    /// <summary>판이 바뀔 때(불러오기·새 게임) 지난 장면의 흐름 표시를 걷는다.
    /// 하나라도 남으면 새 장면에서 입력이 막히거나, 죽은 채로 다음 전투가 게임오버로 끝난다.</summary>
    void ResetTransientState()
    {
        OnBattle = OnConversation = OnLever = OnFade = OnDirect = OnInteract = OnInputLock = false;
        OnStaticResolution = false;
        IsPlayerDead = false;
        // 분열 슬라임은 맵 데이터에 없이 그때 태어난다. 불러오면 없으니 센 것도 처음부터다 —
        // 안 그러면 다시 싸울 때 셋을 다 잡기 전에 4층 출구가 열렸다.
        TotalKillSplitSlime = 0;
        FightGate.Reset();
    }

    /// <summary>메모리 위의 판을 새로 세운다. 파일은 건드리지 않는다.
    ///
    /// PlayerData 를 통째로 새로 만든다. Clear() 는 스탯만 되돌려서, 타이틀이 켜자마자 불러 둔
    /// 세이브의 열쇠·장비 칸이 새 게임으로 그대로 넘어갔다 (열쇠 칸 HUD 는 꺼진 채로).</summary>
    void ResetRun()
    {
        Managers.Data.ResetActiveDic();
        PlayerData = new CurPlayerData();
        PlayerData.Clear();
        KeyInventory.InitKeyInventory();
        AttackCount = 0;
        DefenceCoolTime = 0;
        PlayTime = 0;
        GameSpeed = 1;
        ResetTransientState();
    }

    #endregion

    #region ForData
    public Define.ScriptType ScriptType = Define.ScriptType.None;
    public Define.ScreenType ScreenType = Define.ScreenType.None;

    public void DeleteGameData()
    {
        SaveStore.Delete(SaveStore.DirectoryPath);
        File.Delete(Path.Combine(SaveStore.DirectoryPath, "SaveData.json"));
        StoryDirector.ClearSeen();      // 새 판은 이야기도 처음부터 (StorySeen.json)
        LastSaveError = null;
        //PlayerPrefs.DeleteAll();
        // ISFIRST를 지워야하나? 진짜 최초는 아닌데
        PlayerPrefs.DeleteKey("ISFIRST");
        PlayerPrefs.DeleteKey("ISFIRSTBATTLE");
        PlayerPrefs.DeleteKey("ISFIRSTLEVER");
        PlayerPrefs.DeleteKey("ISFIRSTRECOVERY");
        PlayerPrefs.DeleteKey("ISFIRSTKEY");
        PlayerPrefs.DeleteKey(UI_GameScene.KeysHintPref);   // 마검의 눈 키 안내(M 도감·V 예측)
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

        ResetRun();
        Debug.Log("Complete DeleteGameData");
    }

    #endregion

    public void Init()
    {
        _path = Application.persistentDataPath + "/SaveData.json";

        // Initialize a fresh in-memory run without deleting an unreadable checkpoint.
        if (LoadGame() == false)
            ResetRun();
    }
}
