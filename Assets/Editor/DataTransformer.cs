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
            string path = Application.dataPath + "/@Resources/Data/SaveData.json";
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
        ParseScriptData("Script");
        ParseStageInfoData("StageInfo");
        ParseEventData("Event");
        Debug.Log("Complete DataTransformer");
    }

    [MenuItem("Tools/SaveDecoAsJson")]
    public static void SaveLightAsJson()
    {
        string path = Application.dataPath + "/@Resources/Data/JsonData/DecoData.json";
        DungeonDecoDataLoader loader = new DungeonDecoDataLoader();

        foreach(KeyValuePair<string, Data.MapData> data in Managers.Data.MapDic)
        {
            if (GameObject.Find(data.Key) == null)
                return;
            GameObject parent = GameObject.Find(data.Key).transform.Find("Deco").gameObject;
            string dungeon = parent.transform.parent.name; // DG Name
            if (parent == null)
            {
                Debug.Log("Parent is not exists!");
                return;
            }

            Transform[] lights = parent.GetComponentsInChildren<Transform>();
            List<DecoData> lightDatas = new List<DecoData>();

            for (int i = 1; i < lights.Length; i++)
            {
                MyVector3 pos = new MyVector3 { X = lights[i].localPosition.x, Y = lights[i].localPosition.y, Z = lights[i].localPosition.z };
                MyVector3 scale = new MyVector3 { X = lights[i].localScale.x, Y = lights[i].localScale.y, Z = lights[i].localScale.z };
                MyVector3 rot = new MyVector3 { X = lights[i].localRotation.x, Y = lights[i].localRotation.y, Z = lights[i].localRotation.z };

                if (lights[i].name.Contains(Define.DecoType.Torch.ToString()))
                {
                    lightDatas.Add(new DecoData { LightType = (int)Define.DecoType.Torch, Position = pos, Scale = scale, Rotation = rot });
                }
                else if (lights[i].name.Contains(Define.DecoType.FireBowl.ToString()))
                {
                    lightDatas.Add(new DecoData { LightType = (int)Define.DecoType.FireBowl, Position = pos, Scale = scale, Rotation = rot });
                }
                else if (lights[i].name.Contains(Define.DecoType.PointLight.ToString()))
                {
                    lightDatas.Add(new DecoData { LightType = (int)Define.DecoType.PointLight, Position = pos, Scale = scale, Rotation = rot });
                }
                else if (lights[i].name.Contains(Define.DecoType.Handcuff.ToString()))
                {
                    lightDatas.Add(new DecoData { LightType = (int)Define.DecoType.Handcuff, Position = pos, Scale = scale, Rotation = rot });
                }
                else if (lights[i].name.Contains(Define.DecoType.GodRay.ToString()))
                {
                    lightDatas.Add(new DecoData { LightType = (int)Define.DecoType.GodRay, Position = pos, Scale = scale, Rotation = rot });
                }
            }

            loader.lights.Add(new DungeonDecoData
            {
                DGName = dungeon,
                DecoData = lightDatas
            });

        }

        string newJson = JsonConvert.SerializeObject(loader, Formatting.Indented);
        File.WriteAllText(path, newJson);

        Debug.Log("Complete SaveLightAsJson");
    }

    [MenuItem("Tools/SaveTileDataAsJson")]
    public static void SaveTitleAsJson()
    {

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
            cd.BattleParticleAttack = ConvertValue<string>(row[i++]);
            cd.BattleParticleHit = ConvertValue<string>(row[i++]);
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
        //int floorIndex = 1;

        #region Excel
        foreach (FileInfo file in di.GetFiles())
        {
            int totalCItemCount = 0;
            int totalEItemCount = 0;
            int totalMonsterCount = 0;
            int totalBossCount = 0;
            int totalDoorCount = 0;

            if (file.Name.Contains("Dungeon") && !file.Name.Contains("meta"))
            {
                int totalPillarCount = 0;

                List<Data.TileData> tiles = new List<Data.TileData>();
                string[] lines = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/{file.Name}").Split("\n");
                float zPos = 0;

                for (int y = 0; y < lines.Length; y++)
                {
                    string[] row = lines[y].Replace("\r", "").Split(',');
                    float xPos = 0;
                    zPos = (-1) * y * Define.TILE_SIZE;

                    if (row.Length == 0)
                        continue;
                    //if (string.IsNullOrEmpty(row[0]))
                    //    continue;

                    for (int x = 0; x < row.Length; x++)
                    {
                        string block = row[x];

                        xPos = x * Define.TILE_SIZE;

                        if (block.Length == 0)
                        {
                            block = "0";
                        }

                        if(block == "-1")
                        {
                            Data.TileData tile = new Data.TileData
                            {
                                PrefabID = -1,
                                Position = new Data.MyVector3
                                {
                                    X = xPos,
                                    Y = 0,
                                    Z = zPos,
                                },
                                TileType = (int)Define.TileType.VoidTile,
                            };
                            tiles.Add(tile);

                        }
                        else if (block == "-2")
                        {
                            Data.TileData tile = new Data.TileData
                            {
                                PrefabID = (int)Define.TileType.ObjectTile,
                                Position = new Data.MyVector3
                                {
                                    X = xPos,
                                    Y = 0,
                                    Z = zPos,
                                },
                                TileType = (int)Define.TileType.ObjectTile,
                            };
                            tiles.Add(tile);
                        }
                        else if (block[0] == 'I')
                        {
                            Data.Occupied tile = new Data.Occupied
                            {
                                PrefabID = 1,
                                Position = new Data.MyVector3
                                {
                                    X = xPos,
                                    Y = 0,
                                    Z = zPos,
                                },
                                TileType = (int)Define.TileType.Floor,

                                Type = (int)Define.OccupiedType.CItem,
                                Index = int.Parse(Regex.Replace(block, "[^0-9]", "")),
                                TotalCount = totalCItemCount++,
                                IsActive = true,
                            };
                            tiles.Add(tile);

                        }
                        else if (block[0] == 'E')
                        {
                            Data.Occupied tile = new Data.Occupied
                            {
                                PrefabID = 1,
                                Position = new Data.MyVector3
                                {
                                    X = xPos,
                                    Y = 0,
                                    Z = zPos,
                                },
                                TileType = (int)Define.TileType.Floor,

                                Type = (int)Define.OccupiedType.EItem,
                                Index = int.Parse(Regex.Replace(block, "[^0-9]", "")),
                                TotalCount = totalEItemCount++,
                                IsActive = true,
                            };
                            tiles.Add(tile);
                        }
                        else if (block[0] == 'M')
                        {
                            Data.Occupied tile = new Data.Occupied
                            {
                                PrefabID = 1,
                                Position = new Data.MyVector3
                                {
                                    X = xPos,
                                    Y = 0,
                                    Z = zPos,
                                },
                                TileType = (int)Define.TileType.Floor,

                                Type = (int)Define.OccupiedType.Monster,
                                Index = int.Parse(Regex.Replace(block, "[^0-9]", "")),
                                TotalCount = totalMonsterCount++,
                                IsActive = true,
                            };
                            tiles.Add(tile);
                        }
                        else if (block[0] == 'B')
                        {
                            Data.Occupied tile = new Data.Occupied
                            {
                                PrefabID = 1,
                                Position = new Data.MyVector3
                                {
                                    X = xPos,
                                    Y = 0,
                                    Z = zPos,
                                },
                                TileType = (int)Define.TileType.Floor,

                                Type = (int)Define.OccupiedType.Boss,
                                Index = int.Parse(Regex.Replace(block, "[^0-9]", "")),
                                TotalCount = totalBossCount++,
                                IsActive = true,
                            };
                            tiles.Add(tile);
                        }
                        else if (block[0] == 'W')
                        {
                            int prefabID = int.Parse(Regex.Replace(block, "[^0-9]", ""));
                            Data.TileData tile = new Data.TileData
                            {
                                PrefabID = prefabID,
                                Position = new Data.MyVector3
                                {
                                    X = xPos,
                                    Y = 0,
                                    Z = zPos,
                                },
                                TileType = (int)Define.TileType.Wall,
                            };
                            tiles.Add(tile);
                        }
                        else
                        {
                            int prefabID = int.Parse(Regex.Replace(block, "[^0-9]", ""));

                            if (prefabID >= 3 && prefabID <= 8)
                            {
                                Data.DoorData tile = new Data.DoorData
                                {
                                    PrefabID = prefabID,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    TileType = (int)Define.TileType.Door,

                                    TotalCount = totalDoorCount++,
                                    IsActive = true,
                                };
                                tiles.Add(tile);
                            }
                            else if (prefabID == 9)
                            {
                                Data.StairsData tile = new Data.StairsData
                                {
                                    PrefabID = prefabID,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    TileType = (int)Define.TileType.Portal,

                                    //Floor = floorIndex,
                                    StairsType = (int)Define.Stairs.Upstairs,
                                };
                                tiles.Add(tile);
                            }
                            else if (prefabID == 10)
                            {
                                Data.StairsData tile = new Data.StairsData
                                {
                                    PrefabID = prefabID,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    TileType = (int)Define.TileType.Portal,

                                    //Floor = floorIndex,
                                    StairsType = (int)Define.Stairs.Downstairs,
                                };
                                tiles.Add(tile);
                            }
                            else if (prefabID == 12)
                            {
                                Data.LeverData tile = new Data.LeverData
                                {
                                    PrefabID = prefabID,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    TileType = (int)Define.TileType.Lever,
                                    IsActive = false,
                                };
                                tiles.Add(tile);
                            }
                            else if (prefabID == 13)
                            {
                                Data.PillarData tile = new Data.PillarData
                                {
                                    PrefabID = prefabID,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    TileType = (int)Define.TileType.Pillar,

                                    TotalCount = totalPillarCount++,
                                    IsActive = true,
                                };
                                tiles.Add(tile);
                            }
                            else if (prefabID == 14)
                            {
                                Data.StairsData tile = new Data.StairsData
                                {
                                    PrefabID = prefabID,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    TileType = (int)Define.TileType.Portal,
                                    StairsType = (int)Define.Stairs.Upstairs,
                                };
                                tiles.Add(tile);
                            }
                            else if (prefabID == 15)
                            {
                                Data.StairsData tile = new Data.StairsData
                                {
                                    PrefabID = prefabID,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    TileType = (int)Define.TileType.Portal,
                                    StairsType = (int)Define.Stairs.Downstairs,
                                };
                                tiles.Add(tile);
                            }
                            else if (prefabID == 16)
                            {
                                Data.StairsData tile = new Data.StairsData
                                {
                                    PrefabID = prefabID,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    TileType = (int)Define.TileType.Portal,
                                    StairsType = (int)Define.Stairs.BossRoom,
                                };
                                tiles.Add(tile);
                            }
                            else
                            {
                                Data.TileData tile = new Data.TileData
                                {
                                    PrefabID = prefabID,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    TileType = prefabID,
                                };
                                tiles.Add(tile);
                            }
                        }
                    }
                }

                MapData mapData = new MapData
                {
                    Key = file.Name.Replace(".csv", ""),
                    Tiles = tiles,
                };
                loader.maps.Add(mapData);
                //floorIndex++;
            }
        }
        #endregion

        string mapDicJsonStr = JsonConvert.SerializeObject(loader, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });
        File.WriteAllText($"{Application.dataPath}/@Resources/Data/JsonData/MapData.json", mapDicJsonStr);
        File.WriteAllText($"{Application.dataPath}/Resources/DecoJson/MapData.json", mapDicJsonStr);
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
            ed.AttackFX = ConvertValue<string>(row[i++]);
            ed.HitFX = ConvertValue<string>(row[i++]);
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
