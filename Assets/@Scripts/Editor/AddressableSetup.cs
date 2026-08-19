using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// MapBuilder 가 런타임에 로드하는 프리팹을 Addressables("PreLoad" 라벨)에 등록한다.
///
///   Unity.exe -batchmode -quit -executeMethod AddressableSetup.RegisterRuntimePrefabs
///
/// ResourceManager.Load 는 "PreLoad" 라벨로 선로드한 딕셔너리에서만 꺼내온다.
/// 라벨이 없으면 조용히 null 이 나오고, 층에 벽이 하나도 안 생긴 채로 게임이 돌아간다
/// (플레이어가 미로를 뚫고 지나가 버린다). 그래서 여기서 강제로 맞춰 둔다.
/// </summary>
public static class AddressableSetup
{
    const string Label = "PreLoad";
    const string GroupName = "Prefabs";

    // 어드레스 -> 에셋 경로. MapBuilder 가 이름으로 찾는 것들만 넣는다.
    static readonly Dictionary<string, string> Required = new Dictionary<string, string>
    {
        { "Tilemap_C00_W01", "Assets/Resources/DecoTiles/Dungeon_00/Tilemap_C00_W01.prefab" },
        { "Tilemap_C00_W02", "Assets/Resources/DecoTiles/Dungeon_00/Tilemap_C00_W02.prefab" },
        { "Tilemap_C00_W03", "Assets/Resources/DecoTiles/Dungeon_00/Tilemap_C00_W03.prefab" },

        // 룬 획득 이펙트 (기획서 29쪽). 프리팹은 있는데 등록이 안 돼 있어서
        // ConsumableItemData 의 이름으로 찾으면 null 이 나왔다.
        { "FX_RunStone_Red", "Assets/Retro Arsenal/FX_Particle/FX_RunStone_Red.prefab" },
        { "FX_RunStone_Blue", "Assets/Retro Arsenal/FX_Particle/FX_RunStone_Blue.prefab" },
        { "FX_RunStone_Green", "Assets/Retro Arsenal/FX_Particle/FX_RunStone_Green.prefab" },
    };

    [MenuItem("TheSword/Register Runtime Prefabs (Addressables)")]
    public static void RegisterRuntimePrefabs()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        if (settings == null)
        {
            Debug.LogError("[AddressableSetup] Addressables 설정이 없다.");
            Exit(1);
            return;
        }

        AddressableAssetGroup group = settings.FindGroup(GroupName) ?? settings.DefaultGroup;

        int added = 0, missing = 0;
        foreach (KeyValuePair<string, string> kv in Required)
        {
            if (File.Exists(kv.Value) == false)
            {
                Debug.LogError($"[AddressableSetup] 에셋 없음: {kv.Value}");
                missing++;
                continue;
            }

            string guid = AssetDatabase.AssetPathToGUID(kv.Value);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = kv.Key;
            entry.SetLabel(Label, true, true);
            added++;
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AddressableSetup] 등록 {added}건, 누락 {missing}건");
        Exit(missing == 0 ? 0 : 1);
    }

    static void Exit(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}
