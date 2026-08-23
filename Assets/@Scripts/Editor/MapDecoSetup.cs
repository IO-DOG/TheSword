using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

/// <summary>
/// 층마다 장식 프리팹(Deco_CC_FFF)을 만든다.
///
/// 왜 맵 데이터가 아니라 프리팹인가
/// --------------------------------
/// 장식은 놀이에 영향을 주지 않는다. 그런데 맵 데이터(MapData)에 섞어 넣으면
/// 밸런스·경로 검증을 돌릴 때마다 장식까지 딸려 다니고, 미술을 손보려면 데이터
/// 생성기를 다시 돌려야 한다. 손수 만든 1~4층이 이미 Deco_00_000 처럼 층 이름과
/// 짝지은 별도 프리팹을 쓰고 있어서, 생성 층도 같은 규칙을 따른다.
///
///   맵  Dungeon_00_004   ...   장식  Deco_00_004
///
/// 미술을 바꾸고 싶으면 그 프리팹만 열면 된다. 맵도 데이터도 건드릴 필요가 없다.
///
///   Unity.exe -projectPath . -executeMethod MapDecoSetup.Build
/// </summary>
public static class MapDecoSetup
{
    const string DecoDir = "Assets/@Resources/Maps/Deco";
    const string StreamDir = "Assets/StreamingAssets/Data/Excel";
    const string Label = "PreLoad";
    const string GroupName = "Prefabs";
    const float TileSize = 0.32f;

    // 손수 만든 도입부는 이미 미술이 들어가 있다. 건드리지 않는다.
    static readonly HashSet<string> HandAuthored = new HashSet<string>
    {
        "00_000", "00_001", "00_002", "00_003",
    };

    /// <summary>챕터마다 다른 것을 건다. 스무 층이 지나면 눈에 띄게 달라야 한다.</summary>
    struct ChapterDeco
    {
        public string Wall;
        public string Corner;
        public int WallEvery;
        public Color Light;
        public float LightRange;
    }

    static readonly ChapterDeco[] Chapters =
    {
        // 00 이끼 낀 지하 묘소 - 횃불이 촘촘하다. 아직은 사람이 살던 곳이다.
        new ChapterDeco { Wall = "Deco_Torch", Corner = "Deco_FireBowl", WallEvery = 5,
                          Light = new Color(1.00f, 0.82f, 0.55f), LightRange = 2.6f },
        // 01 무너진 수로 - 불이 드물고 푸르다.
        new ChapterDeco { Wall = "Deco_Torch", Corner = "Deco_Handcuff", WallEvery = 7,
                          Light = new Color(0.62f, 0.85f, 1.00f), LightRange = 2.2f },
        // 02 잿빛 용광로 - 화로가 많고 붉다.
        new ChapterDeco { Wall = "Deco_FireBowl", Corner = "Deco_FireBowl", WallEvery = 4,
                          Light = new Color(1.00f, 0.60f, 0.35f), LightRange = 3.0f },
        // 03 얼어붙은 심층 - 빛이 거의 없다. 차갑다.
        new ChapterDeco { Wall = "Deco_Torch", Corner = "Deco_Handcuff", WallEvery = 9,
                          Light = new Color(0.72f, 0.90f, 1.00f), LightRange = 2.0f },
        // 04 왕좌의 균열 - 쇠사슬과 보랏빛. 사람의 흔적이 아니다.
        new ChapterDeco { Wall = "Deco_Handcuff", Corner = "Deco_FireBowl", WallEvery = 6,
                          Light = new Color(0.72f, 0.55f, 1.00f), LightRange = 2.4f },
    };

    [MenuItem("TheSword/Build Map Decorations")]
    public static void Build()
    {
        Directory.CreateDirectory(DecoDir);

        int made = 0;
        var addresses = new List<KeyValuePair<string, string>>();

        foreach (string csv in Directory.GetFiles(StreamDir, "Dungeon_*.csv"))
        {
            string dungeonId = Path.GetFileNameWithoutExtension(csv).Substring("Dungeon_".Length);
            if (HandAuthored.Contains(dungeonId))
                continue;

            int chapter;
            if (int.TryParse(dungeonId.Substring(0, 2), out chapter) == false)
                continue;

            GameObject root = BuildOne(dungeonId, chapter, ReadGrid(csv));
            if (root == null)
                continue;

            string path = DecoDir + "/Deco_" + dungeonId + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            addresses.Add(new KeyValuePair<string, string>("Deco_" + dungeonId, path));
            made++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Register(addresses);

        Debug.Log("[MapDeco] 장식 프리팹 " + made + "개 생성 (Deco_CC_FFF)");
    }

    static string[][] ReadGrid(string csv)
    {
        string[] lines = File.ReadAllLines(csv);
        var grid = new string[lines.Length][];
        for (int i = 0; i < lines.Length; i++)
            grid[i] = lines[i].Split(',');
        return grid;
    }

    static bool IsWall(string[][] grid, int x, int y)
    {
        if (y < 0 || y >= grid.Length || x < 0 || x >= grid[y].Length)
            return false;
        return grid[y][x].Trim().StartsWith("W");
    }

    static bool IsOpen(string[][] grid, int x, int y)
    {
        if (y < 0 || y >= grid.Length || x < 0 || x >= grid[y].Length)
            return false;
        string c = grid[y][x].Trim();
        return c.Length > 0 && c != "0" && c.StartsWith("W") == false;
    }

    static GameObject BuildOne(string dungeonId, int chapter, string[][] grid)
    {
        ChapterDeco deco = Chapters[Mathf.Clamp(chapter, 0, Chapters.Length - 1)];

        GameObject root = new GameObject("Deco_" + dungeonId);
        // 층마다 같은 자리에 같은 것이 놓이도록 씨앗을 고정한다.
        System.Random rng = new System.Random(dungeonId.GetHashCode());

        int seen = 0;
        int placed = 0;

        for (int y = 0; y < grid.Length; y++)
        {
            for (int x = 0; x < grid[y].Length; x++)
            {
                if (IsWall(grid, x, y) == false)
                    continue;

                // 길에 면한 벽에만 건다. 뒤쪽 벽에 걸면 보이지도 않는다.
                bool facesFloor = IsOpen(grid, x, y + 1) || IsOpen(grid, x, y - 1)
                                  || IsOpen(grid, x + 1, y) || IsOpen(grid, x - 1, y);
                if (facesFloor == false)
                    continue;

                seen++;
                if (seen % deco.WallEvery != 0)
                    continue;

                Vector3 pos = new Vector3(x * TileSize, 0f, -y * TileSize);
                Attach(root.transform, deco.Wall, pos);
                AddLight(root.transform, pos, deco);
                placed++;
            }
        }

        // 구석 장식은 드물게. 층마다 한둘이면 충분하다.
        int corners = 1 + (rng.Next() % 2);
        for (int i = 0; i < corners; i++)
        {
            int cx = rng.Next() % Mathf.Max(1, grid[0].Length);
            int cy = rng.Next() % Mathf.Max(1, grid.Length);
            if (IsOpen(grid, cx, cy) == false)
                continue;
            Attach(root.transform, deco.Corner, new Vector3(cx * TileSize, 0f, -cy * TileSize));
        }

        // 위층 계단 위에 빛기둥. 어디로 가야 하는지가 멀리서도 보인다.
        for (int y = 0; y < grid.Length; y++)
        {
            for (int x = 0; x < grid[y].Length; x++)
            {
                if (grid[y][x].Trim() != "14")
                    continue;
                Attach(root.transform, "Deco_GodRay", new Vector3(x * TileSize, 0f, -y * TileSize));
            }
        }

        if (placed == 0 && root.transform.childCount == 0)
        {
            Object.DestroyImmediate(root);
            return null;
        }
        return root;
    }

    static void Attach(Transform parent, string address, Vector3 pos)
    {
        GameObject prefab = FindByAddress(address);
        if (prefab == null)
            return;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = pos;
    }

    static void AddLight(Transform parent, Vector3 pos, ChapterDeco deco)
    {
        GameObject prefab = FindByAddress("Deco_PointLight");
        if (prefab == null)
            return;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = pos + new Vector3(0f, 0.4f, 0f);

        Light light = go.GetComponentInChildren<Light>();
        if (light != null)
        {
            light.color = deco.Light;
            light.range = deco.LightRange;
        }
    }

    static readonly Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();

    /// <summary>어드레서블 주소로 프리팹을 찾는다. 주소는 파일 이름과 다를 수 있다.</summary>
    static GameObject FindByAddress(string address)
    {
        GameObject found;
        if (_cache.TryGetValue(address, out found))
            return found;

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;
                foreach (var entry in group.entries)
                {
                    if (entry.address != address)
                        continue;
                    found = AssetDatabase.LoadAssetAtPath<GameObject>(entry.AssetPath);
                    _cache[address] = found;
                    return found;
                }
            }
        }

        Debug.LogWarning("[MapDeco] 주소를 못 찾았다: " + address);
        _cache[address] = null;
        return null;
    }

    /// <summary>만든 장식 프리팹을 어드레서블에 등록한다. 안 하면 런타임에 못 찾는다.</summary>
    static void Register(List<KeyValuePair<string, string>> items)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[MapDeco] 어드레서블 설정이 없다");
            return;
        }

        var group = settings.FindGroup(GroupName);
        if (group == null)
            group = settings.DefaultGroup;

        int added = 0;
        foreach (var item in items)
        {
            string guid = AssetDatabase.AssetPathToGUID(item.Value);
            if (string.IsNullOrEmpty(guid))
                continue;

            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = item.Key;
            entry.SetLabel(Label, true, true);
            added++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[MapDeco] 어드레서블 등록 " + added + "개 (" + Label + " 라벨)");
    }
}
