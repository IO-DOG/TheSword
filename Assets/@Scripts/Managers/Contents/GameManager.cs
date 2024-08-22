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
using UnityEngine.Playables;
using UnityEngine.Rendering;
using static Define;
using static UnityEditor.Progress;
using static UnityEngine.EventSystems.EventTrigger;

public class GameManager
{
    public bool OnBattle = false;
    public bool OnConversation = false;
    public bool OnLever = false;
    public bool OnFade = false;
    public bool OnDirect = false;

    public PlayerController Player; // ������ ������ ����
    public MonsterController Monster; // ������ ������ ����
    public CurMonsterData MonsterData = new CurMonsterData(); // ���� ���� ���� ����
    public ContinueData CurPlayerData = new ContinueData(); // ���� ���� �÷��̾� ����
    public CurConsumableItemData ConsumableItemData = new CurConsumableItemData(); // Current Consumable Item Data
    public KeyInventory KeyInventory = new KeyInventory(); //Inventory

    public Action OnBattleAction;
    public Action OnBattleDataRefreshAction;
    public Action OnBattleCreatureDefeceAction;
    public Action OnBattleCreatureDamagedAction;
    public Action OnBattlePlayerDefeceAction;
    public Action OnBattlePlayerDamagedAction;
    public Texture2D _screenShot = null;
    public Sprite _screenShot2 = null;

    public Camera MainCamera;
    public Camera RenderCamera;
    public GameObject Map;
    public GameObject Monsters;
    public GameObject Items;
    public GameObject Lights;

    #region CurPlayerData
    public class ContinueData
    {
        public int Level { get; set; } // Lv
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
        public string Name { get; set; }
        public float Heal { get; set; }
        public float AttackUp { get; set; }
        public float DefenceUp { get; set; }
        public float HPUp { get; set; }
        public string Description { get; set; }
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

        List<MapData> mapData = new List<MapData>(Managers.Data.MapDic.Values);
        var mapContainer = new { Maps = mapData };
        string MapDicJsonStr = JsonConvert.SerializeObject(mapContainer, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });
        File.WriteAllText(Application.dataPath + "/@Resources/Data/JsonData/MapData.json", MapDicJsonStr);
    }

    public bool LoadGame()
    {
        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
        {
            string path = Application.dataPath + "/@Resources/Data/SaveData.json";
            if (File.Exists(path))
                File.Delete(path);

            int level = 1;
            Managers.Game.CurPlayerData.Level = Managers.Data.PlayerDic[level].id + 1;
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
        string chapter = Managers.Data.StageInfoDic[key].DungeonID.Substring(0, 2) + "_";
        int maxCount = Managers.Data.GetChapterCount(chapter);
        bool isSpawned = false;

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

            GameObject items = Util.FindChildByName(map.transform, "Items").gameObject;
            GameObject monsters = Util.FindChildByName(map.transform, "Monsters").gameObject;
            GameObject bossMonsters = Util.FindChildByName(map.transform, "BossMonsters").gameObject;

            map.transform.localPosition += new Vector3(count * 100, 0, 0);
            map.transform.parent = parent.transform;

            foreach (Data.TileData tile in mapData.Tiles)
            {
                if (tile is DoorData doorTile)
                {
                    if (doorTile.IsActive == false)
                    {
                        foreach(Door child in doors)
                        {
                            if (doorTile.TotalCount == child._doorIndex_forActive)
                                child.gameObject.SetActive(false);
                        }
                    }
                }
                else if (tile is Occupied citemTile && citemTile.Type == (int)Define.OccupiedType.CItem)
                {
                    GameObject item = Managers.Resource.Instantiate("ConsumableItem", items.transform);
                    item.transform.localPosition = new Vector3 (citemTile.Position.X, citemTile.Position.Y, citemTile.Position.Z);
                    item.GetComponent<ConsumableItem>().id = citemTile.Index;
                    item.name = $"CItem{citemTile.TotalCount}";
                    item.GetComponent<ConsumableItem>()._itemIndex_forActive = citemTile.TotalCount;

                    if (citemTile.IsActive == false)
                        item.SetActive(false);
                }
                else if (tile is Occupied eitemTile && eitemTile.Type == (int)Define.OccupiedType.EItem)
                {
                    GameObject item = Managers.Resource.Instantiate("EquipItem", items.transform);
                    item.transform.localPosition = new Vector3(eitemTile.Position.X, eitemTile.Position.Y, eitemTile.Position.Z);
                    item.GetComponent<Equip>().Id = eitemTile.Index;
                    item.name = $"EItem{eitemTile.TotalCount}";
                    item.GetComponent<Equip>()._itemIndex_forActive = eitemTile.TotalCount;

                    if (eitemTile.IsActive == false)
                        item.SetActive(false);
                }
                else if (tile is Occupied monsterTile && monsterTile.Type == (int)Define.OccupiedType.Monster)
                {
                    GameObject monster = Managers.Resource.Instantiate("Monster", monsters.transform);
                    monster.transform.localPosition = new Vector3(monsterTile.Position.X, monsterTile.Position.Y, monsterTile.Position.Z);
                    monster.GetComponent<MonsterController>().id = monsterTile.Index;
                    monster.name = $"monster{monsterTile.TotalCount}";
                    monster.GetComponent<MonsterController>()._monsterIndex_forActive = monsterTile.TotalCount;

                    if (monsterTile.IsActive == false)
                        monster.SetActive(false);
                }
                else if (tile is Occupied bossMonsterTile && bossMonsterTile.Type == (int)Define.OccupiedType.Boss)
                {
                    GameObject boss = Managers.Resource.Instantiate("BossMonster", bossMonsters.transform);
                    boss.transform.localPosition = new Vector3(bossMonsterTile.Position.X, bossMonsterTile.Position.Y, bossMonsterTile.Position.Z);
                    boss.GetComponent<BossMonsterController>().id = bossMonsterTile.Index;
                    boss.name = $"bossMonster{bossMonsterTile.TotalCount}";
                    boss.GetComponent<BossMonsterController>()._monsterIndex_forActive = bossMonsterTile.TotalCount;

                    int id = boss.GetComponent<BossMonsterController>().id;
                    string name = Managers.Data.MonsterDic[id].Name;
                    switch (name)
                    {
                        case "블랙슬라임":
                            boss.AddComponent<BlackSlimeController>();
                            break;
                        default:
                            break;
                    }

                    if (bossMonsterTile.IsActive == false)
                        boss.SetActive(false);
                }
                else
                {
                    if (!isSpawned && tile.PrefabID == (int)Define.TileType.SpawnPoint && stageName != GetBossRoomName(chapter))
                    {
                        Managers.Game.Player.transform.position = new Vector3(tile.Position.X * 0.33f + count * 100, 2.6f, tile.Position.Z * 0.33f);
                        Managers.Game.Player._cellPos = Managers.Game.Player.transform.position;

                        isSpawned = true;
                    }
                }
            }

            Items = items;
            Monsters = monsters;
            //Lights = lights;

            items.transform.localPosition = items.transform.localPosition + new Vector3(0f, 1.6f, -0.4f);
            monsters.transform.localPosition = monsters.transform.localPosition + new Vector3(0f, 3f, -1.1f);
            bossMonsters.transform.localPosition = bossMonsters.transform.localPosition + new Vector3(0f, 1.5f, -0.55f);

            count++;

            if (count == maxCount - 1)
                break;

            MainCamera.GetComponentInChildren<CameraController>().ChangeView(Define.CAMERA_ANGLE, Managers.Game.Monsters);
            MainCamera.GetComponentInChildren<CameraController>().ChangeView(Define.CAMERA_ANGLE, Managers.Game.Items);
            //InstantiateLights(key, lights.transform);
        }

        MainCamera.GetComponentInChildren<CameraController>().ChangeView(Define.CAMERA_ANGLE, Managers.Game.Player.gameObject);
        CameraController.SetCameraConfiner();
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
    #endregion

    public void Init()
    {
        _path = Application.dataPath + "/@Resources/Data/SaveData.json";

        if (LoadGame())
            return;

        PlayerPrefs.SetInt("ISFIRST", 0);
        SaveGame();
    }
}
