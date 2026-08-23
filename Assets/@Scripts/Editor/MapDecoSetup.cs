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

    /// <summary>
    /// 타일 아트는 한 칸을 3.2 유닛으로 그려 놓았고, 맵은 0.32 간격으로 조립한다.
    /// 그래서 벽 프리팹(Tilemap_C00_W02)이 0.1 로 놓여 있다 — 같은 팩에서 나온
    /// 장식도 같은 값이어야 한 칸에 맞는다. 1 로 두면 사슬 하나가 여섯 칸을 덮는다.
    /// </summary>
    const float TileScale = 0.1f;

    // 손수 만든 도입부는 이미 미술이 들어가 있다. 건드리지 않는다.
    static readonly HashSet<string> HandAuthored = new HashSet<string>
    {
        "00_000", "00_001", "00_002", "00_003",
    };

    /// <summary>챕터마다 다른 것을 건다. 스무 층이 지나면 눈에 띄게 달라야 한다.</summary>
    struct ChapterDeco
    {
        public string Fire;        // 불. 층마다 몇 개 안 놓고, 하나마다 점광원을 단다
        public int FireCount;      // 층당 개수 — 넷다섯이면 충분하다
        public bool OnFloor;       // 화로는 바닥에 놓고, 횃불은 벽에 건다
        public string Ambient;     // 불이 아닌 소품. 광원이 없어 얼마가 있어도 싸다
        public int AmbientEvery;   // 길에 면한 벽 몇 칸마다 하나
        public Color Light;
        public float LightRange;
    }

    static readonly ChapterDeco[] Chapters =
    {
        // 00 이끼 낀 지하 묘소 - 아직 사람이 살던 곳이다. 불이 가장 많다.
        new ChapterDeco { Fire = "Deco_Torch", FireCount = 5, OnFloor = false,
                          Ambient = "Deco_Handcuff", AmbientEvery = 12,
                          Light = new Color(1.00f, 0.82f, 0.55f), LightRange = 2.4f },
        // 01 무너진 수로 - 불이 드물고 푸르다.
        new ChapterDeco { Fire = "Deco_Torch", FireCount = 4, OnFloor = false,
                          Ambient = "Deco_Handcuff", AmbientEvery = 9,
                          Light = new Color(0.62f, 0.85f, 1.00f), LightRange = 2.0f },
        // 02 잿빛 용광로 - 바닥의 화로가 탄다. 붉다.
        new ChapterDeco { Fire = "Deco_FireBowl", FireCount = 5, OnFloor = true,
                          Ambient = "Deco_Handcuff", AmbientEvery = 14,
                          Light = new Color(1.00f, 0.60f, 0.35f), LightRange = 2.6f },
        // 03 얼어붙은 심층 - 빛이 거의 없다. 차갑다.
        new ChapterDeco { Fire = "Deco_Torch", FireCount = 3, OnFloor = false,
                          Ambient = "Deco_Handcuff", AmbientEvery = 11,
                          Light = new Color(0.72f, 0.90f, 1.00f), LightRange = 1.8f },
        // 04 왕좌의 균열 - 쇠사슬과 보랏빛. 사람의 흔적이 아니다.
        new ChapterDeco { Fire = "Deco_FireBowl", FireCount = 4, OnFloor = true,
                          Ambient = "Deco_Handcuff", AmbientEvery = 6,
                          Light = new Color(0.72f, 0.55f, 1.00f), LightRange = 2.2f },
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

        var walls = new List<Vector3>();   // 길에 면한 벽 — 거는 것
        var edges = new List<Vector3>();   // 벽에 붙은 바닥 — 놓는 것

        for (int y = 0; y < grid.Length; y++)
        {
            for (int x = 0; x < grid[y].Length; x++)
            {
                bool wall = IsWall(grid, x, y);
                bool open = IsOpen(grid, x, y);
                if (wall == false && open == false)
                    continue;

                bool touchesWall = IsWall(grid, x, y + 1) || IsWall(grid, x, y - 1)
                                   || IsWall(grid, x + 1, y) || IsWall(grid, x - 1, y);
                bool touchesFloor = IsOpen(grid, x, y + 1) || IsOpen(grid, x, y - 1)
                                    || IsOpen(grid, x + 1, y) || IsOpen(grid, x - 1, y);

                Vector3 pos = new Vector3(x * TileSize, 0f, -y * TileSize);
                if (wall && touchesFloor)
                    walls.Add(pos);          // 뒤쪽 벽에 걸면 보이지도 않는다
                else if (open && touchesWall)
                    edges.Add(pos);
            }
        }

        // 불은 층당 넷다섯이다.
        //
        // 처음에는 횃불마다 점광원을 달아 층마다 서른 개였다. 같은 100층 녹화가
        // 31분에서 57분으로 늘었고, 절반으로 줄여도 열다섯 개였다. URP 포워드
        // 렌더링에서 점광원은 화면에 몇 개가 겹치느냐로 값을 치른다. 그리고
        // 무엇보다, 그렇게 많으면 불이 장식이 아니라 배경이 된다.
        List<Vector3> fireCells = deco.OnFloor ? edges : walls;
        int fires = Mathf.Min(deco.FireCount, fireCells.Count);
        var used = new HashSet<int>();
        for (int i = 0; i < fires; i++)
        {
            int at = (int)((i + 0.5f) * fireCells.Count / fires);   // 층 전체에 고르게 흩는다
            used.Add(at);
            Attach(root.transform, deco.Fire, fireCells[at], TileScale);
            AddLight(root.transform, fireCells[at], deco);
        }

        // 나머지 소품은 광원이 없어 싸다. 벽을 훑으며 일정 간격으로 건다.
        for (int i = 0; i < walls.Count; i++)
        {
            if (i % deco.AmbientEvery != deco.AmbientEvery / 2)
                continue;
            if (deco.OnFloor == false && used.Contains(i))
                continue;
            Attach(root.transform, deco.Ambient, walls[i], TileScale);
        }

        // 위층 계단 위에 빛기둥. 어디로 가야 하는지가 멀리서도 보인다.
        // 이건 타일 팩이 아니라 파티클이라 축척을 건드리지 않는다.
        for (int y = 0; y < grid.Length; y++)
        {
            for (int x = 0; x < grid[y].Length; x++)
            {
                if (grid[y][x].Trim() != "14")
                    continue;
                Attach(root.transform, "Deco_GodRay", new Vector3(x * TileSize, 0f, -y * TileSize), 1f);
            }
        }

        if (root.transform.childCount == 0)
        {
            Object.DestroyImmediate(root);
            return null;
        }
        return root;
    }

    static void Attach(Transform parent, string address, Vector3 pos, float scale)
    {
        GameObject prefab = FindByAddress(address);
        if (prefab == null)
            return;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * scale;
    }

    static void AddLight(Transform parent, Vector3 pos, ChapterDeco deco)
    {
        GameObject prefab = FindByAddress("Deco_PointLight");
        if (prefab == null)
            return;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        // 불꽃이 있는 높이에 둔다. 벽은 0.2~0.5 를 차지하고, 횃불 불꽃은 그 아래쪽이다.
        go.transform.localPosition = pos + new Vector3(0f, deco.OnFloor ? 0.15f : 0.28f, 0f);

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
