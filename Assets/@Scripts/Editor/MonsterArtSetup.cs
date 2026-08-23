using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 스프라이트 시트로 몬스터 애니메이션 클립을 만들고 컨트롤러에 등록한다.
///
/// 왜 필요한가
/// -----------
/// 1) 그림이 여덟 종뿐이라 100층을 채우면 같은 놈이 계속 나온다. 그런데 Assets 에는
///    쓰지 않고 놀고 있는 시트가 더 있다 — Boss_C1_I000 과 예전 Monster_Idle 이
///    기존 몹과 규격이 같은데(344x86, 4프레임) 클립이 없어서 못 쓰고 있었다.
/// 2) 공격 클립이 아예 없다. MonsterData 의 공격 애니메이션 이름(Mob_C0_A000…)이
///    가리키는 상태가 없어서 전투 내내 "State could not be found" 만 찍히고
///    몬스터는 공격해도 가만히 있었다. 공격 시트는 진작부터 있었다.
///
///   Unity.exe -projectPath . -executeMethod MonsterArtSetup.Build
/// </summary>
public static class MonsterArtSetup
{
    const string ClipDir = "Assets/@Resources/Animations/MonsterAnimations";
    const string MapController = ClipDir + "/MonsterAnim.controller";
    const string UIController = "Assets/@Resources/Animations/UIMonsterAnimations/UIMonsterAnimController.controller";

    const string IdleDir = "Assets/@Resources/Sprites/MonsterSprites/Idle";
    const string AttackDir = "Assets/@Resources/Sprites/MonsterSprites/Attack";
    const string LegacyDir = "Assets/@Resources/Sprites/LegacySprites";

    const float Fps = 12f;

    /// <summary>클립 이름 -> 스프라이트 시트 경로.</summary>
    static Dictionary<string, string> Wanted()
    {
        var map = new Dictionary<string, string>();

        // 기존 여덟 종의 대기/공격
        for (int i = 0; i < 8; i++)
        {
            map[$"Mob_C0_I{i:000}"] = $"{IdleDir}/Mob_{i:000}_Idle.png";
            map[$"Mob_C0_A{i:000}"] = $"{AttackDir}/Mob_{i:000}_Attack.png";
        }

        // 놀고 있던 시트 둘을 새 몬스터로 편입한다.
        map["Mob_C0_I008"] = $"{IdleDir}/Boss_C1_I000.png";
        map["Mob_C0_I009"] = $"{LegacyDir}/Monster_Idle.png";
        map["Mob_C0_A009"] = $"{LegacyDir}/Monster_Attack.png";   // 위에서 잘라 준다

        return map;
    }

    /// <summary>단일 스프라이트로 들어와 프레임을 못 꺼내는 시트. 먼저 잘라 준다.</summary>
    static readonly string[] NeedSlicing =
    {
        LegacyDir + "/Monster_Attack.png",
    };

    [MenuItem("TheSword/Build Monster Animations")]
    public static void Build()
    {
        Directory.CreateDirectory(ClipDir);

        foreach (string path in NeedSlicing)
        {
            if (File.Exists(path) && LoadFrames(path).Length <= 1)
                SliceHorizontally(path);
        }

        // 잘린 조각 수가 시트 크기와 안 맞으면 다시 자른다.
        //
        // 까마귀(Mob_002_Idle)가 그랬다. 프레임 넷짜리 시트에 "시트 전체" 를 가리키는
        // 다섯 번째 조각이 하나 더 들어가 있어서, 그 프레임이 재생될 때 까마귀가
        // 가로로 네 마리 늘어선 것처럼 보였다. 손으로 자르다 남은 조각으로 보인다.
        foreach (string path in Wanted().Values)
        {
            if (File.Exists(path) == false)
                continue;

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null || tex.height <= 0 || tex.width % tex.height != 0)
                continue;

            int expected = tex.width / tex.height;
            if (expected > 1 && LoadFrames(path).Length != expected)
            {
                Debug.LogWarning($"[MonsterArt] {Path.GetFileNameWithoutExtension(path)} 조각 수가 " +
                                 $"{LoadFrames(path).Length}개다 ({expected}개여야 한다). 다시 자른다.");
                SliceHorizontally(path);
            }
        }

        var made = new List<string>();
        var missing = new List<string>();

        foreach (KeyValuePair<string, string> pair in Wanted())
        {
            if (File.Exists(pair.Value) == false)
            {
                missing.Add($"{pair.Key} <- {Path.GetFileName(pair.Value)} (시트 없음)");
                continue;
            }

            Sprite[] frames = LoadFrames(pair.Value);
            if (frames.Length == 0)
            {
                // 잘라 놓지 않은 시트는 프레임을 꺼낼 수 없다. 억지로 쓰지 않는다.
                missing.Add($"{pair.Key} <- {Path.GetFileName(pair.Value)} (슬라이스 안 됨)");
                continue;
            }

            if (WriteClip($"{ClipDir}/{pair.Key}.anim", pair.Key, frames))
                made.Add($"{pair.Key} ({frames.Length}프레임)");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int added = Register(MapController) + Register(UIController);
        added += BuildEquipDropClips();

        Debug.Log($"[MonsterArt] 클립 {made.Count}개 생성/갱신, 컨트롤러에 {added}개 상태 추가");
        foreach (string s in made)
            Debug.Log($"[MonsterArt]   + {s}");
        foreach (string s in missing)
            Debug.LogWarning($"[MonsterArt]   - {s}");
    }


    // ---------------------------------------------------------------- 떨군 장비
    const string EquipClipDir = "Assets/@Resources/Animations/ItemAnimatios/Item";
    const string EquipController = EquipClipDir + "/EquipItemAnimator.controller";
    const string NecklaceSprite = "Assets/@Resources/Sprites/Equip/EquipSprite/Equip_Necklace_00.png";
    const string RingSprite = "Assets/@Resources/Sprites/Equip/EquipSprite/Equip_Ring_00.png";

    /// <summary>맵에 떨어진 장비가 재생하는 EquipItem_{id} 상태를 채운다.
    ///
    /// Equip.Start 는 무조건 그 이름의 상태를 재생하는데, 0~4 밖에 없었다.
    /// 없는 상태를 재생하면 경고만 찍히고 아무 그림도 안 나온다 — 즉 떨어진 장비가
    /// 눈에 보이지 않는다. 챕터 보스가 목걸이(5~8)를 떨구므로 그 자리를 채운다.
    /// 목걸이 그림은 한 장뿐이라 넷이 같은 그림을 쓴다.</summary>
    static int BuildEquipDropClips()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(NecklaceSprite);
        if (sprite == null)
        {
            Debug.LogWarning($"[MonsterArt] 목걸이 그림이 없다: {NecklaceSprite}");
            return 0;
        }

        var names = new List<string>();
        for (int id = 5; id <= 8; id++)
        {
            string clipName = $"EquipItem_{id}";
            WriteClip($"{EquipClipDir}/{clipName}.anim", clipName, new[] { sprite });
            names.Add(clipName);
        }

        // 워프석 반지(32). 챕터 보스가 떨구므로 바닥에 보여야 한다.
        Sprite ring = AssetDatabase.LoadAssetAtPath<Sprite>(RingSprite);
        if (ring != null)
        {
            WriteClip($"{EquipClipDir}/EquipItem_32.anim", "EquipItem_32", new[] { ring });
            names.Add("EquipItem_32");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(EquipController);
        if (controller == null)
        {
            Debug.LogWarning($"[MonsterArt] 컨트롤러가 없다: {EquipController}");
            return 0;
        }

        var have = new HashSet<string>();
        foreach (AnimatorControllerLayer layer in controller.layers)
            foreach (ChildAnimatorState st in layer.stateMachine.states)
                have.Add(st.state.name);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        int added = 0;
        foreach (string clipName in names)
        {
            if (have.Contains(clipName))
                continue;
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{EquipClipDir}/{clipName}.anim");
            if (clip == null)
                continue;
            machine.AddState(clip.name).motion = clip;
            added++;
        }

        if (added > 0)
        {
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }
        Debug.Log($"[MonsterArt] 떨군 장비 상태 {added}개 추가 (EquipItem_5~8)");
        return added;
    }


    /// <summary>가로로 이어 붙인 시트를 프레임 수만큼 잘라 준다.
    ///
    /// 예전 Monster_Attack.png 는 다른 공격 시트와 같은 258x86(86 짜리 세 장)인데
    /// 단일 스프라이트로 들어와 있어서 프레임을 꺼낼 수가 없었다. 그래서 그 그림은
    /// 공격 동작을 못 쓰고 대기만 반복했다.</summary>
    static bool SliceHorizontally(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return false;

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null || tex.height <= 0)
            return false;

        int size = tex.height;                 // 프레임은 정사각형이다
        int count = tex.width / size;
        if (count <= 1 || tex.width % size != 0)
            return false;

        string baseName = Path.GetFileNameWithoutExtension(path);
        var rects = new List<SpriteRect>();
        for (int i = 0; i < count; i++)
        {
            rects.Add(new SpriteRect
            {
                name = $"{baseName}_{i}",
                rect = new Rect(i * size, 0, size, size),
                pivot = new Vector2(0.5f, 0.5f),
                alignment = SpriteAlignment.Center,
                spriteID = GUID.Generate(),
            });
        }

        importer.spriteImportMode = SpriteImportMode.Multiple;
        var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        provider.SetSpriteRects(rects.ToArray());
        provider.Apply();

        importer.SaveAndReimport();
        Debug.Log($"[MonsterArt] {baseName} 를 {count}프레임으로 잘랐다");
        return true;
    }

    /// <summary>시트에서 프레임을 이름 순서대로 꺼낸다 (_0, _1, _2 …).</summary>
    static Sprite[] LoadFrames(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(s => FrameIndex(s.name))
            .ToArray();
    }

    static int FrameIndex(string name)
    {
        int at = name.LastIndexOf('_');
        int n;
        if (at >= 0 && int.TryParse(name.Substring(at + 1), out n))
            return n;
        return 0;
    }

    static bool WriteClip(string path, string clipName, Sprite[] frames)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool isNew = clip == null;
        if (isNew)
            clip = new AnimationClip();

        clip.name = clipName;
        clip.frameRate = Fps;

        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / Fps, value = frames[i] };

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        // 전투창은 SpriteRenderer 가 아니라 Image 다. 두 바인딩을 다 넣어 두면
        // 맵과 전투창이 같은 클립을 쓸 수 있다.
        EditorCurveBinding uiBinding = EditorCurveBinding.PPtrCurve("", typeof(UnityEngine.UI.Image), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, uiBinding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNew)
            AssetDatabase.CreateAsset(clip, path);
        else
            EditorUtility.SetDirty(clip);
        return true;
    }

    /// <summary>컨트롤러에 없는 클립을 상태로 추가한다. 이미 있으면 건드리지 않는다.</summary>
    static int Register(string controllerPath)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogWarning($"[MonsterArt] 컨트롤러가 없다: {controllerPath}");
            return 0;
        }

        var have = new HashSet<string>();
        foreach (AnimatorControllerLayer layer in controller.layers)
            foreach (ChildAnimatorState st in layer.stateMachine.states)
                have.Add(st.state.name);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        int added = 0;
        foreach (string clipName in Wanted().Keys)
        {
            if (have.Contains(clipName))
                continue;

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipDir}/{clipName}.anim");
            if (clip == null)
                continue;

            machine.AddState(clip.name).motion = clip;
            added++;
        }

        if (added > 0)
            EditorUtility.SetDirty(controller);
        return added;
    }
}
