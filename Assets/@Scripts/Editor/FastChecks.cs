using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 몇 초 만에 도는 빠른 검사. 40분짜리 완주 녹화에 들어가기 전에 거르는 그물이다.
///
///   메뉴: TheSword/Fast Checks
///   Unity.exe -projectPath . -executeMethod FastChecks.Run
///
/// 이미 있는 검사와 겹치지 않는 것만 넣는다.
///   ContentValidator      데이터 표 (계단 쌍·ID 유효성·레벨 표)
///   MapBuilderSmokeTest   한 층이 조립되는가
///   ProgressionTest       층과 층이 이어지는가
///   layout_gen(파이썬)    CSV 격자의 문 규칙·포탈 봉인
///
/// 여기서 보는 것은 <b>이번 세션에 실제로 터진 것들</b>이고, 넷 다 "돌려 보고 나서"
/// 알았던 것이다.
///
///   1) 코드·데이터가 부르는 리소스 키가 실재하는가
///      스킬이 없는 사운드 키("PlayerAttack0_SFX")를 불러 발동마다 널참조였다.
///      키가 <b>있어도 PreLoad 라벨이 없으면</b> 런타임 캐시에 안 들어가 null 이다
///      (룬 이펙트가 그랬다).
///   2) 데이터가 가리키는 애니메이션 상태가 컨트롤러에 있는가
///      없는 상태를 재생하면 경고만 찍히고 그림이 안 나온다 — 떨어진 장비가 안 보였다.
///   3) 포탈 뒤에 갇혀 못 가는 몬스터·아이템이 있는가
///      파이썬은 CSV 격자를 보고, 여기서는 <b>런타임이 읽는 MapData</b> 를 본다.
///      CSV 를 고치고 MapData 를 다시 안 뽑으면 파이썬은 통과하고 게임만 깨진다.
///   4) 문 셀 코드 -> 프리팹 번역이 방향·색과 맞는가
///      표를 또 적지 않는다. <b>MapBuilder.BuildDoor 를 실제로 돌려</b> 놓인 문틀의
///      긴 축을 재고, 벽이 있는 축과 같은지 본다. 파이썬의 door_art 는 어디까지나
///      파이썬의 사본이라, C# 쪽이 어긋나도 파이썬은 0건이라고 답한다.
/// </summary>
public static class FastChecks
{
    const string PreLoadLabel = "PreLoad";
    const string ScriptDir = "Assets/@Scripts";

    static readonly List<string> Errors = new List<string>();
    static readonly List<string> Notes = new List<string>();
    // 같은 말을 층마다 되풀이하지 않는다.
    static readonly HashSet<string> Reported = new HashSet<string>();

    // 주소 -> 에셋 경로. PreLoad 라벨이 붙은 것만 런타임 캐시(ResourceManager)에 들어간다.
    static readonly Dictionary<string, string> Preload = new Dictionary<string, string>();
    // 라벨과 상관없이 등록만 된 주소. "있는데 라벨이 없다" 를 갈라 말하려고 따로 둔다.
    static readonly HashSet<string> Addressed = new HashSet<string>();

    [MenuItem("TheSword/Fast Checks")]
    public static void Run()
    {
        Errors.Clear();
        Notes.Clear();
        Reported.Clear();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("===== TheSword 빠른 검사 =====");

        try
        {
            if (BuildAddressIndex())
                Execute(sb);
        }
        catch (System.Exception e)
        {
            // 배치모드에서 executeMethod 예외는 스택 없이 한 줄만 남는다. 직접 찍는다.
            Errors.Add("검사 중 예외:\n" + e);
        }
        finally
        {
            Teardown();
        }

        Report(sb);
    }

    static void Execute(StringBuilder sb)
    {
        Data.MonsterDataLoader monsters = Json<Data.MonsterDataLoader>("MonsterData");
        Data.EquipDataLoader equips = Json<Data.EquipDataLoader>("EquipData");
        Data.ConsumableItemDataLoader items = Json<Data.ConsumableItemDataLoader>("ConsumableItemData");
        Data.MapDataLoader maps = Json<Data.MapDataLoader>("MapData");
        Data.StageInfoDataLoader stages = Json<Data.StageInfoDataLoader>("StageInfoData");

        if (monsters == null || equips == null || items == null || maps == null || stages == null)
            return;

        Dictionary<int, Data.MonsterData> monsterDic = monsters.MakeDict();
        Dictionary<int, Data.EquipData> equipDic = equips.MakeDict();
        Dictionary<int, Data.ConsumableItemData> itemDic = items.MakeDict();
        Dictionary<int, Data.MapData> mapDic = maps.MakeDict();
        Dictionary<int, Data.StageInfoData> stageDic = stages.MakeDict();

        Placed placed = ScanPlaced(mapDic, monsterDic);

        CheckResourceKeys(sb, monsterDic, equipDic, itemDic, placed);
        CheckAnimStates(sb, monsterDic, placed);
        CheckPortalSeal(sb, mapDic, stageDic);
        // 씬에 오브젝트를 만드는 것은 이것뿐이라 맨 뒤에 둔다.
        CheckDoorArt(sb, mapDic, stageDic);
    }

    /// <summary>
    /// 에디터에서 데이터 표를 올린다.
    ///
    /// 게임은 타이틀에서 어드레서블 "PreLoad" 라벨을 통째로 선로드한 뒤에야
    /// DataManager.Init 이 돈다. 에디터에는 그 단계가 없어서 표가 빈 채로 남는다.
    /// 다른 에디터 도구나 임시 검증에서도 쓸 수 있게 열어 둔다.
    /// </summary>
    public static void EditorBootData()
    {
        BuildAddressIndex();
        Setup();
    }

    // ------------------------------------------------------------------ 어드레서블 색인

    static bool BuildAddressIndex()
    {
        Preload.Clear();
        Addressed.Clear();

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        if (settings == null)
        {
            Errors.Add("Addressables 설정이 없다 — 키 검사를 할 수 없다");
            return false;
        }

        foreach (AddressableAssetGroup group in settings.groups)
        {
            if (group == null)
                continue;
            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.address))
                    continue;
                Addressed.Add(entry.address);
                if (entry.labels != null && entry.labels.Contains(PreLoadLabel))
                    Preload[entry.address] = entry.AssetPath;
            }
        }
        return true;
    }

    static T Json<T>(string address) where T : class
    {
        string path;
        if (Preload.TryGetValue(address, out path) == false)
        {
            Errors.Add($"데이터 표 '{address}' 가 {PreLoadLabel} 라벨로 등록돼 있지 않다 — DataManager.Init 이 널참조로 죽는다");
            return null;
        }

        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (asset == null)
        {
            Errors.Add($"데이터 에셋을 못 읽었다: {path}");
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(asset.text);
        }
        catch (System.Exception e)
        {
            Errors.Add($"{address}.json 파싱 실패: {e.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------ 맵에 실제로 놓인 것

    class Placed
    {
        public readonly HashSet<int> Monsters = new HashSet<int>();   // 보스 포함
        public readonly HashSet<int> Bosses = new HashSet<int>();
        public readonly HashSet<int> Items = new HashSet<int>();
        public readonly HashSet<int> Equips = new HashSet<int>();     // 놓인 것 + 몬스터가 떨구는 것
    }

    /// <summary>표 전체가 아니라 <b>층에 놓인 것만</b> 본다. 안 쓰는 줄까지 걸고 넘어지면
    /// 오류 목록이 길어져서 진짜 문제가 묻힌다.</summary>
    static Placed ScanPlaced(Dictionary<int, Data.MapData> mapDic,
                             Dictionary<int, Data.MonsterData> monsterDic)
    {
        Placed placed = new Placed();

        foreach (KeyValuePair<int, Data.MapData> kv in mapDic)
        {
            if (kv.Value.Objects == null)
                continue;
            foreach (Data.ObjectData o in kv.Value.Objects)
            {
                switch ((Define.ObjectType)o.ObjectType)
                {
                    case Define.ObjectType.Monster:
                        placed.Monsters.Add(o.Id);
                        break;
                    case Define.ObjectType.BossMonster:
                        placed.Monsters.Add(o.Id);
                        placed.Bosses.Add(o.Id);
                        break;
                    case Define.ObjectType.CItem:
                        placed.Items.Add(o.Id);
                        break;
                    case Define.ObjectType.Eitem:
                        placed.Equips.Add(o.Id);
                        break;
                }
            }
        }

        // 몬스터가 떨군 장비도 바닥에 놓인다. 데이터에 없는 자리라 맵만 훑으면 놓친다.
        foreach (int id in placed.Monsters)
        {
            Data.MonsterData md;
            if (monsterDic.TryGetValue(id, out md) && md.RewardItem > 0)
                placed.Equips.Add(md.RewardItem);
        }

        return placed;
    }

    // ------------------------------------------------------------------ 1) 리소스 키

    static readonly Regex[] AddressablePatterns =
    {
        new Regex("Managers\\.Sound\\.Play\\(\\s*(?:Define\\.)?Sound\\.\\w+\\s*,\\s*\"([^\"]+)\""),
        new Regex("Managers\\.Resource\\.(?:Instantiate|Load\\s*<[^>]+>|LoadAsync\\s*<[^>]+>)\\(\\s*\"([^\"]+)\""),
    };

    // Managers.Resource 를 거치지 않고 Resources 폴더에서 바로 읽는 것 (바닥 타일·커서).
    static readonly Regex ResourcesPattern =
        new Regex("(?<!Managers\\.)Resources\\.Load\\s*(?:<[^>]+>)?\\(\\s*\"([^\"]+)\"");

    static void CheckResourceKeys(StringBuilder sb,
                                  Dictionary<int, Data.MonsterData> monsterDic,
                                  Dictionary<int, Data.EquipData> equipDic,
                                  Dictionary<int, Data.ConsumableItemData> itemDic,
                                  Placed placed)
    {
        int scanned = 0;

        string[] sources = new string[0];
        if (Directory.Exists(ScriptDir))
            sources = Directory.GetFiles(ScriptDir, "*.cs", SearchOption.AllDirectories);
        else
            Notes.Add($"'{ScriptDir}' 를 못 찾아 코드에 박힌 키는 못 봤다 (프로젝트 루트에서 실행해야 한다)");

        foreach (string file in sources)
        {
            // 에디터 스크립트는 이 캐시를 안 쓴다 (EditorPreload 로 직접 채운다).
            if (file.Replace('\\', '/').Contains("/Editor/"))
                continue;

            string[] lines = File.ReadAllLines(file);
            string name = Path.GetFileName(file);

            for (int i = 0; i < lines.Length; i++)
            {
                // 주석 처리해 둔 옛 코드까지 걸고 넘어지지 않는다.
                if (lines[i].TrimStart().StartsWith("//"))
                    continue;

                string where = $"{name}:{i + 1}";

                foreach (Regex re in AddressablePatterns)
                {
                    foreach (Match m in re.Matches(lines[i]))
                    {
                        scanned++;
                        RequireKey(m.Groups[1].Value, where);
                    }
                }

                foreach (Match m in ResourcesPattern.Matches(lines[i]))
                {
                    scanned++;
                    string path = m.Groups[1].Value;
                    if (Resources.Load(path) == null)
                        Errors.Add($"{where}: Resources 에 없는 경로 '{path}'");
                }
            }
        }

        // 데이터가 부르는 이펙트·재질. '-' 는 "없음" 이라는 표시라 건너뛴다.
        // (없는 키를 부르면 Instantiate 가 null 을 주고, 그걸 그대로 만지는 쪽이 죽는다.)
        foreach (int id in placed.Monsters)
        {
            Data.MonsterData md;
            if (monsterDic.TryGetValue(id, out md) == false)
                continue;
            scanned += 3;
            RequireKey(md.BattleParticleAttack, "MonsterData.BattleParticleAttack");
            RequireKey(md.BattleParticleHit, "MonsterData.BattleParticleHit");
            RequireKey(md.Shadow, "MonsterData.Shadow");
        }

        foreach (int id in placed.Equips)
        {
            Data.EquipData ed;
            if (equipDic.TryGetValue(id, out ed) == false)
                continue;
            scanned += 3;
            RequireKey(ed.AttackFX, "EquipData.AttackFX");
            RequireKey(ed.HitFX, "EquipData.HitFX");
            RequireKey(ed.Shadow, "EquipData.Shadow");
        }

        foreach (int id in placed.Items)
        {
            Data.ConsumableItemData cd;
            if (itemDic.TryGetValue(id, out cd) == false)
                continue;
            scanned += 2;
            RequireKey(cd.PrefabName, "ConsumableItemData.PrefabName");
            RequireKey(cd.Shadow, "ConsumableItemData.Shadow");
        }

        sb.AppendLine($"  리소스 키 {scanned}곳 확인 (등록된 주소 {Addressed.Count}개, {PreLoadLabel} {Preload.Count}개)");
    }

    static void RequireKey(string key, string where)
    {
        if (string.IsNullOrEmpty(key) || key == "-")
            return;
        if (Reported.Add($"key|{where}|{key}") == false)
            return;

        // 스프라이트 주소에는 .sprite 가 붙는다 (ResourceManager.Load 가 보정해 준다).
        if (Preload.ContainsKey(key) || Preload.ContainsKey(key + ".sprite"))
            return;

        if (Addressed.Contains(key) || Addressed.Contains(key + ".sprite"))
            Errors.Add($"{where}: '{key}' 에 {PreLoadLabel} 라벨이 없다 — 선로드가 안 돼 런타임에 null 이 나온다");
        else
            Errors.Add($"{where}: 어드레서블에 없는 키 '{key}'");
    }

    // ------------------------------------------------------------------ 2) 애니메이션 상태

    static void CheckAnimStates(StringBuilder sb,
                                Dictionary<int, Data.MonsterData> monsterDic,
                                Placed placed)
    {
        // 컨트롤러 경로를 적어 두지 않는다. 게임이 실제로 만드는 프리팹에서 타고 들어간다.
        HashSet<string> mapStates = StatesOf("Monster", null);
        HashSet<string> bossStates = StatesOf("BossMonster", null);
        HashSet<string> cardStates = StatesOf("UI_MonsterCard", "CreatureImage");
        HashSet<string> equipStates = StatesOf("EquipItem", null);

        int kinds = 0;
        foreach (int id in placed.Monsters)
        {
            Data.MonsterData md;
            if (monsterDic.TryGetValue(id, out md) == false)
                continue;   // 없는 id 는 ContentValidator 가 잡는다

            bool boss = placed.Bosses.Contains(id);
            RequireState(boss ? bossStates : mapStates, boss ? "BossMonster" : "Monster",
                         md.IdleAnimStr, $"몬스터 {id} 대기");
            RequireState(cardStates, "UI_MonsterCard/CreatureImage", md.IdleAnimStr, $"몬스터 {id} 대기");
            RequireState(cardStates, "UI_MonsterCard/CreatureImage", md.AttackAnimStr, $"몬스터 {id} 공격");
            kinds++;
        }

        foreach (int id in placed.Equips)
            RequireState(equipStates, "EquipItem", $"EquipItem_{id}", $"떨군 장비 {id}");

        sb.AppendLine($"  애니메이션 상태: 몬스터 {kinds}종, 장비 {placed.Equips.Count}종 확인");
    }

    /// <summary>프리팹(또는 그 자식)에 붙은 애니메이터의 상태 이름을 모은다.
    /// 못 찾으면 null 을 주고 <b>검사를 건너뛴다</b> — 못 본 것을 통과로 적지 않는다.</summary>
    static HashSet<string> StatesOf(string address, string childName)
    {
        string path;
        if (Preload.TryGetValue(address, out path) == false)
        {
            Notes.Add($"'{address}' 가 {PreLoadLabel} 로 등록돼 있지 않아 애니메이션 상태를 못 봤다");
            return null;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Notes.Add($"'{address}' 프리팹을 못 읽어 애니메이션 상태를 못 봤다 ({path})");
            return null;
        }

        Transform target = prefab.transform;
        if (string.IsNullOrEmpty(childName) == false)
        {
            target = FindDeep(prefab.transform, childName);
            if (target == null)
            {
                Notes.Add($"'{address}' 안에 '{childName}' 자식이 없어 애니메이션 상태를 못 봤다");
                return null;
            }
        }

        Animator animator = target.GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Notes.Add($"'{address}' 에 애니메이터가 없어 상태를 못 봤다");
            return null;
        }

        HashSet<string> states = new HashSet<string>();
        AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            // 오버라이드 컨트롤러 같은 것. 상태 이름을 못 읽으니 클립 이름으로 대신 본다.
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                states.Add(clip.name);
            Notes.Add($"'{address}' 는 상태가 아니라 클립 이름으로 봤다 (AnimatorController 가 아니다)");
            return states;
        }

        foreach (AnimatorControllerLayer layer in controller.layers)
            CollectStates(layer.stateMachine, states);
        return states;
    }

    static void CollectStates(AnimatorStateMachine machine, HashSet<string> into)
    {
        if (machine == null)
            return;
        foreach (ChildAnimatorState s in machine.states)
        {
            if (s.state != null)
                into.Add(s.state.name);
        }
        foreach (ChildAnimatorStateMachine sub in machine.stateMachines)
            CollectStates(sub.stateMachine, into);
    }

    static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
                return t;
        }
        return null;
    }

    static void RequireState(HashSet<string> states, string owner, string state, string what)
    {
        if (states == null || string.IsNullOrEmpty(state) || state == "-")
            return;
        if (states.Contains(state))
            return;
        if (Reported.Add($"anim|{owner}|{state}") == false)
            return;

        Errors.Add($"{owner} 컨트롤러에 '{state}' 상태가 없다 ({what}) — 재생해도 경고만 찍히고 그림이 안 나온다");
    }

    // ------------------------------------------------------------------ 3) 포탈 봉인

    static readonly Vector2Int[] Neighbors =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
    };

    /// <summary>포탈(계단·보스방)은 밟고 지나갈 수 없는 마개다. 그 뒤에 갇히는 것이 있으면
    /// 사람도 봇도 닿을 수 없다. 파이썬과 같은 규칙이지만 <b>런타임 MapData</b> 를 본다.</summary>
    static void CheckPortalSeal(StringBuilder sb,
                                Dictionary<int, Data.MapData> mapDic,
                                Dictionary<int, Data.StageInfoData> stageDic)
    {
        int floors = 0;
        int sealedCount = 0;

        foreach (KeyValuePair<int, Data.MapData> kv in mapDic)
        {
            // 손수 만든 층은 프리팹이 실물이라 CSV 로 판정하지 않는다.
            if (IsHandAuthored(kv.Key, stageDic))
                continue;

            Dictionary<Vector2Int, Data.ObjectData> cells = Grid(kv.Value);
            Vector2Int start = Vector2Int.zero;
            bool found = false;
            foreach (KeyValuePair<Vector2Int, Data.ObjectData> c in cells)
            {
                if ((Define.ObjectType)c.Value.ObjectType == Define.ObjectType.SpawnPoint)
                {
                    start = c.Key;
                    found = true;
                    break;
                }
            }
            if (found == false)
                continue;   // 스폰이 없는 층은 ContentValidator 가 잡는다

            floors++;
            HashSet<Vector2Int> reachable = Flood(cells, start, true);

            foreach (Vector2Int c in Flood(cells, start, false))
            {
                if (reachable.Contains(c))
                    continue;

                Define.ObjectType type = (Define.ObjectType)cells[c].ObjectType;
                if (type != Define.ObjectType.Monster && type != Define.ObjectType.BossMonster
                    && type != Define.ObjectType.CItem && type != Define.ObjectType.Eitem)
                    continue;

                sealedCount++;
                if (sealedCount <= 10)
                    Errors.Add($"{Did(kv.Key, stageDic)}: 포탈 뒤에 갇혔다 — {type} {cells[c].Id} (칸 {c.x},{c.y})");
            }
        }

        if (sealedCount > 10)
            Errors.Add($"포탈에 갇힌 것이 모두 {sealedCount}건이다");

        sb.AppendLine($"  포탈 봉인: {floors}개 층 확인, 갇힌 것 {sealedCount}건");
    }

    /// <summary>MapData 의 월드 좌표를 칸 좌표로 되돌린다 (x, z 평면).</summary>
    static Dictionary<Vector2Int, Data.ObjectData> Grid(Data.MapData map)
    {
        Dictionary<Vector2Int, Data.ObjectData> cells = new Dictionary<Vector2Int, Data.ObjectData>();
        if (map.Objects == null)
            return cells;

        foreach (Data.ObjectData o in map.Objects)
        {
            Vector2Int c = Cell(o.Position.X, o.Position.Z);
            cells[c] = o;   // 한 칸에 하나다
        }
        return cells;
    }

    static Vector2Int Cell(float x, float z)
    {
        return new Vector2Int(Mathf.RoundToInt(x / Define.TILE_SIZE),
                              Mathf.RoundToInt(z / Define.TILE_SIZE));
    }

    static HashSet<Vector2Int> Flood(Dictionary<Vector2Int, Data.ObjectData> cells,
                                     Vector2Int start, bool portalsBlock)
    {
        HashSet<Vector2Int> seen = new HashSet<Vector2Int> { start };
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            Vector2Int cur = stack.Pop();
            foreach (Vector2Int d in Neighbors)
            {
                Vector2Int n = cur + d;
                Data.ObjectData o;
                if (seen.Contains(n) || cells.TryGetValue(n, out o) == false)
                    continue;

                Define.ObjectType type = (Define.ObjectType)o.ObjectType;
                if (type == Define.ObjectType.Wall || type == Define.ObjectType.Void)
                    continue;
                // 문은 열쇠로 열리므로 벽이 아니다. 포탈만 영영 못 지나간다.
                if (portalsBlock && type == Define.ObjectType.Portal)
                    continue;

                seen.Add(n);
                stack.Push(n);
            }
        }
        return seen;
    }

    static bool IsWall(Dictionary<Vector2Int, Data.ObjectData> cells, Vector2Int c)
    {
        Data.ObjectData o;
        if (cells.TryGetValue(c, out o) == false)
            return true;   // 격자 밖은 벽으로 친다
        Define.ObjectType type = (Define.ObjectType)o.ObjectType;
        return type == Define.ObjectType.Wall || type == Define.ObjectType.Void;
    }

    // ------------------------------------------------------------------ 4) 문 그림

    // 열쇠 색 순서. ConsumableItemData 0·1·2 가 초록·노랑·빨강이고,
    // 문 그림의 텍스처가 Tilemap_Door_G/Y/R 이다. (재질 이름은 셋 다 palette 라 못 쓴다.)
    const string KeyTones = "GYR";
    static readonly Regex DoorTexture = new Regex("^Tilemap_Door_([GYR])$");

    static void CheckDoorArt(StringBuilder sb,
                             Dictionary<int, Data.MapData> mapDic,
                             Dictionary<int, Data.StageInfoData> stageDic)
    {
        // 층 하나에 문은 셋뿐이다. 여섯 가지 셀 코드를 덮는 최소한의 층만 고른다.
        HashSet<int> want = new HashSet<int> { 3, 4, 5, 6, 7, 8 };
        List<int> picked = new List<int>();

        foreach (int mapId in mapDic.Keys.OrderBy(k => k))
        {
            if (want.Count == 0)
                break;
            if (IsHandAuthored(mapId, stageDic))
                continue;

            HashSet<int> ids = DoorIdsOf(mapDic[mapId]);
            if (ids.Overlaps(want) == false)
                continue;

            picked.Add(mapId);
            want.ExceptWith(ids);
        }

        if (picked.Count == 0)
        {
            Notes.Add("문이 있는 생성 층이 없어 문 검사를 건너뛴다");
            return;
        }
        if (want.Count > 0)
            Notes.Add($"문 셀 코드 {string.Join(",", want.OrderBy(v => v))} 는 어느 층에도 없어 못 봤다");

        // 돌아가는 게임의 매니저와 씬을 건드리지 않는다.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Notes.Add("플레이 모드라 문 검사를 건너뛴다 (에디터를 멈춘 채로 다시 눌러야 본다)");
            return;
        }

        Setup();

        GameObject root = new GameObject("FastChecksRoot");
        int doors = 0;
        try
        {
            foreach (int mapId in picked)
            {
                GameObject map = MapBuilder.Build(mapId, root.transform);
                if (map == null)
                {
                    Errors.Add($"{Did(mapId, stageDic)}: MapBuilder.Build 가 null 을 반환");
                    continue;
                }

                Transform holders = map.transform.Find("Doors");
                Dictionary<Vector2Int, Data.ObjectData> cells = Grid(mapDic[mapId]);
                if (holders != null)
                {
                    foreach (Transform holder in holders)
                    {
                        doors++;
                        CheckOneDoor(holder, cells, Did(mapId, stageDic));
                    }
                }

                Object.DestroyImmediate(map);
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        sb.AppendLine($"  문 그림: {picked.Count}개 층에서 문 {doors}개 확인");
    }

    static HashSet<int> DoorIdsOf(Data.MapData map)
    {
        HashSet<int> ids = new HashSet<int>();
        if (map.Objects == null)
            return ids;
        foreach (Data.ObjectData o in map.Objects)
        {
            if ((Define.ObjectType)o.ObjectType == Define.ObjectType.Door)
                ids.Add(o.Id);
        }
        return ids;
    }

    /// <summary>놓인 문 하나를 실측한다.
    ///
    /// 방향은 <b>문틀의 긴 축</b>과 <b>벽이 있는 축</b>을 견줘서 본다. 셀 코드에서
    /// 프리팹 이름으로 가는 표를 여기에 또 적으면 두 벌이 되어, 같은 방식으로 다시
    /// 어긋난다 (매 층 두 번째 문 96개가 90° 돌아 있던 것이 그랬다).</summary>
    static void CheckOneDoor(Transform holder,
                             Dictionary<Vector2Int, Data.ObjectData> cells,
                             string dungeonId)
    {
        Vector3 p = holder.localPosition;
        Vector2Int c = Cell(p.x, p.z);

        Data.ObjectData obj;
        if (cells.TryGetValue(c, out obj) == false
            || (Define.ObjectType)obj.ObjectType != Define.ObjectType.Door)
        {
            Errors.Add($"{dungeonId}: 문이 데이터의 문 칸이 아닌 곳에 놓였다 ({c.x},{c.y})");
            return;
        }

        bool wallsOnX = IsWall(cells, c + Vector2Int.left) && IsWall(cells, c + Vector2Int.right);
        bool wallsOnZ = IsWall(cells, c + Vector2Int.up) && IsWall(cells, c + Vector2Int.down);
        if (wallsOnX == wallsOnZ)
        {
            // 한쪽 축만 벽이어야 문이 그 틈을 막는다. 둘 다면 지나갈 데가 없고,
            // 둘 다 아니면 문틀이 떠 있어 옆으로 돌아갈 수 있다.
            if (Reported.Add($"doorplace|{obj.Id}|{dungeonId}|{c.x},{c.y}"))
                Errors.Add($"{dungeonId}: 셀 {obj.Id} 문이 벽 사이에 있지 않다 ({c.x},{c.y})");
            return;
        }

        Bounds bounds = new Bounds();
        bool measured = false;
        foreach (Renderer r in holder.GetComponentsInChildren<Renderer>(true))
        {
            if (measured == false)
            {
                bounds = r.bounds;
                measured = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        if (measured == false)
        {
            Errors.Add($"{dungeonId}: 셀 {obj.Id} 문에 그림이 하나도 없다 ({c.x},{c.y}) — 프리팹을 못 찾았다");
            return;
        }

        bool longOnX = bounds.size.x > bounds.size.z;
        if (longOnX != wallsOnX && Reported.Add($"doordir|{obj.Id}"))
        {
            Errors.Add($"{dungeonId}: 셀 {obj.Id} 문이 90도 돌아 있다 — 문틀 {bounds.size.x:0.00}x{bounds.size.z:0.00}, " +
                       $"벽은 {(wallsOnX ? "좌우" : "위아래")}에 있다 (MapBuilder.BuildDoor 의 artId 번역)");
        }

        CheckDoorColor(holder, obj, dungeonId, c);
    }

    static void CheckDoorColor(Transform holder, Data.ObjectData obj, string dungeonId, Vector2Int c)
    {
        int keyIndex = -1;
        foreach (Door d in holder.GetComponentsInChildren<Door>(true))
        {
            keyIndex = d._keyIndex;
            break;
        }
        if (keyIndex < 0)
        {
            Errors.Add($"{dungeonId}: 셀 {obj.Id} 문에 Door 컴포넌트가 없다 ({c.x},{c.y}) — 열쇠로 열 수 없다");
            return;
        }

        int tone = -1;
        foreach (Renderer r in holder.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null || m.mainTexture == null)
                    continue;
                Match match = DoorTexture.Match(m.mainTexture.name);
                if (match.Success)
                {
                    tone = KeyTones.IndexOf(match.Groups[1].Value[0]);
                    break;
                }
            }
            if (tone >= 0)
                break;
        }

        if (tone < 0)
        {
            if (Reported.Add($"doortone|{obj.Id}"))
                Notes.Add($"셀 {obj.Id} 문의 색을 못 읽었다 (텍스처 이름이 Tilemap_Door_G/Y/R 이 아니다)");
            return;
        }

        if (tone != keyIndex && Reported.Add($"doorcolor|{obj.Id}"))
            Errors.Add($"{dungeonId}: 셀 {obj.Id} 문의 그림 색({KeyTones[tone]})이 열쇠 색({keyIndex})과 다르다");
    }

    // ------------------------------------------------------------------ 씬 준비/정리

    static bool _setupDone;
    static bool _madeManagers;
    static bool _madeCursor;

    /// <summary>MapBuilder 를 돌릴 수 있게 런타임과 <b>같은 방식으로</b> 캐시를 채운다.
    /// 목록을 손으로 적지 않는다 — PreLoad 라벨이 붙은 것만 넣는 것이 곧 런타임 규칙이다.</summary>
    static void Setup()
    {
        if (_setupDone)
            return;

        _madeManagers = GameObject.Find("@Managers") == null;
        _madeCursor = GameObject.Find("@Cursor") == null;

        Managers.Init();
        foreach (KeyValuePair<string, string> kv in Preload)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(kv.Value);
            if (asset != null)
                Managers.Resource.EditorPreload(kv.Key, asset);
        }
        Managers.Data.Init();

        _setupDone = true;
    }

    /// <summary>에디터에서 눌렀을 때 씬에 찌꺼기를 남기지 않는다.
    /// 원래 있던 오브젝트는 건드리지 않는다.</summary>
    static void Teardown()
    {
        if (_setupDone == false)
            return;
        _setupDone = false;

        if (_madeManagers)
            DestroyByName("@Managers");
        if (_madeCursor)
            DestroyByName("@Cursor");
    }

    static void DestroyByName(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
            Object.DestroyImmediate(go);
    }

    // ------------------------------------------------------------------ 공통

    static bool IsHandAuthored(int mapId, Dictionary<int, Data.StageInfoData> stageDic)
    {
        Data.StageInfoData info;
        return stageDic.TryGetValue(mapId, out info) && MapBuilder.IsHandAuthored(info.DungeonID);
    }

    static string Did(int mapId, Dictionary<int, Data.StageInfoData> stageDic)
    {
        Data.StageInfoData info;
        return stageDic.TryGetValue(mapId, out info) ? info.DungeonID : mapId.ToString();
    }

    static void Report(StringBuilder sb)
    {
        List<string> notes = Notes.Distinct().ToList();
        List<string> errors = Errors.Distinct().ToList();

        foreach (string n in notes)
            sb.AppendLine("  [확인 못 함] " + n);

        if (errors.Count == 0)
        {
            sb.AppendLine($"  통과. (확인 못 한 것 {notes.Count}건)");
            Debug.Log(sb.ToString());
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
            return;
        }

        int shown = Mathf.Min(errors.Count, 40);
        for (int i = 0; i < shown; i++)
            sb.AppendLine("  [오류] " + errors[i]);
        if (errors.Count > shown)
            sb.AppendLine($"  ... 외 {errors.Count - shown}건");
        sb.AppendLine($"  실패: 오류 {errors.Count}건");

        Debug.LogError(sb.ToString());
        if (Application.isBatchMode)
            EditorApplication.Exit(1);
    }
}
