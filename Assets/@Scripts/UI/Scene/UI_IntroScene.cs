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
    int totalCount = 5;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        GetImage((int)Images.SceneImage).gameObject.SetActive(false);
        GetText((int)Texts.SceneText).text = Managers.GetString(6);
        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            NextScene();
        }
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
        };
        List<string> TextList = new List<string>()
        {
            Managers.GetString(7),
            Managers.GetString(8),
            Managers.GetString(9),
            Managers.GetString(10),
            Managers.GetString(11),
        };

        if (idx == 0) // 처음 클릭시
        {
            GetImage((int)Images.SceneImage).gameObject.SetActive(true);
            GetText((int)Texts.SceneText).gameObject.transform.position = Util.WorldToScreenCood(new Vector3(0, -400, 0));
        }
        if (idx == totalCount - 1)
        {
            Debug.Log("인트로 끝");
        }
        GetImage((int)Images.SceneImage).sprite = ImageList[idx];
        GetText((int)Texts.SceneText).text = TextList[idx];

        idx++;
        idx = Mathf.Min(idx, totalCount - 1);
    }
}
