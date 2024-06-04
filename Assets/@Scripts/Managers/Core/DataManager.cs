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

    public void Init()
    {
        AssetDatabase.Refresh();

        PlayerDic = LoadJson<Data.PlayerDataLoader, int, Data.PlayerData>("PlayerData").MakeDict();
        MonsterDic = LoadJson<Data.MonsterDataLoader, int, Data.MonsterData>("MonsterData").MakeDict();
        ConsumableItemDic = LoadJson<Data.ConsumableItemDataLoader, int, Data.ConsumableItemData>("ConsumableItemData").MakeDict();
        MonsterClassDic = LoadJson<Data.MonsterClassDataLoader, int, Data.MonsterClassData>("MonsterClassData").MakeDict();
        MapDic = LoadJson<Data.MapDataLoader, string, Data.MapData>("MapData").MakeDict();
    }

    Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    {
        TextAsset textAsset = Managers.Resource.Load<TextAsset>($"{path}");
        return JsonConvert.DeserializeObject<Loader>(textAsset.text);
    }

    #region Active Off
    public void MonsterActiveOff(int index)
    {
        foreach (KeyValuePair<string, Data.MapData> entry in Managers.Data.MapDic)
        {
            string key = entry.Key;
            Data.MapData mapData = entry.Value;

            foreach (Data.Tile tile in mapData.Tile)
            {
               if(tile.Occupied.Type == "Monster" && tile.Occupied.TotalIndex == index)
                {
                    tile.Occupied.IsActive = false;
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

            foreach (Data.Tile tile in mapData.Tile)
            {
                if (tile.Occupied.Type == "CItem" && tile.Occupied.TotalIndex == index)
                {
                    tile.Occupied.IsActive = false;
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

            foreach (Data.Tile tile in mapData.Tile)
            {
                if (tile.Occupied.Type == "Door" && tile.Occupied.TotalIndex == index)
                {
                    tile.Occupied.IsActive = false;
                }
            }
        }
    }
    #endregion

}
