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
        //ParseMapData("Map");
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

    static void ParseMapData(string filename)
    {
        #region ExcelData
        string str = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{filename}Data.csv");
        Debug.Log(str);
        string[] lines = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{filename}Data.csv").Split("\n");

        GameObject parent = GameObject.Find("Parent");
        float coX = 0, coY = 0, coZ = 0;
        float toAdd = 3.2f;

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

                if (block == "0")
                {

                }
                else if (block[0] == 'I') // 아이템일 경우
                {
                    GameObject floor = Resources.Load<GameObject>($"Tilemap_1");
                    UnityEngine.Object.Instantiate(floor, new Vector3(coX, coY + 1.55f, coZ), Quaternion.identity, parent.transform);

                    // TODO 아이템 생성

                }
                else if (block[0] == 'M') // 몬스터일 경우
                {
                    GameObject floor = Resources.Load<GameObject>($"Tilemap_1");
                    UnityEngine.Object.Instantiate(floor, new Vector3(coX, coY + 1.55f, coZ), Quaternion.identity, parent.transform);

                    // TODO 몬스터 생성
                    GameObject monster = Resources.Load<GameObject>($"Monster");
                    UnityEngine.Object.Instantiate(monster, new Vector3(coX, coY, coZ), Quaternion.identity, parent.transform);
                    monster.GetComponent<MonsterController>().id = block[2] - '0';
                }
                else if (block[0] == 'B') // 보스 몬스터일 경우
                {
                    GameObject floor = Resources.Load<GameObject>($"Tilemap_1");
                    UnityEngine.Object.Instantiate(floor, new Vector3(coX, coY + 1.55f, coZ), Quaternion.identity, parent.transform);

                    // TODO 보스 몬스터 생성
                }
                else
                {
                    GameObject floor = Resources.Load<GameObject>($"Tilemap_1");
                    UnityEngine.Object.Instantiate(floor, new Vector3(coX, coY + 1.55f, coZ), Quaternion.identity, parent.transform);
                    // TODO 타일 생성
                    GameObject tile = Resources.Load<GameObject>($"Tilemap_{block}");
                    UnityEngine.Object.Instantiate(tile, new Vector3(coX, coY, coZ), Quaternion.identity, parent.transform);
                }
                coX += toAdd;
            }
            coZ += toAdd;
            coX = 0;
        }

        #endregion
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
