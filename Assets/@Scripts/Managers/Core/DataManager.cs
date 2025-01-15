using Data;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
    public Dictionary<int, Data.MapData> MapDic { get; set; } = new Dictionary<int, Data.MapData>();
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
        //AssetDatabase.Refresh();

        PlayerDic = LoadJson<Data.PlayerDataLoader, int, Data.PlayerData>("PlayerData").MakeDict();
        MonsterDic = LoadJson<Data.MonsterDataLoader, int, Data.MonsterData>("MonsterData").MakeDict();
        ConsumableItemDic = LoadJson<Data.ConsumableItemDataLoader, int, Data.ConsumableItemData>("ConsumableItemData").MakeDict();
        MonsterClassDic = LoadJson<Data.MonsterClassDataLoader, int, Data.MonsterClassData>("MonsterClassData").MakeDict();
        MapDic = LoadJson<Data.MapDataLoader, int, Data.MapData>("MapData").MakeDict();
        EquipDic = LoadJson<Data.EquipDataLoader, int, Data.EquipData>("EquipData").MakeDict();
        ScriptDic = LoadJson<Data.ScriptDataLoader, int, Data.ScriptData>("ScriptData").MakeDict();
        StageInfoDic = LoadJson<Data.StageInfoDataLoader, int, Data.StageInfoData>("StageInfoData").MakeDict();
        EventDic = LoadJson<Data.EventDataLoader, int, Data.EventData>("EventData").MakeDict();

        CheckSaveData();
    }

    Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    {
        TextAsset textAsset = Managers.Resource.Load<TextAsset>(path);

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

    public void UpdateActiveDic()
    {
        string monsterActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.MonsterActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/MonsterActiveData.json", monsterActiveDicJsonStr);
        string bossMonsterActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.BossMonsterActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/BossMonsterActiveData.json", bossMonsterActiveDicJsonStr);
        string cItemActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.CItemActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/CItemActiveData.json", cItemActiveDicJsonStr);
        string eItemActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.EItemActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/EItemActiveData.json", eItemActiveDicJsonStr);
        string doorActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.DoorActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/DoorActiveData.json", doorActiveDicJsonStr);
        string pillarActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.PillarActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/PillarActiveData.json", pillarActiveDicJsonStr);
        string leverActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.LeverActiveDic, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/LeverActiveData.json", leverActiveDicJsonStr);
    }

    public void LoadActiveDic()
    {
        string monsterActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/MonsterActiveData.json");
        Dictionary<int, bool> monsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(monsterActiveDicFile);
        Managers.Data.MonsterActiveDic = monsterActiveDic;
        string bossMonsterActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/BossMonsterActiveData.json");
        Dictionary<int, bool> bossMonsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(bossMonsterActiveDicFile);
        Managers.Data.BossMonsterActiveDic = bossMonsterActiveDic;
        string cItemActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/CItemActiveData.json");
        Dictionary<int, bool> cItemActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(cItemActiveDicFile);
        Managers.Data.CItemActiveDic = cItemActiveDic;
        string eItemActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/EItemActiveData.json");
        Dictionary<int, bool> eItemActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(eItemActiveDicFile);
        Managers.Data.EItemActiveDic = eItemActiveDic;
        string doorActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/DoorActiveData.json");
        Dictionary<int, bool> doorActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(doorActiveDicFile);
        Managers.Data.DoorActiveDic = doorActiveDic;
        string pillarActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/PillarActiveData.json");
        Dictionary<int, bool> pillarActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(pillarActiveDicFile);
        Managers.Data.PillarActiveDic = pillarActiveDic;
        string leverActiveDicFile = File.ReadAllText(Application.persistentDataPath + "/LeverActiveData.json");
        Dictionary<int, bool> leverActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(leverActiveDicFile);
        Managers.Data.LeverActiveDic = leverActiveDic;
    }

    #region ActiveDic
    public void ResetActiveDic()
    {
        MapDataLoader loader = new MapDataLoader();
        DirectoryInfo di = new DirectoryInfo($"{Application.streamingAssetsPath}/Data/Excel/");

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
                string[] lines = File.ReadAllText($"{Application.streamingAssetsPath}/Data/Excel/{file.Name}").Split("\n");
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

        //AssetDatabase.Refresh();
        #endregion

        string mapDicJsonStr = JsonConvert.SerializeObject(loader);
        File.WriteAllText($"{Application.persistentDataPath}/MapData.json", mapDicJsonStr);
        File.WriteAllText($"{Application.streamingAssetsPath}/Data/JsonData/MapData.json", mapDicJsonStr);
        //AssetDatabase.Refresh();
    }
    #endregion
}
