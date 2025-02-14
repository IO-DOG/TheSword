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

    void OnClickReGameButton()
    {
        // 마검 습득 후
        if (PlayerPrefs.GetInt("ISMEETSWORD") == 1)
        {
            Managers.Game.PlayerData.Clear();
            Managers.Game.Player.gameObject.SetActive(true);
            Managers.Game.Player.SetPlayerPosition(Managers.Game.SpawnPoints[2].position);
            Managers.Game.LoadGame();
        }
        else
        {
            Managers.Game.Player.gameObject.SetActive(true);
            Managers.Game.PlayerData.Ability = (int)Define.Trait.None;
            Debug.Log("Cllck OnClickNewGameButton");
            Managers.Game.DeleteGameData();
            Managers.Data.Init();
            Managers.Scene.LoadScene(Define.Scene.GameScene);
        }
    }

    void DeadAni()
    {
        // 장비 없애기
        Managers.Game.Player._isEquiptShield = false;
        Managers.Game.Player._isEquiptWeapon = false;
        GameObject.Find("UI_PlayerHPBar")?.SetActive(false);

        // player 죽는 파티클 생성
        Vector3 particlePos = Managers.Game.Player.gameObject.transform.position;
        GameObject deathSoulPurple = Managers.Resource.Instantiate("FX_UserDeath");
        deathSoulPurple.transform.position = particlePos;
        //deathSoulPurple.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Destroy(deathSoulPurple, 3);

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

        // todo
        // 죽음 인겜 연출 재생 (1.5초)

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

        Managers.Game.Player.gameObject.SetActive(true);
        GameObject.Find("UI_PlayerHPBar")?.SetActive(true);

        Managers.Game.LoadGame();
        //Managers.Game.PlayerData.Ability = (int)Define.Trait.None;
        //Debug.Log("Cllck OnClickNewGameButton");
        //Managers.Game.DeleteGameData();
        //Managers.Data.Init();
        Managers.Game.OnInputLock = false;
        Managers.Game.IsPlayerDead = false;

        // 죽을때 타격횟수 초기화
        Managers.Game.AttackCount = 0;

        GameObject.Find("Maps")?.SetActive(false);
        Managers.Game.GameScene.gameObject?.SetActive(false);
        //Managers.Scene.LoadScene(Define.Scene.TitleScene);
        Managers.Scene.LoadScene(Define.Scene.GameScene);
        //Managers.Game.LoadGame();

        //Managers.Game.Player.gameObject.SetActive(true);

        ClosePopupUI();
    }

}
