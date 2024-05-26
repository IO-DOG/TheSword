using Data;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DataTransformer : EditorWindow
{
#if UNITY_EDITOR
    [MenuItem("Tools/DeleteGameData ")]
    public static void DeleteGameData()
    {
        PlayerPrefs.DeleteAll();
        {
            string path = Application.dataPath + "/@Resources/Data/SaveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.dataPath + "/@Resources/Data/SaveMonsterActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.dataPath + "/@Resources/Data/SaveBossMonsterActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.dataPath + "/@Resources/Data/SaveItemActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.dataPath + "/@Resources/Data/SaveDoorActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }

        Debug.Log("Complete DeleteGameData");
    }

    [MenuItem("Tools/ParseExcel %#K")]
    public static void ParseExcel()
    {
        ParsePlayerData("Player");
        ParseMonsterData("Monster");
        ParseConsumableItemData("ConsumableItem");
        ParseMapData();
        ParseMonsterClassData("MonsterClass");
        Debug.Log("Complete DataTransformer");
    }

    static void ParsePlayerData(string filename)
    {
        PlayerDataLoader loader = new PlayerDataLoader();

        #region ExcelData
        string str = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{filename}Data.csv");
        Debug.Log(str);
        string[] lines = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{filename}Data.csv").Split("\n");

        for (int y = 1; y < lines.Length; y++)
        {
            string[] row = lines[y].Replace("\r", "").Split(',');

            if (row.Length == 0)
                continue;
            if (string.IsNullOrEmpty(row[0]))
                continue;

            int i = 0;
            PlayerData cd = new PlayerData();
            cd.id = ConvertValue<int>(row[i++]);
            cd.NeedExp = ConvertValue<float>(row[i++]);
            cd.TotalExp = ConvertValue<float>(row[i++]);
            cd.Attack = ConvertValue<float>(row[i++]);
            cd.Defence = ConvertValue<float>(row[i++]);
            cd.MaxHP = ConvertValue<float>(row[i++]);
            cd.AttackSpeed = ConvertValue<float>(row[i++]);
            cd.DefenceSpeed = ConvertValue<float>(row[i++]);
            cd.Critical = ConvertValue<float>(row[i++]);
            cd.CriticalAttack = ConvertValue<float>(row[i++]);
            cd.MoveSpeed = ConvertValue<float>(row[i++]);
            loader.creatures.Add(cd);
        }

        #endregion

        string jsonStr = JsonConvert.SerializeObject(loader, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/{filename}Data.json", jsonStr);
        AssetDatabase.Refresh();
    }

    static void ParseMonsterData(string filename)
    {
        MonsterDataLoader loader = new MonsterDataLoader();

        #region ExcelData
        string str = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{filename}Data.csv");
        Debug.Log(str);
        string[] lines = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{filename}Data.csv").Split("\n");

        for (int y = 1; y < lines.Length; y++)
        {
            string[] row = lines[y].Replace("\r", "").Split(',');

            if (row.Length == 0)
                continue;
            if (string.IsNullOrEmpty(row[0]))
                continue;

            int i = 0;
            MonsterData cd = new MonsterData();
            cd.id = ConvertValue<int>(row[i++]);
            cd.Chapter = ConvertValue<int>(row[i++]);
            cd.Class = ConvertValue<string>(row[i++]);
            cd.Feature = ConvertValue<int>(row[i++]);
            cd.Name = ConvertValue<string>(row[i++]);
            cd.Attack = ConvertValue<float>(row[i++]);
            cd.Defence = ConvertValue<float>(row[i++]);
            cd.MaxHP = ConvertValue<float>(row[i++]);
            cd.AttackSpeed = ConvertValue<float>(row[i++]);
            cd.DefenceSpeed = ConvertValue<float>(row[i++]);
            cd.Critical = ConvertValue<float>(row[i++]);
            cd.CriticalAttack = ConvertValue<float>(row[i++]);
            cd.RewardExp = ConvertValue<float>(row[i++]);
            cd.RewardItem = ConvertValue<int>(row[i++]);
            cd.IdleAnimStr = ConvertValue<string>(row[i++]);
            cd.AttackAnimStr = ConvertValue<string>(row[i++]);
            cd.DefenceAnimStr = ConvertValue<string>(row[i++]);
            cd.HitAnimStr = ConvertValue<string>(row[i++]);
            loader.creatures.Add(cd);
        }

        #endregion

        string jsonStr = JsonConvert.SerializeObject(loader, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/{filename}Data.json", jsonStr);
        AssetDatabase.Refresh();
    }

    static void ParseConsumableItemData(string filename)
    {
        ConsumableItemDataLoader loader = new ConsumableItemDataLoader();

        #region ExcelData
        string str = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{filename}Data.csv");
        Debug.Log(str);
        string[] lines = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{filename}Data.csv").Split("\n");

        for (int y = 1; y < lines.Length; y++)
        {
            string[] row = lines[y].Replace("\r", "").Split(',');

            if (row.Length == 0)
                continue;
            if (string.IsNullOrEmpty(row[0]))
                continue;

            int i = 0;
            ConsumableItemData cd = new ConsumableItemData();
            cd.id = ConvertValue<int>(row[i++]);
            cd.Name = ConvertValue<string>(row[i++]);
            cd.Heal = ConvertValue<float>(row[i++]);
            cd.AttackUp = ConvertValue<float>(row[i++]);
            cd.DefenceUp = ConvertValue<float>(row[i++]);
            cd.HPUp = ConvertValue<float>(row[i++]);
            cd.Description = ConvertValue<string>(row[i++]);
            loader.consumableItems.Add(cd);
        }

        #endregion

        string jsonStr = JsonConvert.SerializeObject(loader, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/{filename}Data.json", jsonStr);
        AssetDatabase.Refresh();
    }

    static void ParseMapData()
    {
        int monsterIndex = 0;
        int bossMonsterIndex = 0;
        int itemIndex = 0;
        int doorIndex = 0;

        Dictionary<int, bool> monsterActiveDic = new Dictionary<int, bool>();
        Dictionary<int, bool> bossMonsterActiveDic = new Dictionary<int, bool>();
        Dictionary<int, bool> itemActiveDic = new Dictionary<int, bool>();
        Dictionary<int, bool> doorActiveDic = new Dictionary<int, bool>();

        string[] lines = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/MapData.csv").Split("\n");
        for (int y = 0; y < lines.Length; y++)
        {
            string[] row = lines[y].Replace("\r", "").Split(',');

            if (row.Length == 0)
                continue;
            if (string.IsNullOrEmpty(row[0]))
                continue;
            for (int x = 0; x < row.Length; ++x)
            {
                string block = row[x];

                if (block[0] == 'M')
                {
                    monsterActiveDic.Add(monsterIndex++, true);
                }
                else if (block[0] == 'B')
                {
                    bossMonsterActiveDic.Add(bossMonsterIndex++, true);
                }
                else if (block[0] == 'I')
                {
                    itemActiveDic.Add(itemIndex++, true);
                }
                // door is 3, 4, 5, 6, 7, 8
                else if (block[0] >= '3' && block[0] <= '8')
                {
                    doorActiveDic.Add(doorIndex++, true);
                }
            }
        }

        string monsterActiveDicJsonStr = JsonConvert.SerializeObject(monsterActiveDic, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/MonsterActiveData.json", monsterActiveDicJsonStr);
        AssetDatabase.Refresh();
        string bossMonsterActiveDicJsonStr = JsonConvert.SerializeObject(bossMonsterActiveDic, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/BossMonsterActiveData.json", bossMonsterActiveDicJsonStr);
        AssetDatabase.Refresh();
        string itemActiveDicJsonStr = JsonConvert.SerializeObject(itemActiveDic, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/ItemActiveData.json", itemActiveDicJsonStr);
        AssetDatabase.Refresh();
        string doorActiveDicJsonStr = JsonConvert.SerializeObject(doorActiveDic, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/DoorActiveData.json", doorActiveDicJsonStr);
    }

    static void ParseMonsterClassData(string filename)
    {
        MonsterClassDataLoader loader = new MonsterClassDataLoader();

        #region ExcelData
        string str = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{filename}Data.csv");
        Debug.Log(str);
        string[] lines = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{filename}Data.csv").Split("\n");

        for (int y = 1; y < lines.Length; y++)
        {
            string[] row = lines[y].Replace("\r", "").Split(',');

            if (row.Length == 0)
                continue;
            if (string.IsNullOrEmpty(row[0]))
                continue;

            int i = 0;
            MonsterClassData cd = new MonsterClassData();
            cd.id = ConvertValue<int>(row[i++]);
            cd.ClassName = ConvertValue<string>(row[i++]);
            cd.ClassDesc = ConvertValue<string>(row[i++]);
            cd.Image = ConvertValue<string>(row[i++]);
            cd.AttackFX = ConvertValue<string>(row[i++]);
            cd.ClassId = ConvertValue<int>(row[i++]);
            cd.EffectDescId = ConvertValue<int>(row[i++]);
            loader.monsterClasses.Add(cd);
        }

        #endregion

        string jsonStr = JsonConvert.SerializeObject(loader, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/{filename}Data.json", jsonStr);
        AssetDatabase.Refresh();
    }

    public static T ConvertValue<T>(string value)
    {
        if (string.IsNullOrEmpty(value))
            return default(T);

        TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
        return (T)converter.ConvertFromString(value);
    }

    public static List<T> ConvertList<T>(string value)
    {
        if (string.IsNullOrEmpty(value))
            return new List<T>();

        return value.Split('&').Select(x => ConvertValue<T>(x)).ToList();
    }

#endif
}
