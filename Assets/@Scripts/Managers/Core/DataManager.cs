using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public Dictionary<int, bool> MonsterActiveDic { get; set; } = new Dictionary<int, bool>();
    public Dictionary<int, bool> BossMonsterActiveDic { get; set; } = new Dictionary<int, bool>();
    public Dictionary<int, bool> ItemActiveDic { get; set; } = new Dictionary<int, bool>();

    public void Init()
    {
        PlayerDic = LoadJson<Data.PlayerDataLoader, int, Data.PlayerData>("PlayerData").MakeDict();
        MonsterDic = LoadJson<Data.MonsterDataLoader, int, Data.MonsterData>("MonsterData").MakeDict();
        ConsumableItemDic = LoadJson<Data.ConsumableItemDataLoader, int, Data.ConsumableItemData>("ConsumableItemData").MakeDict();

        TextAsset monsterActiveDataTextAsset = Managers.Resource.Load<TextAsset>("MonsterActiveData");
        MonsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(monsterActiveDataTextAsset.text);
        TextAsset bossMonsterActiveDataTextAsset = Managers.Resource.Load<TextAsset>("BossMonsterActiveData");
        BossMonsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(bossMonsterActiveDataTextAsset.text);
        TextAsset itemActiveDataTextAsset = Managers.Resource.Load<TextAsset>("ItemActiveData");
        ItemActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(itemActiveDataTextAsset.text);

        CheckSaveData();
    }

    Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    {
        TextAsset textAsset = Managers.Resource.Load<TextAsset>($"{path}");
        return JsonConvert.DeserializeObject<Loader>(textAsset.text);
    }

    void CheckSaveData()
    {
        {
            string path = Application.dataPath + "/@Resources/Data/SaveMonsterActiveData.json";
            if (File.Exists(path))
            {
                string file = Application.dataPath + "/@Resources/Data/SaveMonsterActiveData.json";
                string fileStr = File.ReadAllText(file);
                MonsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(fileStr);
            }
        }
        {
            string path = Application.dataPath + "/@Resources/Data/SaveBossMonsterActiveData.json";
            if (File.Exists(path))
            {
                string file = Application.dataPath + "/@Resources/Data/SaveBossMonsterActiveData.json";
                string fileStr = File.ReadAllText(file);
                BossMonsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(fileStr);
            }
        }
        {
            string path = Application.dataPath + "/@Resources/Data/SaveItemActiveData.json";
            if (File.Exists(path))
            {
                string file = Application.dataPath + "/@Resources/Data/SaveItemActiveData.json";
                string fileStr = File.ReadAllText(file);
                ItemActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(fileStr);
            }
        }
    }
}
