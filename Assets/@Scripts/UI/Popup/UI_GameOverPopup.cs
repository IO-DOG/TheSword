using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameOverPopup : UI_Popup
{
    #region Enum
    enum Images
    {
        BG,
        GameOverIllust,
    }


    #endregion

    // 이만큼(실시간 초) 지나면 아무 키·클릭으로 곧장 되살아난다. 예전에는 죽을 때마다 8.6초를 기다렸다.
    const float SkipAfter = 2f;

    bool _respawned;
    float _openedAt;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        _openedAt = Time.unscaledTime;
        BindImage(typeof(Images));

        GetImage((int)Images.BG).gameObject.SetActive(false);
        GetImage((int)Images.GameOverIllust).gameObject.SetActive(false);
        ShowRecap();

        DeadAni();

        return true;
    }

    #region 죽음 되짚기
    // 죽으면 그 층 입구로 돌아갈 뿐 무엇이 모자랐는지는 말해 주지 않았다(11-28). 사실만 적는다 — 누구에게·어느 층에서,
    // 외눈이 보인 값과 그때 가진 체력, 싸우지 않고 닿는데 남긴 물약, 안 쓴 스킬. 되살리기까지 2초(SkipAfter)는 넘길 수 없으니
    // 그림보다 먼저, 창이 서자마자 띄운다. 연출 글이라 무엇이 틀어져도 되살리기를 막지 않는다.
    const int RecapWho = 690, RecapCost = 691, RecapHp = 692, RecapShort = 693, RecapPotions = 694, RecapSkills = 695;
    const int SkillNameFirst = 191;     // 강타·철벽·흡혈 (ui_text_parts/ui.py)

    void ShowRecap()
    {
        try
        {
            List<string> lines = RecapLines();
            if (lines.Count == 0)
                return;
            TMP_FontAsset font = CodeUI.ProseFont;
            if (font == null)
                return;
            // 글 밑에 어두운 띠 — 게임오버 그림의 아래쪽(주인공의 발치·검날 끝)과 겹쳐도 읽히게. 줄 수만큼의 높이.
            Image back = CodeUI.NewImage(transform, "RecapBack", null, new Color(0f, 0f, 0f, 0f));
            CodeUI.Place(back.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(1700f, lines.Count * 56f + 24f));
            back.DOFade(0.55f, 0.4f).SetUpdate(true).SetLink(back.gameObject);
            TextMeshProUGUI text = CodeUI.NewText(transform, "Recap", font, 44f, new Color(0.88f, 0.88f, 0.88f), TextAlignmentOptions.Bottom);
            CodeUI.Place(text.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1700f, 200f));
            text.fontSharedMaterial = CodeUI.Outlined(font, 0.25f);     // 처음 2.4초는 맵 위에 뜬다
            text.text = string.Join("\n", lines);
            text.alpha = 0f;
            text.DOFade(1f, 0.4f).SetUpdate(true).SetLink(text.gameObject);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GameOver] 되짚기를 못 띄웠다: {e.Message}");
        }
    }

    /// <summary>방금 진 싸움의 되짚기 줄. 싸움 밖에서 죽었거나 몬스터를 모르면 비어 있다.</summary>
    public static List<string> RecapLines()
    {
        var lines = new List<string>();
        GameManager g = Managers.Game;
        GameManager.CurPlayerData p = g != null ? g.PlayerData : null;
        if (p == null || LastBattle.Won || LastBattle.MonsterId <= 0)
            return lines;
        // 이름: 표에 있는 몬스터는 표에서, 킹 슬라임·분열 슬라임(9001~, 연출이 세워 표에 없다)은 방금 싸운 상대의 자료에서
        int nameId = 0;
        if (Managers.Data.MonsterDic.TryGetValue(LastBattle.MonsterId, out Data.MonsterData md))
            nameId = md.MonsterNameId;
        else if (g.MonsterData != null && g.MonsterData.Count > 0 && g.MonsterData[0] != null)
            nameId = g.MonsterData[0].MonsterNameId;
        string who = nameId != 0 ? Managers.GetString(nameId) : "";
        if (string.IsNullOrEmpty(who))
            return lines;

        lines.Add(string.Format(Managers.GetString(RecapWho), p.CurStageid + 1, who));

        // 값은 마검의 외눈이 보는 것이라 계약 뒤에만 말한다. 못 쓰러뜨리는 상대(Kills 가 아님)의 값은 뜻이 없다.
        int hp = Mathf.FloorToInt(LastBattle.HpBefore);
        int lack = LastBattle.Foretold - hp + 1;        // 이기려면 체력이 값보다 커야 한다 (BattleForecast.Win)
        if (p.IsContractedSword && LastBattle.ForetoldKills && lack > 0)
            lines.Add(string.Format(Managers.GetString(RecapCost), "-" + LastBattle.Foretold) + "   "
                      + string.Format(Managers.GetString(RecapHp), hp) + "   "
                      + "<color=#FF7A5C>" + string.Format(Managers.GetString(RecapShort), lack) + "</color>");

        var left = new List<string>();
        int potions = PotionsInReach(g);
        if (potions > 0)
            left.Add(string.Format(Managers.GetString(RecapPotions), potions));
        if (BattleSkills.Unlocked)
        {
            string names = "";
            for (int i = 0; i < BattleSkills.Count; i++)
                if (BattleSkills.IsUsed(i) == false)
                    names += " " + Managers.GetString(SkillNameFirst + i);
            if (names.Length > 0)
                left.Add(Managers.GetString(RecapSkills) + " " + names);
        }
        if (left.Count > 0)
            lines.Add(string.Join("      ", left));
        return lines;
    }

    /// <summary>
    /// 지금 선 자리에서 싸우지도 문을 열지도 않고 닿는 회복 물약 수. 클릭 이동과 같은 격자(PathMover, 열쇠 안 씀)로 잰다 —
    /// 물건 칸은 막힌 칸으로 세니 옆 칸에 닿으면 닿는 것이다.
    /// </summary>
    static int PotionsInReach(GameManager g)
    {
        GameObject map;
        if (g.Player == null || g.Maps.TryGetValue(g.PlayerData.CurStageid, out map) == false || map == null)
            return 0;
        var grid = new PathMover { UseKeys = false };
        grid.Begin(map, g.Player.transform.position.y + PathMover.Tile * 0.5f);
        grid.Flood(PathMover.Cell(map, g.Player.transform.position));

        int count = 0;
        foreach (ConsumableItem item in map.GetComponentsInChildren<ConsumableItem>(false))
        {
            Data.ConsumableItemData d;
            if (item.id < ConsumableItem.NUM_OF_KEYS || item.id >= ConsumableItem.NUM_OF_POTIONS
                || Managers.Data.ConsumableItemDic.TryGetValue(item.id, out d) == false || d.Heal <= 0)
                continue;
            Vector2Int cell = PathMover.Cell(map, item.transform.position);
            foreach (Vector2Int dir in PathMover.Dirs)
                if (grid.Dist.ContainsKey(cell + dir))
                {
                    count++;
                    break;
                }
        }
        return count;
    }
    #endregion

    void DeadAni()
    {
        // 장비 없애기
        Managers.Game.Player._isEquiptShield = false;
        Managers.Game.Player._isEquiptWeapon = false;
        GameObject.Find("UI_PlayerHPBar")?.SetActive(false);

        // player 죽는 파티클 생성
        Vector3 particlePos = Managers.Game.Player.gameObject.transform.position;
        // 이 파티클이 null 이면 아래 SetState(Death) 와 CoDeadAni 가 통째로 날아가
        // 게임오버 화면이 영영 안 뜬다. 없으면 연출만 건너뛴다.
        GameObject deathSoulPurple = Managers.Resource.Instantiate("FX_UserDeath");
        if (deathSoulPurple != null)
        {
            deathSoulPurple.transform.position = particlePos;
            Destroy(deathSoulPurple, 3);
        }

        Managers.Game.Player.SetState(Define.PlayerState.Death);
        //Destroy(Managers.Game.Player.gameObject);

        Managers.Sound.FadeAndStopBGM(3f);
        StartCoroutine(CoDeadAni());
    }

    IEnumerator CoDeadAni()
    {
        yield return new WaitForSeconds(2.4f);

        GetImage((int)Images.BG).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.GameOverIllust).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.BG).gameObject.SetActive(true);
        GetImage((int)Images.GameOverIllust).gameObject.SetActive(true);

        // 인겜 죽음 연출(FX_UserDeath + PlayerState.Death)은 DeadAni 가 이미 재생한다.
        // 여기에 1.5초짜리 비트를 더 끼울지는 기획 판단이라 비워 둔다.

        StartCoroutine(Util.CoFade(GetImage((int)Images.BG), 0.2f));
        yield return new WaitForSeconds(0.2f);

        // 게임 오버 일러 서서히 등장
        //StartCoroutine(Util.CoFade(GetImage((int)Images.GameOverIllust), 2f));
        //yield return new WaitForSeconds(2f);

        GetImage((int)Images.GameOverIllust).GetComponent<Animator>().Play("UI_GameOverAni");
        Managers.Sound.Play(Define.Sound.Effect, "GameOver_SFX");

        // 등장 1.5초뒤
        yield return new WaitForSeconds(6f);

        // 게임 오버 일러스트 페이드 아웃
        //StartCoroutine(Util.CoFade(GetImage((int)Images.GameOverIllust), 1f, false));

        Respawn();
    }

    // 맨 위일 때만 — 첫 전투에서 죽으면 전투 안내(UI_GuidePopup)가 이 위에 뜬다. 그걸 닫는 Enter·Esc 로 같이 넘기지 않는다:
    // 안내는 닫히는 그 자리에서 목록을 빠지므로, 같은 프레임에 이쪽이 나중에 돌면 제가 맨 위로 보인다(ClosedThisFrame).
    void Update()
    {
        if (_respawned || Time.unscaledTime - _openedAt < SkipAfter || Managers.UI.TopPopup != this
            || Managers.UI.ClosedThisFrame || GameKeys.AnyDown == false)
            return;
        // 곡이 3초에 걸쳐 꺼지는 중이다(DeadAni). 끝까지 기다렸을 때처럼 꺼 둔다 — 반쯤 꺼지던 곡은 되살린 층이 같은 곡을
        // 틀려 할 때 "이미 도는 중" 으로 보여 새로 틀리지 않고, 그 뒤에 마저 꺼져 층이 무음이 된다.
        AudioSource bgm = Managers.Sound.GetAudioSource(Define.Sound.Bgm);
        if (bgm != null)
        {
            bgm.DOKill();
            bgm.Stop();
        }
        Respawn();
    }

    void Respawn()
    {
        if (_respawned)
            return;
        _respawned = true;
        Restart();
        ClosePopupUI();
    }

    /// <summary>
    /// 체크포인트에서 되살린다. 못 읽으면 타이틀로 간다 — 죽은 PlayerData(HP 0 이하)를 든 채
    /// 게임 화면을 다시 세우면 다음 전투의 첫 대에 또 죽는다. 타이틀은 켜질 때 Game.Init 으로
    /// 판을 다시 세운다(읽히면 그 체크포인트, 아니면 새 판).
    /// </summary>
    static void Restart()
    {
        GameEvents.RespawnPending = true;
        // 치명 횟수(AttackCount)는 LoadGame 이 체크포인트 값으로 되돌린다 — "이 층 다시 시작" 과 같다. 예전에는 여기서 0 으로 지워
        // 죽은 뒤에만 HUD 의 "치명까지 N타" 와 맵 위 예측이 체크포인트와 달랐다.
        if (Managers.Game.RestartFromCheckpoint())
            return;

        GameEvents.RespawnPending = false;
        Debug.LogError($"[GameOver] 체크포인트를 읽지 못해 타이틀로 간다: {Managers.Game.LastSaveError}");
        Managers.Scene.LoadScene(Define.Scene.TitleScene);
    }

    void OnDestroy()
    {
        // 연출 도중에 창이 사라졌다(Esc·창 정리). 이 창이 되살리기를 맡고 있어서, 그대로 두면
        // 죽은 채 입력 잠금(OnInputLock)이 걸린 채로 멈춘다. 씬이 내려가는 중이면 그쪽에 맡긴다.
        // 플레이를 끄는 중(매니저가 먼저 부서졌다)에도 끼어들지 않는다 — 되살리기가 @Managers 를 새로 만든다.
        if (_respawned || gameObject.scene.isLoaded == false || Managers.IsAlive == false)
            return;
        CoroutineManager.StartCoroutine(CoRestartIfStranded());
    }

    // 한 프레임 뒤에 본다. 같은 프레임에 다른 씬으로 가는 길이 열렸다면(타이틀로 가기 등)
    // 그때는 게임 화면이 이미 내려가 있어 여기서 끼어들지 않는다.
    static IEnumerator CoRestartIfStranded()
    {
        yield return null;
        if (Managers.Game.IsPlayerDead && Managers.Game.GameScene != null)
            Restart();
    }
}
