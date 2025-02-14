using Data;
using DG.Tweening;
using Febucci.UI;
using Febucci.UI.Examples;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UI_IntroScene : UI_Scene
{
    #region Enum
    enum Images
    {
        SceneImage,
        EscapeGauge,
        CurtainCall,
        //SceneFrameImage,
    }

    enum Texts
    {
        SceneText,
    }
    #endregion

    int idx = 0;
    int totalCount;
    bool isPressing = false;
    float requiredHoldTime = 3f;
    float holdTime = 0f;
    List<ScriptData> _scripts;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        Managers.Sound.Play(Define.Sound.Effect, "StartIntro_SFX");
        Managers.Sound.SetBGMVolume(PlayerPrefs.GetFloat("CURBGMSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1));

        StartCoroutine(CoCurtaintCall(2f));

        GetImage((int)Images.SceneImage).gameObject.SetActive(false);
        //GetImage((int)Images.SceneFrameImage).gameObject.SetActive(false);

        _scripts = Managers.Data.LoadScriptData(Define.INTRO_STORY);
        totalCount = 7;

        GetText((int)Texts.SceneText).text = Managers.GetString(_scripts[idx++].id);
        GetImage((int)Images.EscapeGauge).gameObject.SetActive(false);
        return true;
    }

    private void Awake()
    {
        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        GetText((int)Texts.SceneText).GetComponent<TAnimSoundWriter>().source = Managers.Sound.GetAudioSource(Define.Sound.Effect);
    }

    private void Update()
    {
        if (_lock)
            return;

        if (!GetText((int)Texts.SceneText).GetComponent<TextAnimator_TMP>().allLettersShown && (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
        {
            GetText((int)Texts.SceneText).GetComponent<TextAnimator_TMP>().SetVisibilityEntireText(true);
        }
        else if (GetText((int)Texts.SceneText).GetComponent<TextAnimator_TMP>().allLettersShown && (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
        {
            NextScene();
        }

        //test
        if (Input.GetKey(KeyCode.Escape))
        {
            isPressing = true;
        }

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            isPressing = false;
            GetImage((int)Images.EscapeGauge).fillAmount = 0;
            GetImage((int)Images.EscapeGauge).gameObject.SetActive(false);
            holdTime = 0f;
        }

        if (isPressing)
        {
            GetImage((int)Images.EscapeGauge).gameObject.SetActive(true);
            holdTime += Time.deltaTime;
            float progress = holdTime / requiredHoldTime;
            GetImage((int)Images.EscapeGauge).fillAmount = progress;
            if (holdTime >= requiredHoldTime)
            {
                Managers.Scene.LoadScene(Define.Scene.GameScene);
            }
        }
    }

    bool _lock = true;
    IEnumerator CoCurtaintCall(float time)
    {
        yield return new WaitForSeconds(time);

        Managers.Sound.Play(Define.Sound.Bgm, "StartIntro_BGM");

        StartCoroutine(Util.CoFade(GetImage((int)Images.CurtainCall), 0.3f, false));
        _lock = false;
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
            Managers.Scene.LoadScene(Define.Scene.GameScene);
            return;
        }

        GetImage((int)Images.SceneImage).sprite = ImageList[idx - 1];
        if (idx != totalCount - 1)
            GetText((int)Texts.SceneText).text = Managers.GetString(_scripts[idx].id);

        if (idx == 1) // ó�� Ŭ����
        {
            GetImage((int)Images.SceneImage).gameObject.SetActive(true);
            GetText((int)Texts.SceneText).gameObject.transform.position = Util.WorldToScreenCood(new Vector3(0, -Screen.height * 0.37f, 0));
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
            GetImage((int)Images.SceneImage).transform.position = new Vector3(Screen.width * 0.5f, Screen.height * 1.02f, 0);
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

        Managers.Scene.LoadScene(Define.Scene.GameScene);
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

        bool flag = false;
        while (GetImage((int)Images.SceneImage).transform.position.y > 0)
        {
            if (!flag && GetImage((int)Images.SceneImage).transform.position.y < Screen.height * 0.463f)
            {
                flag = true;
                StartCoroutine(CoFadeOutImage());
            }

            GetImage((int)Images.SceneImage).transform.position -= new Vector3(0, 1.5f, 0);
            yield return tick;
        }
        //yield return null;

        //StartCoroutine(CoFadeOutImage(GetImage((int)Images.SceneFrameImage)));


        GetText((int)Texts.SceneText).gameObject.transform.position = Util.WorldToScreenCood(new Vector3(0, 0, 0));

        //GetText((int)Texts.SceneText).text = Managers.GetString(_scripts[totalCount - 1].id);
    }
}
