using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 수로(챕터 1)의 물살 — 11-24 오너 "챕터마다 챕터별 기믹".
///
/// 물살 칸에는 화살표 방향으로만 들어가고, 물살 칸에서 거꾸로는 나오지 못한다. 그래서 물살을 타면 그 너머 곁가지에서
/// 수문 레버를 당겨야 돌아온다(레버 길목은 곁길 몬스터가 지킨다 — 머리 위 예측이 X 면 타지 말 것). 레버를 당기면 그 층의 물살이 멈춘다.
/// 칸과 방향은 생성기가 굽는다(GeneratedGimmicks.Currents, Tools/gimmicks.py) — 지도 CSV 에는 레버(셀 12) 하나만 있다.
/// 걸음(PlayerController)·클릭 이동·봇(PathMover)이 같은 <see cref="Blocks"/> 를 본다. 손수 만든 층에는 없다.
/// 레버 상태는 LeverActiveDic(체크포인트에 남는다)이 쥔다 — 이 컴포넌트는 그것을 읽기만 한다.
/// </summary>
public class FloorCurrents : MonoBehaviour
{
    // 지도 CSV 의 방향(0 위 1 오른쪽 2 아래 3 왼쪽) -> PathMover 칸(x = 열, y = -행)
    static readonly Vector2Int[] Step = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

    readonly Dictionary<Vector2Int, int> _flow = new Dictionary<Vector2Int, int>();
    readonly List<SpriteRenderer> _looks = new List<SpriteRenderer>();
    Lever _lever;
    bool _shownActive = true;
    float _clock;
    int _frame;

    /// <summary>그 걸음을 물살이 막는가. map 은 층 루트(MapBuilder.Build 가 돌려준 것).</summary>
    public static bool Blocks(GameObject map, Vector2Int from, Vector2Int to)
    {
        if (map == null)
            return false;
        FloorCurrents fc = map.GetComponent<FloorCurrents>();
        return fc != null && fc.BlocksStep(from, to);
    }

    /// <summary>그 칸이 (흐르는) 물살 칸인가 — 고르지 않은 걸음(StoryDirector 의 뒷걸음)이 들어서지 않게.</summary>
    public static bool IsCurrent(GameObject map, Vector2Int cell)
    {
        FloorCurrents fc = map != null ? map.GetComponent<FloorCurrents>() : null;
        return fc != null && fc.Active && fc._flow.ContainsKey(cell);
    }

    public bool Active
    {
        get
        {
            if (_lever == null)
                return true;
            return Managers.Data.LeverActiveDic.TryGetValue(_lever._leverIndex_forActive, out bool alive) == false || alive;
        }
    }

    public bool BlocksStep(Vector2Int from, Vector2Int to)
    {
        if (_flow.Count == 0 || Active == false)
            return false;
        Vector2Int step = to - from;
        if (_flow.TryGetValue(to, out int d) && Step[d] != step)
            return true;                               // 물살 칸에는 그 방향으로만 들어간다
        if (_flow.TryGetValue(from, out int e) && Step[e] == -step)
            return true;                               // 물살 칸에서 거꾸로는 못 나온다
        return false;
    }

    /// <summary>MapBuilder 가 층을 다 지은 뒤 부른다. 그 층에 물살이 없으면 아무것도 안 한다.</summary>
    public static void Attach(GameObject root, int floor)
    {
        if (root == null || GeneratedGimmicks.Currents.TryGetValue(floor, out (int x, int y, int dir)[] cells) == false)
            return;
        FloorCurrents fc = root.AddComponent<FloorCurrents>();
        fc._lever = root.GetComponentInChildren<Lever>(true);
        foreach ((int x, int y, int dir) c in cells)
        {
            var cell = new Vector2Int(c.x, -c.y);
            fc._flow[cell] = c.dir;
            fc._looks.Add(fc.Look(root.transform, cell, c.dir));
        }
        fc.Refresh(true);
    }

    SpriteRenderer Look(Transform root, Vector2Int cell, int dir)
    {
        var go = new GameObject($"Current_{cell.x}_{-cell.y}");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(cell.x * PathMover.Tile, 0.012f, cell.y * PathMover.Tile);
        go.transform.localRotation = Quaternion.Euler(90f, 90f * dir, 0f);   // 바닥에 눕히고 화살표를 물살 쪽으로
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Frames(true)[0];
        // 화살표는 바닥에 눕히면 피사계 심도에 번져 빛 덩어리로 보인다 — 계단 표지처럼 칸 위에 세운다(화면 기준 ↑→↓←).
        Vector3 at = root.TransformPoint(new Vector3(cell.x * PathMover.Tile, 0f, cell.y * PathMover.Tile)) + new Vector3(0f, 0.06f, -0.02f);
        GameObject mark = KeyMark.StandSprite(root, Arrow(dir), at);
        if (mark != null)
        {
            mark.name = "CurrentArrow";
            _arrows.Add(mark);
        }
        return sr;
    }

    readonly List<GameObject> _arrows = new List<GameObject>();
    static Sprite[] s_arrows;

    static Sprite Arrow(int dir)
    {
        if (s_arrows == null)
        {
            string[] up = { "....X....", "...XOX...", "..XOOOX..", ".XOOOOOX.", "XXXOOOXXX", "..XOOOX..", "..XOOOX..", "..XXXXX.." };
            s_arrows = new Sprite[4];
            for (int d = 0; d < 4; d++)
                s_arrows[d] = KeyMark.DrawShape(Rotate(up, d), new Color32(150, 215, 255, 255));
        }
        return s_arrows[dir & 3];
    }

    /// <summary>위를 가리키는 모양을 시계 방향으로 d 번 돌린다.</summary>
    static string[] Rotate(string[] rows, int d)
    {
        string[] r = rows;
        for (int k = 0; k < d; k++)
        {
            int h = r.Length, w = r[0].Length;
            var n = new string[w];
            for (int x = 0; x < w; x++)
            {
                var sb = new System.Text.StringBuilder();
                for (int y = h - 1; y >= 0; y--)
                    sb.Append(r[y][x]);
                n[x] = sb.ToString();
            }
            r = n;
        }
        return r;
    }

    void Update()
    {
        bool active = Active;
        if (active != _shownActive)
            Refresh(false);
        _clock += Time.deltaTime;
        if (_clock < 0.14f)
            return;
        _clock = 0f;
        _frame = (_frame + 1) % FrameCount;
        Sprite[] set = Frames(active);
        foreach (SpriteRenderer sr in _looks)
            if (sr != null)
                sr.sprite = set[_frame];
    }

    void Refresh(bool first)
    {
        bool active = Active;
        if (first == false && active == false && _shownActive)
            Managers.Sound.Play(Define.Sound.Effect, "Gimic_leverDown_SFX");
        _shownActive = active;
        foreach (GameObject a in _arrows)
            if (a != null)
                a.SetActive(active);
        Sprite[] set = Frames(active);
        foreach (SpriteRenderer sr in _looks)
            if (sr != null)
                sr.sprite = set[_frame % set.Length];
    }

    // ------------------------------------------------------------ 그림 (코드로 그린다 — 32x32, 한 칸)
    const int Size = 32, FrameCount = 4;
    static Sprite[] s_flow, s_calm;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { s_flow = null; s_calm = null; s_arrows = null; }

    static Sprite[] Frames(bool flowing)
    {
        if (flowing)
            return s_flow ?? (s_flow = Draw(true));
        return s_calm ?? (s_calm = Draw(false));
    }

    static Sprite[] Draw(bool flowing)
    {
        var deep = new Color32(38, 92, 156, 200);
        var mid = new Color32(70, 140, 210, 210);
        var foam = new Color32(120, 180, 235, 220);
        var frames = new Sprite[FrameCount];
        for (int f = 0; f < FrameCount; f++)
        {
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    // 물결 줄: 흐르면 위(물살 쪽)로 한 칸씩 밀린다, 멈추면 제자리에서 옅게
                    int phase = flowing ? (y - f * 2 + 64) % 8 : (y + 64) % 8;
                    bool wave = phase == 0 && ((x + (y / 8) * 3) % 6) < 4;
                    Color32 c = (x == 0 || y == 0 || x == Size - 1 || y == Size - 1) ? deep : wave ? (flowing ? foam : mid) : (x % 7 == (y / 4) % 7 ? mid : deep);
                    px[y * Size + x] = c;
                }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            frames[f] = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
        return frames;
    }
}
