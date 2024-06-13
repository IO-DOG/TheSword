using Data;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}

public class DataManager
{
    public Dictionary<int, Data.PlayerData> PlayerDic { get; private set; } = new Dictionary<int, Data.PlayerData>();
    public Dictionary<int, Data.MonsterData> MonsterDic { get; private set; } = new Dictionary<int, Data.MonsterData>();
    public Dictionary<int, Data.ConsumableItemData> ConsumableItemDic { get; private set; } = new Dictionary<int, Data.ConsumableItemData>();
    public Dictionary<int, Data.MonsterClassData> MonsterClassDic { get; set; } = new Dictionary<int, Data.MonsterClassData>();
    public Dictionary<string, Data.MapData> MapDic { get; set; } = new Dictionary<string, Data.MapData>();
    public Dictionary<string, Data.DungeonDecoData> DecoDic { get; set; } = new Dictionary<string, Data.DungeonDecoData>();
    public Dictionary<int, Data.EquipData> EquipDic { get; set; } = new Dictionary<int, Data.EquipData>();
    public Dictionary<int, Data.ScriptData> ScriptDic { get; set; } = new Dictionary<int, Data.ScriptData>();
    public Dictionary<string, Data.ConversationData> ConversationDic { get; set; } = new Dictionary<string, ConversationData>();

    
    public void Init()
    {
        AssetDatabase.Refresh();

        PlayerDic = LoadJson<Data.PlayerDataLoader, int, Data.PlayerData>("PlayerData").MakeDict();
        MonsterDic = LoadJson<Data.MonsterDataLoader, int, Data.MonsterData>("MonsterData").MakeDict();
        ConsumableItemDic = LoadJson<Data.ConsumableItemDataLoader, int, Data.ConsumableItemData>("ConsumableItemData").MakeDict();
        MonsterClassDic = LoadJson<Data.MonsterClassDataLoader, int, Data.MonsterClassData>("MonsterClassData").MakeDict();
        MapDic = LoadJson<Data.MapDataLoader, string, Data.MapData>("MapData").MakeDict();
        DecoDic = LoadJson<Data.DungeonDecoDataLoader, string, Data.DungeonDecoData>("DecoData").MakeDict();
        EquipDic = LoadJson<Data.EquipDataLoader, int, Data.EquipData>("EquipData").MakeDict();
        ScriptDic = LoadJson<Data.ScriptDataLoader, int, Data.ScriptData>("ScriptData").MakeDict();
        ConversationDic = LoadJson<Data.ConversationDataLoader, string, Data.ConversationData>("ConversationTestData").MakeDict();
    }

    Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    {
        TextAsset textAsset = Managers.Resource.Load<TextAsset>($"{path}");

        if (path == "MapData")
        {
            return JsonConvert.DeserializeObject<Loader>(textAsset.text, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
        }
        else
        {
            return JsonConvert.DeserializeObject<Loader>(textAsset.text);
        }
    }

    #region Active Off
    public void MonsterActiveOff(int index)
    {
        foreach (KeyValuePair<string, Data.MapData> entry in Managers.Data.MapDic)
        {
            string key = entry.Key;
            Data.MapData mapData = entry.Value;

            foreach (Data.TileData tile in mapData.Tiles)
            {
               if(tile is Occupied monsterTile && monsterTile.Type == (int)Define.OccupiedType.Monster && monsterTile.TotalCount == index)
                {
                    monsterTile.IsActive = false;
                }
            }
        }
    }

    public void CItemActiveOff(int index)
    {
        foreach (KeyValuePair<string, Data.MapData> entry in Managers.Data.MapDic)
        {
            string key = entry.Key;
            Data.MapData mapData = entry.Value;

            foreach (Data.TileData tile in mapData.Tiles)
            {
                if (tile is Occupied citemTile && citemTile.Type == (int)Define.OccupiedType.CItem && citemTile.TotalCount == index)
                {
                    citemTile.IsActive = false;
                }
            }
        }
    }

    public void DoorActiveOff(int index)
    {
        foreach (KeyValuePair<string, Data.MapData> entry in Managers.Data.MapDic)
        {
            string key = entry.Key;
            Data.MapData mapData = entry.Value;

            foreach (Data.TileData tile in mapData.Tiles)
            {
                if (tile is DoorData doorTile && doorTile.TileType == (int)Define.TileType.Door && doorTile.TotalCount == index)
                {
                    doorTile.IsActive = false;
                }
            }
        }
    }

    public void EItemActiveOff(int index)
    {
        foreach (KeyValuePair<string, Data.MapData> entry in Managers.Data.MapDic)
        {
            string key = entry.Key;
            Data.MapData mapData = entry.Value;

            foreach (Data.TileData tile in mapData.Tiles)
            {
                if (tile is Occupied eItemTile && eItemTile.Type == (int)Define.OccupiedType.EItem && eItemTile.TotalCount == index)
                {
                    eItemTile.IsActive = false;
                }
            }
        }
    }
    #endregion

}
