using Data;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;

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
            string path = Application.dataPath + "/@Resources/Data/JsonData/KeyInventory.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        ParseMapData();
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
        ParseEquipData("Equip");
        Debug.Log("Complete DataTransformer");
    }

    [MenuItem("Tools/SaveLightAsJson ")]
    public static void SaveLightAsJson()
    {
        string path = Application.dataPath + "/@Resources/Data/JsonData/LightData.json";
        DungeonLightDataLoader loader = new DungeonLightDataLoader();

        foreach(KeyValuePair<string, Data.MapData> data in Managers.Data.MapDic)
        {
            if (GameObject.Find(data.Key) == null)
                return;
            GameObject parent = GameObject.Find(data.Key).transform.Find("Lights").gameObject;
            string dungeon = parent.transform.parent.name; // DG Name
            if (parent == null)
            {
                Debug.Log("Parent is not exists!");
                return;
            }

            Transform[] lights = parent.GetComponentsInChildren<Transform>();
            List<LightData> lightDatas = new List<LightData>();

            for (int i = 1; i < lights.Length; i++)
            {
                Position pos = new Position { X = lights[i].localPosition.x, Y = lights[i].localPosition.y, Z = lights[i].localPosition.z };

                if (lights[i].name.Contains(Define.LightType.Torch.ToString()))
                {
                    lightDatas.Add(new LightData { LightType = (int)Define.LightType.Torch, Position = pos });
                }
                else if (lights[i].name.Contains(Define.LightType.Bowl.ToString()))
                {
                    lightDatas.Add(new LightData { LightType = (int)Define.LightType.Bowl, Position = pos });
                }
            }

            loader.lights.Add(new DungeonLightData
            {
                DGName = dungeon,
                LightData = lightDatas
            });

        }

        string newJson = JsonConvert.SerializeObject(loader, Formatting.Indented);
        File.WriteAllText(path, newJson);

        Debug.Log("Complete SaveLightAsJson");
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
        MapDataLoader loader = new MapDataLoader();
        DirectoryInfo di = new DirectoryInfo($"{Application.dataPath}/@Resources/Data/Excel/");
        int floorIndex = 1;

        #region Excel
        foreach (FileInfo file in di.GetFiles())
        {
            int totalCItemIndex = 0;
            int totalEItemIndex = 0;
            int totalMonsterIndex = 0;
            int totalBossIndex = 0;
            int totalDoorIndex = 0;


            if (file.Name.Contains("Dungeon") && !file.Name.Contains("meta"))
            {
                List<Tile> tiles = new List<Tile>();
                string[] lines = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{file.Name}").Split("\n");
                float zPos = 0;

                for (int y = 0; y < lines.Length; y++)
                {
                    string[] row = lines[y].Replace("\r", "").Split(',');
                    float xPos = 0;
                    zPos = y * Define.TILE_SIZE;

                    if (row.Length == 0)
                        continue;
                    if (string.IsNullOrEmpty(row[0]))
                        continue;

                    for (int x = 0; x < row.Length; x++)
                    {
                        string block = row[x];

                        int tileID;

                        string occupiedType;
                        int occupiedIndex;
                        int occupiedTotalIndex;
                        bool occupiedIsActive;

                        xPos = x * Define.TILE_SIZE;

                        if (block[0] == 'I')
                        {
                            tileID = 1;

                            occupiedIndex = int.Parse(Regex.Replace(block, "[^0-9]", ""));
                            occupiedType = "CItem";
                            occupiedTotalIndex = totalCItemIndex++;
                            occupiedIsActive = true;
                        }
                        else if (block[0] == 'E')
                        {
                            tileID = 1;

                            occupiedIndex = int.Parse(Regex.Replace(block, "[^0-9]", ""));
                            occupiedType = "EItem";
                            occupiedTotalIndex = totalEItemIndex++;
                            occupiedIsActive = true;
                        }
                        else if (block[0] == 'M')
                        {
                            tileID = 1;

                            occupiedIndex = int.Parse(Regex.Replace(block, "[^0-9]", ""));
                            occupiedType = "Monster";
                            occupiedTotalIndex = totalMonsterIndex++;
                            occupiedIsActive = true;
                        }
                        else if (block[0] == 'B')
                        {
                            tileID = 1;

                            occupiedIndex = int.Parse(Regex.Replace(block, "[^0-9]", ""));
                            occupiedType = "Boss";
                            occupiedTotalIndex = totalBossIndex++;
                            occupiedIsActive = true;
                        }
                        else
                        {
                            tileID = 1;

                            occupiedType = "none";
                            occupiedIndex = -1;
                            occupiedTotalIndex = -1;
                            occupiedIsActive = false;

                            if (block != "1")
                            {
                                tileID = int.Parse(Regex.Replace(block, "[^0-9]", ""));

                                if (tileID >= 3 && tileID <= 8)
                                {
                                    occupiedType = "Door";
                                    occupiedTotalIndex = totalDoorIndex++;
                                    occupiedIsActive = true;
                                }
                                if (tileID == 9)
                                {
                                    occupiedType = "Stairs";
                                    occupiedTotalIndex = floorIndex;
                                    occupiedIndex = (int)Define.Stairs.Upstairs;
                                }
                                else if(tileID == 10)
                                {
                                    occupiedType = "Stairs";
                                    occupiedTotalIndex = floorIndex;
                                    occupiedIndex = (int)Define.Stairs.Downstairs;
                                }
                            }
                        }

                        Tile tile = new Tile
                        {
                            ID = tileID,
                            Position = new Position
                            {
                                X = xPos,
                                Y = 0,
                                Z = zPos,
                            },
                            Occupied = new Occupied
                            {
                                Type = occupiedType,
                                Index = occupiedIndex,
                                TotalIndex = occupiedTotalIndex,
                                IsActive = occupiedIsActive
                            }
                        };
                        tiles.Add(tile);
                    }
                }

                MapData mapData = new MapData
                {
                    Key = file.Name.Replace(".csv", ""),
                    Tile = tiles,
                };
                loader.maps.Add(mapData);
                floorIndex++;
            }
        }
        #endregion

        string mapDicJsonStr = JsonConvert.SerializeObject(loader, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/MapData.json", mapDicJsonStr);
        AssetDatabase.Refresh();
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

    static void ParseEquipData(string filename)
    {
        EquipDataLoader loader = new EquipDataLoader();

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
            EquipData ed = new EquipData();
            ed.id = ConvertValue<int>(row[i++]);
            ed.Name = ConvertValue<string>(row[i++]);
            ed.Type = ConvertValue<int>(row[i++]);
            ed.ATK = ConvertValue<float>(row[i++]);
            ed.DEF = ConvertValue<float>(row[i++]);
            ed.HP = ConvertValue<float>(row[i++]);
            ed.ASPD = ConvertValue<float>(row[i++]);
            ed.DSPD = ConvertValue<float>(row[i++]);
            ed.CRI = ConvertValue<float>(row[i++]);
            ed.CRIATK = ConvertValue<float>(row[i++]);
            ed.MSPD = ConvertValue<float>(row[i++]);
            ed.AbilityId = ConvertValue<int>(row[i++]);
            ed.ImageName = ConvertValue<string>(row[i++]);
            ed.NameId = ConvertValue<int>(row[i++]);
            ed.DescId = ConvertValue<int>(row[i++]);
            loader.equips.Add(ed);
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
