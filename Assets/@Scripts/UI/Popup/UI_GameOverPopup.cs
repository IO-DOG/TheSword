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
        StartCoroutine(CoDeadAni());
    }

    IEnumerator CoDeadAni()
    {
        GetImage((int)Images.GameOverIllust).color = new Color(1, 1, 1, 0);

        // todo
        // 죽음 인겜 연출 재생 (1.5초)

        StartCoroutine(Util.CoFade(GetImage((int)Images.BG), 0.2f));
        yield return new WaitForSeconds(0.2f);

        // 게임 오버 일러 서서히 등장
        StartCoroutine(Util.CoFade(GetImage((int)Images.GameOverIllust), 2f));
        // 등장 1.5초뒤
        yield return new WaitForSeconds(3.5f);

        // 게임 오버 일러스트 페이드 아웃
        StartCoroutine(Util.CoFade(GetImage((int)Images.GameOverIllust), 1f, false));


        Managers.Game.LoadGame();

        Managers.Game.OnInputLock = false;

        Managers.Game.Player.gameObject.SetActive(true);

        ClosePopupUI();
    }

}
