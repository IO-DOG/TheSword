using Data;
using Febucci.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_IntroScene : UI_Scene
{
    #region Enum
    enum Images
    {
        SceneImage,
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

        _scripts = Managers.Data.LoadScriptData(Define.INTRO_STORY);
        totalCount = _scripts.Count;

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
            Managers.Resource.Load<Sprite>("Intro05"),
        };

        if (idx == totalCount)
        {
            Managers.Scene.LoadScene(Define.Scene.TutorialScene);
            return;
        }

        GetImage((int)Images.SceneImage).sprite = ImageList[idx - 1];
        GetText((int)Texts.SceneText).text = Managers.GetString(_scripts[idx].id);

        if (idx == 1) // 처음 클릭시
        {
            GetImage((int)Images.SceneImage).gameObject.SetActive(true);
            GetText((int)Texts.SceneText).gameObject.transform.position = Util.WorldToScreenCood(new Vector3(0, -400, 0));
        }
        if (idx == totalCount - 2)
        {
            StartCoroutine(CoInvertedImage());
        }
        if (idx == totalCount - 1)
        {
            GetImage((int)Images.SceneImage).sprite = ImageList[idx - 1];
            GetImage((int)Images.SceneImage).SetNativeSize();
            GetImage((int)Images.SceneImage).transform.position = new Vector3(960, 540, 0);
            GetImage((int)Images.SceneImage).transform.localScale = new Vector3(0.34f, 0.34f, 0);
        }
        
        idx++;
        idx = Mathf.Min(idx, totalCount);
    }

    IEnumerator CoInvertedImage()
    {
        WaitForSeconds delay = new WaitForSeconds(1f);
        GetImage((int)Images.SceneImage).sprite = Managers.Resource.Load<Sprite>("Intro03");
        yield return delay;
        GetImage((int)Images.SceneImage).sprite = Managers.Resource.Load<Sprite>("Intro04");
    }
}
