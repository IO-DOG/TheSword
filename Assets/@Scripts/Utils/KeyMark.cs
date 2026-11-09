using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// 열쇠 색마다 모양 하나 — 초록 ●, 노랑 ▲, 빨강 ◆. 문 위·맵의 열쇠·HUD 열쇠 칸에 같은 것을 단다.
///
/// 다섯 챕터의 문이 같은 판자문이라 색으로만 갈렸다. 색약이면 초록·빨강 문이 같아 보이고, 색약이 아니어도
/// 용광로의 주황 조명 아래에서는 노랑 문이 빨강 문처럼, 열쇠는 다 갈색으로 보였다(2026-11-10 43층 화면).
/// 그래서 조명을 받지 않는 기본 스프라이트 재질로 그리고, 그림은 생성 모델 없이 여기서 점을 찍는다
/// (11x11, 한 점 = 아트 한 픽셀 — 픽셀 퍼펙트 카메라에서 다른 그림과 같은 굵기다).
/// </summary>
public static class KeyMark
{
    // X 테두리, O 채움. 첫 줄이 맨 위다.
    static readonly string[][] Shapes =
    {
        new[] { "...XXXXX...", "..XOOOOOX..", ".XOOOOOOOX.", "XOOOOOOOOOX", "XOOOOOOOOOX", "XOOOOOOOOOX",
                "XOOOOOOOOOX", "XOOOOOOOOOX", ".XOOOOOOOX.", "..XOOOOOX..", "...XXXXX..." },
        new[] { ".....X.....", "....XOX....", "....XOX....", "...XOOOX...", "...XOOOX...", "..XOOOOOX..",
                "..XOOOOOX..", ".XOOOOOOOX.", ".XOOOOOOOX.", "XOOOOOOOOOX", "XXXXXXXXXXX" },
        new[] { ".....X.....", "....XOX....", "...XOOOX...", "..XOOOOOX..", ".XOOOOOOOX.", "XOOOOOOOOOX",
                ".XOOOOOOOX.", "..XOOOOOX..", "...XOOOX...", "....XOX....", ".....X....." },
    };
    static readonly Color32[] Fill = { new Color32(96, 224, 104, 255), new Color32(248, 208, 72, 255), new Color32(240, 88, 88, 255) };
    static readonly Color32 Line = new Color32(24, 18, 26, 255);

    static Sprite[] s_sprites;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => s_sprites = null;

    public static Sprite Get(int keyIndex)
    {
        if (keyIndex < 0 || keyIndex >= Shapes.Length)
            return null;
        if (s_sprites == null)
            s_sprites = new Sprite[Shapes.Length];
        if (s_sprites[keyIndex] == null)
            s_sprites[keyIndex] = Draw(Shapes[keyIndex], Fill[keyIndex]);
        return s_sprites[keyIndex];
    }

    static Sprite Draw(string[] rows, Color32 fill)
    {
        int h = rows.Length, w = rows[0].Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char c = rows[h - 1 - y][x];
                px[y * w + x] = c == 'X' ? Line : c == 'O' ? fill : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);     // CPU 사본은 버린다 — 그래서 메시는 알파를 읽지 않는 FullRect(11x11 이면 그쪽이 싸다)
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), Define.PPU, 0, SpriteMeshType.FullRect);
    }

    /// <summary>맵 위에 세운다. at 은 표지 아래끝 가운데(월드). parent 가 꺼지면(열쇠를 주우면) 같이 사라진다.</summary>
    public static GameObject Stand(Transform parent, int keyIndex, Vector3 at)
    {
        Sprite sprite = Get(keyIndex);
        if (sprite == null || parent == null)
            return null;
        var go = new GameObject("KeyMark");
        go.AddComponent<SpriteRenderer>().sprite = sprite;
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(at, Quaternion.identity);   // 다른 그림처럼 X회전 0 으로 서서 카메라를 본다
        Vector3 ls = parent.lossyScale;
        go.transform.localScale = new Vector3(1f / ls.x, MapBuilder.BillboardStretch() / ls.y, 1f / ls.z);

        // 카메라 후처리의 피사계 심도는 깊이로 흐린다. 스프라이트 재질은 깊이를 안 쓰고 바닥(BG)도 스프라이트라, 바닥 위에 뜬
        // 표지는 "아주 먼 것" 으로 잡혀 번졌다(문짝 앞의 것은 문이 깊이를 써서 또렷했다). 같은 그림을 아이템 재질(알파 컷,
        // 깊이를 쓴다)로 밑에 한 장 더 깔아 깊이만 남긴다 — 색은 위의 무광 표지가 같은 자리에 덮는다.
        Material depth = Managers.Resource.Load<Material>("Item_Shadow_000");
        if (depth != null)
        {
            SpriteRenderer under = new GameObject("Depth").AddComponent<SpriteRenderer>();
            under.sprite = sprite;
            under.sharedMaterial = depth;
            under.shadowCastingMode = ShadowCastingMode.Off;
            under.transform.SetParent(go.transform, false);
        }
        return go;
    }

    /// <summary>문짝 위끝에 세운다 — 가로문은 앞면, 세로문은 막대 가운데.</summary>
    public static GameObject OnDoor(Door door)
    {
        Renderer look = door.GetComponentInChildren<Renderer>();
        if (look == null)
            return null;
        Bounds b = look.bounds;
        return Stand(door.transform, door._keyIndex,
            new Vector3(b.center.x, b.max.y + 0.02f, b.center.z - Mathf.Min(b.extents.z, 0.05f) - 0.01f));
    }

    /// <summary>HUD 열쇠 칸 오른쪽 위 모서리에 단다. 이미 있으면 그대로 둔다.</summary>
    public static void Badge(Transform slot, int keyIndex)
    {
        Sprite sprite = Get(keyIndex);
        if (slot == null || sprite == null || slot.Find("KeyMark") != null)
            return;
        var rt = new GameObject("KeyMark", typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(slot, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one;
        rt.anchoredPosition = new Vector2(-6f, -6f);
        rt.sizeDelta = new Vector2(22f, 22f);
        Image image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
    }
}
