using UnityEngine;

/// <summary>
/// 체험판의 끝 (MASTER_PLAN D1). DEMO 정의로 구운 빌드(GameBuild.WindowsDemo)에서만 돈다 — 본편에서는 아무 일도 없다.
///
/// 20층 보스와 렌의 반지까지가 체험판이다. 21층에 서면 끝 카드(UI_DemoEndPopup)를 띄운다. 계단으로 올라온 때(FloorEntered)
/// 말고 21층 체크포인트를 이어 한 때도 있어서 HUD 가 설 때(HudRefreshed)도 본다 — 이어하기에는 FloorEntered 가 오지 않는다.
/// 계단으로 오면 카드는 EnterFloor 바로 앞의 HUD 갱신(PortalController 의 GameScene.Refresh)에서 뜨고, 21층 입구 체크포인트는
/// 같은 호출 안에서 그 뒤에 적힌다(GameManager.EnterFloor). 본편은 같은 폴더를 읽지만 지도(MapData 해시)가 같을 때만 그 저장을
/// 잇는다 — 체험판을 낸 뒤 본편 지도를 고치면 거절된다(지우지는 않는다). 그래서 카드는 잇는다고 약속하지 않는다(steam2.py 561).
/// 봇(GameEvents.IsAutoPlaying)은 막지 않는다 — 카드 없이 지나간다.
/// </summary>
public static class DemoGate
{
    /// <summary>체험판의 마지막 층 (stageId, 0 부터 — 20층). 이 층을 넘어서면 끝이다.</summary>
    public const int LastStage = 19;

#if DEMO
    // GameEvents 는 SubsystemRegistration 에서 구독자를 비우므로 그 뒤(BeforeSceneLoad)에 붙는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        GameEvents.FloorEntered += (stageId, firstVisit) => Check(stageId);
        GameEvents.HudRefreshed += () => Check(Managers.Game.PlayerData.CurStageid);
    }

    static void Check(int stageId)
    {
        if (stageId > LastStage && GameEvents.IsAutoPlaying == false)
            UI_DemoEndPopup.Show();
    }
#endif
}
