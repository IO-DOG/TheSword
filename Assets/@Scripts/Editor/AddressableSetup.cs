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

    // 어드레스 -> 에셋 경로. 코드가 이름으로 찾는데 등록이 빠져 있던 것들만 넣는다.
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

        // 스토리 카드 그림 (Tools/story_gen.py 의 IMAGE_ADDRESS). 스프라이트는 "파일 이름.sprite" 로
        // 올려야 한다 — ResourceManager 가 "주소[스프라이트 이름]" 으로 꺼내고, 스프라이트 이름은 파일 이름이다.
        { "04_탑_용병수정.sprite", "Assets/@Resources/Sprites/UI/Intro/04_탑_용병수정.PNG" },   // TowerArrival
        { "05_마을.sprite", "Assets/@Resources/Sprites/UI/Intro/05_마을.PNG" },                 // TowerGraves
        { "00_배경.sprite", "Assets/@Resources/Sprites/UI/Intro/00_배경.PNG" },                 // Parchment
        { "ThankYouForPlaying.sprite", "Assets/@Resources/Sprites/ThankYouForPlaying/ThankYouForPlaying.png" },   // Ending
        { "GameOver1.sprite", "Assets/@Resources/Sprites/UI/UI_Popup/UI_GameOverPopup/GameOver1.png" },

        // 폴백 글꼴 원본 (FontFallback.PixelFontKey). Silver SDF 에 없는 글자(히라가나·。，：▶)를
        // 이 TTF 로 런타임에 굽는다. 등록이 없으면 윈도 글꼴로만 그린다.
        { "Silver", "Assets/@Resources/Font/Silver.ttf" },

        // 인벤토리 아이콘 (EquipData.ImageName -> Load<Sprite>). 검(Equip_Sword_00~11)만 등록돼 있어서
        // 신발·목걸이·반지·방패 칸이 비어 보였다. 그림이 종류마다 한 장뿐이라 같은 종류는 한 아이콘을 쓴다.
        // Sprites/Item 쪽이 점 필터라 검 아이콘과 같은 선명도로 나온다 (맵에 떨군 장비도 이 그림이다).
        { "Equip_Boot_00.sprite", "Assets/@Resources/Sprites/Item/Equip_Boot_00.png" },
        { "Equip_Necklace_00.sprite", "Assets/@Resources/Sprites/Item/Equip_Necklace_00.png" },
        { "Equip_Ring_00.sprite", "Assets/@Resources/Sprites/Item/Equip_Ring_00.png" },
        { "Equip_Shield_00.sprite", "Assets/@Resources/Sprites/Item/Equip_Shield_00.png" },
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
