using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 1층에서 100층까지 실제로 "이어지는지" 확인한다.
///
///   Unity.exe -batchmode -nographics -executeMethod ProgressionTest.Run
///
/// ContentValidator 는 데이터, MapBuilderSmokeTest 는 한 층의 조립을 본다.
/// 여기서는 층과 층 사이 — 계단이 서로를 찾는지, 보스 관문이 열리고 닫히는지 — 를 본다.
/// PortalController.SearchPortal 과 같은 규칙으로 찾는다:
///   F층의 UpStairs 는 "F+1 층의 DownStairs" 를 같은 챕터 안에서 찾는다.
/// </summary>
public static class ProgressionTest
{
    const string JsonDir = "Assets/@Resources/Data/JsonData";
    const int TotalFloors = 100;
    const int FloorsPerChapter = 20;

    // DataManager.Init 이 읽는 목록을 손으로 관리하면 하나 빠질 때마다 NRE 가 난다
    // (EventData 가 실제로 그랬다). 폴더에 있는 json 을 전부 넣는다.

    [MenuItem("TheSword/Progression Test (1F -> 100F)")]
    public static void Run()
    {
        // 배치모드에서 예외가 나면 스택 없이 "threw exception" 만 남아 원인을 못 본다.
        try
        {
            Execute();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ProgressionTest] {e}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    static void Execute()
    {
        var errors = new List<string>();
        var sb = new StringBuilder();
        sb.AppendLine("===== TheSword 1~100층 진행 검증 =====");

        Managers.Init();
        Preload(errors);
        if (errors.Count > 0)
        {
            Finish(sb, errors);
            return;
        }
        Managers.Data.Init();
        LoadActiveDicFromMapData();

        int reached = 1;
        int gatesChecked = 0;

        for (int chapterStart = 0; chapterStart < TotalFloors; chapterStart += FloorsPerChapter)
        {
            int chapterEnd = Mathf.Min(chapterStart + FloorsPerChapter - 1, TotalFloors - 1);

            var maps = new Dictionary<int, GameObject>();
            GameObject root = new GameObject($"Chapter_{chapterStart}");

            for (int id = chapterStart; id <= chapterEnd; id++)
            {
                string did = Managers.Data.StageInfoDic[id].DungeonID;
                // 손수 만든 층은 프리팹이 실물이라 조립 대상이 아니다.
                GameObject map = MapBuilder.IsHandAuthored(did)
                    ? null
                    : MapBuilder.Build(id, root.transform);
                if (map != null)
                    maps.Add(id, map);
            }

            // 챕터 안에서 F -> F+1 로 계단이 이어지는가
            for (int id = chapterStart; id < chapterEnd; id++)
            {
                if (maps.ContainsKey(id) == false || maps.ContainsKey(id + 1) == false)
                    continue;   // 손수 만든 구간은 프리팹 연결이라 여기서 검사하지 않는다

                if (HasPortal(maps[id], id, PortalController.Type.UpStairs) == false)
                    errors.Add($"{id + 1}층: 위층 계단 없음");
                else if (HasPortal(maps[id + 1], id + 1, PortalController.Type.DownStairs) == false)
                    errors.Add($"{id + 1}층 -> {id + 2}층: 도착지(아래층 계단)를 못 찾는다");
                else
                    reached = Mathf.Max(reached, id + 2);
            }

            // 챕터 경계: 마지막 층의 위층 계단은 다음 챕터 첫 층으로 이어져야 한다
            if (chapterEnd < TotalFloors - 1 && maps.ContainsKey(chapterEnd))
            {
                if (HasPortal(maps[chapterEnd], chapterEnd, PortalController.Type.UpStairs) == false)
                    errors.Add($"{chapterEnd + 1}층: 다음 챕터로 나가는 계단이 없다");
            }

            // 보스 관문: 보스가 살아 있으면 잠기고, 잡으면 열려야 한다
            foreach (KeyValuePair<int, GameObject> kv in maps)
            {
                if (Managers.Data.StageInfoDic[kv.Key].Type != Define.DungeonType.Boss)
                    continue;

                MonsterController boss = FindBoss(kv.Value);
                if (boss == null)
                {
                    errors.Add($"{kv.Key + 1}층: 보스 층인데 Boss 태그 몬스터가 없다");
                    continue;
                }

                gatesChecked++;
                Managers.Game.Maps = maps;

                Managers.Data.MonsterActiveDic[boss._monsterIndex_forActive] = true;
                Managers.Game.RefreshBossGates();
                if (GateOpen(kv.Value))
                    errors.Add($"{kv.Key + 1}층: 보스가 살아 있는데 위층 계단이 열려 있다");

                Managers.Data.MonsterActiveDic[boss._monsterIndex_forActive] = false;
                Managers.Game.RefreshBossGates();
                if (GateOpen(kv.Value) == false)
                    errors.Add($"{kv.Key + 1}층: 보스를 잡았는데 위층 계단이 안 열린다 — 진행 불가");
            }

            Object.DestroyImmediate(root);
        }

        sb.AppendLine($"  계단 연결 확인: {reached}층까지");
        sb.AppendLine($"  보스 관문 확인: {gatesChecked}개");
        if (reached < TotalFloors)
            errors.Add($"{reached}층에서 진행이 끊긴다 (기대 {TotalFloors}층)");

        Finish(sb, errors);
    }

    static bool HasPortal(GameObject map, int mapId, PortalController.Type type)
    {
        foreach (PortalController p in map.GetComponentsInChildren<PortalController>(true))
            if (p._mapId == mapId && p._portalType == type)
                return true;
        return false;
    }

    static MonsterController FindBoss(GameObject map)
    {
        foreach (MonsterController mc in map.GetComponentsInChildren<MonsterController>(true))
            if (mc.CompareTag("Boss"))
                return mc;
        return null;
    }

    static bool GateOpen(GameObject map)
    {
        foreach (PortalController p in map.GetComponentsInChildren<PortalController>(true))
        {
            if (p._portalType != PortalController.Type.UpStairs)
                continue;
            return p.transform.parent != null && p.transform.parent.gameObject.activeSelf;
        }
        return false;
    }

    /// <summary>ActiveDic 은 보통 런타임에 만들어진다. 배치에서는 MapData 로 채운다.</summary>
    static void LoadActiveDicFromMapData()
    {
        var alive = new Dictionary<int, bool>();
        foreach (KeyValuePair<int, Data.MapData> kv in Managers.Data.MapDic)
        {
            if (kv.Value.Objects == null)
                continue;
            foreach (Data.ObjectData o in kv.Value.Objects)
            {
                if (o.ObjectType == (int)Define.ObjectType.Monster
                    || o.ObjectType == (int)Define.ObjectType.BossMonster)
                    alive[o.Count] = true;
            }
        }
        Managers.Data.MonsterActiveDic = alive;
    }

    static void Preload(List<string> errors)
    {
        if (Directory.Exists(JsonDir) == false)
        {
            errors.Add($"데이터 폴더 없음: {JsonDir}");
            return;
        }

        foreach (string path in Directory.GetFiles(JsonDir, "*.json"))
        {
            string key = Path.GetFileNameWithoutExtension(path);
            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path.Replace('\\', '/'));
            if (ta == null)
                errors.Add($"데이터 에셋 없음: {path}");
            else
                Managers.Resource.EditorPreload(key, ta);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null)
                Managers.Resource.EditorPreload(go.name, go);
        }
    }

    static void Finish(StringBuilder sb, List<string> errors)
    {
        if (errors.Count == 0)
        {
            sb.AppendLine("  통과");
            Debug.Log(sb.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(0);
            return;
        }
        int shown = Mathf.Min(errors.Count, 30);
        for (int i = 0; i < shown; i++) sb.AppendLine("  [오류] " + errors[i]);
        if (errors.Count > shown) sb.AppendLine($"  ... 외 {errors.Count - shown}건");
        sb.AppendLine($"  실패: 오류 {errors.Count}건");
        Debug.LogError(sb.ToString());
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}
