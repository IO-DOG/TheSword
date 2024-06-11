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
using static UnityEditor.Progress;

public class GameManager
{
    public bool OnBattle = false;

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

    public Camera MainCamera;

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
        public string DefenceAnimStr { get; set; }
        public string HitAnimStr { get; set; }
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
        string jsonStr = JsonConvert.SerializeObject(CurPlayerData, Formatting.Indented);
        File.WriteAllText(_path, jsonStr);

        List<MapData> mapData = new List<MapData>(Managers.Data.MapDic.Values);
        var mapContainer = new { Maps = mapData };
        string MapDicJsonStr = JsonConvert.SerializeObject(mapContainer, Formatting.Indented);
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

    public void InstantiateMap(string mapName)
    {

        int count = 0;

        foreach (KeyValuePair<string, Data.MapData> entry in Managers.Data.MapDic)
        {
            string key = entry.Key;
            Data.MapData mapData = entry.Value;

            if (!key.Contains(mapName))
                continue;

            GameObject parent = new GameObject() { name = key };
            GameObject tiles = new GameObject() { name = "Tiles" };
            GameObject items = new GameObject() { name = "Items" };
            GameObject monsters = new GameObject() { name = "Monsters" };
            GameObject lights = new GameObject() { name = "Lights" };

            parent.transform.localPosition += new Vector3(count * 100, 0, 0);
            parent.transform.parent = GameObject.Find("Map").transform;
            tiles.transform.parent = parent.transform;
            items.transform.parent = parent.transform;
            monsters.transform.parent = parent.transform;
            lights.transform.parent = parent.transform;

            foreach (Data.Tile tile in mapData.Tile)
            {
                if (tile.Occupied.Type == "none")
                {
                    GameObject go = Managers.Resource.Instantiate($"Tilemap_{tile.ID}", tiles.transform);

                    if (tile.ID != 1 && tile.ID != 11)
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y - Define.TILE_SIZE / 2, tile.Position.Z);
                    else
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);
                }
                else if (tile.Occupied.Type == "CItem")
                {
                    GameObject go = Managers.Resource.Instantiate($"Tilemap_{tile.ID}", tiles.transform);
                    go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);

                    GameObject item = Managers.Resource.Instantiate("ConsumableItem", items.transform);
                    item.transform.position = go.transform.position;
                    item.GetComponent<ConsumableItem>().id = tile.Occupied.Index;
                    item.name = $"CItem{tile.Occupied.TotalIndex}";
                    item.GetComponent<ConsumableItem>()._itemIndex_forActive = tile.Occupied.TotalIndex;

                    if (tile.Occupied.IsActive == false)
                        item.SetActive(false);
                }
                else if (tile.Occupied.Type == "EItem")
                {
                    GameObject go = Managers.Resource.Instantiate($"Tilemap_{tile.ID}", tiles.transform);
                    go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);

                    GameObject item = Managers.Resource.Instantiate("EquipItem", items.transform);
                    item.transform.position = go.transform.position;
                    item.GetComponent<Equip>().Id = tile.Occupied.Index;
                    item.name = $"EItem{tile.Occupied.TotalIndex}";
                    item.GetComponent<Equip>()._itemIndex_forActive = tile.Occupied.TotalIndex;

                    if (tile.Occupied.IsActive == false)
                        item.SetActive(false);
                }
                else if (tile.Occupied.Type == "Monster")
                {
                    GameObject go = Managers.Resource.Instantiate($"Tilemap_{tile.ID}", tiles.transform);
                    go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);

                    GameObject monster = Managers.Resource.Instantiate("Monster", monsters.transform);
                    monster.transform.position = go.transform.position;
                    monster.GetComponent<MonsterController>().id = tile.Occupied.Index;
                    monster.name = $"monster{tile.Occupied.TotalIndex}";
                    monster.GetComponent<MonsterController>()._monsterIndex_forActive = tile.Occupied.TotalIndex;

                    if (tile.Occupied.IsActive == false)
                        monster.SetActive(false);
                }
                else if (tile.Occupied.Type == "Door")
                {
                    GameObject go = Managers.Resource.Instantiate($"Tilemap_1", tiles.transform);
                    go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);

                    GameObject door = Managers.Resource.Instantiate($"Tilemap_{tile.ID}", tiles.transform);
                    door.transform.position = new Vector3(tile.Position.X, tile.Position.Y - Define.TILE_SIZE / 2, tile.Position.Z);
                    door.name = $"door{tile.Occupied.TotalIndex}";
                    door.GetComponentInChildren<Door>()._doorIndex_forActive = tile.Occupied.TotalIndex;

                    if (tile.Occupied.IsActive == false)
                        door.SetActive(false);
                }
                else if(tile.Occupied.Type == "Stairs")
                {
                    GameObject stairs = Managers.Resource.Instantiate($"Tilemap_{tile.ID}", tiles.transform);
                    stairs.name = $"stairs{tile.Occupied.TotalIndex}";
                    stairs.GetComponentInChildren<PortalController>()._floor = tile.Occupied.TotalIndex;
                    stairs.GetComponentInChildren<PortalController>()._stairs = (int)tile.Occupied.Index;

                    if (tile.Occupied.Index == (int)Define.Stairs.Downstairs)
                    {
                        stairs.transform.position = new Vector3(tile.Position.X, tile.Position.Y - Define.TILE_SIZE * 1.5f, tile.Position.Z);
                    }
                    else
                    {
                        GameObject go = Managers.Resource.Instantiate($"Tilemap_1", tiles.transform);
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);

                        stairs.transform.position = new Vector3(tile.Position.X, tile.Position.Y - Define.TILE_SIZE / 2, tile.Position.Z);
                    }

                }
            }

            parent.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);
            items.transform.localPosition = items.transform.localPosition + Vector3.up * 2f + Vector3.forward * 0.7f * (-1);
            monsters.transform.localPosition = monsters.transform.localPosition + Vector3.up * 3f + Vector3.forward * 0.7f * (-1);
            MainCamera.GetComponentInChildren<CameraController>().AdjustCameraPitch(Define.CAMERA_ANGLE, items);
            MainCamera.GetComponentInChildren<CameraController>().AdjustCameraPitch(Define.CAMERA_ANGLE, monsters);
            MainCamera.GetComponentInChildren<CameraController>().AdjustCameraPitch(Define.CAMERA_ANGLE, lights);
            count++;

            InstantiateLights(key, lights.transform);
        }

        MainCamera.GetComponentInChildren<CameraController>().AdjustCameraPitch(Define.CAMERA_ANGLE, Player.gameObject);
    }

    void InstantiateLights(string DGName, Transform parent)
    {
        foreach (KeyValuePair<string, DungeonLightData> entry in Managers.Data.LightDic)
        {
            string key = entry.Key;
            Data.DungeonLightData lightList = entry.Value;

            if (!key.Contains(DGName))
                continue;

            foreach (LightData data in lightList.LightData)
            {
                if(data.LightType == (int)Define.LightType.Torch)
                {
                    GameObject go = Managers.Resource.Instantiate("Object_DungeonTorch", parent.transform);
                    go.transform.localPosition = new Vector3(data.Position.X, data.Position.Y, data.Position.Z);
                }
                else if(data.LightType == (int)Define.LightType.Bowl)
                {
                    GameObject go = Managers.Resource.Instantiate("Object_FireBowl", parent.transform);
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
