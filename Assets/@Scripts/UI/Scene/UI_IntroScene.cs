using Data;
using DG.Tweening;
using Febucci.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_IntroScene : UI_Scene
{
    #region Enum
    enum Images
    {
        SceneImage,
        //SceneFrameImage,
    }

    enum Texts
    {
        SceneText,
    }
    #endregion

    int idx = 0;
    int totalCount;
    List<ScriptData> _scripts;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        GetImage((int)Images.SceneImage).gameObject.SetActive(false);
        //GetImage((int)Images.SceneFrameImage).gameObject.SetActive(false);

        _scripts = Managers.Data.LoadScriptData(Define.INTRO_STORY);
        totalCount = 7;

        GetText((int)Texts.SceneText).text = Managers.GetString(_scripts[idx++].id);
        return true;
    }

    private void Update()
    {
        if (!GetText((int)Texts.SceneText).GetComponent<TextAnimator_TMP>().allLettersShown && (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
        {
            GetText((int)Texts.SceneText).GetComponent<TextAnimator_TMP>().SetVisibilityEntireText(true);
        }
        else if (GetText((int)Texts.SceneText).GetComponent<TextAnimator_TMP>().allLettersShown && (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
        {
            NextScene();
        }

        //test
        if (Input.GetKeyDown(KeyCode.Escape))
            Managers.Scene.LoadScene(Define.Scene.TutorialScene);
    }

    void NextScene()
    {
        List<Sprite> ImageList = new List<Sprite>()
        {
            Managers.Resource.Load<Sprite>("Intro01"),
            Managers.Resource.Load<Sprite>("Intro02"),
            Managers.Resource.Load<Sprite>("Intro03"),
            Managers.Resource.Load<Sprite>("Intro04"),
            Managers.Resource.Load<Sprite>("Intro01"),
            Managers.Resource.Load<Sprite>("Intro05"),
        };

        if (idx == totalCount)
        {
            Managers.Scene.LoadScene(Define.Scene.TutorialScene);
            return;
        }

        GetImage((int)Images.SceneImage).sprite = ImageList[idx - 1];
        if (idx != totalCount - 1)
        GetText((int)Texts.SceneText).text = Managers.GetString(_scripts[idx].id);

        if (idx == 1) // ó�� Ŭ����
        {
            GetImage((int)Images.SceneImage).gameObject.SetActive(true);
            GetText((int)Texts.SceneText).gameObject.transform.position = Util.WorldToScreenCood(new Vector3(0, -400, 0));
        }
        if (idx == totalCount - 3)
        {
            StartCoroutine(CoInvertedImage());
        }
        if (idx == totalCount - 1)
        {
            StopAllCoroutines();
            GetText((int)Texts.SceneText).text = "";
            GetImage((int)Images.SceneImage).sprite = ImageList[idx - 1];
            GetImage((int)Images.SceneImage).SetNativeSize();
            GetImage((int)Images.SceneImage).transform.position = new Vector3(960, 1100, 0);
            GetImage((int)Images.SceneImage).transform.localScale = new Vector3(0.7f, 0.7f, 0);
            StartCoroutine(CoLastIntroImage());
        }

        idx++;
        idx = Mathf.Min(idx, totalCount);
    }

    IEnumerator CoInvertedImage()
    {
        WaitForSeconds delay = new WaitForSeconds(1f);
        GetImage((int)Images.SceneImage).sprite = Managers.Resource.Load<Sprite>("Intro03");
        StartCoroutine(CoFadeOutImage(GetImage((int)Images.SceneImage)));
        yield return delay;
        GetImage((int)Images.SceneImage).sprite = Managers.Resource.Load<Sprite>("Intro04");
        float tick = 0;
        while (tick < 1)
        {
            GetImage((int)Images.SceneImage).color += new Color(0, 0, 0, 0.1f);
            tick += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator CoFadeOutImage()
    {
        GetImage((int)Images.SceneImage).color = Color.white;
        float tick = 0;
        while (tick < 1)
        {
            GetImage((int)Images.SceneImage).color += new Color(0, 0, 0, -0.1f);
            tick += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        yield return null;

        Managers.Scene.LoadScene(Define.Scene.TutorialScene);
    }

    IEnumerator CoFadeOutImage(Image image)
    {
        float tick = 0;
        while (tick < 1)
        {
            image.color += new Color(0, 0, 0, -0.1f);
            tick += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        yield return null;
    }

    IEnumerator CoLastIntroImage()
    {
        //GetImage((int)Images.SceneFrameImage).gameObject.SetActive(true);
        GetImage((int)Images.SceneImage).sprite = Managers.Resource.Load<Sprite>("Intro05");

        WaitForSeconds tick = new WaitForSeconds(0.01f);

        //GetImage((int)Images.SceneFrameImage).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.SceneImage).color = new Color(1, 1, 1, 0);
        while (true)
        {
            if (GetImage((int)Images.SceneImage).color.a > 1)
                break;
            //GetImage((int)Images.SceneFrameImage).color += new Color(0, 0, 0, 0.1f);
            GetImage((int)Images.SceneImage).color += new Color(0, 0, 0, 0.1f);
            yield return new WaitForSeconds(0.1f);
        }

        while (GetImage((int)Images.SceneImage).transform.position.y > 0)
        {
            GetImage((int)Images.SceneImage).transform.position -= new Vector3(0, 1.5f, 0);
            yield return tick;
        }
        yield return null;

        StartCoroutine(CoFadeOutImage());
        //StartCoroutine(CoFadeOutImage(GetImage((int)Images.SceneFrameImage)));


        GetText((int)Texts.SceneText).gameObject.transform.position = Util.WorldToScreenCood(new Vector3(0, 0, 0));

        //GetText((int)Texts.SceneText).text = Managers.GetString(_scripts[totalCount - 1].id);
    }
}
