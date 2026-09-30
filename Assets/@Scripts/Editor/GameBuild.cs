using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
///
/// Windows 는 콘텐츠 검증(validate_content.py)을 통과해야 굽고, 굽기 전에 출력 폴더를 비운다.
/// 내보내면 안 되는 디버그 기호 폴더(*_DoNotShip)는 Build/Symbols/{버전}/ 으로 옮기고, 개발용 steam_appid.txt 는 지운다.
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
            if (ContentIsValid() == false)
                return 6;

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

            // 실행 파일. 지난 빌드에서 남은 파일(지운 씬·에셋의 데이터)이 섞이지 않게 비우고 시작한다.
            string dir = Path.GetFullPath(OutDir);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = ScenePaths(),
                locationPathName = Path.Combine(dir, ExeName),
                target = target,
                // 압축하지 않으면 .assets/.resS 가 날것으로 들어간다 (536MB 중 텍스처가 88%).
                options = BuildOptions.CompressWithLz4HC,
            };

            // 콘텐츠는 위에서 한 번 구웠다. 플레이어 빌드가 또 굽지 않게 그 결과를 그대로 건넨다
            // (예전에는 46.9초 + 13.1초로 두 번 구웠다). 위에서 따로 굽는 쪽을 남기는 까닭은
            // 매번 비우고(CleanPlayerContent) 굽고, 실패를 여기서 종료 코드로 알리기 위해서다.
            AddressablesPlayerBuildProcessor.BuildAddressablesOverride = _ => aaResult;
            BuildReport report;
            try
            {
                Debug.Log($"[GameBuild] 플레이어 빌드 -> {opt.locationPathName} (씬 {opt.scenes.Length}개)");
                report = BuildPipeline.BuildPlayer(opt);
            }
            finally
            {
                AddressablesPlayerBuildProcessor.BuildAddressablesOverride = null;
            }
            BuildSummary sum = report.summary;

            Debug.Log($"[GameBuild] 결과 {sum.result} · {sum.totalSize / (1024 * 1024)}MB · {sum.totalTime}");
            if (sum.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[GameBuild] 실패: 오류 {sum.totalErrors}건");
                return 4;
            }

            MoveSymbols(dir);
            // steam_appid.txt 는 개발용이다 — 남으면 Steam 을 거치지 않고 켜진다(SteamManager.AppId). 개발 빌드가 아니면 지운다.
            if ((opt.options & BuildOptions.Development) == 0)
                File.Delete(Path.Combine(dir, "steam_appid.txt"));
            Debug.Log("[GameBuild] 완료: " + opt.locationPathName);
            Debug.Log("[GameBuild] Steam 에 올리기(SteamPipe, beta 브랜치): powershell -ExecutionPolicy Bypass -File Steam/scripts/upload.ps1");
            return 0;
        }
        catch (Exception e)
        {
            Debug.LogError("[GameBuild] 예외: " + e);
            return 5;
        }
    }

    /// <summary>콘텐츠가 깨졌으면 굽지 않는다.
    ///
    /// ContentValidator.Validate 를 부르지 않는 까닭: 배치모드에서는 보고를 찍자마자 에디터를 끈다
    /// (EditorApplication.Exit — 통과해도 0 으로). 굽기와 같은 실행에서 부르면 실행 파일 없이
    /// "성공" 으로 끝난다. 그래서 같은 검사를 하는 파이썬 쪽을 부른다 — 에디터와 상관없이 돈다.</summary>
    static bool ContentIsValid()
    {
        ProcessStartInfo info = new ProcessStartInfo("python", "Tools/validate_content.py")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };
        info.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
        try
        {
            using (Process p = Process.Start(info))
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode == 0)
                {
                    Debug.Log("[GameBuild] 콘텐츠 검증 통과\n" + output);
                    return true;
                }
                Debug.LogError($"[GameBuild] 콘텐츠 검증 실패(종료 코드 {p.ExitCode}) — 굽지 않는다\n{output}");
                return false;
            }
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            Debug.LogError("[GameBuild] python 을 못 찾아 콘텐츠를 검증하지 못했다 — 굽지 않는다: " + e.Message);
            return false;
        }
    }

    /// <summary>Unity 가 실행 파일 옆에 늘 만드는 디버그 기호 폴더를 버전별로 옮겨 둔다.
    /// 내보내면 안 되지만 크래시를 풀 때 필요하다 (Burst 는 *_DoNotShip, IL2CPP 는 *_ButDontShipItWithYourGame).</summary>
    static void MoveSymbols(string dir)
    {
        string symbols = Path.GetFullPath($"Build/Symbols/{PlayerSettings.bundleVersion}");
        foreach (string from in Directory.GetDirectories(dir, "*_DoNotShip")
                     .Concat(Directory.GetDirectories(dir, "*_ButDontShipItWithYourGame")))
        {
            Directory.CreateDirectory(symbols);
            string to = Path.Combine(symbols, Path.GetFileName(from));
            if (Directory.Exists(to))
                Directory.Delete(to, true);
            Directory.Move(from, to);
            Debug.Log($"[GameBuild] 기호 폴더 -> {to}");
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
