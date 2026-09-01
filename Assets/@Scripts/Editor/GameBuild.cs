using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 사람이 직접 해 볼 빌드를 만든다.
///
/// 에디터를 띄워 두고 메뉴를 누르는 대신 명령줄에서 돌리려고 만들었다.
/// 에디터가 프로젝트를 잠그고 있으면 두 번째 인스턴스가 열리지 않으니,
/// 에디터를 닫은 상태에서 부른다.
///
/// <code>
/// Unity.exe -quit -batchmode -nographics -projectPath . -executeMethod GameBuild.Prepare
/// Unity.exe -quit -batchmode -nographics -projectPath . -executeMethod GameBuild.Windows
/// </code>
///
/// 순서가 중요하다. 이 게임은 프리팹·데이터·소리를 전부 어드레서블 "PreLoad"
/// 라벨로 읽는다(UI_TitleScene). 콘텐츠를 먼저 굽지 않으면 실행 파일은 만들어지되
/// 타이틀에서 한 발짝도 못 나간다 — 로드할 것이 아무것도 없기 때문이다.
/// </summary>
public static class GameBuild
{
    const string OutDir = "Build/Windows";
    const string ExeName = "TheSword.exe";

    /// <summary>1단계: 새 프리팹·데이터를 어드레서블에 등록하고 저장한다.
    ///
    /// 빌드와 같은 실행에 두면 안 된다. 등록이 에셋을 다시 임포트하면서 도메인
    /// 리로드가 걸리고, -executeMethod 로 돌던 함수가 그 자리에서 조용히 끊긴다
    /// (로그에 "등록 6건" 만 찍히고 에디터가 그대로 종료됐다).</summary>
    public static void Prepare()
    {
        int code = 0;
        try
        {
            Debug.Log("[GameBuild] 어드레서블 등록");
            AddressableSetup.RegisterRuntimePrefabs();
            AssetDatabase.SaveAssets();
            Debug.Log("[GameBuild] 등록 끝");
        }
        catch (Exception e)
        {
            Debug.LogError("[GameBuild] 등록 예외: " + e);
            code = 1;
        }
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }

    /// <summary>2단계: 콘텐츠를 굽고 실행 파일을 만든다.</summary>
    public static void Windows()
    {
        int code = Run(BuildTarget.StandaloneWindows64);
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }

    [MenuItem("TheSword/Build Windows Player")]
    static void MenuBuild()
    {
        Run(BuildTarget.StandaloneWindows64);
    }

    static int Run(BuildTarget target)
    {
        try
        {
            // 콘텐츠를 굽는다. 등록은 Prepare 가 먼저 끝내 둔다.
            Debug.Log("[GameBuild] 어드레서블 콘텐츠 빌드");
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[GameBuild] 어드레서블 설정이 없다");
                return 2;
            }

            AddressableAssetSettings.CleanPlayerContent();
            UnityEditor.AddressableAssets.Build.AddressablesPlayerBuildResult aaResult;
            AddressableAssetSettings.BuildPlayerContent(out aaResult);
            if (aaResult != null && string.IsNullOrEmpty(aaResult.Error) == false)
            {
                Debug.LogError("[GameBuild] 콘텐츠 빌드 실패: " + aaResult.Error);
                return 3;
            }
            Debug.Log($"[GameBuild] 콘텐츠 빌드 {(aaResult != null ? aaResult.Duration.ToString("F1") + "초" : "완료")}");

            // 실행 파일.
            string dir = Path.GetFullPath(OutDir);
            Directory.CreateDirectory(dir);

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = ScenePaths(),
                locationPathName = Path.Combine(dir, ExeName),
                target = target,
                options = BuildOptions.None,
            };

            Debug.Log($"[GameBuild] 플레이어 빌드 -> {opt.locationPathName} (씬 {opt.scenes.Length}개)");
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            BuildSummary sum = report.summary;

            Debug.Log($"[GameBuild] 결과 {sum.result} · {sum.totalSize / (1024 * 1024)}MB · {sum.totalTime}");
            if (sum.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[GameBuild] 실패: 오류 {sum.totalErrors}건");
                return 4;
            }

            Debug.Log("[GameBuild] 완료: " + opt.locationPathName);
            return 0;
        }
        catch (Exception e)
        {
            Debug.LogError("[GameBuild] 예외: " + e);
            return 5;
        }
    }

    /// <summary>빌드 설정에서 켜져 있는 씬만. 순서가 곧 시작 씬을 정한다.</summary>
    static string[] ScenePaths()
    {
        System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (s.enabled)
                list.Add(s.path);
        }
        return list.ToArray();
    }
}
