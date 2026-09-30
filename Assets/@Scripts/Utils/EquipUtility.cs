using UnityEngine;

/// <summary>
/// 장비의 "유틸 기능 해금" (기획서 34쪽).
///
/// EquipData 의 [어빌리티 ID] 열은 여태 인벤토리 설명 문구에만 쓰이고 게임에는
/// 아무 영향이 없었다. 무엇을 해금하는지는 아이템 이름이 말해 준다.
///
///   부츠 4종 (어빌리티 2~5)  레인저 / 스피릿 윈드 / 엔젤 윙 / 타르타로스
///                            -> 이동 속도 등급
///   목걸이 4종 (어빌리티 6~9) 모래시계 / 에온의 고리 / 키르케의 시계추 / 크로노스의 시계
///                            -> 전부 시간 계열 이름이다. 전투 배속 등급.
///   반지 (어빌리티 1)         워프석 반지 -> 층 워프 해금
///
/// 기획서 본문은 "반지=배속, 목걸이=워프" 라고 적었지만 아이템 이름은 그 반대다.
/// 이름을 따랐다 — 크로노스의 시계가 워프를 해금하는 것보다 시간을 당기는 편이 자연스럽다.
/// </summary>
public static class EquipUtility
{
    public const int AbilityWarp = 1;          // 워프석 반지
    public const int AbilityMoveFirst = 2;     // 부츠 2~5
    public const int AbilityMoveLast = 5;
    public const int AbilitySpeedFirst = 6;    // 목걸이 6~9
    public const int AbilitySpeedLast = 9;

    // 등급별 배수. 1등급이 가장 약하다.
    static readonly float[] MoveScale = { 1.15f, 1.30f, 1.45f, 1.60f };

    /// <summary>부츠를 신기 전의 이동 속도 — GameScene 이 들어설 때마다 넣는 값이다.</summary>
    const float BaseMove = 1f;
    static readonly int[] BattleSpeed = { 2, 3, 4, 5 };

    /// <summary>워프를 쓸 수 있는가 (워프석 반지를 끼고 있는가).</summary>
    public static bool WarpUnlocked
    {
        get { return AbilityOf(Managers.Game.PlayerData.CurRing) == AbilityWarp; }
    }

    /// <summary>
    /// 낀 목걸이의 전투 배속 (없으면 1). 배속은 이제 설정(GameSettings.BattleSpeed)이 처음부터 주므로, 목걸이는
    /// 그 설정보다 높을 때만 값을 한다 — UI_BattlePopup.Speed 가 둘 중 큰 쪽을 쓴다. 예전처럼 GameManager.GameSpeed 에
    /// 쓰지 않는다: 그 칸은 이제 봇(AutoPlayer)의 배속만 든다.
    /// </summary>
    public static int NecklaceSpeed
    {
        get
        {
            GameManager.CurPlayerData p = Managers.Game.PlayerData;
            int neck = p == null ? 0 : AbilityOf(p.CurNecklace);
            return neck >= AbilitySpeedFirst && neck <= AbilitySpeedLast ? BattleSpeed[neck - AbilitySpeedFirst] : 1;
        }
    }

    /// <summary>착용 중인 장비의 유틸 효과를 지금 상태에 반영한다.
    ///
    /// 스탯(ATK/DEF/HP…)은 SwapEquip 이 착용/해제 때 더하고 뺀다. 여기서 다루는 것은
    /// 그렇게 누적하면 안 되는 것 — 이동 속도는 "지금 신은 것" 하나로 정해져야 해서,
    /// 매번 기준값에서 다시 계산한다. (목걸이의 전투 배속은 NecklaceSpeed 가 그때그때 읽는다.)</summary>
    public static void Apply()
    {
        GameManager.CurPlayerData p = Managers.Game.PlayerData;
        if (p == null)
            return;

        // 기준 속도는 레벨 표에서 읽으면 안 된다. PlayerData 의 이동속도 열은
        // 절대값이 아니라 레벨당 증가치이고, Lv2 부터는 0 이다. 그걸 그대로 넣으면
        // 이동속도가 0 이 되고 PlayerController.Speed 세터가 1/0 을 물어서
        // 한 칸도 못 가고 멈춘다. 실제로 그렇게 1층에서 굳었다.
        // 게임이 실제로 쓰는 기준값(GameScene 이 넣는 1)을 쓴다. 예전처럼 "처음 부를 때의
        // MoveSpeed" 를 기억하면, 타이틀에서 첫 검을 쥘 때(표의 Lv1 값 2.5) 불린 뒤로
        // 부츠가 2.5배 위에 얹혔다.
        float move = BaseMove;
        int shoes = AbilityOf(p.CurShoes);
        if (shoes >= AbilityMoveFirst && shoes <= AbilityMoveLast)
            move *= MoveScale[shoes - AbilityMoveFirst];

        if (move > 0f && Mathf.Abs(p.MoveSpeed - move) > 0.0001f)
            p.MoveSpeed = move;

        // 값이 같아도 플레이어에는 늘 넣는다. 씬을 다시 올리면(죽음·이어하기) 새 플레이어는 기본 5
        // (이동속도 1)로 서는데, 체크포인트의 MoveSpeed 는 이미 부츠 값이라 "바뀌었을 때만" 넣으면
        // 부츠를 신은 채로 걸음만 느렸다.
        if (Managers.Game.Player != null)
            Managers.Game.Player.Speed = 0f;   // 세터가 MoveSpeed 를 다시 읽는다
    }

    /// <summary>그 장비가 가진 어빌리티 id. 없으면 0.</summary>
    public static int AbilityOf(int equipId)
    {
        Data.EquipData eq;
        if (equipId <= 0 || Managers.Data.EquipDic.TryGetValue(equipId, out eq) == false)
            return 0;
        return eq.AbilityId;
    }
}
