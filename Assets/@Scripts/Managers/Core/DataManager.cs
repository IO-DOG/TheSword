using Data;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static Define;

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
    public Dictionary<int, Data.StageInfoData> StageInfoDic { get; set; } = new Dictionary<int, StageInfoData>();
    public Dictionary<int, Data.EventData> EventDic { get; set; } = new Dictionary<int, EventData>();

    public Dictionary<int, bool> MonsterActiveDic { get; set; } = new Dictionary<int, bool>();
    public Dictionary<int, bool> BossMonsterActiveDic { get; set; } = new Dictionary<int, bool>();
    public Dictionary<int, bool> CItemActiveDic { get; set; } = new Dictionary<int, bool>();
    public Dictionary<int, bool> EItemActiveDic { get; set; } = new Dictionary<int, bool>();
    public Dictionary<int, bool> PillarActiveDic { get; set; } = new Dictionary<int, bool>();
    public Dictionary<int, bool> LeverActiveDic { get; set; } = new Dictionary<int, bool>();
    public Dictionary<int, bool> DoorActiveDic { get; set; } = new Dictionary<int, bool>();

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
        StageInfoDic = LoadJson<Data.StageInfoDataLoader, int, Data.StageInfoData>("StageInfoData").MakeDict();
        EventDic = LoadJson<Data.EventDataLoader, int, Data.EventData>("EventData").MakeDict();

        #region Active Dic
        TextAsset monsterActiveDataTextAsset = Managers.Resource.Load<TextAsset>("MonsterActiveData");
        MonsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(monsterActiveDataTextAsset.text);
        TextAsset bossMonsterActiveDataTextAsset = Managers.Resource.Load<TextAsset>("BossMonsterActiveData");
        BossMonsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(bossMonsterActiveDataTextAsset.text);
        TextAsset cItemActiveDataTextAsset = Managers.Resource.Load<TextAsset>("CItemActiveData");
        CItemActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(cItemActiveDataTextAsset.text);
        TextAsset eItemActiveDataTextAsset = Managers.Resource.Load<TextAsset>("EItemActiveData");
        EItemActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(eItemActiveDataTextAsset.text);
        TextAsset doorActiveDataTextAsset = Managers.Resource.Load<TextAsset>("DoorActiveData");
        DoorActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(doorActiveDataTextAsset.text);
        TextAsset pillarActiveDataTextAsset = Managers.Resource.Load<TextAsset>("PillarActiveData");
        PillarActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(pillarActiveDataTextAsset.text);
        TextAsset leverActiveDataTextAsset = Managers.Resource.Load<TextAsset>("LeverActiveData");
        LeverActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(leverActiveDataTextAsset.text);
        #endregion

        CheckSaveData();
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

    void CheckSaveData()
    {
        {
            string path = Application.persistentDataPath + "/MonsterActiveData.json";
            if (File.Exists(path))
            {
                string file = Application.persistentDataPath + "/MonsterActiveData.json";
                string fileStr = File.ReadAllText(file);
                MonsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(fileStr);
            }
        }
        {
            string path = Application.persistentDataPath + "/BossMonsterActiveData.json";
            if (File.Exists(path))
            {
                string file = Application.persistentDataPath + "/BossMonsterActiveData.json";
                string fileStr = File.ReadAllText(file);
                BossMonsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(fileStr);
            }
        }
        {
            string path = Application.persistentDataPath + "/CItemActiveData.json";
            if (File.Exists(path))
            {
                string file = Application.persistentDataPath + "/CItemActiveData.json";
                string fileStr = File.ReadAllText(file);
                CItemActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(fileStr);
            }
        }
        {
            string path = Application.persistentDataPath + "/EItemActiveData.json";
            if (File.Exists(path))
            {
                string file = Application.persistentDataPath + "/EItemActiveData.json";
                string fileStr = File.ReadAllText(file);
                EItemActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(fileStr);
            }
        }
        {
            string path = Application.persistentDataPath + "/DoorActiveData.json";
            if (File.Exists(path))
            {
                string file = Application.persistentDataPath + "/DoorActiveData.json";
                string fileStr = File.ReadAllText(file);
                DoorActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(fileStr);
            }
        }
        {
            string path = Application.persistentDataPath + "/PillarActiveData.json";
            if (File.Exists(path))
            {
                string file = Application.persistentDataPath + "/PillarActiveData.json";
                string fileStr = File.ReadAllText(file);
                PillarActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(fileStr);
            }
        }
        {
            string path = Application.persistentDataPath + "/LeverActiveData.json";
            if (File.Exists(path))
            {
                string file = Application.persistentDataPath + "/LeverActiveData.json";
                string fileStr = File.ReadAllText(file);
                LeverActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(fileStr);
            }
        }
    }

    public List<ScriptData> LoadScriptData(int scriptCode)
    {
        List<ScriptData> scripts = new List<ScriptData>();
        int i = scriptCode;
        while (Managers.Data.ScriptDic.ContainsKey(i))
        {
            scripts.Add(Managers.Data.ScriptDic[i]);
            i++;
        }

        return scripts;
    }

    public int FindKeyByValue_StageInfoData(string targetValue)
    {
        foreach(KeyValuePair<int, Data.StageInfoData> kvp in Managers.Data.StageInfoDic)
        {
            if (kvp.Value.DungeonID == targetValue)
            {
                return kvp.Key;
            }
        }

        return -1;
    }

    public int GetChapterCount(string chpater)
    {
        int count = 0;

        foreach (KeyValuePair<int, Data.StageInfoData> kvp in Managers.Data.StageInfoDic)
        {
            if (kvp.Value.DungeonID.StartsWith(chpater))
            {
                count++;
            }
        }

        return count;
    }
}
