using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_EndingScene : UI_Scene
{
    enum Images
    {
        Fade,
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindImage(typeof(Images));

        StartCoroutine(StartDirecting());

        return true;
    }

    IEnumerator StartDirecting()
    {
        GetImage((int)Images.Fade).GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Screen.height);
        GetImage((int)Images.Fade).DOColor(new Color(0f, 0f, 0f, 0f), 2f);
        yield return new WaitForSeconds(2);

        // 대기 시간
        yield return new WaitForSeconds(8);

        GetImage((int)Images.Fade).GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Screen.height);
        GetImage((int)Images.Fade).DOColor(new Color(0f, 0f, 0f, 1f), 2f);
        yield return new WaitForSeconds(2);

        SceneManager.LoadScene("TitleScene");
    }

}
