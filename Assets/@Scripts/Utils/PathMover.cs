using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 층 격자와 길찾기. 자동 플레이 봇(AutoPlayer)과 클릭 이동(PlayerController)이 같은 것을 쓴다 —
/// 봇이 100층을 돌며 다듬은 규칙을 사람의 클릭이 그대로 물려받는다. 봇에 있던 것을 옮긴 것이라 셈은 그대로다.
///
/// 칸은 맵 기준 좌표를 칸 크기로 나눠 반올림한 것이고, 칸이 막혔는지는 콜라이더를 직접 재 본다(Physics.CheckBox).
/// 쓰는 쪽은 걸음마다 Begin → Flood 로 다시 잰다. 막힘은 Begin 사이에만 기억한다.
/// </summary>
public class PathMover
{
    public const float Tile = 0.32f;

    /// <summary>칸을 재는 상자의 반지름.</summary>
    public static readonly Vector3 ProbeHalf = new Vector3(Tile * 0.3f, Tile * 0.3f, Tile * 0.3f);

    public static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(1, 0),
    };

    // 길에 두면 "지나가다" 건드려 버리는 것들. 전부 막고, 목표일 때만 옆에서 부딪힌다.
    // 이걸 안 막으면 포션을 향해 가다 몬스터를 밟아 원치 않는 전투가 나고,
    // 계단을 밟아 층을 건너뛰기도 한다 — 1층에서 죽던 진짜 원인이었다.
    public static readonly int BlockMask = (1 << (int)Define.Layer.Wall)
                                         | (1 << (int)Define.Layer.InteractObjects)
                                         | (1 << (int)Define.Layer.BossDoor)
                                         | (1 << (int)Define.Layer.Monster)
                                         | (1 << (int)Define.Layer.CItem)
                                         | (1 << (int)Define.Layer.EItem)
                                         | (1 << (int)Define.Layer.Portal)
                                         | (1 << (int)Define.Layer.Lever);
    public static readonly int DoorMask = 1 << (int)Define.Layer.Door;
    public static readonly int WallMask = 1 << (int)Define.Layer.Wall;
    public static readonly int ItemMask = (1 << (int)Define.Layer.CItem)
                                        | (1 << (int)Define.Layer.EItem);

    /// <summary>탐색 상자의 반지름. 층이 23x27 이라 32 면 어디서 재도 전부 들어온다.</summary>
    const int FloodRadius = 32;

    /// <summary>마지막 Flood 에서 걸어서 닿는 칸과 그 걸음 수.</summary>
    public readonly Dictionary<Vector2Int, int> Dist = new Dictionary<Vector2Int, int>();

    // BFS 재사용 버퍼
    readonly Dictionary<Vector2Int, Vector2Int> _from = new Dictionary<Vector2Int, Vector2Int>();
    readonly Queue<Vector2Int> _queue = new Queue<Vector2Int>();
    readonly Dictionary<Vector2Int, bool> _solid = new Dictionary<Vector2Int, bool>();
    static readonly Collider[] s_hits = new Collider[8];

    /// <summary>이번 탐색에서 아이템/몬스터 칸을 밟을 수 있다고 볼 것인가. 봇이 갇혔을 때만 켠다.</summary>
    public bool PassItems;

    /// <summary>
    /// 맞는 열쇠가 있는 문은 지나갈 수 있다고 본다. 봇은 이 규칙으로 방 순서 = 전투 순서를 따른다.
    /// 클릭 이동은 끈다 — 지나가다 열쇠를 쓰면 안 된다. 열쇠는 탑 전체에서 모자라서 어느 문을 열지가 곧 질문이다.
    /// </summary>
    public bool UseKeys = true;

    GameObject _map;

    /// <summary>칸을 재는 높이. 플레이어 키높이(발 + 반 칸)다.</summary>
    public float ProbeY { get; private set; }

    /// <summary>이 맵에서 다시 잰다. 막힘 기억을 비운다.</summary>
    public void Begin(GameObject map, float probeY)
    {
        _map = map;
        ProbeY = probeY;
        _solid.Clear();
        UpdateBounds(map);
    }

    /// <summary>막힘 기억만 비운다 — PassItems 를 바꿔 같은 맵을 다시 잴 때.</summary>
    public void ForgetSolid() => _solid.Clear();

    /// <summary>재 둔 범위를 버린다. 층이 다 조립되기 전에 잰 것일 수 있다.</summary>
    public void ForgetBounds(GameObject map)
    {
        _wallBounds.Remove(map);
        _hasBounds = false;
    }

    GameObject _boundsMap;
    Bounds _mapBounds;
    bool _hasBounds;

    /// <summary>
    /// 이 층이 실제로 차지하는 범위. 콜라이더를 전부 감싸서 잰다.
    /// 층마다 한 번만 계산한다 — 맵 오브젝트가 바뀔 때까지 그대로다.
    /// </summary>
    readonly Dictionary<GameObject, Bounds> _wallBounds = new Dictionary<GameObject, Bounds>();

    /// <summary>
    /// 이 맵이 벽으로 두르는 범위. 맵마다 한 번만 재서 들고 있는다.
    /// 벽만 센다 — 콜라이더를 전부 감싸면 연출용 트리거나 카메라 영역까지 들어와서
    /// 상자가 던전보다 훨씬 커지고, 맵 밖 (24,-23) 에 서 있어도 안으로 쳤다.
    /// </summary>
    public bool TryWallBounds(GameObject map, out Bounds bounds)
    {
        if (_wallBounds.TryGetValue(map, out bounds))
            return true;

        bool found = false;
        Bounds acc = new Bounds();
        foreach (Collider c in map.GetComponentsInChildren<Collider>(false))
        {
            if (c.gameObject.layer != (int)Define.Layer.Wall)
                continue;
            if (found == false)
            {
                acc = c.bounds;
                found = true;
            }
            else
            {
                acc.Encapsulate(c.bounds);
            }
        }

        if (found == false)
            return false;   // 아직 조립 중이다. 다음 프레임에 다시 잰다.

        _wallBounds[map] = acc;
        bounds = acc;
        return true;
    }

    public static bool InsideXZ(Bounds b, Vector3 world)
    {
        return world.x >= b.min.x && world.x <= b.max.x
               && world.z >= b.min.z && world.z <= b.max.z;
    }

    void UpdateBounds(GameObject map)
    {
        if (ReferenceEquals(_boundsMap, map) && _hasBounds)
            return;

        _boundsMap = map;
        _hasBounds = TryWallBounds(map, out _mapBounds);
    }

    /// <summary>탐색이 볼 수 있는 범위. 벽 칸 자체는 안에 들어야 한다.</summary>
    bool InsideMap(Vector3 world)
    {
        return Within(world, Tile);
    }

    /// <summary>
    /// 플레이어가 정말 던전 안에 서 있는가.
    /// 바깥 테두리는 벽이니, 제대로 배치됐다면 벽보다 한 칸은 안쪽에 있다.
    /// 탐색 범위와 같은 여유를 주면 딱 한 칸 밖 (-1,-23) 으로 새어 나갔다.
    /// </summary>
    public bool InsidePlayArea(Vector3 world)
    {
        return Within(world, -Tile);
    }

    bool Within(Vector3 world, float margin)
    {
        if (_hasBounds == false)
            return true;
        return world.x >= _mapBounds.min.x - margin && world.x <= _mapBounds.max.x + margin
               && world.z >= _mapBounds.min.z - margin && world.z <= _mapBounds.max.z + margin;
    }

    public void Flood(Vector2Int start)
    {
        _from.Clear();
        Dist.Clear();
        _queue.Clear();

        Dist[start] = 0;
        _queue.Enqueue(start);

        while (_queue.Count > 0)
        {
            Vector2Int cur = _queue.Dequeue();
            int d = Dist[cur];

            for (int i = 0; i < Dirs.Length; i++)
            {
                Vector2Int next = cur + Dirs[i];

                // 층 하나는 아무리 커도 23x27 이다. 어느 칸에서 재도 반대쪽 끝까지
                // Radius 안에 들어온다. 이 상자를 벽으로 쳐서 밖으로 새는 것을 막는다.
                // 3층 (17,-9) 처럼 경계가 뚫린 자리가 있고, 예전에는 거기서
                // 3200만 칸까지 퍼져 한 프레임이 몇 분씩 걸렸다.
                if (Mathf.Abs(next.x - start.x) > FloodRadius
                    || Mathf.Abs(next.y - start.y) > FloodRadius)
                    continue;

                // 층이 실제로 차지하는 범위 밖은 벽으로 친다.
                // 3층 (17,-9) 처럼 경계가 뚫린 자리가 있다.
                if (InsideMap(CellCenter(next)) == false)
                    continue;

                if (Dist.ContainsKey(next) || Solid(next))
                    continue;
                Dist[next] = d + 1;
                _from[next] = cur;
                _queue.Enqueue(next);
            }
        }
    }

    /// <summary>
    /// 그 칸을 지나갈 수 없는가. 콜라이더를 직접 재 본다 —
    /// 손수 만든 층은 벽 하나가 여러 칸을 덮기도 해서 오브젝트 위치로 세면 구멍이 난다.
    ///
    /// 열쇠가 없는 문도 벽으로 친다. 이 규칙이 방 순서 = 전투 순서를 강제한다.
    /// </summary>
    bool Solid(Vector2Int cell)
    {
        bool cached;
        if (_solid.TryGetValue(cell, out cached))
            return cached;

        Vector3 world = CellCenter(cell);
        Vector3 half = ProbeHalf;

        // 막혔을 때는 아이템과 몬스터 칸을 밟을 수 있다고 치고 다시 훑는다.
        // 아이템은 밟으면 주워지고, 몬스터는 부딪히면 싸움이 시작된다 —
        // 둘 다 그 자리를 비우는 길이다.
        //  - 9층: 아이템 하나가 한 칸 통로를 봉해 27칸에 갇혔다.
        //  - 20층: 보스의 큰 콜라이더가 제 주변 칸을 다 막아 아무도 못 다가갔다.
        int mask = PassItems
            ? (BlockMask & ~ItemMask & ~(1 << (int)Define.Layer.Monster))
            : BlockMask;
        bool blocked = Physics.CheckBox(world, half, Quaternion.identity, mask,
                                        QueryTriggerInteraction.Collide);

        if (blocked == false && Physics.CheckBox(world, half, Quaternion.identity, DoorMask,
                                                 QueryTriggerInteraction.Collide))
        {
            blocked = UseKeys == false || HasKeyFor(world, half) == false;
        }

        _solid[cell] = blocked;
        return blocked;
    }

    static bool HasKeyFor(Vector3 world, Vector3 half)
    {
        List<int> keys = Managers.Game.KeyInventory != null ? Managers.Game.KeyInventory._keys : null;
        if (keys == null)
            return false;

        int n = Physics.OverlapBoxNonAlloc(world, half, s_hits, Quaternion.identity, DoorMask,
                                           QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            Door door = Door.Find(s_hits[i].gameObject);
            if (door == null)
                continue;
            int idx = door._keyIndex;
            return idx >= 0 && idx < keys.Count && keys[idx] > 0;
        }
        return false;
    }

    /// <summary>목표까지의 경로를 되짚어 첫 한 칸의 방향을 낸다.</summary>
    public Define.MoveDir FirstStep(Vector2Int start, Vector2Int goal)
    {
        Vector2Int cur = goal;
        while (true)
        {
            Vector2Int prev;
            if (_from.TryGetValue(cur, out prev) == false)
                return Define.MoveDir.None;   // 시작점이거나 경로가 끊겼다
            if (prev == start)
                break;
            cur = prev;
        }

        Vector2Int step = cur - start;
        if (step.y > 0) return Define.MoveDir.Up;
        if (step.y < 0) return Define.MoveDir.Down;
        if (step.x < 0) return Define.MoveDir.Left;
        if (step.x > 0) return Define.MoveDir.Right;
        return Define.MoveDir.None;
    }

    public static Define.MoveDir ToDir(Vector2Int step)
    {
        if (step.y > 0) return Define.MoveDir.Up;
        if (step.y < 0) return Define.MoveDir.Down;
        if (step.x < 0) return Define.MoveDir.Left;
        if (step.x > 0) return Define.MoveDir.Right;
        return Define.MoveDir.None;
    }

    public Vector3 CellCenter(Vector2Int cell)
    {
        Vector3 world = _map.transform.TransformPoint(new Vector3(cell.x * Tile, 0f, cell.y * Tile));
        world.y = ProbeY;
        return world;
    }

    public static Vector2Int Cell(GameObject map, Vector3 world)
    {
        Vector3 local = map.transform.InverseTransformPoint(world);
        return new Vector2Int(Mathf.RoundToInt(local.x / Tile), Mathf.RoundToInt(local.z / Tile));
    }
}
