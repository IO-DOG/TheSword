using UnityEngine;

/// <summary>
/// 왕좌(챕터 4)의 짝 균열 — 11-24 오너 "챕터마다 기믹". 보라 균열 한쪽에 들어서면 다른 쪽으로 옮겨진다.
/// 두 칸은 같은 구역 안에서 관문 몬스터 하나를 사이에 둔다(문은 건너뛰지 않는다 — 열쇠 셈). 그래서 그놈을 건너뛰면
/// 지금 HP 를 아끼지만 경험치를 못 받는다(층당 1레벨 설계라 다음 층이 그만큼 아프다). 완주 보장은 균열을 안 쓰는 길로 잰다.
/// 칸은 생성기가 굽는다(GeneratedGimmicks.Rifts, Tools/gimmicks.py) — 지도 CSV 에는 없다(빈 바닥 그대로).
/// 걸음(PlayerController 가 걸음을 마칠 때)·클릭 이동·봇(PathMover 가 순간이동 간선으로 잰다)이 같은 <see cref="TryPair"/> 를 본다.
/// </summary>
public class FloorRifts : MonoBehaviour
{
    Vector2Int _a, _b;

    /// <summary>그 칸이 균열이면 짝 칸을 준다. map 은 층 루트.</summary>
    public static bool TryPair(GameObject map, Vector2Int cell, out Vector2Int pair)
    {
        pair = cell;
        FloorRifts fr = map != null ? map.GetComponent<FloorRifts>() : null;
        if (fr == null)
            return false;
        if (cell == fr._a) { pair = fr._b; return true; }
        if (cell == fr._b) { pair = fr._a; return true; }
        return false;
    }

    /// <summary>MapBuilder 가 층을 다 지은 뒤 부른다. 그 층에 균열이 없으면 아무것도 안 한다.</summary>
    public static void Attach(GameObject root, int floor)
    {
        if (root == null || GeneratedGimmicks.Rifts.TryGetValue(floor, out (int ax, int ay, int bx, int by) r) == false)
            return;
        FloorRifts fr = root.AddComponent<FloorRifts>();
        fr._a = new Vector2Int(r.ax, -r.ay);
        fr._b = new Vector2Int(r.bx, -r.by);
        fr.Look(root.transform, fr._a);
        fr.Look(root.transform, fr._b);
    }

    void Look(Transform root, Vector2Int cell)
    {
        var go = new GameObject($"Rift_{cell.x}_{-cell.y}");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(cell.x * PathMover.Tile, 0.012f, cell.y * PathMover.Tile);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Swirl();
        go.AddComponent<RiftSpin>();
        Vector3 at = root.TransformPoint(new Vector3(cell.x * PathMover.Tile, 0f, cell.y * PathMover.Tile)) + new Vector3(0f, 0.06f, -0.02f);
        GameObject mark = KeyMark.StandSprite(root, Mark(), at);
        if (mark != null)
            mark.name = "RiftMark";
    }

    // ------------------------------------------------------------ 그림 (코드로 그린다)
    static Sprite s_swirl, s_mark;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { s_swirl = null; s_mark = null; }

    static Sprite Mark()
    {
        // 위아래로 이어진 두 마름모 — "여기와 저기가 하나" (열쇠의 ◆ 와 헷갈리지 않게 겹쳐 그린다)
        return s_mark ?? (s_mark = KeyMark.DrawShape(new[]
        {
            "....X....", "...XOX...", "..XOOOX..", ".XOOOOOX.", "..XOOOX..", "...XOX...", "..XOOOX..", ".XOOOOOX.",
            "..XOOOX..", "...XOX...", "....X....",
        }, new Color32(196, 140, 255, 255)));
    }

    static Sprite Swirl()
    {
        if (s_swirl != null)
            return s_swirl;
        const int n = 32;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - n / 2f + 0.5f, dy = y - n / 2f + 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 14f)
                {
                    px[y * n + x] = new Color32(0, 0, 0, 0);
                    continue;
                }
                float ang = Mathf.Atan2(dy, dx);
                bool arm = Mathf.Repeat(ang * 2f / Mathf.PI + r * 0.18f, 1f) < 0.38f;     // 소용돌이 팔 넷
                byte a = (byte)(r > 12f ? 150 : 215);
                px[y * n + x] = r < 3f ? new Color32(18, 8, 30, 240)
                              : arm ? new Color32(176, 110, 255, a) : new Color32(70, 34, 120, a);
            }
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply(false, true);
        s_swirl = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return s_swirl;
    }
}

/// <summary>균열 소용돌이를 천천히 돌린다(바닥에 누운 그림을 그 자리에서 Y 축으로).</summary>
public class RiftSpin : MonoBehaviour
{
    float _yaw;

    void Update()
    {
        _yaw = Mathf.Repeat(_yaw - 40f * Time.deltaTime, 360f);
        transform.localRotation = Quaternion.Euler(90f, _yaw, 0f);
    }
}
