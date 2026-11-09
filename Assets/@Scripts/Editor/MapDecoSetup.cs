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
/// 새 그림(Meshy — Documents/TheSword_Meshy_Prompts.md)은 MeshyArtImport 가 Prop_C{챕터}_{자리} 로
/// 들여온다. 그 자리에 그림이 있으면 새 규칙으로 놓고, 없으면 옛 장식이 그대로 구워진다.
/// 새 규칙이 따로인 이유: 옛 횃불(±0.03)·사슬은 벽 칸 한가운데에 놓여 벽(±0.15, 높이 0.2~0.5) 속에
/// 들어가 있다. 새 것은 카메라가 보는 남쪽 벽면에 걸고(프리팹이 그 면으로 비켜 서 있다), 큰 것은 방 안쪽
/// 벽 한 칸을 대신하고, 바닥 것은 아무것도 없는 바닥에만 둔다.
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
    internal const float TileScale = 0.1f;

    /// <summary>새 소품의 자리 이름 — 파일 이름 Prop_C{챕터}_{자리} 의 끝. MeshyArtImport 는 이것만 받는다.</summary>
    internal static readonly string[] PropSlots = { "Light", "Light2", "Wall", "Floor", "Extra", "Big" };

    // 새 그림의 밀도. 큰 소품은 층에 둘(벽 한 칸씩), 바닥 소품은 벽에 붙은 빈 바닥 몇 칸마다 하나.
    const int BigPerFloor = 2;
    const int FloorEvery = 16;
    const int ExtraEvery = 40;

    // 새 불의 점광원 자리. 벽에 건 불은 남쪽 면(z -0.15) 바로 앞, 벽 높이의 가운데쯤 —
    // 옛 횃불처럼 벽 속에 두면 불이 걸린 그 면은 빛의 뒤쪽이라 밝아지지 않는다.
    static readonly Vector3 FaceGlow = new Vector3(0f, 0.32f, -0.22f);
    static readonly Vector3 FloorGlow = new Vector3(0f, 0.15f, 0f);
    static readonly Color EmberLight = new Color(1.00f, 0.60f, 0.30f);   // 마지막 불씨(Light2)

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

    /// <summary>그 챕터의 불(Light)을 바닥에 놓는가 — 아니면 벽에 건다. MeshyArtImport 가 크기를 맞출 때 본다.</summary>
    internal static bool LightOnFloor(int chapter)
    {
        return Chapters[Mathf.Clamp(chapter, 0, Chapters.Length - 1)].OnFloor;
    }

    [MenuItem("TheSword/Build Map Decorations")]
    public static void Build()
    {
        // 같은 세션에서 새 그림을 들여온 뒤 다시 구우면, 지난번의 "그 주소 없음" 을 믿으면 안 된다.
        _cache.Clear();
        Directory.CreateDirectory(DecoDir);

        int made = 0;
        var addresses = new List<KeyValuePair<string, string>>();

        foreach (string csv in Directory.GetFiles(StreamDir, "Dungeon_*.csv"))
        {
            string dungeonId = Path.GetFileNameWithoutExtension(csv).Substring("Dungeon_".Length);
            if (MapBuilder.IsHandAuthored(dungeonId))   // 손수 만든 도입부는 이미 미술이 들어가 있다
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

    /// <summary>아무것도 서 있지 않은 바닥("1"). 몬스터·아이템·문·계단 칸에는 새 장식을 겹치지 않는다.</summary>
    static bool IsPlain(string[][] grid, int x, int y)
    {
        return y >= 0 && y < grid.Length && x >= 0 && x < grid[y].Length && grid[y][x].Trim() == "1";
    }

    /// <summary>바닥에 세우는 것(화로·바닥 소품)을 놓아도 되는 방 칸인가 — 폭 한 칸 길(통로·꺾인 모퉁이·기둥 옆 틈)도,
    /// 문·계단 바로 옆도 아니다. 콜라이더는 없지만 통로 한가운데나 문 앞에 서면 길을 막은 것처럼 보였다
    /// (2026-11-10, 43층 초록 문 바로 북쪽 통로 칸의 화로). 11-09 의 새 지도는 방 사이가 전부 이런 통로다.
    /// 처음엔 "마주 보는 두 쪽이 벽" 으로 쟀는데 꺾인 모퉁이 칸이 빠졌다(47층 화로) — 2x2 로 트인 덩어리에 드는 칸만 방이다.</summary>
    static bool IsRoomEdge(string[][] grid, int x, int y)
    {
        bool inRoom = false;
        for (int dx = -1; dx <= 0 && inRoom == false; dx++)
            for (int dy = -1; dy <= 0 && inRoom == false; dy++)
                inRoom = IsOpen(grid, x + dx, y + dy) && IsOpen(grid, x + dx + 1, y + dy)
                      && IsOpen(grid, x + dx, y + dy + 1) && IsOpen(grid, x + dx + 1, y + dy + 1);
        return inRoom
            && IsGate(grid, x - 1, y) == false && IsGate(grid, x + 1, y) == false
            && IsGate(grid, x, y - 1) == false && IsGate(grid, x, y + 1) == false;
    }

    /// <summary>문(3~8)·계단(14~16) 칸.</summary>
    static bool IsGate(string[][] grid, int x, int y)
    {
        if (y < 0 || y >= grid.Length || x < 0 || x >= grid[y].Length)
            return false;
        int code;
        return int.TryParse(grid[y][x].Trim(), out code) && ((code >= 3 && code <= 8) || (code >= 14 && code <= 16));
    }

    static GameObject BuildOne(string dungeonId, int chapter, string[][] grid)
    {
        ChapterDeco deco = Chapters[Mathf.Clamp(chapter, 0, Chapters.Length - 1)];

        GameObject root = new GameObject("Deco_" + dungeonId);

        var walls = new List<Vector3>();   // 길에 면한 벽 — 거는 것
        var edges = new List<Vector3>();   // 벽에 붙은 바닥 — 놓는 것

        // 새 그림이 고르는 자리. 옛 장식은 위 둘만 쓴다.
        var faces = new List<Vector3>();   // 남쪽(카메라 쪽)이 빈 바닥인 벽 — 그 면이 보인다
        var backs = new List<Vector3>();   // 그중 나머지 세 쪽도 벽인 칸 — 큰 소품이 벽 하나를 대신한다
        var floors = new List<Vector3>();  // 벽에 붙은 빈 바닥 — 몬스터·아이템·문·계단 칸은 뺀다

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
                else if (open && touchesWall && IsRoomEdge(grid, x, y))
                    edges.Add(pos);

                if (wall && IsPlain(grid, x, y + 1))
                {
                    faces.Add(pos);
                    if (IsWall(grid, x, y - 1) && IsWall(grid, x - 1, y) && IsWall(grid, x + 1, y))
                        backs.Add(pos);
                }
                else if (open && touchesWall && IsPlain(grid, x, y) && IsRoomEdge(grid, x, y))
                {
                    floors.Add(pos);
                }
            }
        }

        GameObject light = Art(chapter, "Light"), light2 = Art(chapter, "Light2"), hang = Art(chapter, "Wall");
        GameObject floor = Art(chapter, "Floor"), extra = Art(chapter, "Extra"), big = Art(chapter, "Big");
        var taken = new HashSet<Vector3>();   // 새 그림이 차지한 칸

        // 큰 소품(석관·물레바퀴 …)은 방 안쪽 벽 한 칸을 대신한다. 생성 층에는 "벽 옆 빈칸" 이 없다 —
        // 바닥에 닿은 빈 칸은 layout_gen 이 전부 벽으로 채운다. 걷는 칸에 세우면 길을 막는 것처럼
        // 보여 퍼즐이 거짓말을 한다. 그래서 벽(콜라이더)은 그대로 두고 MapBuilder 가 그 칸의 벽 그림만 끈다.
        if (big != null)
        {
            foreach (Vector3 p in Spread(backs, BigPerFloor))
            {
                Attach(root.transform, big, p, 1f).name = MapBuilder.WALL_SWAP;
                taken.Add(p);
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
        if (light == null)
        {
            for (int i = 0; i < fires; i++)
            {
                int at = (int)((i + 0.5f) * fireCells.Count / fires);   // 층 전체에 고르게 흩는다
                used.Add(at);
                Attach(root.transform, deco.Fire, fireCells[at], TileScale);
                AddLight(root.transform, fireCells[at], deco);
            }
        }
        else
        {
            // 새 불은 보이는 자리에만 놓고 점광원은 불마다 하나다 — 옛 횃불·화로 프리팹은
            // 제 안에 점광원을 하나 더 갖고 있었다(범위 20·10). 챕터 3 은 셋 + 마지막 불씨 하나.
            foreach (Vector3 p in Spread(Free(deco.OnFloor ? floors : faces, taken), deco.FireCount))
            {
                Attach(root.transform, light, p, 1f);
                AddLight(root.transform, p + (deco.OnFloor ? FloorGlow : FaceGlow), deco.Light, deco.LightRange);
                taken.Add(p);
            }
        }

        // 나머지 소품은 광원이 없어 싸다. 벽을 훑으며 일정 간격으로 건다.
        if (hang == null)
        {
            for (int i = 0; i < walls.Count; i++)
            {
                if (i % deco.AmbientEvery != deco.AmbientEvery / 2)
                    continue;
                if (deco.OnFloor == false && used.Contains(i))
                    continue;
                Attach(root.transform, deco.Ambient, walls[i], TileScale);
            }
        }
        else
        {
            Every(root.transform, hang, faces, taken, deco.AmbientEvery);
        }

        // 바닥 소품은 걷는 칸에 선다 — 그래서 발목 높이로 줄어 들어오고(MeshyArtImport) 콜라이더도 없다.
        Every(root.transform, floor, floors, taken, FloorEvery);
        Every(root.transform, extra, floors, taken, ExtraEvery);

        // 마지막 불씨(챕터 3) — 층에 하나. 차가운 챕터의 단 하나 따뜻한 빛이다.
        if (light2 != null)
        {
            foreach (Vector3 p in Spread(Free(floors, taken), 1))
            {
                Attach(root.transform, light2, p, 1f);
                AddLight(root.transform, p + FloorGlow, EmberLight, deco.LightRange);
                taken.Add(p);
            }
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
        Attach(parent, FindByAddress(address), pos, scale);
    }

    static GameObject Attach(Transform parent, GameObject prefab, Vector3 pos, float scale)
    {
        if (prefab == null)
            return null;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    /// <summary>새 그림 소품(Prop_C{챕터}_{자리}). 없으면 null — 그 자리는 옛 장식이 돈다.
    /// 새 소품은 MeshyArtImport 가 칸 가운데를 피벗으로 크기·자리를 맞춰 두었으니 배율 1 로 놓는다.</summary>
    static GameObject Art(int chapter, string slot)
    {
        return FindByAddress($"Prop_C{chapter}_{slot}", true);
    }

    static List<Vector3> Free(List<Vector3> cells, HashSet<Vector3> taken)
    {
        return cells.FindAll(c => taken.Contains(c) == false);
    }

    /// <summary>층 전체에 고르게 n 개 — 옛 불과 같은 식이다.</summary>
    static IEnumerable<Vector3> Spread(List<Vector3> cells, int n)
    {
        n = Mathf.Min(n, cells.Count);
        for (int i = 0; i < n; i++)
            yield return cells[(int)((i + 0.5f) * cells.Count / n)];
    }

    /// <summary>비어 있는 칸을 훑으며 every 칸마다 하나 — 옛 소품과 같은 간격 규칙이다.</summary>
    static void Every(Transform parent, GameObject prefab, List<Vector3> cells, HashSet<Vector3> taken, int every)
    {
        if (prefab == null)
            return;
        List<Vector3> free = Free(cells, taken);
        for (int i = every / 2; i < free.Count; i += every)
        {
            Attach(parent, prefab, free[i], 1f);
            taken.Add(free[i]);
        }
    }

    static void AddLight(Transform parent, Vector3 pos, ChapterDeco deco)
    {
        // 불꽃이 있는 높이에 둔다. 벽은 0.2~0.5 를 차지하고, 횃불 불꽃은 그 아래쪽이다.
        AddLight(parent, pos + new Vector3(0f, deco.OnFloor ? 0.15f : 0.28f, 0f), deco.Light, deco.LightRange);
    }

    static void AddLight(Transform parent, Vector3 at, Color color, float range)
    {
        GameObject prefab = FindByAddress("Deco_PointLight");
        if (prefab == null)
            return;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = at;

        Light light = go.GetComponentInChildren<Light>();
        if (light != null)
        {
            light.color = color;
            light.range = range;
        }
    }

    static readonly Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();

    /// <summary>어드레서블 주소로 프리팹을 찾는다. 주소는 파일 이름과 다를 수 있다.
    /// quiet 면 없어도 경고하지 않는다 — 새 그림은 없는 것이 보통이다.</summary>
    internal static GameObject FindByAddress(string address, bool quiet = false)
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

        if (quiet == false)
            Debug.LogWarning("[MapDeco] 주소를 못 찾았다: " + address);
        _cache[address] = null;
        return null;
    }

    /// <summary>만든 장식 프리팹을 어드레서블에 등록한다. 안 하면 런타임에 못 찾는다.</summary>
    internal static void Register(List<KeyValuePair<string, string>> items)
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
