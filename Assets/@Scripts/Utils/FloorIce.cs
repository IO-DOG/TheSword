using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 심층(챕터 3)의 얼음 미끄럼 — 11-24 오너 "챕터마다 기믹". 얼음 칸에 들어서면 막힐 때까지 같은 방향으로 미끄러진다.
/// 벽·몬스터·문·레버·계단 앞에서는 아무것도 건드리지 않고 서고(미끄러지다 부딪혀 싸움이 열리지 않는다), 물건 칸에 닿으면 주우며 그 칸에 선다,
/// 얼음이 아닌 바닥에 닿으면 거기 선다. 그래서 몬스터가 '멈춤돌' 이 되고, 잡는 순서가 설 수 있는 자리를 바꾼다(마탑식 순서 퍼즐).
/// 칸은 생성기가 굽는다(GeneratedGimmicks.Ice, Tools/gimmicks.py — check_ice 가 다 잡고 다 줍고 갇히지 않는 방만 고른다).
/// 걸음(PlayerController 가 걸음을 마칠 때 이어 미끄러진다)·클릭 이동·봇(PathMover 가 미끄럼 끝 칸을 간선으로 잰다)이 같은 규칙이다.
/// </summary>
public class FloorIce : MonoBehaviour
{
    readonly HashSet<Vector2Int> _ice = new HashSet<Vector2Int>();

    public static bool IsIce(GameObject map, Vector2Int cell)
    {
        if (map == null)
            return false;
        FloorIce fi = map.GetComponent<FloorIce>();
        return fi != null && fi._ice.Contains(cell);
    }

    /// <summary>MapBuilder 가 층을 다 지은 뒤 부른다. 그 층에 얼음이 없으면 아무것도 안 한다.</summary>
    public static void Attach(GameObject root, int floor)
    {
        if (root == null || GeneratedGimmicks.Ice.TryGetValue(floor, out (int x, int y)[] cells) == false)
            return;
        FloorIce fi = root.AddComponent<FloorIce>();
        foreach ((int x, int y) c in cells)
        {
            var cell = new Vector2Int(c.x, -c.y);
            fi._ice.Add(cell);
            var go = new GameObject($"Ice_{c.x}_{c.y}");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(cell.x * PathMover.Tile, 0.011f, cell.y * PathMover.Tile);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.AddComponent<SpriteRenderer>().sprite = Sheet((c.x * 7 + c.y * 3) & 3);
        }
    }

    // ------------------------------------------------------------ 그림 (코드로 그린다 — 칸 하나 32x32, 금 무늬 넷)
    static Sprite[] s_sheets;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => s_sheets = null;

    static Sprite Sheet(int v)
    {
        if (s_sheets == null)
        {
            s_sheets = new Sprite[4];
            for (int k = 0; k < 4; k++)
                s_sheets[k] = Draw(k);
        }
        return s_sheets[v & 3];
    }

    static Sprite Draw(int k)
    {
        const int n = 32;
        var px = new Color32[n * n];
        var rng = new System.Random(4021 + k);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // 옅은 얼음 막 — 위(북쪽)가 조금 밝고, 대각선 광택 줄이 지난다
                byte a = 150;
                Color32 c = new Color32((byte)(168 + y / 2), (byte)(208 + y / 3), 240, a);
                int d = (x + y + k * 5) % 23;
                if (d == 0 || d == 1)
                    c = new Color32(232, 246, 255, 190);
                if (x == 0 || y == 0)
                    c = new Color32(120, 170, 215, 170);
                px[y * n + x] = c;
            }
        // 금(crack) 한 줄
        int cx = 6 + rng.Next(18), cy = 6 + rng.Next(18);
        for (int i = 0; i < 9; i++)
        {
            cx = Mathf.Clamp(cx + rng.Next(3) - 1, 1, n - 2);
            cy = Mathf.Clamp(cy + (i % 2 == 0 ? 1 : 0), 1, n - 2);
            px[cy * n + cx] = new Color32(96, 140, 190, 200);
        }
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }
}
