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
            GameObject map = Managers.Resource.Instantiate($"Dungeon_{Managers.Data.StageInfoDic[i].DungeonID}", ParentMap.transform);
            map.transform.position = new Vector3(count * 100, 0f, 0f);
            Maps.Add(i, map);
            RefreshMap(i);
            count++;
        }
        DropItems = new GameObject(name: "DropItems");
        DropItems.transform.parent = ParentMap.transform;
        Portals = ParentMap.GetComponentsInChildren<PortalController>();
        SpawnPoints = ParentMap.GetComponentsInChildren<Transform>().Where(child => child.CompareTag("SpawnPoint")).ToArray();
        BossRoomId = Managers.Data.StageInfoDic.Where(pair => pair.Value.Type == Define.DungeonType.Boss)
                    .Select(pair => pair.Key).FirstOrDefault();

        if (Managers.Game.PlayerData.CurStageid == 2)
        {
            Managers.Game.OnStaticResolution = true;
        }
        //MainCamera.GetComponentInChildren<CustomCameraLimiter>().SetBG();

        Managers.Game.Portals[Managers.Game.Portals.Length - 1].transform.parent.gameObject.SetActive(false);
        Managers.Resource.Instantiate($"Effects_{CurChapter}", ParentMap.transform);
    }

    public MonsterController GetBoss()
    {
        int bossRoomIdx = BossRoomId - GetChapterCount(PlayerData.CurStageid).Key;
        MonsterController[] monsters = Maps[bossRoomIdx].GetComponentsInChildren<MonsterController>();
        for (int i = 0; i < monsters.Length; i++)
            if (monsters[i].gameObject.tag == "Boss")
                return monsters[i];
        return null;
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
            case 3:
                curIdx = Managers.Game.PlayerData.CurRing;
                Managers.Game.PlayerData.CurRing = idx;
                break;
            case 4:
                //curIdx = Managers.Game.CurPlayerData.CurSword;
                break;
            case 5:
                curIdx = Managers.Game.PlayerData.CurShoes;
                Managers.Game.PlayerData.CurShoes = idx;
                break;
            case 6:
                //curIdx = Managers.Game.CurPlayerData.CurSword;
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

        if (Managers.Game.GameScene != null)
            Managers.Game.GameScene.Refresh();
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
