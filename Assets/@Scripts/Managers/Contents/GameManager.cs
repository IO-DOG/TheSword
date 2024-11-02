using Cinemachine;
using Data;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
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
    public bool OnInteract = false;
    public bool OnMeetKingSlime = false;

    public int CurEventID;
    public int TotalKillSplitSlime = 0;

    public GameObject CurInteractObject;
    public Light DirectionalLight;

    public Transform BossRoom;

    public PlayerController Player; // ������ ������ ����
    public MonsterController Monster; // ������ ������ ����
    public CurMonsterData MonsterData = new CurMonsterData(); // ���� ���� ���� ����
    public ContinueData CurPlayerData = new ContinueData(); // ���� ���� �÷��̾� ����
    public CurConsumableItemData ConsumableItemData = new CurConsumableItemData(); // Current Consumable Item Data
    public KeyInventory KeyInventory = new KeyInventory(); //Inventory

    public Action<float> OnFadeAction;

    public Action OnBattleAction;
    public Action OnBattleDataRefreshAction;
    public Action OnBattleCreatureDefeceAction;
    public Action OnBattleCreatureDamagedAction;
    public Action OnBattlePlayerDefeceAction;
    public Action OnBattlePlayerDamagedAction;
    public Action OnKingSlimeDeadAction;

    public Action OnPortalAction;

    public Texture2D _screenShot = null;
    public Sprite _screenShot2 = null;

    public Camera MainCamera;
    public GameObject Map;
    public GameObject Monsters;
    public GameObject Items;
    public GameObject Lights;

    #region CurPlayerData
    public bool playerControllLock = false;

    public class ContinueData
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
                    LevelUp();
                }
            }
        }
        public float MaxHP { get; set; }
        public float CurHP { get; set; }
        public float Attack { get; set; }
        public float Defence { get; set; }
        public float AttackSpeed { get; set; }
        public float DefenceSpeed { get; set; }
        public float Critical { get; set; }
        public float CriticalAttack { get; set; }
        public float MoveSpeed { get; set; }
        public bool IsDefence { get; set; }
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
    }
    #endregion

    #region CurMonsterData
    public class CurMonsterData
    {
        public int id { get; set; }
        public int Chapter { get; set; }
        public string Class { get; set; }
        public string Name { get; set; }
        public int Feature { get; set; }
        public string Image { get; set; }
        public float MaxHP { get; set; }
        public float CurHP { get; set; }
        public float Attack { get; set; }
        public float Defence { get; set; }
        public float AttackSpeed { get; set; }
        public float DefenceSpeed { get; set; }
        public float Critical { get; set; }
        public float CriticalAttack { get; set; }
        public float RewardExp { get; set; }
        public int RewardItem { get; set; }
        public string IdleAnimStr { get; set; }
        public string AttackAnimStr { get; set; }
        public string BattleParticleAttack { get; set; }
        public string BattleParticleHit { get; set; }
        public int MonsterNameId { get; set; }
        public int MonsterDescId { get; set; }
        public bool IsDefence { get; set; }
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

    public static void LevelUp()
    {
        Managers.Game.CurPlayerData.MaxHP += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].MaxHP;
        Managers.Game.CurPlayerData.CurHP += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].MaxHP;
        Managers.Game.CurPlayerData.Attack += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].Attack;
        Managers.Game.CurPlayerData.Defence += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].Defence;
        Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].AttackSpeed;
        Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].DefenceSpeed;
        Managers.Game.CurPlayerData.Critical += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].Critical;
        Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].CriticalAttack;
        Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].MoveSpeed;
    }

    public void SwapEquip(int curIdx, int idx)
    {
        if (curIdx == 0)
        {
            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[curIdx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[curIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[curIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[curIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[curIdx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[curIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[curIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[curIdx].MSPD;
            return;
        }
        else
        {
            Managers.Game.CurPlayerData.Attack -= Managers.Data.EquipDic[curIdx].ATK;
            Managers.Game.CurPlayerData.Defence -= Managers.Data.EquipDic[curIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP -= Managers.Data.EquipDic[curIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed -= Managers.Data.EquipDic[curIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed -= Managers.Data.EquipDic[curIdx].DSPD;
            Managers.Game.CurPlayerData.Critical -= Managers.Data.EquipDic[curIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack -= Managers.Data.EquipDic[curIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed -= Managers.Data.EquipDic[curIdx].MSPD;

            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[idx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[idx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[idx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[idx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[idx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[idx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[idx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[idx].MSPD;
        }

        Managers.Game.GameScene.Refresh();
    }

    public void SwapEquip(int idx)
    {
        int type = Managers.Data.EquipDic[idx].Type;
        int curIdx = 1;
        switch (type)
        {
            case 1:
                curIdx = Managers.Game.CurPlayerData.CurSword;
                break;
            case 2:
                curIdx = Managers.Game.CurPlayerData.CurShield;
                break;
            case 3:
                curIdx = Managers.Game.CurPlayerData.CurRing;
                break;
            case 4:
                //curIdx = Managers.Game.CurPlayerData.CurSword;
                break;
            case 5:
                curIdx = Managers.Game.CurPlayerData.CurShoes;
                break;
            case 6:
                //curIdx = Managers.Game.CurPlayerData.CurSword;
                break;
            default:
                break;
        }

        if (curIdx == 0)
        {
            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[curIdx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[curIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[curIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[curIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[curIdx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[curIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[curIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[curIdx].MSPD;
            return;
        }
        else
        {
            Managers.Game.CurPlayerData.Attack -= Managers.Data.EquipDic[curIdx].ATK;
            Managers.Game.CurPlayerData.Defence -= Managers.Data.EquipDic[curIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP -= Managers.Data.EquipDic[curIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed -= Managers.Data.EquipDic[curIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed -= Managers.Data.EquipDic[curIdx].DSPD;
            Managers.Game.CurPlayerData.Critical -= Managers.Data.EquipDic[curIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack -= Managers.Data.EquipDic[curIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed -= Managers.Data.EquipDic[curIdx].MSPD;

            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[idx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[idx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[idx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[idx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[idx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[idx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[idx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[idx].MSPD;
        }

        Managers.Game.GameScene.Refresh();
    }

    #endregion

    #region Save&Load

    string _path;

    public void SaveGame()
    {
        if (Managers.Game.Player == null)
        {
            Managers.Game.CurPlayerData.CurPosition = new Data.MyVector3
            {
                X = 0,
                Y = 0,
                Z = 0,
            };
        }
        else
        {
            Managers.Game.CurPlayerData.CurPosition = new Data.MyVector3
            {
                X = Managers.Game.Player.transform.position.x,
                Y = Managers.Game.Player.transform.position.y,
                Z = Managers.Game.Player.transform.position.z,
            };
        }

        string jsonStr = JsonConvert.SerializeObject(CurPlayerData, Formatting.Indented);
        File.WriteAllText(_path, jsonStr);

        //List<MapData> mapData = new List<MapData>(Managers.Data.MapDic.Values);
        //var mapContainer = new { Maps = mapData };
        //string MapDicJsonStr = JsonConvert.SerializeObject(mapContainer, new JsonSerializerSettings
        //{
        //    TypeNameHandling = TypeNameHandling.Auto
        //});
        //File.WriteAllText(Application.dataPath + "/@Resources/Data/JsonData/MapData.json", MapDicJsonStr);

        #region ActiveDic
        string monsterActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.MonsterActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/MonsterActiveData.json", monsterActiveDicJsonStr);
        string bossMonsterActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.BossMonsterActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/BossMonsterActiveData.json", bossMonsterActiveDicJsonStr);
        string cItemActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.CItemActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/CItemActiveData.json", cItemActiveDicJsonStr);
        string eItemActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.EItemActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/EItemActiveData.json", eItemActiveDicJsonStr);
        string doorActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.DoorActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/DoorActiveData.json", doorActiveDicJsonStr);
        string pillarActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.PillarActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/PillarActiveData.json", pillarActiveDicJsonStr);
        string leverActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.LeverActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/LeverActiveData.json", leverActiveDicJsonStr);
        #endregion
    }

    public bool LoadGame()
    {
        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
        {
            string path = Application.persistentDataPath + "/SaveData.json";
            if (File.Exists(path))
                File.Delete(path);

            int level = 1;
            Managers.Game.CurPlayerData.Level = Managers.Data.PlayerDic[level].id;
            Managers.Game.CurPlayerData.CurExp = 0;
            Managers.Game.CurPlayerData.MaxHP = Managers.Data.PlayerDic[level].MaxHP;
            Managers.Game.CurPlayerData.CurHP = Managers.Data.PlayerDic[level].MaxHP;
            Managers.Game.CurPlayerData.Attack = Managers.Data.PlayerDic[level].Attack;
            Managers.Game.CurPlayerData.Defence = Managers.Data.PlayerDic[level].Defence;
            Managers.Game.CurPlayerData.AttackSpeed = Managers.Data.PlayerDic[level].AttackSpeed;
            Managers.Game.CurPlayerData.DefenceSpeed = Managers.Data.PlayerDic[level].DefenceSpeed;
            Managers.Game.CurPlayerData.Critical = Managers.Data.PlayerDic[level].Critical;
            Managers.Game.CurPlayerData.CriticalAttack = Managers.Data.PlayerDic[level].CriticalAttack;
            Managers.Game.CurPlayerData.MoveSpeed = Managers.Data.PlayerDic[level].MoveSpeed;
            Managers.Game.CurPlayerData.IsDefence = false;
            Managers.Game.CurPlayerData.CurStageid = 0;

            KeyInventory.InitKeyInventory();

            for (int i = 0; i < 10; ++i)
            {
                Managers.Game.CurPlayerData.Inventory.Add(new List<int>());
            }
            // 오픈하면 1로 변경해야함.
            PlayerPrefs.SetInt("ISOPENSWORD", 0);
            PlayerPrefs.SetInt("ISOPENPORTAL", 0);

            return false;
        }

        if (File.Exists(_path) == false)
        {
            Debug.Log("�÷��̾� ������ �ε� ����");
            return false;
        }

        string fileStr = File.ReadAllText(_path);
        ContinueData data = JsonConvert.DeserializeObject<ContinueData>(fileStr);
        if (data != null)
        {
            CurPlayerData = data;

            #region Active Dic
            string monsterActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/MonsterActiveData.json");
            Dictionary<int, bool> monsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(monsterActiveDicFile);
            Managers.Data.MonsterActiveDic = monsterActiveDic;
            string bossMonsterActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/BossMonsterActiveData.json");
            Dictionary<int, bool> bossMonsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(bossMonsterActiveDicFile);
            Managers.Data.BossMonsterActiveDic = bossMonsterActiveDic;
            string cItemActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/CItemActiveData.json");
            Dictionary<int, bool> cItemActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(cItemActiveDicFile);
            Managers.Data.CItemActiveDic = cItemActiveDic;
            string eItemActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/EItemActiveData.json");
            Dictionary<int, bool> eItemActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(eItemActiveDicFile);
            Managers.Data.EItemActiveDic = eItemActiveDic;
            string doorActiveDicFile = File.ReadAllText(Application.persistentDataPath+ "/DoorActiveData.json");
            Dictionary<int, bool> doorActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(doorActiveDicFile);
            Managers.Data.DoorActiveDic = doorActiveDic;
            string pillarActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/PillarActiveData.json");
            Dictionary<int, bool> pillarActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(pillarActiveDicFile);
            Managers.Data.PillarActiveDic = pillarActiveDic;
            string leverActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/LeverActiveData.json");
            Dictionary<int, bool> leverActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(leverActiveDicFile);
            Managers.Data.LeverActiveDic = leverActiveDic;
            #endregion
            Debug.Log("�÷��̾� ������ �ε� �Ϸ�");
        }

        KeyInventory.InitKeyInventory();

        return true;
    }

    #endregion

    #region Map Instantiate


    string GetBossRoomName(string chapter)
    {
        string bossRoomName = "";
        foreach (KeyValuePair<int, Data.StageInfoData> entry in Managers.Data.StageInfoDic)
        {
            if (entry.Key == Managers.Data.GetChapterCount(chapter) - 1)
                break;

            if(entry.Value.BossRoom != "-")
                bossRoomName = entry.Value.BossRoom;
        }
        return bossRoomName;
    }

    public void InstantiateMap(int key)
    {
        int count = 0;
        string chapter = Managers.Data.StageInfoDic[key].DungeonID.Substring(0, 2) + "_"; // ex 00_
        int maxCount = Managers.Data.GetChapterCount(chapter);
        bool isSpawned = false;
        InteractObjectController interacts = null;
        foreach (KeyValuePair<string, Data.MapData> entry in Managers.Data.MapDic)
        {
            Data.MapData mapData = entry.Value;
            string stageName = entry.Key.Replace("Dungeon_", "");

            if (!entry.Key.Contains(chapter))
                continue;

            GameObject parent = GameObject.Find("Map");
            if(parent == null)
                parent = new GameObject() { name = "Map" };

            GameObject map = Managers.Resource.Instantiate("Dungeon_" + chapter + count.ToString("D3"));

            Door[] doors = map.GetComponentsInChildren<Door>();
            Pillar[] pillars = map.GetComponentsInChildren<Pillar>();
            interacts = map.GetComponentInChildren<InteractObjectController>();

            GameObject items = Util.FindChildByName(map.transform, "Items").gameObject;
            GameObject monsters = Util.FindChildByName(map.transform, "Monsters").gameObject;
            GameObject bossMonsters = Util.FindChildByName(map.transform, "BossMonsters").gameObject;
            GameObject deco = Util.FindChildByName(map.transform, "Deco").gameObject;
            bossMonsters.transform.localScale = new Vector3(1, 2, 1);

            map.transform.localPosition += new Vector3(count * 100, 0, 0);
            map.transform.parent = parent.transform;

            foreach (Data.TileData tile in mapData.Tiles)
            {
                if (tile is DoorData doorTile)
                {
                    foreach(Door child in doors)
                    {
                        child.GetComponentInChildren<Door>()._doorIndex_forActive = doorTile.TotalCount;
                        if (Managers.Data.DoorActiveDic[doorTile.TotalCount] == false)
                            child.transform.parent.gameObject.SetActive(false);
                    }
                }
                else if (tile is PillarData pillarTile)
                {
                    foreach (Pillar child in pillars)
                    {
                        child.GetComponentInChildren<Pillar>()._pillarIndex_forActive = pillarTile.TotalCount;
                        if (Managers.Data.PillarActiveDic[pillarTile.TotalCount] == false)
                            child._pillar.gameObject.SetActive(false);
                    }
                }
                //else if (tile is StairsData stairsTile)
                //{
                //    if (stairsTile.StairsType == (int)Define.Stairs.BossRoom)
                //    {
                //        GameObject go = Managers.Resource.Instantiate("BossPortal", items.transform);
                //        go.transform.localPosition = new Vector3(stairsTile.Position.X, -0.32f, stairsTile.Position.Z);
                //    }
                //}
                else if (tile is Occupied citemTile && citemTile.Type == (int)Define.OccupiedType.CItem)
                {
                    GameObject item = Managers.Resource.Instantiate("ConsumableItem", items.transform);
                    item.transform.localPosition = new Vector3 (citemTile.Position.X, citemTile.Position.Y, citemTile.Position.Z);
                    item.GetComponent<ConsumableItem>().id = citemTile.Index;
                    item.name = $"CItem{citemTile.TotalCount}";
                    item.GetComponent<ConsumableItem>()._itemIndex_forActive = citemTile.TotalCount;

                    if (Managers.Data.CItemActiveDic[citemTile.TotalCount] == false)
                        item.SetActive(false);
                }
                else if (tile is Occupied eitemTile && eitemTile.Type == (int)Define.OccupiedType.EItem)
                {
                    GameObject item = Managers.Resource.Instantiate("EquipItem", items.transform);
                    item.transform.localPosition = new Vector3(eitemTile.Position.X, eitemTile.Position.Y, eitemTile.Position.Z);
                    item.GetComponent<Equip>().Id = eitemTile.Index;
                    item.name = $"EItem{eitemTile.TotalCount}";
                    item.GetComponent<Equip>()._itemIndex_forActive = eitemTile.TotalCount;

                    if (Managers.Data.EItemActiveDic[eitemTile.TotalCount] == false)
                        item.SetActive(false);
                }
                else if (tile is Occupied monsterTile && monsterTile.Type == (int)Define.OccupiedType.Monster)
                {
                    GameObject monster = Managers.Resource.Instantiate("Monster", monsters.transform);
                    monster.transform.localPosition = new Vector3(monsterTile.Position.X, monsterTile.Position.Y, monsterTile.Position.Z);
                    monster.transform.localScale = monsters.transform.localPosition + new Vector3(0.8f, 0.8f, 1f);
                    monster.GetComponent<MonsterController>().id = monsterTile.Index;
                    monster.name = $"monster{monsterTile.TotalCount}";
                    monster.GetComponent<MonsterController>()._monsterIndex_forActive = monsterTile.TotalCount;
                   
                    if (Managers.Data.MonsterActiveDic[monsterTile.TotalCount] == false)
                        monster.SetActive(false);
                }
                else if (tile is Occupied bossMonsterTile && bossMonsterTile.Type == (int)Define.OccupiedType.Boss)
                {
                    GameObject boss = Managers.Resource.Instantiate("BossMonster", bossMonsters.transform);
                    boss.transform.localPosition = new Vector3(bossMonsterTile.Position.X, bossMonsterTile.Position.Y, bossMonsterTile.Position.Z);
                    int tileIndex = bossMonsterTile.Index;
                    switch (tileIndex)
                    {
                        case 0:
                            boss.GetComponent<BossMonsterController>().id = Define.KingSlime;
                            boss.gameObject.name = "KingSlime";
                            break;
                        default:
                            break;
                    }
                    Debug.Log($"bossMonsterTile.index : {bossMonsterTile.Index}");
                    boss.name = $"bossMonster{bossMonsterTile.TotalCount}";
                    boss.GetComponent<BossMonsterController>()._monsterIndex_forActive = bossMonsterTile.TotalCount;

                    int id = boss.GetComponent<BossMonsterController>().id;
                    Debug.Log($"bossMonsterId : {id}");
                    string name = Managers.Data.MonsterDic[id].Name;
                    switch (id)
                    {
                        case Define.KingSlime:
                            boss.AddComponent<BlackSlimeController>();
                            boss.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);
                            boss.transform.localPosition += new Vector3(0, 1.7f, -1.84f);
                            boss.GetOrAddComponent<BoxCollider>().center = new Vector3(0, -0.4f, 0);
                            boss.GetOrAddComponent<BoxCollider>().size = new Vector3(1.2f, 1.1f, -0.32f);
                            break;
                        default:
                            break;
                    }

                    if (Managers.Data.BossMonsterActiveDic[bossMonsterTile.TotalCount] == false)
                        bossMonsters.SetActive(false);
                }
                else if (tile is LeverData leverTile)
                {
                    GameObject lever = Managers.Resource.Instantiate($"Tilemap_{tile.PrefabID}", items.transform);
                    lever.GetComponentInChildren<Lever>()._leverIndex_forActive = leverTile.TotalCount;
                    lever.name = $"Lever";
                    lever.transform.localPosition = new Vector3(leverTile.Position.X, -0.08f, leverTile.Position.Z);

                    if (Managers.Data.LeverActiveDic[leverTile.TotalCount] == false)
                    {
                        lever.GetComponentInChildren<Lever>()._IsActive = true;
                        lever.GetComponentInChildren<Lever>().SetActive();
                        lever.GetComponentInChildren<Lever>().Play(0.0f);
                    }
                }
                else
                {
                    if (!isSpawned && tile.PrefabID == (int)Define.TileType.SpawnPoint && stageName != GetBossRoomName(chapter))
                    {
                        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
                        {
                            Managers.Game.Player.transform.position = new Vector3(tile.Position.X + count * 100, 0f, tile.Position.Z);
                            Managers.Game.Player._cellPos = Managers.Game.Player.transform.position;

                            isSpawned = true;
                        }
                        else
                        {
                            Managers.Game.Player.transform.position = new Vector3(Managers.Game.CurPlayerData.CurPosition.X, 0, Managers.Game.CurPlayerData.CurPosition.Z);
                            Managers.Game.Player._cellPos = Managers.Game.Player.transform.position;

                            isSpawned = true;
                        }
                    }
                }
            }

            if (interacts != null && Managers.Game.CurPlayerData.IsContractedSword == true)
            {
                interacts.transform.gameObject.SetActive(false);
            }

            Items = items;
            Monsters = monsters;
            //Lights = lights;

            items.transform.localPosition = items.transform.localPosition + new Vector3(0f, 0f, 0f);
            monsters.transform.localPosition = monsters.transform.localPosition + new Vector3(0f, 0f, -0.05f);
            bossMonsters.transform.localPosition = bossMonsters.transform.localPosition + new Vector3(0f, 0f, 0f);

            count++;

            if (count == maxCount - 1)
                break;

            MainCamera.GetComponentInChildren<CameraController>().ChangeView(Define.CAMERA_ANGLE, Managers.Game.Monsters);
            MainCamera.GetComponentInChildren<CameraController>().ChangeView(Define.CAMERA_ANGLE, Managers.Game.Items);
            //InstantiateLights(key, lights.transform);
        }
        MainCamera.GetComponentInChildren<CameraController>().ChangeView(Define.CAMERA_ANGLE, Managers.Game.Player.gameObject);
        GameObject effects = Managers.Resource.Instantiate($"Effects_{chapter.Replace("_", "")}");
        CameraController.SetConfinerBounds();
    }

    void InstantiateLights(string DGName, Transform parent)
    {
        foreach (KeyValuePair<string, DungeonDecoData> entry in Managers.Data.DecoDic)
        {
            string key = entry.Key;
            Data.DungeonDecoData lightList = entry.Value;

            if (!key.Contains(DGName))
                continue;

            foreach (DecoData data in lightList.DecoData)
            {
                if (data.LightType == (int)Define.DecoType.Torch)
                {
                    GameObject go = Managers.Resource.Instantiate($"Deco_{Define.DecoType.Torch.ToString()}", parent.transform);
                    go.transform.localPosition = new Vector3(data.Position.X, data.Position.Y, data.Position.Z);
                }
                else if (data.LightType == (int)Define.DecoType.FireBowl)
                {
                    GameObject go = Managers.Resource.Instantiate($"Deco_{Define.DecoType.FireBowl.ToString()}", parent.transform);
                    go.transform.localPosition = new Vector3(data.Position.X, data.Position.Y, data.Position.Z);
                }
                else if (data.LightType == (int)Define.DecoType.Handcuff)
                {
                    GameObject go = Managers.Resource.Instantiate($"Deco_{Define.DecoType.Handcuff.ToString()}", parent.transform);
                    go.transform.localPosition = new Vector3(data.Position.X, data.Position.Y, data.Position.Z);
                }
                else if (data.LightType == (int)Define.DecoType.GodRay)
                {
                    GameObject go = Managers.Resource.Instantiate($"Deco_{Define.DecoType.GodRay.ToString()}", parent.transform);
                    go.transform.localPosition = new Vector3(data.Position.X, data.Position.Y, data.Position.Z);
                }
                else if (data.LightType == (int)Define.DecoType.PointLight)
                {
                    GameObject go = Managers.Resource.Instantiate($"Deco_{Define.DecoType.PointLight.ToString()}", parent.transform);
                    go.transform.localPosition = new Vector3(data.Position.X, data.Position.Y, data.Position.Z);
                }
            }
        }
    }
    #endregion

    #region ForData
    public Define.ScriptType ScriptType = Define.ScriptType.None;
    public Define.ScreenType ScreenType = Define.ScreenType.None;

    public void DeleteGameData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.DeleteKey("ISFIRST");
        {
            string path = Application.persistentDataPath + "/SaveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/MonsterActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/BossMonsterActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/CItemActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/EItemActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/DoorActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/PillarActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/LeverActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        //ParseMapData();
        Debug.Log("Complete DeleteGameData");
    }

    #endregion

    public void Init()
    {
        _path = Application.persistentDataPath + "/SaveData.json";

        if (LoadGame())
            return;

        SaveGame();
    }
}
