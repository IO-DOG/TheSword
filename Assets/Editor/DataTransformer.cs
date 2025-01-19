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
using UnityEngine;
using UnityEngine.EventSystems;

public class DataTransformer : EditorWindow
{
#if UNITY_EDITOR
    [MenuItem("Tools/DeleteGameData ")]
    public static void DeleteGameData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.DeleteKey("ISFIRST");
        {
            string path = Application.persistentDataPath + "/SaveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/MonsterActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/BossMonsterActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/CItemActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/EItemActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/DoorActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/PillarActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }
        {
            string path = Application.persistentDataPath + "/LeverActiveData.json";
            if (File.Exists(path))
                File.Delete(path);
        }

        //ParseMapData();
        Debug.Log("Complete DeleteGameData");
    }

    [MenuItem("Tools/ParseExcel %#K")]
    public static void ParseExcel()
    {
        AssetDatabase.Refresh();
        ParsePlayerData("Player");
        ParseMonsterData("Monster");
        ParseConsumableItemData("ConsumableItem");
        ParseMapData();
        ParseMonsterClassData("MonsterClass");
        ParseEquipData("Equip");
        ParseScriptData("Script");
        ParseStageInfoData("StageInfo");
        ParseEventData("Event");
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
            cd.Ability = ConvertValue<int>(row[i++]);
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
            cd.BattleParticleAttack = ConvertValue<string>(row[i++]);
            cd.BattleParticleHit = ConvertValue<string>(row[i++]);
            cd.Shadow = ConvertValue<string>(row[i++]);
            cd.MonsterNameId = ConvertValue<int>(row[i++]);
            cd.MonsterDescId = ConvertValue<int>(row[i++]);
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
            cd.Heal = ConvertValue<float>(row[i++]);
            cd.AttackUp = ConvertValue<float>(row[i++]);
            cd.DefenceUp = ConvertValue<float>(row[i++]);
            cd.HPUp = ConvertValue<float>(row[i++]);
            cd.Img = ConvertValue<string>(row[i++]);
            cd.PrefabName = ConvertValue<string>(row[i++]);
            cd.Shadow = ConvertValue<string>(row[i++]);
            cd.ScriptNameId = ConvertValue<int>(row[i++]);
            cd.ScriptDescriptionId = ConvertValue<int>(row[i++]);
            loader.consumableItems.Add(cd);
        }

        #endregion

        string jsonStr = JsonConvert.SerializeObject(loader, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/{filename}Data.json", jsonStr);
        AssetDatabase.Refresh();
    }

    public static void ParseMapData()
    {
        MapDataLoader loader = new MapDataLoader();
        DirectoryInfo di = new DirectoryInfo($"{Application.dataPath}/@Resources/Data/Excel/");

        #region Active Dic
        Dictionary<int, bool> monsterActiveDic = new Dictionary<int, bool>();
        Dictionary<int, bool> bossMonsterActiveDic = new Dictionary<int, bool>();
        Dictionary<int, bool> cItemActiveDic = new Dictionary<int, bool>();
        Dictionary<int, bool> eItemActiveDic = new Dictionary<int, bool>();
        Dictionary<int, bool> doorActiveDic = new Dictionary<int, bool>();
        Dictionary<int, bool> pillarActiveDic = new Dictionary<int, bool>();
        Dictionary<int, bool> leverActiveDic = new Dictionary<int, bool>();
        #endregion

        int mapId = 0;

        #region count
        int citemCount = 0;
        int eitemCount = 0;
        int monsterCount = 0;
        int bossMonsterCount = 0;
        int doorCount = 0;
        int pillarCount = 0;
        int leverCount = 0;
        #endregion

        #region Excel
        foreach (FileInfo file in di.GetFiles())
        {
            if (file.Name.Contains("Dungeon") && !file.Name.Contains("meta"))
            {
                List<Data.ObjectData> tiles = new List<Data.ObjectData>();
                string[] lines = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{file.Name}").Split("\n");
                float zPos = 0;

                for (int y = 0; y < lines.Length; y++)
                {
                    string[] row = lines[y].Replace("\r", "").Split(',');
                    float xPos = 0;
                    zPos = (-1) * y * Define.TILE_SIZE;

                    if (row.Length == 0)
                        continue;

                    for (int x = 0; x < row.Length; x++)
                    {
                        string block = row[x];
                        if (block.Length == 0)
                        {
                            block = "0";
                        }

                        int id = int.Parse(Regex.Replace(block, "[^0-9]", ""));
                        xPos = x * Define.TILE_SIZE;

                        if (block[0] == 'I')
                        {
                            cItemActiveDic.Add(citemCount, true);
                            Data.ObjectData tile = new Data.ObjectData
                            {
                                Id = id,
                                Count = citemCount++,
                                ObjectType = (int)Define.ObjectType.CItem,
                                Position = new Data.MyVector3
                                {
                                    X = xPos,
                                    Y = 0,
                                    Z = zPos,
                                }
                            };
                            tiles.Add(tile);
                        }
                        else if (block[0] == 'E')
                        {
                            eItemActiveDic.Add(eitemCount, true);
                            Data.ObjectData tile = new Data.ObjectData
                            {
                                Id = id,
                                Count = eitemCount++,
                                ObjectType = (int)Define.ObjectType.Eitem,
                                Position = new Data.MyVector3
                                {
                                    X = xPos,
                                    Y = 0,
                                    Z = zPos,
                                }
                            };
                            tiles.Add(tile);
                        }                    
                        else if (block[0] == 'M')
                        {
                            monsterActiveDic.Add(monsterCount, true);
                            Data.ObjectData tile = new Data.ObjectData
                            {
                                Id = id,
                                Count = monsterCount++,
                                ObjectType = (int)Define.ObjectType.Monster,
                                Position = new Data.MyVector3
                                {
                                    X = xPos,
                                    Y = 0,
                                    Z = zPos,
                                }
                            };
                            tiles.Add(tile);
                        }
                        else if (block[0] == 'B')
                        {
                            bossMonsterActiveDic.Add(bossMonsterCount, true);
                            Data.ObjectData tile = new Data.ObjectData
                            {
                                Id = id,
                                Count = bossMonsterCount++,
                                ObjectType = (int)Define.ObjectType.BossMonster,
                                Position = new Data.MyVector3
                                {
                                    X = xPos,
                                    Y = 0,
                                    Z = zPos,
                                }
                            };
                            tiles.Add(tile);
                        }
                        else if (block[0] == 'W')
                        {
                            Data.ObjectData tile = new Data.ObjectData
                            {
                                Id = id,
                                Position = new Data.MyVector3
                                {
                                    X = xPos,
                                    Y = 0,
                                    Z = zPos,
                                },
                                ObjectType = (int)Define.ObjectType.Wall,
                            };
                            tiles.Add(tile);
                        }
                        else
                        {
                            if (id >= 3 && id <= 8)
                            {
                                doorActiveDic.Add(doorCount, true);
                                Data.ObjectData tile = new Data.ObjectData
                                {
                                    Id = id,
                                    Count = doorCount++,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    ObjectType = (int)Define.ObjectType.Door,
                                };
                                tiles.Add(tile);
                            }
                            else if (id == 11)
                            {
                                Data.ObjectData tile = new Data.ObjectData
                                {
                                    Id = id,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    ObjectType = (int)Define.ObjectType.SpawnPoint,
                                };
                                tiles.Add(tile);
                            }
                            else if (id == 12)
                            {
                                leverActiveDic.Add(leverCount, true);

                                Data.ObjectData tile = new Data.ObjectData
                                {
                                    Id = id,
                                    Count = leverCount++,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    ObjectType = (int)Define.ObjectType.Lever,
                                };
                                tiles.Add(tile);
                            }
                            else if (id == 13)
                            {
                                pillarActiveDic.Add(pillarCount, true);

                                Data.ObjectData tile = new Data.ObjectData
                                {
                                    Id = id,
                                    Count = pillarCount++,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    ObjectType = (int)Define.ObjectType.Pillar,
                                };
                                tiles.Add(tile);
                            }
                            else if (id == 14 || id == 15)
                            {
                                Data.ObjectData tile = new Data.ObjectData
                                {
                                    Id = id,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    ObjectType = (int)Define.ObjectType.Portal,
                                };
                                tiles.Add(tile);
                            }
                            else if (id == 16)
                            { 
                                Data.ObjectData tile = new Data.ObjectData
                                {
                                    Id = id,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    ObjectType = (int)Define.ObjectType.Portal,
                                };
                                tiles.Add(tile);
                            }
                            else if (id == 0)
                            {
                                Data.ObjectData tile = new Data.ObjectData
                                {
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    ObjectType = (int)Define.ObjectType.Void,
                                };
                                tiles.Add(tile);
                            }
                        }
                    }
                }

                MapData mapData = new MapData
                {
                    Key = mapId++,
                    Objects = tiles,
                };
                loader.maps.Add(mapData);
            }
        }
        #endregion

        #region Active Dic
        string monsterActiveDicJsonStr = JsonConvert.SerializeObject(monsterActiveDic, Formatting.Indented);
        File.WriteAllText($"{Application.persistentDataPath}/MonsterActiveData.json", monsterActiveDicJsonStr);
        string bossMonsterActiveDicJsonStr = JsonConvert.SerializeObject(bossMonsterActiveDic, Formatting.Indented);
        File.WriteAllText($"{Application.persistentDataPath}/BossMonsterActiveData.json", bossMonsterActiveDicJsonStr);
        string cItemActiveDicJsonStr = JsonConvert.SerializeObject(cItemActiveDic, Formatting.Indented);
        File.WriteAllText($"{Application.persistentDataPath}/CItemActiveData.json", cItemActiveDicJsonStr);
        string eItemActiveDicJsonStr = JsonConvert.SerializeObject(eItemActiveDic, Formatting.Indented);
        File.WriteAllText($"{Application.persistentDataPath}/EItemActiveData.json", eItemActiveDicJsonStr);
        string doorActiveDicJsonStr = JsonConvert.SerializeObject(doorActiveDic, Formatting.Indented);
        File.WriteAllText($"{Application.persistentDataPath}/DoorActiveData.json", doorActiveDicJsonStr);
        string pillarActiveDicJsonStr = JsonConvert.SerializeObject(pillarActiveDic, Formatting.Indented);
        File.WriteAllText($"{Application.persistentDataPath}/PillarActiveData.json", pillarActiveDicJsonStr);
        string leverActiveDicJsonStr = JsonConvert.SerializeObject(leverActiveDic, Formatting.Indented);
        File.WriteAllText($"{Application.persistentDataPath}/LeverActiveData.json", leverActiveDicJsonStr);

        AssetDatabase.Refresh();
        #endregion

        string mapDicJsonStr = JsonConvert.SerializeObject(loader);
        File.WriteAllText($"{Application.persistentDataPath}/MapData.json", mapDicJsonStr);
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
            cd.ClassName = ConvertValue<int>(row[i++]);
            cd.ClassDesc = ConvertValue<int>(row[i++]);
            cd.AbilityImage = ConvertValue<string>(row[i++]);
            cd.BattleBGImage = ConvertValue<string>(row[i++]);
            cd.Weapon = ConvertValue<string>(row[i++]);
            cd.Shield = ConvertValue<string>(row[i++]);
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
            ed.AttackFX = ConvertValue<string>(row[i++]);
            ed.HitFX = ConvertValue<string>(row[i++]);
            ed.Shadow = ConvertValue<string>(row[i++]);
            ed.IllustFX = ConvertValue<string>(row[i++]);
            ed.Illust = ConvertValue<string>(row[i++]);
            ed.IllustBG = ConvertValue<string>(row[i++]);
            ed.NameId = ConvertValue<int>(row[i++]);
            ed.DescId = ConvertValue<int>(row[i++]);
            loader.equips.Add(ed);
        }
        #endregion

        string jsonStr = JsonConvert.SerializeObject(loader, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/{filename}Data.json", jsonStr);
        AssetDatabase.Refresh();
    }
    static void ParseScriptData(string filename)
    {
        ScriptDataLoader loader = new ScriptDataLoader();

        #region ExcelData
        string str = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{filename}Data.csv");
        Debug.Log(str);
        string[] lines = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{filename}Data.csv").Split("\n");

        for (int y = 1; y < lines.Length; y++)
        {
            string[] row = lines[y].Replace("\r", "").Split(',');

            for (int x = 0; x < row.Count(); x++)
            {
                row[x].Replace("^", ",");
            }

            if (row.Length == 0)
                continue;
            if (string.IsNullOrEmpty(row[0]))
                continue;

            int i = 0;
            ScriptData sd = new ScriptData();
            sd.id = ConvertValue<int>(row[i++]);
            Debug.Log(sd.id);
            sd.ScriptKr = ConvertValue<string>(row[i++]);
            Debug.Log(sd.ScriptKr);
            sd.ScriptEn = ConvertValue<string>(row[i++]);
            sd.ScriptJp = ConvertValue<string>(row[i++]);
            sd.ScriptCn = ConvertValue<string>(row[i++]);

            loader.scripts.Add(sd);
        }
        #endregion

        string jsonStr = JsonConvert.SerializeObject(loader, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/{filename}Data.json", jsonStr);
        AssetDatabase.Refresh();
    }

    static void ParseStageInfoData(string filename)
    {
        StageInfoDataLoader loader = new StageInfoDataLoader();

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
            StageInfoData sd = new StageInfoData();
            sd.id = ConvertValue<int>(row[i++]);
            sd.DungeonID = ConvertValue<string>(row[i++]);
            sd.Type = ConvertValue<Define.DungeonType>(row[i++]);
            sd.UpStage = ConvertValue<string>(row[i++]);
            sd.DownStage = ConvertValue<string>(row[i++]);
            sd.BossRoom = ConvertValue<string>(row[i++]);
            sd.ATK = ConvertValue<int>(row[i++]);
            sd.DEF = ConvertValue<int>(row[i++]);
            sd.EXP = ConvertValue<int>(row[i++]);
            sd.BGM = ConvertValue<string>(row[i++]);
            sd.DungeonNameScriptID = ConvertValue<int>(row[i++]);
            loader.stageInfos.Add(sd);
        }
        #endregion

        string jsonStr = JsonConvert.SerializeObject(loader, Formatting.Indented);
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/{filename}Data.json", jsonStr);
        AssetDatabase.Refresh();
    }
    static void ParseEventData(string filename)
    {
        EventDataLoader loader = new EventDataLoader();

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
            EventData ed = new EventData();
            ed.id = ConvertValue<int>(row[i++]);
            ed.IllustLeft = ConvertValue<string>(row[i++]);
            ed.IllustRight = ConvertValue<string>(row[i++]);
            ed.HeroEmoji = ConvertValue<string>(row[i++]);
            ed.OtherEmoji = ConvertValue<string>(row[i++]);
            ed.ScriptID = ConvertValue<int>(row[i++]);
            ed.Class = ConvertValue<int>(row[i++]);
            ed.Delay = ConvertValue<float>(row[i++]);
            loader.events.Add(ed);
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
