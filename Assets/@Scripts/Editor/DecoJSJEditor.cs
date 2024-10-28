using Data;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
[CustomEditor(typeof(ScriptableObjects.DecoJSJ))]
[CanEditMultipleObjects]
public class DecoJSJEditor : Editor
{
    public Dictionary<string, Data.MapData> MapDic { get; set; } = new Dictionary<string, Data.MapData>();
    // Stage의 프로퍼티들에 대 한 직렬화 객체들 정의
    private SerializedProperty _stageName;

    GameObject[] Tilemap = new GameObject[100];
    GameObject VoidTile;
    GameObject ObjectTile;

    private void OnEnable()
    {
        _stageName = serializedObject.FindProperty("_stageName");
        MapDic = LoadJson<Data.MapDataLoader, string, Data.MapData>().MakeDict();

        VoidTile = Resources.Load("DecoTiles/Tilemap_-1") as GameObject;
        ObjectTile = Resources.Load("DecoTiles/Tilemap_-2") as GameObject;
        Tilemap[0] = Resources.Load("DecoTiles/Tilemap_0") as GameObject;
        Tilemap[1] = Resources.Load("DecoTiles/Tilemap_1") as GameObject;
        Tilemap[3] = Resources.Load("DecoTiles/Tilemap_3") as GameObject;
        Tilemap[4] = Resources.Load("DecoTiles/Tilemap_4") as GameObject;
        Tilemap[5] = Resources.Load("DecoTiles/Tilemap_5") as GameObject;
        Tilemap[6] = Resources.Load("DecoTiles/Tilemap_6") as GameObject;
        Tilemap[7] = Resources.Load("DecoTiles/Tilemap_7") as GameObject;
        Tilemap[8] = Resources.Load("DecoTiles/Tilemap_8") as GameObject;
        Tilemap[9] = Resources.Load("DecoTiles/Tilemap_9") as GameObject;
        Tilemap[10] = Resources.Load("DecoTiles/Tilemap_10") as GameObject;
        Tilemap[11]= Resources.Load("DecoTiles/Tilemap_11") as GameObject;
        Tilemap[12] = Resources.Load("DecoTiles/Tilemap_12") as GameObject;
        Tilemap[13] = Resources.Load("DecoTiles/Tilemap_13") as GameObject;
        Tilemap[14] = Resources.Load("DecoTiles/Tilemap_14") as GameObject;
        Tilemap[15] = Resources.Load("DecoTiles/Tilemap_15") as GameObject;
        Tilemap[16] = Resources.Load("DecoTiles/Tilemap_16") as GameObject;

        Tilemap[20] = Resources.Load("DecoTiles/Dungeon_00/Tilemap_C00_W00") as GameObject;
        Tilemap[21] = Resources.Load("DecoTiles/Dungeon_00/Tilemap_C00_W01") as GameObject;
        Tilemap[22] = Resources.Load("DecoTiles/Dungeon_00/Tilemap_C00_W02") as GameObject;
        Tilemap[23] = Resources.Load("DecoTiles/Dungeon_00/Tilemap_C00_W03") as GameObject;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        GUILayout.BeginVertical();
        EditorGUILayout.PropertyField(_stageName);

        if(EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }

        if(GUILayout.Button("Load Stage"))
        {
            InstantiateMap(_stageName.enumDisplayNames[_stageName.enumValueIndex]);
            Debug.Log(_stageName.enumDisplayNames[_stageName.enumValueIndex]);
        }
        GUILayout.EndVertical();
    }

    Loader LoadJson<Loader, Key, Value>() where Loader : ILoader<Key, Value>
    {
        string textAsset = File.ReadAllText($"{Application.dataPath}/@Resources/Data/JsonData/MapData.json");

        return JsonConvert.DeserializeObject<Loader>(textAsset, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

    }

    #region Map Instantiate

    public void InstantiateMap(string mapName)
    {
        int count = 0;

        GameObject map = new GameObject("Map");

        foreach (KeyValuePair<string, Data.MapData> entry in MapDic)
        {
            string key = entry.Key;
            Data.MapData mapData = entry.Value;

            if (!key.Contains(mapName))
                continue;

            GameObject parent = new GameObject() { name = key };
            GameObject tiles = new GameObject() { name = "Tiles" };
            GameObject walls = new GameObject() { name = "Walls" };
            GameObject items = new GameObject() { name = "Items" };
            GameObject monsters = new GameObject() { name = "Monsters" };
            GameObject bossMonsters = new GameObject() { name = "BossMonsters" };
            GameObject decos = new GameObject() { name = "Deco" };
            GameObject pillars = new GameObject() { name = "Pillars" };

            parent.transform.localPosition += new Vector3(count * 100, 0, 0);
            parent.transform.parent = map.transform;
            tiles.transform.parent = parent.transform;
            walls.transform.parent = parent.transform;
            items.transform.parent = parent.transform;
            monsters.transform.parent = parent.transform;
            bossMonsters.transform.parent = parent.transform;
            decos.transform.parent = parent.transform;
            pillars.transform.parent = parent.transform;

            foreach (Data.TileData tile in mapData.Tiles)
            {
                #region 주석
                //if (tile is Occupied citemTile && citemTile.Type == (int)Define.OccupiedType.CItem)
                //{
                //    GameObject go = Managers.Resource.Instantiate($"Tilemap_{citemTile.PrefabID}", tiles.transform);
                //    go.transform.position = new Vector3(citemTile.Position.X, citemTile.Position.Y, citemTile.Position.Z);

                //    GameObject item = Managers.Resource.Instantiate("ConsumableItem", items.transform);
                //    item.transform.position = go.transform.position;
                //    item.GetComponent<ConsumableItem>().id = citemTile.Index;
                //    item.name = $"CItem{citemTile.TotalCount}";
                //    item.GetComponent<ConsumableItem>()._itemIndex_forActive = citemTile.TotalCount;

                //    if (citemTile.IsActive == false)
                //        item.SetActive(false);
                //}
                //else if (tile is Occupied eitemTile && eitemTile.Type == (int)Define.OccupiedType.EItem)
                //{
                //    GameObject go = Managers.Resource.Instantiate($"Tilemap_{eitemTile.PrefabID}", tiles.transform);
                //    go.transform.position = new Vector3(eitemTile.Position.X, eitemTile.Position.Y, eitemTile.Position.Z);

                //    GameObject item = Managers.Resource.Instantiate("EquipItem", items.transform);
                //    item.transform.position = go.transform.position;
                //    item.GetComponent<Equip>().Id = eitemTile.Index;
                //    item.name = $"EItem{eitemTile.TotalCount}";
                //    item.GetComponent<Equip>()._itemIndex_forActive = eitemTile.TotalCount;

                //    if (eitemTile.IsActive == false)
                //        item.SetActive(false);
                //}
                //else if (tile is Occupied monsterTile && monsterTile.Type == (int)Define.OccupiedType.Monster)
                //{
                //    GameObject go = Managers.Resource.Instantiate($"Tilemap_{monsterTile.PrefabID}", tiles.transform);
                //    go.transform.position = new Vector3(monsterTile.Position.X, monsterTile.Position.Y, monsterTile.Position.Z);

                //    GameObject monster = Managers.Resource.Instantiate("Monster", monsters.transform);
                //    monster.transform.position = go.transform.position;
                //    monster.GetComponent<MonsterController>().id = monsterTile.Index;
                //    monster.name = $"monster{monsterTile.TotalCount}";
                //    monster.GetComponent<MonsterController>()._monsterIndex_forActive = monsterTile.TotalCount;

                //    if (monsterTile.IsActive == false)
                //        monster.SetActive(false);
                //}
                //else if (tile is Occupied bossMonsterTile && bossMonsterTile.Type == (int)Define.OccupiedType.Boss)
                //{
                //    GameObject go = Managers.Resource.Instantiate($"Tilemap_{bossMonsterTile.PrefabID}", tiles.transform);
                //    go.transform.position = new Vector3(bossMonsterTile.Position.X, bossMonsterTile.Position.Y, bossMonsterTile.Position.Z);

                //    GameObject boss = Managers.Resource.Instantiate("BossMonster", bossMonsters.transform);
                //    boss.transform.position = go.transform.position;
                //    boss.GetComponent<BossMonsterController>().id = bossMonsterTile.Index;
                //    boss.name = $"bossMonster{bossMonsterTile.TotalCount}";
                //    boss.GetComponent<BossMonsterController>()._monsterIndex_forActive = bossMonsterTile.TotalCount;

                //    int id = boss.GetComponent<BossMonsterController>().id;
                //    string name = Managers.Data.MonsterDic[id].Name;
                //    switch (name)
                //    {
                //        case "블랙슬라임":
                //            boss.AddComponent<BlackSlimeController>();
                //            break;
                //        default:
                //            break;
                //    }

                //    if (bossMonsterTile.IsActive == false)
                //        boss.SetActive(false);
                //}
                #endregion

                if (tile is DoorData doorTile)
                {
                    GameObject go = Instantiate<GameObject>(Tilemap[1], tiles.transform);
                    go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);

                    GameObject door = Instantiate<GameObject>(Tilemap[doorTile.PrefabID], tiles.transform);
                    door.transform.position = new Vector3(doorTile.Position.X, doorTile.Position.Y - Define.TILE_SIZE / 2, tile.Position.Z);
                    door.name = $"door{doorTile.TotalCount}";

                    if (doorTile.IsActive == false)
                        door.SetActive(false);
                }
                else if (tile is StairsData stairsTile)
                {
                    GameObject stairs = Instantiate(Tilemap[stairsTile.PrefabID], tiles.transform);
                    stairs.name = "portal";
                    stairs.GetComponentInChildren<PortalController>()._stairs = stairsTile.StairsType;

                    if(stairsTile.PrefabID == 14 || stairsTile.PrefabID == 15 || stairsTile.PrefabID == 16)
                    {
                        stairs.transform.position = new Vector3(stairsTile.Position.X, stairsTile.Position.Y, stairsTile.Position.Z);
                    }
                    else if (stairsTile.StairsType == (int)Define.Stairs.Downstairs)
                    {
                        stairs.transform.position = new Vector3(stairsTile.Position.X, stairsTile.Position.Y - Define.TILE_SIZE * 1.5f, stairsTile.Position.Z);
                    }
                    else
                    {
                        GameObject go = Instantiate(Tilemap[1], tiles.transform);
                        go.transform.position = new Vector3(stairsTile.Position.X, stairsTile.Position.Y, stairsTile.Position.Z);

                        stairs.transform.position = new Vector3(stairsTile.Position.X, stairsTile.Position.Y - Define.TILE_SIZE / 2, stairsTile.Position.Z);
                    }

                }
                else if (tile is LeverData leverTile)
                {
                    GameObject go = Instantiate(Tilemap[1], tiles.transform);
                    go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);
                }
                else if (tile is PillarData pillarTile)
                {
                    GameObject go = Instantiate(Tilemap[1], tiles.transform);
                    go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);

                    GameObject pillar = Instantiate(Tilemap[pillarTile.PrefabID], pillars.transform);
                    pillar.name = $"pillar{pillarTile.TotalCount}";
                    pillar.transform.position = new Vector3(pillarTile.Position.X, pillarTile.Position.Y - Define.TILE_SIZE / 2, pillarTile.Position.Z);

                    if (pillarTile.IsActive == false)
                    {
                        pillar.transform.GetChild(1).gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (tile.TileType == (int)Define.TileType.Wall)
                    {
                        GameObject go = Instantiate(Tilemap[1], tiles.transform);
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);

                        GameObject wall = Instantiate(Tilemap[tile.PrefabID + 20], walls.transform);
                        wall.transform.position = new Vector3(tile.Position.X, tile.Position.Y - Define.TILE_SIZE / 2, tile.Position.Z);
                    }
                    else if (tile.TileType == (int)Define.TileType.Void)
                    {
                        GameObject go = Instantiate(Tilemap[tile.PrefabID], tiles.transform);
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y - Define.TILE_SIZE / 2, tile.Position.Z);
                    }
                    else if (tile.PrefabID == (int)Define.TileType.Floor)
                    {
                        GameObject go = Instantiate(Tilemap[tile.PrefabID], tiles.transform);
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);
                    }
                    else if (tile.PrefabID == (int)Define.TileType.SpawnPoint)
                    {
                        GameObject go = Instantiate(Tilemap[tile.PrefabID], tiles.transform);
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);
                    }
                    else if (tile.PrefabID == (int)Define.TileType.VoidTile)
                    {
                        GameObject go = Instantiate(VoidTile, tiles.transform);
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);
                    }
                    else if (tile.PrefabID == (int)Define.TileType.ObjectTile)
                    {
                        GameObject go = Instantiate(ObjectTile, tiles.transform);
                        go.name = "ObjectTile";
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);
                    }
                }
            }

            #region BG
            Sprite BGSprite = Resources.Load<Sprite>($"Sprites/{mapName.Substring(8, 2)}/FloorField_{mapName.Substring(8)}");
            GameObject BG = new GameObject() { name = "BG"};
            BG.transform.parent = decos.transform;
            if(mapName.Substring(8) == "00_002")
                BG.transform.localPosition = new Vector3(-0.16f, 0, -0.16f);
            else
                BG.transform.localPosition = new Vector3(-0.16f, 0, 0.16f);

            BG.transform.rotation = Quaternion.Euler(new Vector3(90f, 0f, 0f));
            BG.AddComponent<SpriteRenderer>().sprite = BGSprite;
            BG.GetComponent<SpriteRenderer>().material = Resources.Load<Material>("SpriteShadowsMaterial");
            #endregion

            walls.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            //items.transform.localPosition = items.transform.localPosition + new Vector3(0f, 1.6f, -0.4f);
            //monsters.transform.localPosition = monsters.transform.localPosition + new Vector3(0f, 3f, -1.1f);
            //Camera.main.GetComponentInChildren<CameraController>().AdjustCameraPitch(Define.CAMERA_ANGLE, items);
            //Camera.main.GetComponentInChildren<CameraController>().AdjustCameraPitch(Define.CAMERA_ANGLE, monsters);
            //Camera.main.GetComponentInChildren<CameraController>().AdjustCameraPitch(Define.CAMERA_ANGLE, lights);
            count++;
        }
    }
    #endregion
}
#endif