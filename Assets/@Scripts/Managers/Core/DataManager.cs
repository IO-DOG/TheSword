using System;
using System.Linq;
using Data;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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

    public SaveStore.ActiveState CaptureActive() => new SaveStore.ActiveState {
        Monsters = MonsterActiveDic, Bosses = BossMonsterActiveDic, Items = CItemActiveDic,
        Equipment = EItemActiveDic, Doors = DoorActiveDic, Pillars = PillarActiveDic, Levers = LeverActiveDic
    };

    public void ApplyActive(SaveStore.ActiveState state)
    {
        if (state == null || !state.IsValid) throw new InvalidDataException("Incomplete object state.");
        MonsterActiveDic = state.Monsters;
        BossMonsterActiveDic = state.Bosses;
        CItemActiveDic = state.Items;
        EItemActiveDic = state.Equipment;
        DoorActiveDic = state.Doors;
        PillarActiveDic = state.Pillars;
        LeverActiveDic = state.Levers;
    }

    public SaveStore.ActiveState ReadLegacyActive(string directory)
    {
        Dictionary<int, bool> Read(string name) => JsonConvert.DeserializeObject<Dictionary<int, bool>>(
            File.ReadAllText(Path.Combine(directory, name + "ActiveData.json")));
        return new SaveStore.ActiveState {
            Monsters = Read("Monster"), Bosses = Read("BossMonster"), Items = Read("CItem"),
            Equipment = Read("EItem"), Doors = Read("Door"), Pillars = Read("Pillar"), Levers = Read("Lever")
        };
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

    // All checkpoint callers save player and object state together.
    public void UpdateActiveDic() => Managers.Game.SaveGame();

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
        foreach (FileInfo file in di.GetFiles().OrderBy(f => f.Name, StringComparer.Ordinal))
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
                            // 보스도 "몬스터 카운터"를 공유한다.
                            // 전투 결말은 UI_MonsterCard.Dead() 하나뿐이고 거기서 항상
                            // MonsterActiveDic[IsActiveIndex] 를 끈다. 보스에게 별도 0~4 를
                            // 주면 1층 일반 몬스터 0~4 가 대신 죽은 것으로 기록되고,
                            // 정작 보스는 다시 켜진 채로 부활한다.
                            bossMonsterActiveDic.Add(bossMonsterCount++, true);
                            monsterActiveDic.Add(monsterCount, true);
                            Data.ObjectData tile = new Data.ObjectData
                            {
                                Id = id,
                                Count = monsterCount++,
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
                            else if (id == 1)
                            {
                                // 바닥. MapBuilder 가 프리팹 없는 층을 조립할 때 필요하다.
                                Data.ObjectData tile = new Data.ObjectData
                                {
                                    Id = id,
                                    Position = new Data.MyVector3
                                    {
                                        X = xPos,
                                        Y = 0,
                                        Z = zPos,
                                    },
                                    ObjectType = (int)Define.ObjectType.Floor,
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

        ApplyActive(new SaveStore.ActiveState {
            Monsters = monsterActiveDic, Bosses = bossMonsterActiveDic, Items = cItemActiveDic,
            Equipment = eItemActiveDic, Doors = doorActiveDic, Pillars = pillarActiveDic, Levers = leverActiveDic
        });
    }
    #endregion
}
