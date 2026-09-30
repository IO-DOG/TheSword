using System.Collections;
using System.Collections.Generic;
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

    bool _respawned;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindImage(typeof(Images));

        GetImage((int)Images.BG).gameObject.SetActive(false);
        GetImage((int)Images.GameOverIllust).gameObject.SetActive(false);

        DeadAni();

        return true;
    }

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
        if (Managers.Game.RestartFromCheckpoint())
        {
            // 죽을때 타격횟수 초기화
            Managers.Game.AttackCount = 0;
            return;
        }

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
