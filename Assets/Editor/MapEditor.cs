using Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class MapEditor : EditorWindow
{
    public Dictionary<string, Data.MapData> MapDic { get; set; } = new Dictionary<string, Data.MapData>();
    private List<ObjectField> objectFields = new List<ObjectField>();
    private List<GameObject> defaulTileMap = new List<GameObject>();
    private List<GameObject> SelectedTileMap = new List<GameObject>();
    GameObject ConsumableItem;
    GameObject EquipItem;
    GameObject Monster;
    GameObject BossMonster;
    private GameObject VoidTile;
    private VisualElement m_RightPane;
    private WallData wallData;
    private TextField fileNameField;
    private string fileName;
    private int numberOfWall = 11;
    private int numOfdefaultTileMap = 17;

    enum EditMenu
    {
        CreateMap,
        EditTiles,
    }

    [MenuItem("Tools/Map Editor")]
    public static void ShowEditor()
    {
        EditorWindow wnd = GetWindow<MapEditor>();
        wnd.titleContent = new GUIContent("Map Editor");
    }

    private void OnEnable()
    {
        MapDic = LoadJson<Data.MapDataLoader, string, Data.MapData>().MakeDict();

        VoidTile = Resources.Load("DecoTiles/Tilemap_-1") as GameObject;
        for (int i = 0; i < numOfdefaultTileMap; i++)
        {
            defaulTileMap.Add(Resources.Load($"DecoTiles/Tilemap_{i}") as GameObject);
        }
        ConsumableItem = Resources.Load("ConsumableItem") as GameObject;
        EquipItem = Resources.Load("EquipItem") as GameObject;
        Monster = Resources.Load("Monster") as GameObject;
        BossMonster = Resources.Load("BossMonster") as GameObject;
    }

    public void CreateGUI()
    {
        var enumValues = new List<EditMenu>((EditMenu[])Enum.GetValues(typeof(EditMenu)));

        var splitView = new TwoPaneSplitView(0, 100, TwoPaneSplitViewOrientation.Horizontal);

        var leftPane = new ListView();
        leftPane.itemsSource = enumValues;
        splitView.Add(leftPane);

        m_RightPane = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
        splitView.Add(m_RightPane);
        rootVisualElement.Add(splitView);

        leftPane.makeItem = () =>
        {
            var label = new Label();
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            return label;
        };

        leftPane.bindItem = (menu, index) =>
        {
            (menu as Label).text = enumValues[index].ToString();
        };

        leftPane.selectionChanged += OnClickedMenu;
    }

    void OnClickedMenu(IEnumerable<object> selectedItems)
    {
        foreach (var item in selectedItems)
        {
            if (item is EditMenu menuItem)
            {
                switch (menuItem)
                {
                    case EditMenu.CreateMap:
                        ShowCreateMapWindow();
                        break;
                    case EditMenu.EditTiles:
                        ShowEditTilesWindow();
                        break;
                }
            }
        }
    }

    void ShowCreateMapWindow()
    {
        m_RightPane.Clear();

        List<string> mapList = ExtractMapKeys();

        var mapNameDropdown = new DropdownField(mapList, mapList[0]);
        mapNameDropdown.RegisterValueChangedCallback(evt => Debug.Log("Selected: " + evt.newValue));
        m_RightPane.Add(mapNameDropdown);

        List<string> tilesetList = ExtractTilesetNames();

        var TilesetDropdown = new DropdownField(tilesetList, tilesetList[0]);
        TilesetDropdown.RegisterValueChangedCallback(evt => Debug.Log("Selected: " + evt.newValue));
        m_RightPane.Add(TilesetDropdown);

        var generateBtn = new Button(() =>
        {
            string selectedTileSet = TilesetDropdown.value;
            string selectedMapName = mapNameDropdown.value;
            GenerateMap(selectedMapName, selectedTileSet);
        })
        { text = "Generate Map" };
        generateBtn.style.marginTop = 50;
        m_RightPane.Add(generateBtn);
    }
    void ShowEditTilesWindow()
    {
        m_RightPane.Clear();
        objectFields.Clear();

        fileNameField = new TextField("File Name:");
        fileNameField.value = "";
        fileNameField.RegisterValueChangedCallback(evt =>
        {
            fileName = evt.newValue;
        });
        m_RightPane.Add(fileNameField);

        for (int i = 0; i < numberOfWall; i++)
        {
            var objectField = new ObjectField($"W_{i.ToString().PadLeft(2, '0')}");
            objectField.objectType = typeof(GameObject); // 선택할 수 있는 타입을 GameObject로 설정
            objectField.allowSceneObjects = false; // 씬 오브젝트가 아닌 프리팹만 선택 가능

            objectField.RegisterValueChangedCallback(evt =>
            {
                GameObject selectedPrefab = evt.newValue as GameObject;
                if (selectedPrefab != null)
                {
                    Debug.Log("Selected Prefab: " + selectedPrefab.name);
                }
            });
            m_RightPane.Add(objectField);
            objectFields.Add(objectField);
        }
        var saveButton = new Button(SaveTileData) { text = "Save Tile Set" };
        saveButton.style.marginTop = 50;
        m_RightPane.Add(saveButton);
    }

    void GenerateMap(string mapName, string tileSet)
    {
        if (tileSet == "Empty")
        {
            Debug.LogWarning("Tile set is empty!");
            return;
        }
        SelectedTileMap = AssetDatabase.LoadAssetAtPath<WallData>($"Assets/@Resources/Data/TileSet/{tileSet}.asset").wallPrefabs;
        foreach (GameObject go in SelectedTileMap)
        {
            go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }

        int count = 0;

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
            tiles.transform.parent = parent.transform;
            walls.transform.parent = parent.transform;
            items.transform.parent = parent.transform;
            monsters.transform.parent = parent.transform;
            bossMonsters.transform.parent = parent.transform;
            decos.transform.parent = parent.transform;
            pillars.transform.parent = parent.transform;

            foreach (Data.TileData tile in mapData.Tiles)
            {
                if (tile is DoorData doorTile)
                {
                    GameObject go = Instantiate(defaulTileMap[1], tiles.transform);
                    go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);

                    GameObject door = Instantiate(defaulTileMap[doorTile.PrefabID], tiles.transform);
                    door.transform.position = new Vector3(doorTile.Position.X, doorTile.Position.Y - Define.TILE_SIZE / 4, tile.Position.Z);
                    door.name = $"door{doorTile.TotalCount}";

                    //if (doorTile.IsActive == false)
                    //    door.SetActive(false);
                }
                else if (tile is Occupied citemTile && citemTile.Type == (int)Define.OccupiedType.CItem)
                {
                    GameObject item = Instantiate(ConsumableItem, items.transform);
                    item.transform.localPosition = new Vector3(citemTile.Position.X, citemTile.Position.Y, citemTile.Position.Z);
                    item.GetComponent<ConsumableItem>().id = citemTile.Index;
                    item.name = $"CItem{citemTile.TotalCount}";
                    item.GetComponent<ConsumableItem>()._itemIndex_forActive = citemTile.TotalCount;

                    //if (Managers.Data.CItemActiveDic[citemTile.TotalCount] == false)
                    //    item.SetActive(false);
                }
                else if (tile is Occupied eitemTile && eitemTile.Type == (int)Define.OccupiedType.EItem)
                {
                    GameObject item = Instantiate(EquipItem, items.transform);
                    item.transform.localPosition = new Vector3(eitemTile.Position.X, eitemTile.Position.Y, eitemTile.Position.Z);
                    item.GetComponent<Equip>().Id = eitemTile.Index;
                    item.name = $"EItem{eitemTile.TotalCount}";
                    item.GetComponent<Equip>()._itemIndex_forActive = eitemTile.TotalCount;

                    //if (Managers.Data.EItemActiveDic[eitemTile.TotalCount] == false)
                    //    item.SetActive(false);
                }
                else if (tile is Occupied monsterTile && monsterTile.Type == (int)Define.OccupiedType.Monster)
                {
                    GameObject monster = Instantiate(Monster, monsters.transform);
                    monster.transform.localPosition = new Vector3(monsterTile.Position.X, monsterTile.Position.Y, monsterTile.Position.Z);
                    //monster.transform.localScale = monsters.transform.localPosition + new Vector3(0.8f, 0.8f, 1f);
                    monster.GetComponent<MonsterController>().id = monsterTile.Index;
                    monster.name = $"monster{monsterTile.TotalCount}";
                    monster.GetComponent<MonsterController>()._monsterIndex_forActive = monsterTile.TotalCount;

                    //if (Managers.Data.MonsterActiveDic[monsterTile.TotalCount] == false)
                    //    monster.SetActive(false);
                }
                else if (tile is Occupied bossMonsterTile && bossMonsterTile.Type == (int)Define.OccupiedType.Boss)
                {
                    GameObject boss = Instantiate(BossMonster, bossMonsters.transform);
                    boss.transform.localPosition = new Vector3(bossMonsterTile.Position.X, bossMonsterTile.Position.Y, bossMonsterTile.Position.Z);
                    int tileIndex = bossMonsterTile.Index;
                    switch (tileIndex)
                    {
                        case 0:
                            boss.GetComponent<BossMonsterController>().id = Define.KingSlime;
                            boss.gameObject.name = "KingSlime";
                            break;
                        default:
                            break;
                    }
                    boss.name = $"bossMonster{bossMonsterTile.TotalCount}";
                    boss.GetComponent<BossMonsterController>()._monsterIndex_forActive = bossMonsterTile.TotalCount;

                    int id = boss.GetComponent<BossMonsterController>().id;
                    //string name = Managers.Data.MonsterDic[id].Name;
                    switch (id)
                    {
                        case Define.KingSlime:
                            boss.AddComponent<BlackSlimeController>();
                            boss.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);
                            boss.transform.localPosition += new Vector3(0, 1.7f, -1.84f);
                            boss.GetOrAddComponent<BoxCollider>().center = new Vector3(0, -0.4f, 0);
                            boss.GetOrAddComponent<BoxCollider>().size = new Vector3(1.2f, 1.1f, -0.32f);
                            break;
                        default:
                            break;
                    }

                    //if (Managers.Data.BossMonsterActiveDic[bossMonsterTile.TotalCount] == false)
                    //    bossMonsters.SetActive(false);
                }
                else if (tile is StairsData stairsTile)
                {
                    GameObject stairs = Instantiate(defaulTileMap[stairsTile.PrefabID], tiles.transform);
                    stairs.name = "portal";
                    stairs.GetComponentInChildren<PortalController>()._stairs = stairsTile.StairsType;

                    if (stairsTile.PrefabID == 14 || stairsTile.PrefabID == 15 || stairsTile.PrefabID == 16)
                    {
                        stairs.transform.position = new Vector3(stairsTile.Position.X, stairsTile.Position.Y, stairsTile.Position.Z);
                    }
                    else if (stairsTile.StairsType == (int)Define.Stairs.Downstairs)
                    {
                        stairs.transform.position = new Vector3(stairsTile.Position.X, stairsTile.Position.Y - Define.TILE_SIZE * 1.5f, stairsTile.Position.Z);
                    }
                    else
                    {
                        GameObject go = Instantiate(defaulTileMap[1], tiles.transform);
                        go.transform.position = new Vector3(stairsTile.Position.X, stairsTile.Position.Y, stairsTile.Position.Z);

                        stairs.transform.position = new Vector3(stairsTile.Position.X, stairsTile.Position.Y - Define.TILE_SIZE / 2, stairsTile.Position.Z);
                    }

                }
                else if (tile is LeverData leverTile)
                {
                    GameObject go = Instantiate(defaulTileMap[1], tiles.transform);
                    go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);
                }
                else if (tile is PillarData pillarTile)
                {
                    GameObject go = Instantiate(defaulTileMap[1], tiles.transform);
                    go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);

                    GameObject pillar = Instantiate(defaulTileMap[pillarTile.PrefabID], pillars.transform);
                    pillar.name = $"pillar{pillarTile.TotalCount}";
                    pillar.transform.position = new Vector3(pillarTile.Position.X, pillarTile.Position.Y - Define.TILE_SIZE / 2, pillarTile.Position.Z);

                    //if (pillarTile.IsActive == false)
                    //{
                    //    pillar.transform.GetChild(1).gameObject.SetActive(false);
                    //}
                }
                else
                {
                    if (tile.TileType == (int)Define.TileType.Wall)
                    {
                        GameObject go = Instantiate(defaulTileMap[1], tiles.transform);
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);

                        GameObject wall = Instantiate(SelectedTileMap[tile.PrefabID], walls.transform);
                        wall.transform.position = new Vector3(tile.Position.X, tile.Position.Y - Define.TILE_SIZE / 2, tile.Position.Z);
                    }
                    else if (tile.TileType == (int)Define.TileType.Void)
                    {
                        GameObject go = Instantiate(defaulTileMap[tile.PrefabID], tiles.transform);
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y - Define.TILE_SIZE / 2, tile.Position.Z);
                    }
                    else if (tile.PrefabID == (int)Define.TileType.Floor)
                    {
                        GameObject go = Instantiate(defaulTileMap[tile.PrefabID], tiles.transform);
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);
                    }
                    else if (tile.PrefabID == (int)Define.TileType.SpawnPoint)
                    {
                        GameObject go = Instantiate(defaulTileMap[tile.PrefabID], tiles.transform);
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);
                    }
                    else if (tile.PrefabID == (int)Define.TileType.VoidTile)
                    {
                        GameObject go = Instantiate(VoidTile, tiles.transform);
                        go.transform.position = new Vector3(tile.Position.X, tile.Position.Y, tile.Position.Z);
                    }
                }

            }

            #region BG
            Sprite BGSprite = Resources.Load<Sprite>($"Sprites/{mapName.Substring(8, 2)}/FloorField_{mapName.Substring(8)}");
            GameObject BG = new GameObject() { name = "BG" };
            BG.transform.parent = decos.transform;
            if (mapName.Substring(8) == "00_002")
                BG.transform.localPosition = new Vector3(-0.16f, 0, -0.16f);
            else
                BG.transform.localPosition = new Vector3(-0.16f, 0, 0.16f);

            BG.transform.rotation = Quaternion.Euler(new Vector3(90f, 0f, 0f));
            BG.AddComponent<SpriteRenderer>().sprite = BGSprite;
            BG.GetComponent<SpriteRenderer>().material = Resources.Load<Material>("SpriteShadowsMaterial");
            #endregion

            walls.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            items.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            monsters.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            //Camera.main.GetComponentInChildren<CameraController>().ChangeView(Define.CAMERA_ANGLE, items);
            //Camera.main.GetComponentInChildren<CameraController>().ChangeView(Define.CAMERA_ANGLE, monsters);
            //Camera.main.GetComponentInChildren<CameraController>().ChangeView(Define.CAMERA_ANGLE, lights);
            count++;


            string mapPrefabPath = $"Assets/@Resources/Maps/{key}.prefab";
            PrefabUtility.SaveAsPrefabAsset(parent, mapPrefabPath);
            DestroyImmediate(GameObject.Find(parent.name));

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);

            var group = settings.FindGroup("Maps");
            var guid = AssetDatabase.AssetPathToGUID(mapPrefabPath);
            var ent = settings.CreateOrMoveEntry(guid, group);
            ent.address = key;
            ent.SetLabel("PreLoad", true);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

        }
        AssetDatabase.SaveAssets();
    }

    void SaveTileData()
    {
        wallData = CreateInstance<WallData>();

        wallData.wallPrefabs.Clear();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            Debug.LogWarning("파일 이름을 입력해 주세요.");
            return;
        }

        foreach (var objectField in objectFields)
        {
            if (objectField.value is GameObject selectedPrefab)
            {
                wallData.wallPrefabs.Add(selectedPrefab);
            }
        }

        Debug.Log(fileName);
        AssetDatabase.CreateAsset(wallData, $"Assets/@Resources/Data/TileSet/{fileName}.asset");
        AssetDatabase.SaveAssets();

        ShowEditTilesWindow();
    }

    List<string> ExtractMapKeys()
    {
        string filePath = Application.dataPath + "/@Resources/Data/JsonData/MapData.json"; // 예제 경로
        string jsonData = File.ReadAllText(filePath); // 파일에서 JSON 문자열 읽기

        try
        {
            var jObject = JObject.Parse(jsonData); // JSON 파싱

            if (jObject["maps"] is JArray mapsArray)
            {
                var keys = mapsArray.OfType<JObject>()
                                    .Select(mapObj => mapObj["Key"]?.ToString())
                                    .Where(name => !string.IsNullOrEmpty(name)) // null 또는 빈 값 필터링
                                    .ToList();

                return keys;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("JSON 파싱 오류: " + ex.Message);
        }

        return null;
    }

    Loader LoadJson<Loader, Key, Value>() where Loader : ILoader<Key, Value>
    {
        string textAsset = File.ReadAllText($"{Application.dataPath}/@Resources/Data/JsonData/MapData.json");

        return JsonConvert.DeserializeObject<Loader>(textAsset, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

    }


    List<string> ExtractTilesetNames()
    {
        string[] guids = AssetDatabase.FindAssets("", new[] { "Assets/@Resources/Data/TileSet" });
        List<string> names = new List<string>();

        if (guids.Length == 0)
            names.Add("Empty");
        else
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string assetName = System.IO.Path.GetFileNameWithoutExtension(path);
                names.Add(assetName);
            }
        }

        return names;
    }
}
