using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 손수 만든 1~4층 프리팹 안에 무엇이 들어 있는지 그대로 찍는다.
/// 보스방 입구가 포탈인지 문인지 트리거인지 로그로 쫓는 것보다 이게 빠르다.
///
///   Unity.exe -batchmode -nographics -executeMethod HandFloorDump.Run
/// </summary>
public static class HandFloorDump
{
    static readonly string[] Floors = { "Dungeon_00_000", "Dungeon_00_001", "Dungeon_00_002", "Dungeon_00_003" };

    [MenuItem("TheSword/Dump Hand-Authored Floors")]
    public static void Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== 손수 만든 층 덤프 =====");

        foreach (string name in Floors)
        {
            GameObject prefab = null;
            foreach (string guid in AssetDatabase.FindAssets($"{name} t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null && go.name == name)
                {
                    prefab = go;
                    break;
                }
            }

            if (prefab == null)
            {
                sb.AppendLine($"[{name}] 프리팹 없음");
                continue;
            }

            sb.AppendLine($"\n[{name}]");

            foreach (PortalController p in prefab.GetComponentsInChildren<PortalController>(true))
                sb.AppendLine($"  포탈 {p._portalType} mapId={p._mapId} " +
                              $"{Path(p.transform)} pos={Cell(p.transform)} active={p.gameObject.activeSelf}");

            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                int layer = t.gameObject.layer;
                if (layer != (int)Define.Layer.BossDoor
                    && layer != (int)Define.Layer.InteractObjects
                    && layer != (int)Define.Layer.BossEventTrigger
                    && layer != (int)Define.Layer.Lever)
                    continue;
                sb.AppendLine($"  layer={LayerName(layer)} {Path(t)} pos={Cell(t)} " +
                              $"active={t.gameObject.activeSelf} col={(t.GetComponent<Collider>() != null)}");
            }

            foreach (BossDoor d in prefab.GetComponentsInChildren<BossDoor>(true))
                sb.AppendLine($"  BossDoor {Path(d.transform)} pos={Cell(d.transform)} active={d.gameObject.activeSelf}");

            foreach (BossEventTriggerController e in prefab.GetComponentsInChildren<BossEventTriggerController>(true))
                sb.AppendLine($"  BossEventTrigger {Path(e.transform)} pos={Cell(e.transform)} active={e.gameObject.activeSelf}");

            foreach (ConsumableItem ci in prefab.GetComponentsInChildren<ConsumableItem>(true))
                sb.AppendLine($"  아이템 id={ci.id} idx={ci._itemIndex_forActive} {Path(ci.transform)} " +
                              $"pos={Cell(ci.transform)} active={ci.gameObject.activeSelf}");

            foreach (Door dr in prefab.GetComponentsInChildren<Door>(true))
                sb.AppendLine($"  문 key={dr._keyIndex} idx={dr._doorIndex_forActive} {Path(dr.transform)} " +
                              $"pos={Cell(dr.transform)} active={dr.gameObject.activeSelf}");

            foreach (InteractObjectController io in prefab.GetComponentsInChildren<InteractObjectController>(true))
                sb.AppendLine($"  Interact {Path(io.transform)} pos={Cell(io.transform)} active={io.gameObject.activeSelf}");
        }

        Debug.Log(sb.ToString());
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    static string LayerName(int layer)
    {
        return ((Define.Layer)layer).ToString();
    }

    static Vector2Int Cell(Transform t)
    {
        Vector3 p = t.position;
        return new Vector2Int(Mathf.RoundToInt(p.x / 0.32f), Mathf.RoundToInt(p.z / 0.32f));
    }

    static string Path(Transform t)
    {
        string s = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            s = t.name + "/" + s;
        }
        return s;
    }
}
