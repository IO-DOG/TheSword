// Steamworks.NET 과 같은 조건이다 — 그 패키지가 Steam 코드를 빼는 플랫폼에서는 여기도 빈 껍데기로 남는다.
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// Steam 연결 (Steamworks.NET). Managers.Init 이 한 번 켜고, Managers.Update 가 매 프레임 콜백을 돌리고,
/// 앱이 꺼질 때(Application.quitting) 닫는다. 업적·통계·Rich Presence 는 SteamHooks 가 맡는다.
///
/// Steam 이 없으면(꺼져 있다, 게임을 가지고 있지 않다, steam_appid.txt 도 없다) 한 번 적고 아무것도 하지 않는다.
/// 에디터에서도 빌드에서도 Steam 없이 똑같이 돈다 — Steam 때문에 게임이 멈추면 안 된다. 단 AppId 를 넣은 릴리스 빌드를 Steam 밖에서
/// 켜면 RestartAppIfNecessary 가 Steam 으로 다시 켜려고 곧바로 끈다 — Steam 없는 PC·전시용으로 줄 빌드는 exe 옆에 steam_appid.txt.
/// 에디터는 프로젝트 루트의 steam_appid.txt(Steamworks.NET 이 480 = Spacewar 로 만든다)와 켜진 Steam 이 있으면 붙는다.
/// </summary>
public static class SteamManager
{
    /// <summary>
    /// 본편 AppID. 체험판도 찜하기(OpenStorePage)는 이 번호의 스토어를 연다. 아직 앱을 등록하지 않아 0 이다.
    /// 본편과 체험판(DEMO)은 AppID 가 따로다(MASTER_PLAN §7) — Steam/README.md 1절.
    /// </summary>
    public const uint GameAppId = 0;
    /// <summary>체험판 AppID (본편 앱 페이지의 Add Demo 로 만든 앱). DEMO 로 구운 빌드(GameBuild.WindowsDemo)가 쓴다.</summary>
    public const uint DemoAppId = 0;

    /// <summary>
    /// 지금 도는 빌드의 AppID. 체험판 빌드가 본편 AppID 로 RestartAppIfNecessary 를 부르면 체험판 대신 본편을 띄운다.
    /// 0 이면 건너뛴다. 0 이 아니면 Steam 밖에서 켠 릴리스 빌드가 Steam 을 거쳐 다시 켜진다.
    /// steam_appid.txt 는 개발용이라 배포 폴더에 넣지 않는다(GameBuild 가 지운다).
    /// </summary>
#if DEMO
    public const uint AppId = DemoAppId;
#else
    public const uint AppId = GameAppId;
#endif

    /// <summary>본편 스토어 페이지가 있다 — 찜하기 단추를 달 수 있다(UI_DemoEndPopup).</summary>
    public static bool HasStorePage => GameAppId != 0;

    /// <summary>
    /// 본편 스토어 페이지를 연다(찜하기). Steam 이 붙어 있고 오버레이가 켜져 있으면 게임 안에서, 아니면 브라우저로 연다.
    /// 오버레이는 Steam 을 거쳐 켰을 때만 붙는다 — 없는데 오버레이를 부르면 아무 일도 없어서 단추가 죽은 것처럼 보인다.
    /// </summary>
    public static void OpenStorePage()
    {
        if (HasStorePage == false)
            return;
#if !DISABLESTEAMWORKS
        if (Initialized && SteamUtils.IsOverlayEnabled())
        {
            SteamFriends.ActivateGameOverlayToStore(new AppId_t(GameAppId), EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
            return;
        }
#endif
        // 오버레이를 끈 사람은 Steam 앱의 상점으로(브라우저는 대개 Steam 에 로그인돼 있지 않아 찜을 못 한다). Steam 이 없으면 웹.
        Application.OpenURL(Initialized ? "steam://store/" + GameAppId : "https://store.steampowered.com/app/" + GameAppId + "/");
    }

    /// <summary>Steam 에 붙어 있다. 거짓이면 Steam 을 부르지 않는다.</summary>
    public static bool Initialized { get; private set; }

    /// <summary>Managers.Init 이 설정을 맞추기 전에 부른다(Steam 의 게임 언어가 기본 언어가 된다). 두 번째부터는 아무 일도 없다.</summary>
    public static void Init()
    {
#if !DISABLESTEAMWORKS
        if (s_tried)
            return;
        s_tried = true;
        try
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            // Steam 밖에서 켰으면 Steam 이 게임을 다시 띄우고 이쪽은 끝낸다.
            if (AppId != 0 && SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
            {
                Application.Quit();
                return;
            }
#endif
            ESteamAPIInitResult result = SteamAPI.InitEx(out string error);
            if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Debug.Log($"[Steam] 붙지 못했다 — Steam 없이 돈다 ({result}: {error})");
                return;
            }
        }
        catch (System.Exception e)      // steam_api64.dll 이 없거나 읽지 못했다
        {
            Debug.Log($"[Steam] 붙지 못했다 — Steam 없이 돈다: {e.Message}");
            return;
        }

        Initialized = true;
        Application.quitting -= Shutdown;   // 에디터는 정적 이벤트가 플레이마다 쌓인다 — 빼고 건다
        Application.quitting += Shutdown;
        s_overlay = Callback<GameOverlayActivated_t>.Create(OnOverlay);
        // 여기서 난 예외가 Managers.Init 을 빠져나가면 설정 맞추기(ApplySettings: 언어·글꼴·화면·소리)가 통째로 빠진다.
        try { DefaultLanguage(); }
        catch (System.Exception e) { Debug.LogException(e); }
#endif
    }

    /// <summary>Steam 콜백(업적 알림 등)을 돌린다. Managers.Update 가 매 프레임 부른다.</summary>
    public static void RunCallbacks()
    {
#if !DISABLESTEAMWORKS
        if (Initialized)
        {
            SteamAPI.RunCallbacks();
            if (s_overlayUp)
                PauseForOverlay();
            SteamHooks.Flush();
        }
#endif
    }

#if !DISABLESTEAMWORKS
    static bool s_tried;

    // 오버레이(Shift+Tab, 덱의 Steam 단추)는 창 포커스를 가져가지 않아서 그 뒤에서 전투가 그대로 돌았다 — 창을 잃을 때처럼
    // 메뉴를 연다(UI_GameScene.OnApplicationFocus 와 같은 길, 전투도 멈춘다). 콜백은 붙잡아 둬야 GC 가 거두지 않는다.
    // 열리는 순간에 못 열면(연출·보스 등장·전투창이 서기 전 한 프레임 — TryOpenMenu 가 거절한다) 오버레이가 떠 있는 동안 다시 본다.
    static Callback<GameOverlayActivated_t> s_overlay;
    static bool s_overlayUp;

    static void OnOverlay(GameOverlayActivated_t overlay) => s_overlayUp = overlay.m_bActive != 0;

    static void PauseForOverlay()
    {
        if (GameEvents.IsAutoPlaying || Managers.IsAlive == false)
            return;
        UI_GameScene scene = Managers.Game.GameScene;
        if (scene != null && scene.TryOpenMenu())
            s_overlayUp = false;    // 한 번 열었다 — 닫은 메뉴를 다시 열지 않는다
    }

    // 에디터는 도메인 리로드 없이 플레이를 되풀이한다. 지난 플레이는 끄면서 닫았으니(Shutdown) 이번 플레이가 다시 켠다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Initialized = false;
        s_tried = false;
        s_overlay = null;       // 지난 플레이의 콜백은 SteamAPI.Shutdown 이 풀었다
        s_overlayUp = false;
    }

    // 끈 뒤에 오는 호출(OnDestroy 에서 난 GameEvents 등)은 Initialized 가 막는다.
    static void Shutdown()
    {
        if (Initialized == false)
            return;
        SteamHooks.Flush(final: true);      // 1초 묶음에 걸려 아직 안 올린 마지막 해제·통계
        Initialized = false;
        SteamAPI.Shutdown();
    }

    // 언어를 한 번도 고르지 않았으면 Steam 의 게임 언어를 따른다 — 이번 실행만(GameSettings.UseLanguageForSession). 남기면 고른 것으로
    // 굳어서, Steamworks 에 지원 언어를 올리기 전의 첫 실행이 받은 "english" 가 영영 남고 Steam 에서 언어를 바꿔도 따라가지 않았다.
    // 목록에 없는 언어는 OS 언어 그대로다.
    static void DefaultLanguage()
    {
        if (GameSettings.LanguageChosen)
            return;
        switch (SteamApps.GetCurrentGameLanguage())
        {
            case "koreana": GameSettings.UseLanguageForSession(Define.ScriptType.Kr); break;
            case "english": GameSettings.UseLanguageForSession(Define.ScriptType.En); break;
            case "japanese": GameSettings.UseLanguageForSession(Define.ScriptType.Jp); break;
            case "schinese":
            case "tchinese": GameSettings.UseLanguageForSession(Define.ScriptType.Cn); break;
        }
    }
#endif
}
