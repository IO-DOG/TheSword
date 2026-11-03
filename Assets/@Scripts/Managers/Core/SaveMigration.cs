using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Data;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 지도를 고친 패치 뒤에도 옛 체크포인트를 읽는다 (K1).
///
/// 체크포인트는 MapData 해시와 함께 적힌다(SaveStore). 오브젝트 상태의 번호는 층을 가로지르는 카운터라
/// (DataManager.ResetActiveDic, Tools/mapdata_gen.py) 한 층에서 하나만 늘거나 줄어도 그 뒤 층들의 번호가 다 밀린다 —
/// 그래서 해시가 다르면 거절해 왔다. 이제는 빌드마다 그 지도를 해시 이름으로 보관함(StreamingAssets)에 남기고
/// (GameBuild 가 Archive 를 부른다), 옛 체크포인트를 읽을 때 그 지도를 찾아 층 안의 순서로 짝지어 옮긴다.
/// 보관함에 없으면 예전처럼 거절한다 — 지우지 않는다. 보관한 지도는 지우지 않는다: 지우면 그 빌드의 저장이 다시 막힌다.
///
/// 층마다 이렇게 옮긴다(Migrate). "가장 높은 층" 은 처음 밟은 층 표시로 잰다(Frontier).
///   그대로인 층(번호 말고 다 같다)       상태를 그대로
///   바뀐 층, 가장 높은 층보다 아래        다 쓴 것으로 — 몬스터 죽음·아이템 주움·문 열림·레버 당김. 위로 올라갔으니
///                                        관문(문·보스·룬)은 이미 썼고, 두고 온 곁길 보상만 잃는다. 공짜 경험치도 막힌 길도 없다.
///   바뀐 층, 가장 높은 층과 그 위         새 판처럼. 아직 덜 끝낸 층을 다 쓴 것으로 두면 20층 보스의 반지 같은 것을 영영 잃고,
///                                        일부만 이어 붙이면 옮겨진 문 앞에서 열쇠가 모자랄 수 있다. 그 층에서 이미 쓴 것을
///                                        한 번 더 얻을 수는 있다(한 층, 패치 한 번).
/// 지금 층이 아니라 가장 높은 층으로 가르는 까닭은 워프다. 41층까지 갔다 12층 금고로 워프한 판에서 "지금 층 위는 새 판" 이면
/// 13~41층을 통째로 다시 잡는다.
/// 장부·기록·이야기·능력치는 손대지 않는다. 바뀐 층에 서 있었으면 선 자리만 같은 입구로 옮긴다(Relocate).
/// </summary>
public static class SaveMigration
{
    // 던전 CSV 도 이미 StreamingAssets 에서 파일로 읽는다(ResetActiveDic). 어드레서블 PreLoad 에 올리면 판마다 수 MB 를 쥐고 있다.
    public static string ArchiveDirectory => Path.Combine(Application.streamingAssetsPath, "Data", "MapDataArchive");

    /// <summary>이 지도를 보관함에 넣고 그 경로를 준다. 이미 있으면 null. 빌드(GameBuild)가 굽기 전에 부른다.</summary>
    public static string Archive(string mapDataJson, string directory)
    {
        string path = ArchivePath(directory, SaveStore.Hash(mapDataJson));
        if (File.Exists(path))
            return null;
        Directory.CreateDirectory(directory);
        // 반쯤 쓴 파일이 보관본 이름으로 남으면 다음 빌드가 "있다" 고 건너뛴다.
        string temp = path + ".tmp";
        using (var writer = new StreamWriter(new GZipStream(File.Create(temp), System.IO.Compression.CompressionLevel.Optimal),
                   new UTF8Encoding(false)))
            writer.Write(mapDataJson);
        File.Move(temp, path);
        return path;
    }

    /// <summary>그 해시의 지도를 보관함에서 꺼낸다. 없으면 null.</summary>
    public static Dictionary<int, MapData> LoadArchived(string directory, string contentHash)
    {
        string path = ArchivePath(directory, contentHash);
        if (path == null || File.Exists(path) == false)
            return null;
        string text;
        using (var reader = new StreamReader(new GZipStream(File.OpenRead(path), CompressionMode.Decompress), Encoding.UTF8))
            text = reader.ReadToEnd();
        // 이름은 해시 앞 8바이트뿐이다 — 끝까지 맞아야 그 지도다.
        if (SaveStore.Hash(text) != contentHash)
            return null;
        return JsonConvert.DeserializeObject<MapDataLoader>(text).MakeDict();
    }

    // 해시(Base64)에는 '/' 가 섞이고 체크포인트에서 온 글이라 그대로 경로에 넣지 않는다 — 앞 8바이트의 16진수만 쓴다.
    static string ArchivePath(string directory, string contentHash)
    {
        byte[] hash;
        try { hash = Convert.FromBase64String(contentHash); }
        catch (FormatException) { return null; }
        return hash.Length < 8 ? null
            : Path.Combine(directory, $"MapData_{BitConverter.ToString(hash, 0, 8).Replace("-", "")}.json.gz");
    }

    /// <summary>
    /// 다른 지도로 적은 체크포인트를 지금 지도(now)에 맞춘다 — GameManager.ValidateCheckpoint 가 해시가 다를 때 부른다.
    /// 보관함에 그 지도가 없으면 예전처럼 거절한다(InvalidDataException — 지우지 않는다). 해시는 LoadGame 이 바꿔 다시 적는다.
    /// </summary>
    public static void Apply(SaveStore.Snapshot snapshot, Dictionary<int, MapData> now)
    {
        try
        {
            Dictionary<int, MapData> before = LoadArchived(ArchiveDirectory, snapshot.ContentHash);
            if (before == null)
                throw new InvalidDataException("This checkpoint belongs to a different dungeon layout.");
            GameManager.CurPlayerData player = snapshot.Player;
            snapshot.Active = Migrate(before, now, snapshot.Active, Frontier(player));
            before.TryGetValue(player.CurStageid, out MapData was);
            now.TryGetValue(player.CurStageid, out MapData cur);
            player.CurPosition = Relocate(was, cur, player.CurPosition);
        }
        // 꺼내거나 옮기다 터진 것은 "읽을 수 없는 저장" 으로 돌린다. 그대로 두면 타이틀의 선로드(GameManager.Init)까지 같이 끊긴다.
        catch (Exception e) when (SaveStore.IsSaveError(e) == false)
        {
            throw new InvalidDataException("Checkpoint migration failed: " + e.Message, e);
        }
    }

    /// <summary>가장 높이 올라온 층(0 부터) — 처음 밟은 층 표시와 지금 층 중 높은 쪽. 그 아래는 다 지나온 층이다:
    /// 위층에 처음 오르려면 그 층의 문·관문 몬스터·보스·룬을 지나야 하고, 워프는 다녀온 층으로만 간다.</summary>
    public static int Frontier(GameManager.CurPlayerData player) =>
        Math.Max(player.CurStageid, player.FirstEnterMapCheck.LastIndexOf(true));

    /// <summary>옛 지도(before)로 적은 상태를 새 지도(after)의 번호로 옮긴다. 규칙은 클래스 머리 주석.</summary>
    public static SaveStore.ActiveState Migrate(Dictionary<int, MapData> before, Dictionary<int, MapData> after,
        SaveStore.ActiveState old, int frontier)
    {
        SaveStore.ActiveState now = Fresh(after);
        Dictionary<int, bool>[] from = Tables(old), to = Tables(now);
        foreach (MapData map in after.Values)
        {
            before.TryGetValue(map.Key, out MapData was);
            bool same = was != null && Same(was, map);
            bool fresh = was == null || map.Key >= frontier;
            List<ObjectData> olds = same ? Numbered(was) : null, news = Numbered(map);
            for (int i = 0; i < news.Count; i++)
            {
                int t = TableOf(news[i].ObjectType);
                to[t][news[i].Count] = same
                    ? from[t].TryGetValue(olds[i].Count, out bool alive) == false || alive    // 적혀 있지 않으면 새것
                    : fresh;
            }
        }
        // 지도가 번호를 매기지 않는 것 — 마검 열쇠(9000)·킹 슬라임(9001)·분열 슬라임(9002~)과 그 물약(9001). 그대로 둔다.
        Dictionary<int, bool>[] mapped = Tables(Fresh(before));
        for (int t = 0; t < from.Length; t++)
            foreach (KeyValuePair<int, bool> kv in from[t])
                if (mapped[t].ContainsKey(kv.Key) == false && to[t].ContainsKey(kv.Key) == false)
                    to[t][kv.Key] = kv.Value;
        return now;
    }

    /// <summary>
    /// 바뀐 층에 서 있었으면 새 지도의 같은 입구(스폰·같은 계단)로 옮긴다 — 체크포인트는 층에 들어선 자리에 적힌다.
    /// 층들은 x 로 100 씩 떨어져 놓이므로(GameManager.GenerateMap) 100 으로 나눈 나머지가 층 안의 자리다.
    /// </summary>
    public static MyVector3 Relocate(MapData was, MapData now, MyVector3 at)
    {
        if (was == null || now == null || Same(was, now))
            return at;
        float x = at.X % 100f;
        ObjectData from = was.Objects.Where(IsEntrance)
            .OrderBy(o => Math.Abs(o.Position.X - x) + Math.Abs(o.Position.Z - at.Z)).FirstOrDefault();
        ObjectData to = from == null ? null
            : now.Objects.FirstOrDefault(o => o.ObjectType == from.ObjectType && o.Id == from.Id)
              ?? now.Objects.FirstOrDefault(o => o.ObjectType == (int)Define.ObjectType.SpawnPoint);
        if (to == null)
            return at;
        return new MyVector3 { X = at.X + to.Position.X - from.Position.X, Y = at.Y, Z = at.Z + to.Position.Z - from.Position.Z };
    }

    static bool IsEntrance(ObjectData o) =>
        o.ObjectType == (int)Define.ObjectType.SpawnPoint || o.ObjectType == (int)Define.ObjectType.Portal;

    // 층이 그대로인가 — 번호만 빼고 벽·바닥까지 다 본다. 벽 하나가 길을 바꿔도 그 층의 상태는 믿을 수 없다.
    static bool Same(MapData a, MapData b) => a.Objects.Count == b.Objects.Count && a.Objects.Zip(b.Objects, (x, y) =>
        x.ObjectType == y.ObjectType && x.Id == y.Id && x.ChoiceGroup == y.ChoiceGroup &&
        x.Position.X == y.Position.X && x.Position.Y == y.Position.Y && x.Position.Z == y.Position.Z).All(e => e);

    /// <summary>그 지도로 새 판을 시작한 상태 — 번호가 붙는 것이 전부 true 다(ResetActiveDic 과 같다).</summary>
    static SaveStore.ActiveState Fresh(Dictionary<int, MapData> maps)
    {
        var s = new SaveStore.ActiveState {
            Monsters = new Dictionary<int, bool>(), Bosses = new Dictionary<int, bool>(), Items = new Dictionary<int, bool>(),
            Equipment = new Dictionary<int, bool>(), Doors = new Dictionary<int, bool>(),
            Pillars = new Dictionary<int, bool>(), Levers = new Dictionary<int, bool>()
        };
        Dictionary<int, bool>[] tables = Tables(s);
        foreach (MapData map in maps.Values)
            foreach (ObjectData o in Numbered(map))
            {
                tables[TableOf(o.ObjectType)][o.Count] = true;
                if (o.ObjectType == (int)Define.ObjectType.BossMonster)
                    s.Bosses[s.Bosses.Count] = true;    // 읽는 곳은 없지만 ResetActiveDic 과 같은 모양으로
            }
        return s;
    }

    // 번호가 붙는 것과 그 번호가 사는 표(Tables 의 자리). 보스는 몬스터 번호를 같이 쓴다(ResetActiveDic, mapdata_gen).
    static int TableOf(int type)
    {
        switch ((Define.ObjectType)type)
        {
            case Define.ObjectType.Monster:
            case Define.ObjectType.BossMonster: return 0;
            case Define.ObjectType.CItem: return 1;
            case Define.ObjectType.Eitem: return 2;
            case Define.ObjectType.Door: return 3;
            case Define.ObjectType.Pillar: return 4;
            case Define.ObjectType.Lever: return 5;
            default: return -1;
        }
    }

    static Dictionary<int, bool>[] Tables(SaveStore.ActiveState s) =>
        new[] { s.Monsters, s.Items, s.Equipment, s.Doors, s.Pillars, s.Levers };

    static List<ObjectData> Numbered(MapData map) => map.Objects.Where(o => TableOf(o.ObjectType) >= 0).ToList();

    // ---------------------------------------------------------------- 스스로 재 보기

    /// <summary>
    /// 첫 줄이 OK 또는 FAIL. 에디터에서는 표를 먼저 올린다: FastChecks.EditorBootData(); Debug.Log(SaveMigration.SelfTest());
    /// 지금 지도를 그 자신으로 옮기면 그대로여야 하고, 30층·5층을 고친 사본으로 옮기면 머리 주석의 규칙대로여야 한다.
    /// </summary>
    public static string SelfTest(string mapDataJson = null)
    {
        mapDataJson ??= Managers.Resource.Load<TextAsset>("MapData").text;
        Dictionary<int, MapData> before = JsonConvert.DeserializeObject<MapDataLoader>(mapDataJson).MakeDict();
        // 고쳐 볼 사본. 자리(MyVector3)는 바꿀 때 새로 끼우니 나눠 써도 된다. 5MB 를 다시 읽는 것보다 훨씬 빠르다 —
        // 에디터 MCP 의 eval 은 60초를 넘기면 물린다(CLAUDE.md).
        Dictionary<int, MapData> Copy() => before.Values.ToDictionary(m => m.Key, m => new MapData { Key = m.Key,
            Objects = m.Objects.Select(o => new ObjectData { ChoiceGroup = o.ChoiceGroup, Id = o.Id, Count = o.Count,
                ObjectType = o.ObjectType, Position = o.Position }).ToList() });
        const float Cell = 0.32f;   // 한 칸(Define.TILE_SIZE)
        var fails = new List<string>();
        int checks = 0, failed = 0;
        void Check(bool ok, string what)
        {
            checks++;
            if (ok == false && ++failed <= 20)
                fails.Add(what);
        }
        bool Off(Dictionary<int, bool> d, int key) => d.TryGetValue(key, out bool v) && v == false;
        int Bosses(Dictionary<int, MapData> maps) =>
            maps.Values.Sum(m => m.Objects.Count(o => o.ObjectType == (int)Define.ObjectType.BossMonster));

        // 판 하나: top 층까지 올라왔다. 그 아래는 셋 중 둘을 썼고(하나는 곁길에 두고 왔다), top 에서는 하나만 썼다.
        SaveStore.ActiveState Played(int top)
        {
            SaveStore.ActiveState s = Fresh(before);
            Dictionary<int, bool>[] tables = Tables(s);
            foreach (MapData map in before.Values)
            {
                List<ObjectData> list = Numbered(map);
                for (int i = 0; i < list.Count; i++)
                    if (map.Key < top ? i % 3 != 2 : map.Key == top && i == 0)
                        tables[TableOf(list[i].ObjectType)][list[i].Count] = false;
            }
            s.Monsters[9001] = s.Monsters[9002] = false;    // 킹 슬라임·분열 슬라임 — 지도 밖 번호
            s.Items[9000] = false;                          // 마검 열쇠
            return s;
        }

        // changed: 고친 층(0 부터). 그 밖의 층은 번호가 밀려도 같은 오브젝트의 상태를 그대로 가져야 한다.
        void Expect(string name, Dictionary<int, MapData> after, int top, params int[] changed)
        {
            SaveStore.ActiveState old = Played(top), got = Migrate(before, after, old, top);
            Dictionary<int, bool>[] o = Tables(old), g = Tables(got), fresh = Tables(Fresh(after));
            foreach (MapData map in after.Values)
            {
                List<ObjectData> news = Numbered(map), olds = changed.Contains(map.Key) ? null : Numbered(before[map.Key]);
                for (int i = 0; i < news.Count; i++)
                {
                    int t = TableOf(news[i].ObjectType);
                    bool want = olds != null ? o[t][olds[i].Count] : map.Key >= top;
                    bool ok = g[t].TryGetValue(news[i].Count, out bool v) && v == want;
                    Check(ok, ok ? null : $"{name}: {map.Key + 1}층 {i}번째({(Define.ObjectType)news[i].ObjectType}) {v}, 바란 것 {want}");
                }
            }
            for (int t = 0; t < g.Length; t++)
                Check(g[t].Count == fresh[t].Count + (t == 0 ? 2 : t == 1 ? 1 : 0), $"{name}: 표 {t} 의 칸 {g[t].Count}");
            Check(Off(got.Monsters, 9001) && Off(got.Monsters, 9002) && Off(got.Items, 9000), $"{name}: 지도 밖 번호");
            Check(got.Bosses.Count == Bosses(after), $"{name}: 보스 칸 {got.Bosses.Count}");
        }

        Expect("그대로", Copy(), 29);

        Dictionary<int, MapData> moved = Copy();
        ObjectData mob = moved[29].Objects.First(x => x.ObjectType == (int)Define.ObjectType.Monster);
        mob.Position = new MyVector3 { X = mob.Position.X + Cell, Y = mob.Position.Y, Z = mob.Position.Z };
        Expect("30층 몬스터 옮김, 30층에 섰다", moved, 29, 29);                 // 가장 높은 층 — 새 판처럼
        Expect("30층 몬스터 옮김, 12층에 섰다", moved, 11, 29);                 // 아직 못 간 층 — 새 판처럼
        Expect("30층 몬스터 옮김, 41층까지 갔다 12층으로 워프", moved, 40, 29);  // 지나온 층 — 다 쓴 것으로

        Dictionary<int, MapData> removed = Copy();
        removed[29].Objects.Remove(removed[29].Objects.First(x => x.ObjectType == (int)Define.ObjectType.CItem));
        Renumber(removed);
        Expect("30층 아이템 뺌(31층부터 번호가 당겨진다)", removed, 40, 29);

        Dictionary<int, MapData> added = Copy();
        ObjectData twin = added[29].Objects.First(x => x.ObjectType == (int)Define.ObjectType.Monster);
        added[29].Objects.Add(new ObjectData { Id = twin.Id, ObjectType = twin.ObjectType,
            Position = new MyVector3 { X = twin.Position.X, Y = twin.Position.Y, Z = twin.Position.Z - Cell } });
        Renumber(added);
        Expect("30층 몬스터 더함(31층부터 번호가 밀린다)", added, 40, 29);

        Dictionary<int, MapData> early = Copy();
        early[4].Objects.Remove(early[4].Objects.First(x => x.ObjectType == (int)Define.ObjectType.CItem));
        Renumber(early);
        Expect("5층 아이템 뺌, 12층에 섰다", early, 11, 4);

        // 바뀐 층의 입구에 섰으면 같은 입구를 따라간다. 12층은 챕터 0 의 열두 번째 층이라 x 가 1100 에서 시작한다.
        Dictionary<int, MapData> stairs = Copy();
        ObjectData down = stairs[11].Objects.First(x => x.ObjectType == (int)Define.ObjectType.Portal && x.Id == 15);
        var at = new MyVector3 { X = 1100f + down.Position.X, Y = 0f, Z = down.Position.Z };
        down.Position = new MyVector3 { X = down.Position.X + Cell, Y = down.Position.Y, Z = down.Position.Z };
        MyVector3 went = Relocate(before[11], stairs[11], at);
        Check(Math.Abs(went.X - (at.X + Cell)) < 1e-3f && went.Z == at.Z, $"자리: 옮긴 아래 계단을 따라간다 ({went.X}, {went.Z})");
        Check(Relocate(before[11], before[11], at) == at, "자리: 그대로인 층에서는 그대로");

        string dir = Path.Combine(Path.GetTempPath(), "TheSword_SaveMigration_SelfTest");
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
            Check(Archive(mapDataJson, dir) != null && Archive(mapDataJson, dir) == null, "보관함: 한 번만 적는다");
            Dictionary<int, MapData> back = LoadArchived(dir, SaveStore.Hash(mapDataJson));
            Check(back != null && back.Count == before.Count &&
                  before.Values.All(m => back.TryGetValue(m.Key, out MapData b) && Same(m, b)), "보관함: 왕복");
            Check(LoadArchived(dir, SaveStore.Hash(mapDataJson + " ")) == null, "보관함: 없는 해시");
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }

        return failed == 0
            ? $"OK — SaveMigration 검사 {checks}건 (그대로·옮김·뺌·더함·워프·5층·자리·보관함)"
            : $"FAIL — SaveMigration {failed}/{checks}건\n" + string.Join("\n", fails);
    }

    // 시험용: 고친 사본에 mapdata_gen 의 카운터 순서(층 순서, 층 안은 적힌 순서)로 번호를 다시 매긴다.
    static void Renumber(Dictionary<int, MapData> maps)
    {
        int[] next = new int[6];
        foreach (MapData map in maps.Values.OrderBy(m => m.Key))
            foreach (ObjectData o in Numbered(map))
                o.Count = next[TableOf(o.ObjectType)]++;
    }
}
