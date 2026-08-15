using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MapBuilder 가 100층을 실제로 조립하는지 헤드리스로 확인한다.
///
///   unity run . -- -executeMethod MapBuilderSmokeTest.Run
///
/// 데이터 검증(ContentValidator)과 달리 이쪽은 진짜로 GameObject 를 만들어 본다.
/// RefreshMap 이 이름으로 찾는 컨테이너가 다 있는지, 몬스터/아이템/문/계단이
/// 실제 컴포넌트로 붙는지까지 본다.
/// </summary>
public static class MapBuilderSmokeTest
{
    static readonly string[] DataKeys =
    {
        // DataManager.Init() 이 로드하는 것 전부. 하나라도 빠지면 textAsset.text 에서 NRE.
        "PlayerData", "MonsterData", "ConsumableItemData", "MonsterClassData",
        "MapData", "EquipData", "ScriptData", "StageInfoData", "EventData",
    };

    static readonly string[] PrefabKeys =
    {
        "Tilemap_1", "Tilemap_C00_W01", "Tilemap_C00_W02", "Tilemap_C00_W03",
        "Tilemap_3", "Tilemap_4", "Tilemap_5",
        "Tilemap_12", "Tilemap_13", "Tilemap_14", "Tilemap_15", "Tilemap_16",
        "Monster", "BossMonster", "ConsumableItem", "EquipItem",
    };

    [MenuItem("TheSword/Smoke Test MapBuilder")]
    public static void Run()
    {
        // 배치모드에서 executeMethod 예외는 스택 없이 한 줄만 남는다. 직접 찍는다.
        try
        {
            RunInternal();
        }
        catch (System.Exception e)
        {
            Debug.LogError("스모크 테스트 예외:\n" + e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    static void RunInternal()
    {
        var errors = new List<string>();
        var sb = new StringBuilder();
        sb.AppendLine("===== MapBuilder 스모크 테스트 =====");

        Managers.Init();
        PreloadTextAssets(errors);
        PreloadPrefabs(errors);

        if (errors.Count > 0)
        {
            Finish(sb, errors);
            return;
        }

        Managers.Data.Init();
        sb.AppendLine($"  데이터 로드: 스테이지 {Managers.Data.StageInfoDic.Count}, " +
                      $"맵 {Managers.Data.MapDic.Count}, 몬스터 {Managers.Data.MonsterDic.Count}");

        GameObject root = new GameObject("SmokeTestRoot");
        int builtFloors = 0, totalMonsters = 0, totalItems = 0, totalDoors = 0, totalPortals = 0;

        foreach (var kv in Managers.Data.StageInfoDic)
        {
            int mapId = kv.Key;
            string did = kv.Value.DungeonID;

            // 1~4층은 런타임에 조립하지 않는다 (손수 만든 프리팹을 그대로 쓴다).
            // 그 층의 CSV 는 세이브 인덱스 계산용이라 계단/몬스터가 없을 수 있다.
            if (MapBuilder.IsHandAuthored(did))
                continue;

            GameObject map = MapBuilder.Build(mapId, root.transform);
            if (map == null)
            {
                errors.Add($"{did}: Build 가 null 을 반환");
                continue;
            }
            builtFloors++;

            // GameManager.RefreshMap 이 이름으로 찾는 컨테이너
            foreach (string c in new[] { "Monsters", "Items", "Doors", "Pillars", "Levers" })
            {
                if (map.transform.Find(c) == null)
                    errors.Add($"{did}: 컨테이너 '{c}' 없음 — RefreshMap 에서 예외가 난다");
            }

            int mons = map.GetComponentsInChildren<MonsterController>(true).Length;
            int citems = map.GetComponentsInChildren<ConsumableItem>(true).Length;
            int doors = map.GetComponentsInChildren<Door>(true).Length;
            PortalController[] portals = map.GetComponentsInChildren<PortalController>(true);

            totalMonsters += mons;
            totalItems += citems;
            totalDoors += doors;
            totalPortals += portals.Length;

            if (mons == 0) errors.Add($"{did}: 몬스터 컴포넌트가 하나도 안 붙었다");
            if (doors == 0) errors.Add($"{did}: 문이 안 만들어졌다");

            bool hasUp = false, hasDown = false;
            foreach (PortalController p in portals)
            {
                if (p._mapId != mapId)
                    errors.Add($"{did}: 포탈 _mapId 가 {p._mapId} (기대 {mapId})");
                if (p._portalType == PortalController.Type.UpStairs) hasUp = true;
                if (p._portalType == PortalController.Type.DownStairs) hasDown = true;
                // 포탈은 자기 홀더를 가져야 한다 (보스 포탈 숨김이 층 전체를 끄지 않도록)
                if (p.transform.parent == null || p.transform.parent == map.transform)
                    errors.Add($"{did}: 포탈에 홀더가 없다");
            }
            if (hasUp == false && mapId < Managers.Data.StageInfoDic.Count - 1)
                errors.Add($"{did}: 위층 계단 포탈 없음");
            if (hasDown == false && mapId > 0)
                errors.Add($"{did}: 아래층 계단 포탈 없음");

            // 문 자물쇠 위치: Door.Start() 가 transform.parent.GetChild(1) 를 쓴다
            foreach (Door d in map.GetComponentsInChildren<Door>(true))
            {
                if (d.transform.parent == null || d.transform.parent.childCount < 2)
                    errors.Add($"{did}: 문 부모에 자식이 2개 미만 — Door.Start() 에서 예외가 난다");
            }

            // 스폰 포인트 태그
            bool spawn = false;
            foreach (Transform t in map.GetComponentsInChildren<Transform>(true))
                if (t.CompareTag("SpawnPoint")) { spawn = true; break; }
            if (spawn == false) errors.Add($"{did}: SpawnPoint 태그 오브젝트 없음");

            Object.DestroyImmediate(map);
        }

        Object.DestroyImmediate(root);

        sb.AppendLine($"  조립 성공: {builtFloors}층");
        sb.AppendLine($"  몬스터 {totalMonsters}, 소비아이템 {totalItems}, 문 {totalDoors}, 포탈 {totalPortals}");
        Finish(sb, errors);
    }

    static void PreloadTextAssets(List<string> errors)
    {
        foreach (string key in DataKeys)
        {
            string path = $"Assets/@Resources/Data/JsonData/{key}.json";
            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (ta == null) errors.Add($"데이터 에셋 없음: {path}");
            else Managers.Resource.EditorPreload(key, ta);
        }
    }

    static void PreloadPrefabs(List<string> errors)
    {
        foreach (string key in PrefabKeys)
        {
            GameObject go = FindPrefab(key);
            if (go == null) errors.Add($"프리팹 없음: {key}");
            else Managers.Resource.EditorPreload(key, go);
        }
    }

    static GameObject FindPrefab(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{name} t:Prefab"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p) == name)
                return AssetDatabase.LoadAssetAtPath<GameObject>(p);
        }
        return null;
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
