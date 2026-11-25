using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 용광로(챕터 2)의 풀무 화로 — 11-24 오너 "챕터마다 기믹". 화로가 켜진 방의 몬스터는 공격력이 배율만큼 세다(머리 위 예측에 그대로 들어간다).
/// 그 층 밸브(레버, 셀 12)를 잠그면 꺼진다. 밸브 길목은 곁길 몬스터 하나가 지키고, 밸브는 화로 방 몬스터를 안 지나고 닿는다 —
/// "끄러 가서 그놈부터 잡을까, 그냥 비싸게 싸울까". 배율은 생성기가 켜진 채 더 치르는 값 = 그 파수꾼 한 판 값이 되게 푼다(furnace_mul).
/// 완주 보장은 꺼진 화로로 잰다(정답 경로는 다 잡으니 밸브를 먼저 잠근다).
/// 칸·배율·몬스터 id 는 생성기가 굽는다(GeneratedGimmicks.Furnaces). 전투(MonsterController.SetMonster)와 예측(BattleForecast)이 같은 <see cref="AttackMul"/> 을 곱한다.
/// </summary>
public class FloorFurnace : MonoBehaviour
{
    Lever _lever;
    bool _shownLit = true;
    SpriteRenderer _fire;
    SpriteRenderer _fireDepth;     // StandSprite 가 밑에 까는 깊이 판 — 같은 그림을 바꿔 주지 않으면 꺼진 화로 밑에 불꽃이 남는다
    readonly List<GameObject> _embers = new List<GameObject>();
    float _clock;
    int _frame;

    /// <summary>그 몬스터의 공격 배율(화로가 켜져 있고 화로 방 몬스터면 배율, 아니면 1).</summary>
    public static float AttackMul(int monsterId)
    {
        if (monsterId < 1000)
            return 1f;
        int floor = (monsterId - 1000) / 8;
        if (GeneratedGimmicks.Furnaces.TryGetValue(floor, out (int x, int y, float mul, int[] ids) f) == false
            || System.Array.IndexOf(f.ids, monsterId) < 0)
            return 1f;
        return Lit(floor) ? f.mul : 1f;
    }

    /// <summary>그 층 화로가 켜져 있는가. 층 맵이 아직 없으면(다른 챕터) 켜진 것으로 본다.</summary>
    static bool Lit(int floor)
    {
        GameObject map = Managers.Game != null && Managers.Game.Maps != null && Managers.Game.Maps.TryGetValue(floor - 1, out GameObject m) ? m : null;
        FloorFurnace ff = map != null ? map.GetComponent<FloorFurnace>() : null;
        return ff == null || ff.IsLit;
    }

    public bool IsLit
    {
        get
        {
            if (_lever == null)
                return true;
            return Managers.Data.LeverActiveDic.TryGetValue(_lever._leverIndex_forActive, out bool alive) == false || alive;
        }
    }

    /// <summary>MapBuilder 가 층을 다 지은 뒤 부른다. 그 층에 화로가 없으면 아무것도 안 한다.</summary>
    public static void Attach(GameObject root, int floor)
    {
        if (root == null || GeneratedGimmicks.Furnaces.TryGetValue(floor, out (int x, int y, float mul, int[] ids) f) == false)
            return;
        FloorFurnace ff = root.AddComponent<FloorFurnace>();
        ff._lever = root.GetComponentInChildren<Lever>(true);
        Vector3 at = root.transform.TransformPoint(new Vector3(f.x * PathMover.Tile, 0f, -f.y * PathMover.Tile)) + new Vector3(0f, 0f, -0.02f);
        GameObject fire = KeyMark.StandSprite(root.transform, Frames(true)[0], at);
        if (fire != null)
        {
            fire.name = "Furnace";
            fire.transform.localScale *= 2.6f;       // 표지(11px)가 아니라 칸 하나를 채우는 화로로 보이게 — 정수배에 가깝게
            ff._fire = fire.GetComponent<SpriteRenderer>();
            ff._fireDepth = fire.transform.Find("Depth")?.GetComponent<SpriteRenderer>();
        }
        // 화로 방 몬스터 발밑의 잉걸 빛 — 어느 놈이 센지 보이게
        foreach (MonsterController mc in root.GetComponentsInChildren<MonsterController>(true))
        {
            if (System.Array.IndexOf(f.ids, mc.id) < 0)
                continue;
            var go = new GameObject("FurnaceEmber");
            go.transform.SetParent(mc.transform, false);
            go.transform.position = mc.transform.position + new Vector3(0f, 0.011f, 0f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Vector3 ls = mc.transform.lossyScale;
            go.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
            go.AddComponent<SpriteRenderer>().sprite = Ember();
            ff._embers.Add(go);
        }
        ff.Refresh(true);
    }

    void Update()
    {
        bool lit = IsLit;
        if (lit != _shownLit)
            Refresh(false);
        _clock += Time.deltaTime;
        if (_clock < 0.12f || _fire == null)
            return;
        _clock = 0f;
        _frame = (_frame + 1) % 3;
        SetFire(Frames(lit)[lit ? _frame : 0]);
    }

    void SetFire(Sprite s)
    {
        if (_fire != null)
            _fire.sprite = s;
        if (_fireDepth != null)
            _fireDepth.sprite = s;
    }

    void Refresh(bool first)
    {
        bool lit = IsLit;
        if (first == false && lit == false && _shownLit)
        {
            Managers.Sound.Play(Define.Sound.Effect, "Gimic_leverDown_SFX");
            GameEvents.RaiseHudRefreshed();     // 머리 위 예측을 다시 잰다
        }
        _shownLit = lit;
        SetFire(Frames(lit)[0]);
        foreach (GameObject e in _embers)
            if (e != null)
                e.SetActive(lit);
    }

    // ------------------------------------------------------------ 그림 (코드로 그린다)
    static Sprite[] s_lit, s_cold;
    static Sprite s_ember;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { s_lit = null; s_cold = null; s_ember = null; }

    static Sprite[] Frames(bool lit)
    {
        if (lit)
            return s_lit ?? (s_lit = new[] { Brazier(true, 0), Brazier(true, 1), Brazier(true, 2) });
        return s_cold ?? (s_cold = new[] { Brazier(false, 0) });
    }

    /// <summary>쇠 화로(대야 + 다리)와 불꽃 세 장. 꺼지면 재만 남는다.</summary>
    static Sprite Brazier(bool lit, int f)
    {
        string[] body =
        {
            "XXXXXXXXXXX", "XOOOOOOOOOX", ".XOOOOOOOX.", "..XXXXXXX..", "...X...X...", "..XX...XX..",
        };
        string[][] flames =
        {
            new[] { "....F......", "...FYF.F...", "..FYWYFYF..", "..FYWWYF...", "...FYYF....", },
            new[] { "......F....", "..F.FYF....", "..FYFWYF...", "...FYWWYF..", "....FYYF...", },
            new[] { ".....F.....", "...F.FYF...", "..FYFWYF...", "..FYWWYFF..", "...FYYYF...", },
        };
        string[] top = lit ? flames[f] : new[] { "...........", "...........", "...........", "....aaa....", "...aaaaa...", };
        var rows = new List<string>(top);
        rows.AddRange(body);
        int h = rows.Count, w = rows[0].Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char c = rows[h - 1 - y][x];
                px[y * w + x] = c == 'X' ? new Color32(30, 22, 20, 255) : c == 'O' ? new Color32(92, 70, 60, 255)
                              : c == 'F' ? new Color32(232, 96, 30, 255) : c == 'Y' ? new Color32(255, 186, 60, 255)
                              : c == 'W' ? new Color32(255, 244, 200, 255) : c == 'a' ? new Color32(110, 104, 100, 255)
                              : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), Define.PPU, 0, SpriteMeshType.FullRect);
    }

    static Sprite Ember()
    {
        if (s_ember != null)
            return s_ember;
        const int n = 24;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - n / 2f + 0.5f) / (n / 2f), dy = (y - n / 2f + 0.5f) / (n / 2f * 0.6f);
                float r = dx * dx + dy * dy;
                px[y * n + x] = r > 1f ? new Color32(0, 0, 0, 0) : new Color32(255, (byte)(90 + 70 * (1 - r)), 30, (byte)(60 + 170 * (1 - r)));
            }
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply(false, true);
        s_ember = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return s_ember;
    }
}
