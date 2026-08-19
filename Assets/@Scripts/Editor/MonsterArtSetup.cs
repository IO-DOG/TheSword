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

        return map;
    }

    [MenuItem("TheSword/Build Monster Animations")]
    public static void Build()
    {
        Directory.CreateDirectory(ClipDir);

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

        Debug.Log($"[MonsterArt] 클립 {made.Count}개 생성/갱신, 컨트롤러에 {added}개 상태 추가");
        foreach (string s in made)
            Debug.Log($"[MonsterArt]   + {s}");
        foreach (string s in missing)
            Debug.LogWarning($"[MonsterArt]   - {s}");
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
