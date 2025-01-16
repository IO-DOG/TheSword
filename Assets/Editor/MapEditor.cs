
using Codice.CM.SEIDInfo;
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
using UnityEditor.U2D.Aseprite;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

public class MapEditor : EditorWindow
{
    public Dictionary<int, Data.MapData> MapDic { get; set; } = new Dictionary<int, Data.MapData>();
    public Dictionary<int, Data.StageInfoData> StageInfoDic { get; set; } = new Dictionary<int, Data.StageInfoData>();
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
        MapDic = LoadJson<Data.MapDataLoader, int, Data.MapData>("MapData").MakeDict();
        StageInfoDic = LoadJson<Data.StageInfoDataLoader, int, Data.StageInfoData>("StageInfoData").MakeDict();

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
            int key = StageInfoDic.FirstOrDefault(kvp => kvp.Value.DungeonID == selectedMapName).Key;
            GenerateMap(key, selectedTileSet);
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

    void GenerateMap(int mapId, string tileSet)
    {
        if (tileSet == "Empty")
        {
            Debug.LogWarning("Tile set is empty!");
            return;
        }

        SelectedTileMap = AssetDatabase.LoadAssetAtPath<WallData>($"Assets/@Resources/Data/TileSet/{tileSet}.asset").wallPrefabs;

        foreach (GameObject go in SelectedTileMap)
        {
            if (go.name != "Tilemap_C0_W000")
                go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            else
                go.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
        }

        if (MapDic.ContainsKey(mapId))
        {
            #region 하이어라키
            GameObject parent = new GameObject() { name = StageInfoDic[mapId].DungeonID };
            GameObject portals = new GameObject() { name = "Portals" };
            GameObject walls = new GameObject() { name = "Walls" };
            GameObject items = new GameObject() { name = "Items" };
            GameObject monsters = new GameObject() { name = "Monsters" };
            GameObject bossMonsters = new GameObject() { name = "BossMonsters" };
            GameObject decos = new GameObject() { name = "Decos" };
            GameObject pillars = new GameObject() { name = "Pillars" };
            GameObject levers = new GameObject() { name = "Levers" };
            GameObject doors = new GameObject() { name = "Doors" };
            GameObject etcs = new GameObject() { name = "Etcs" };

            portals.transform.parent = parent.transform;
            walls.transform.parent = parent.transform;
            items.transform.parent = parent.transform;
            monsters.transform.parent = parent.transform;
            bossMonsters.transform.parent = parent.transform;
            decos.transform.parent = parent.transform;
            pillars.transform.parent = parent.transform;
            levers.transform.parent = parent.transform;
            doors.transform.parent = parent.transform;
            etcs.transform.parent = parent.transform;
            #endregion

            MapData mapData = MapDic[mapId];

            #region 데이터 돌면서 맵 생성
            foreach (Data.ObjectData objectData in mapData.Objects)
            {
                if (objectData.ObjectType == (int)Define.ObjectType.Door)
                {
                    GameObject door = Instantiate(defaulTileMap[objectData.Id], doors.transform);
                    door.transform.position = new Vector3(objectData.Position.X, objectData.Position.Y - Define.TILE_SIZE / 4, objectData.Position.Z);
                    door.name = $"door{objectData.Count}";
                }
                else if (objectData.ObjectType == (int)Define.ObjectType.CItem)
                {
                    GameObject item = Instantiate(ConsumableItem, items.transform);
                    item.transform.localPosition = new Vector3(objectData.Position.X, objectData.Position.Y, objectData.Position.Z);
                    item.GetComponent<ConsumableItem>().id = objectData.Id;
                    item.name = $"CItem{objectData.Count}";
                    item.GetComponent<ConsumableItem>()._itemIndex_forActive = objectData.Count;
                }
                else if (objectData.ObjectType == (int)Define.ObjectType.Eitem)
                {
                    GameObject item = Instantiate(EquipItem, items.transform);
                    item.transform.localPosition = new Vector3(objectData.Position.X, objectData.Position.Y, objectData.Position.Z);
                    item.GetComponent<Equip>().Id = objectData.Id;
                    item.name = $"EItem{objectData.Count}";
                    item.GetComponent<Equip>()._itemIndex_forActive = objectData.Count;
                }
                else if (objectData.ObjectType == (int)Define.ObjectType.Monster)
                {
                    GameObject monster = Instantiate(Monster, monsters.transform);
                    monster.transform.localPosition = new Vector3(objectData.Position.X, objectData.Position.Y, objectData.Position.Z);
                    monster.GetComponent<MonsterController>().id = objectData.Id;
                    monster.name = $"monster{objectData.Count}";
                    monster.GetComponent<MonsterController>()._monsterIndex_forActive = objectData.Count;
                }
                else if (objectData.ObjectType == (int)Define.ObjectType.BossMonster)
                {
                    GameObject boss = Instantiate(BossMonster, bossMonsters.transform);
                    boss.transform.localPosition = new Vector3(objectData.Position.X, objectData.Position.Y, objectData.Position.Z);
                    boss.GetComponent<MonsterController>().id = (int)Define.Boss.KingSlime;
                    boss.GetComponent<SpriteRenderer>().enabled = false;    
                    boss.name = $"bossMonster{objectData.Id}";

                    int bossId = (int)Define.Boss.KingSlime;
                    switch (bossId)
                    {
                        case (int)Define.Boss.KingSlime:
                            boss.GetComponent<MonsterController>().PromoteToBoss(typeof(KingSlimeController));
                            //boss.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);
                            boss.transform.localPosition += new Vector3(0, 1.7f, -1.84f);
                            boss.GetOrAddComponent<BoxCollider>().center = new Vector3(0, -0.4f, 0);
                            boss.GetOrAddComponent<BoxCollider>().size = new Vector3(1.5f, 3f, 0.2f);
                            break;
                        default:
                            break;
                    }
                }
                else if (objectData.ObjectType == (int)Define.ObjectType.Portal)
                {
                    GameObject portal = Instantiate(defaulTileMap[objectData.Id], portals.transform);
                    portal.gameObject.name = $"portal{objectData.Count}";
                    portal.gameObject.transform.position = new Vector3(objectData.Position.X, objectData.Position.Y, objectData.Position.Z);
                    portal.GetComponentInChildren<PortalController>()._mapId = mapId;

                    if (objectData.Id == 14)
                        portal.GetComponentInChildren<PortalController>()._portalType = PortalController.Type.UpStairs;
                    else if (objectData.Id == 15)
                        portal.GetComponentInChildren<PortalController>()._portalType = PortalController.Type.DownStairs;
                    else
                        portal.GetComponentInChildren<PortalController>()._portalType = PortalController.Type.Boss;    
                }
                else if (objectData.ObjectType == (int)Define.ObjectType.Lever)
                {
                    GameObject lever = Instantiate(defaulTileMap[objectData.Id], levers.transform);
                    lever.GetComponentInChildren<Lever>()._leverIndex_forActive = objectData.Count;
                    lever.name = $"portal{objectData.Count}";
                    lever.transform.position = new Vector3(objectData.Position.X, objectData.Position.Y - Define.TILE_SIZE / 2, objectData.Position.Z);
                }
                else if (objectData.ObjectType == (int)Define.ObjectType.Pillar)
                {
                    GameObject pillar = Instantiate(defaulTileMap[objectData.Id], pillars.transform);
                    pillar.GetComponentInChildren<Pillar>()._pillarIndex_forActive = objectData.Count;
                    pillar.name = $"pillar{objectData.Count}";
                    pillar.transform.position = new Vector3(objectData.Position.X, objectData.Position.Y - Define.TILE_SIZE / 2, objectData.Position.Z);
                }
                else
                {
                    if (objectData.ObjectType == (int)Define.ObjectType.Wall)
                    {
                        GameObject wall = Instantiate(SelectedTileMap[objectData.Id], walls.transform);
                        if (wall.name.Contains("Tilemap_C0_W00"))
                            wall.transform.position = new Vector3(objectData.Position.X, wall.transform.position.y, objectData.Position.Z);
                        else
                            wall.transform.position = new Vector3(objectData.Position.X, wall.transform.position.y - Define.TILE_SIZE / 2, objectData.Position.Z);
                    }
                    else if (objectData.ObjectType == (int)Define.ObjectType.Void)
                    {
                        GameObject go = Instantiate(defaulTileMap[objectData.Id], etcs.transform);
                        go.transform.position = new Vector3(objectData.Position.X, objectData.Position.Y - Define.TILE_SIZE / 2, objectData.Position.Z);
                    }
                    else if (objectData.ObjectType == (int)Define.ObjectType.SpawnPoint)
                    {
                        GameObject go = Instantiate(defaulTileMap[objectData.Id], etcs.transform);
                        go.transform.position = new Vector3(objectData.Position.X, objectData.Position.Y, objectData.Position.Z);
                        go.tag = "SpawnPoint";
                    }
                }

            }
            #endregion

            #region BG
            string mapName = StageInfoDic[mapId].DungeonID;
            Sprite BGSprite = Resources.Load<Sprite>($"Sprites/{mapName.Substring(0, 2)}/FloorField_{mapName}");
            GameObject BG = new GameObject() { name = "BG" };
            BG.transform.parent = decos.transform;
            BG.transform.localPosition = new Vector3(-0.16f, 0, 0.16f);
            BG.transform.rotation = Quaternion.Euler(new Vector3(90f, 0f, 0f));
            BG.AddComponent<SpriteRenderer>().sprite = BGSprite;
            BG.GetComponent<SpriteRenderer>().material = Resources.Load<Material>("SpriteShadowsMaterial");
            BG.tag = "BG";
            #endregion

            #region DECO
            GameObject deco = Resources.Load<GameObject>($"DecoPrefabs/Deco_{mapName}");
            if(deco != null)
            {
                GameObject go = Instantiate<GameObject>(deco, decos.transform);
                go.name = "Deco";
            }

            #endregion

            walls.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            items.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            monsters.transform.localPosition = new Vector3(0f, 0f, -0.1f);

            string mapPrefabPath = $"Assets/@Resources/Maps/Dungeon_{mapName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(parent, mapPrefabPath);
            DestroyImmediate(GameObject.Find(parent.name));

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);

            var group = settings.FindGroup("Maps");
            var guid = AssetDatabase.AssetPathToGUID(mapPrefabPath);
            var ent = settings.CreateOrMoveEntry(guid, group);
            ent.address = "Dungeon_" + mapName;
            ent.SetLabel("PreLoad", true);

            EditorUtility.SetDirty(settings);

            AssetDatabase.SaveAssets();
        }
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
                                    .Select(mapObj => int.Parse(mapObj["Key"].ToString()))
                                    .ToList();

                List<string> dungeonName = new List<string>();

                for(int i =0; i < keys.Count; i++)
                {
                    dungeonName.Add(StageInfoDic[keys[i]].DungeonID);
                }

                return dungeonName;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("JSON 파싱 오류: " + ex.Message);
        }

        return null;
    }

    Loader LoadJson<Loader, Key, Value>(string fileName) where Loader : ILoader<Key, Value>
    {
        string textAsset = File.ReadAllText($"{Application.dataPath}/@Resources/Data/JsonData/{fileName}.json");

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
