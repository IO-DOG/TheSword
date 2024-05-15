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

    [MenuItem("Tools/ParseExcel %#K")]
    public static void ParseExcel()
    {
        ParsePlayerData("Player");
        ParseMonsterData("Monster");
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
            cd.Exp = ConvertValue<float>(row[i++]);
            //cd.Image = ConvertValue<string>(row[i++]);
            loader.creatures.Add(cd);
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
