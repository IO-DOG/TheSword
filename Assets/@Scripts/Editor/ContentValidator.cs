using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 생성된 100층 콘텐츠가 실제로 진행 가능한 형태인지 헤드리스로 검증한다.
///
///   unity run . -- -executeMethod ContentValidator.Validate
///
/// 검사 항목
///   - 스테이지 그래프가 100층으로 이어지는지 (Up/Down 계단 연결)
///   - 층마다 MapData 가 있고 스폰/계단/몬스터가 배치돼 있는지
///   - 몬스터/아이템 ID 가 실제 데이터 테이블에 존재하는지
///   - 층이 참조하는 타일 프리팹이 프로젝트에 존재하는지
///   - 레벨업 테이블이 도달 가능한 최대 레벨보다 길어서 예외가 안 나는지
/// </summary>
public static class ContentValidator
{
    const string JsonDir = "Assets/@Resources/Data/JsonData";
    const int ExpectedFloors = 100;
    // 챕터 보스 5 + 손수 만든 킹슬라임(00_003) 1
    const int ExpectedBossFloors = 6;

    static readonly List<string> Errors = new List<string>();
    static readonly List<string> Warnings = new List<string>();

    [MenuItem("TheSword/Validate 100F Content")]
    public static void Validate()
    {
        Errors.Clear();
        Warnings.Clear();

        var stages = Load<Data.StageInfoDataLoader>("StageInfoData");
        var monsters = Load<Data.MonsterDataLoader>("MonsterData");
        var players = Load<Data.PlayerDataLoader>("PlayerData");
        var items = Load<Data.ConsumableItemDataLoader>("ConsumableItemData");
        var maps = Load<Data.MapDataLoader>("MapData");

        if (stages == null || monsters == null || players == null || maps == null || items == null)
        {
            Report();
            return;
        }

        var stageDic = stages.MakeDict();
        var monsterDic = monsters.MakeDict();
        var playerDic = players.MakeDict();
        var itemDic = items.MakeDict();
        var mapDic = maps.MakeDict();

        CheckStageGraph(stageDic);
        CheckMaps(stageDic, mapDic, monsterDic, itemDic);
        CheckLevelTable(playerDic);

        Report();
    }

    static T Load<T>(string name) where T : class
    {
        string path = Path.Combine(JsonDir, name + ".json");
        if (File.Exists(path) == false)
        {
            Errors.Add($"데이터 파일 없음: {path}");
            return null;
        }
        try
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }
        catch (System.Exception e)
        {
            Errors.Add($"{name}.json 파싱 실패: {e.Message}");
            return null;
        }
    }

    static void CheckStageGraph(Dictionary<int, Data.StageInfoData> stages)
    {
        if (stages.Count != ExpectedFloors)
            Errors.Add($"스테이지 수가 {stages.Count} (기대: {ExpectedFloors})");

        int bossFloors = 0;
        foreach (var kv in stages)
        {
            Data.StageInfoData s = kv.Value;

            if (s.ATK <= 0 || s.DEF <= 0)
                Errors.Add($"{s.DungeonID}: ATK/DEF 보정이 {s.ATK}/{s.DEF} — 곱셈 계수이므로 0 이면 몬스터 스탯이 0 이 된다");
            if (s.EXP <= 0)
                Errors.Add($"{s.DungeonID}: EXP 보정이 0");

            if (s.Type == Define.DungeonType.Boss)
                bossFloors++;

            // 위층 연결 확인 (손수 만든 도입부는 선형이 아니라 예외)
            if (kv.Key < ExpectedFloors - 1 && MapBuilder.IsHandAuthored(s.DungeonID) == false)
            {
                if (s.UpStage == "-" || string.IsNullOrEmpty(s.UpStage))
                    Errors.Add($"{s.DungeonID}: 위층 연결 없음");
                else if (stages.ContainsKey(kv.Key + 1) == false
                         || stages[kv.Key + 1].DungeonID != s.UpStage)
                    Errors.Add($"{s.DungeonID}: 위층 {s.UpStage} 이 다음 스테이지와 불일치");
            }
        }

        if (bossFloors != ExpectedBossFloors)
            Errors.Add($"보스 층이 {bossFloors}개 (기대: {ExpectedBossFloors} — 20층마다 1개 + 킹슬라임)");

        // 도입부 -> 생성 구간 이음매
        Data.StageInfoData kingSlime, fifth;
        if (stages.TryGetValue(3, out kingSlime) && kingSlime.UpStage != "00_004")
            Errors.Add("00_003(킹슬라임)에서 5층으로 올라가는 연결이 끊겼다");
        if (stages.TryGetValue(4, out fifth) && fifth.DownStage != "00_003")
            Errors.Add("5층에서 4층으로 내려가는 연결이 끊겼다");
    }

    static void CheckMaps(Dictionary<int, Data.StageInfoData> stages,
                          Dictionary<int, Data.MapData> maps,
                          Dictionary<int, Data.MonsterData> monsters,
                          Dictionary<int, Data.ConsumableItemData> items)
    {
        var prefabCache = new Dictionary<string, bool>();

        foreach (var kv in stages)
        {
            int id = kv.Key;
            string did = kv.Value.DungeonID;

            Data.MapData map;
            if (maps.TryGetValue(id, out map) == false || map.Objects == null)
            {
                Errors.Add($"{did}: MapData 없음");
                continue;
            }

            // 손수 만든 층은 프리팹이 실물이다. CSV 구조로 판정하지 않는다
            // (00_002 는 마검 이벤트용 막다른 방이라 계단이 없는 게 정상).
            if (MapBuilder.IsHandAuthored(did))
                continue;

            int spawn = 0, upStairs = 0, downStairs = 0, mobs = 0, floors = 0, doors = 0, keys = 0;

            foreach (Data.ObjectData o in map.Objects)
            {
                switch ((Define.ObjectType)o.ObjectType)
                {
                    case Define.ObjectType.Floor: floors++; break;
                    case Define.ObjectType.SpawnPoint: spawn++; break;
                    case Define.ObjectType.Door: doors++; break;
                    case Define.ObjectType.Portal:
                        if (o.Id == 14) upStairs++;
                        else if (o.Id == 15) downStairs++;
                        break;
                    case Define.ObjectType.Monster:
                    case Define.ObjectType.BossMonster:
                        mobs++;
                        if (monsters.ContainsKey(o.Id) == false)
                            Errors.Add($"{did}: 존재하지 않는 몬스터 ID {o.Id}");
                        break;
                    case Define.ObjectType.CItem:
                        if (items.ContainsKey(o.Id) == false)
                            Errors.Add($"{did}: 존재하지 않는 아이템 ID {o.Id}");
                        else if (o.Id < ConsumableItem.NUM_OF_KEYS)
                            keys++;
                        break;
                    case Define.ObjectType.Wall:
                        RequirePrefab($"Tilemap_C00_W{o.Id:00}", did, prefabCache);
                        break;
                }
            }

            if (floors == 0) Errors.Add($"{did}: 바닥 타일이 하나도 없다");
            if (spawn == 0) Errors.Add($"{did}: 스폰 포인트 없음");
            if (mobs == 0) Errors.Add($"{did}: 몬스터 없음");
            if (upStairs == 0 && id < ExpectedFloors - 1)
                Errors.Add($"{did}: 위층 계단(14) 없음");
            // 위로 올라갈 때 PortalController 는 "다음 층의 아래층 계단"을 찾는다.
            if (downStairs == 0 && id > 0)
                Errors.Add($"{did}: 아래층 계단(15) 없음 — 아랫층에서 올라올 수 없다");
            if (doors != keys && doors > 0)
                Warnings.Add($"{did}: 문 {doors}개 / 열쇠 {keys}개 — 개수가 다르다");
        }

        foreach (string key in new[] { "Tilemap_1", "Tilemap_14", "Tilemap_15",
                                       "Tilemap_3", "Tilemap_4", "Tilemap_5",
                                       "Monster", "BossMonster", "ConsumableItem" })
            RequirePrefab(key, "공통", prefabCache);
    }

    static void RequirePrefab(string name, string context, Dictionary<string, bool> cache)
    {
        bool exists;
        if (cache.TryGetValue(name, out exists) == false)
        {
            exists = AssetDatabase.FindAssets($"{name} t:Prefab").Length > 0;
            // 이름이 정확히 일치하는지 확인
            if (exists)
            {
                exists = false;
                foreach (string guid in AssetDatabase.FindAssets($"{name} t:Prefab"))
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileNameWithoutExtension(p) == name) { exists = true; break; }
                }
            }
            cache[name] = exists;
        }
        if (exists == false)
            Errors.Add($"{context}: 프리팹 없음 '{name}'");
    }

    static void CheckLevelTable(Dictionary<int, Data.PlayerData> players)
    {
        // 100층까지 가면 레벨 101 이 된다. CurExp 세터가 PlayerDic[Level + 1] 을
        // 무조건 읽으므로 그보다 넉넉해야 KeyNotFoundException 이 안 난다.
        int max = 0;
        foreach (var kv in players) max = Mathf.Max(max, kv.Key);
        if (max < 103)
            Errors.Add($"PlayerData 최대 레벨이 {max} — 100층 완주(레벨 101) 시 예외가 난다");
    }

    static void Report()
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== TheSword 100층 콘텐츠 검증 =====");
        foreach (string w in Warnings) sb.AppendLine("  [경고] " + w);

        if (Errors.Count == 0)
        {
            sb.AppendLine($"  통과. (경고 {Warnings.Count}건)");
            Debug.Log(sb.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(0);
            return;
        }

        int shown = Mathf.Min(Errors.Count, 40);
        for (int i = 0; i < shown; i++) sb.AppendLine("  [오류] " + Errors[i]);
        if (Errors.Count > shown) sb.AppendLine($"  ... 외 {Errors.Count - shown}건");
        sb.AppendLine($"  실패: 오류 {Errors.Count}건");
        Debug.LogError(sb.ToString());
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}
