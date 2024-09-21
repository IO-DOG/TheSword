using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScene : BaseScene
{
    GameObject ui_titleScene;
    GameObject MainTitle_BG;
    GameObject MainTitle_Layer0;
    GameObject MainTitle_Layer1;
    GameObject MainTitle_Layer2;
    GameObject MainTitle_BGAnim;

    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.TitleScene;

        ui_titleScene = GameObject.Find("UI_TitleScene");
        MainTitle_BG = GameObject.Find("MainTitle_BG");
        MainTitle_Layer0 = GameObject.Find("MainTitle_Layer0");
        MainTitle_Layer1 = GameObject.Find("MainTitle_Layer1");
        MainTitle_Layer2 = GameObject.Find("MainTitle_Layer2");
        MainTitle_BGAnim = GameObject.Find("MainTitle_BGAnim");

        //ui_titleScene.SetActive(false);
        StartCoroutine(CoCheckTitleOpenAnim(MainTitle_BGAnim));

        //TitleUI
    }

    IEnumerator CoCheckTitleOpenAnim(GameObject MainTitle_BGAnim)
    {
        float delay = MainTitle_BGAnim.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(delay);

        //MainTitle_BG.SetActive(false);
        //MainTitle_Layer0.SetActive(false);
        //MainTitle_Layer1.SetActive(false);
        //MainTitle_Layer2.SetActive(false);
        ui_titleScene.SetActive(true);
    }

    public override void Clear()
    {

    }
}
