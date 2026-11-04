using Data;
using DG.Tweening;
using Febucci.UI;
using Febucci.UI.Examples;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
    Coroutine _inverted, _invertedFade;     // 4번 그림의 뒤집기(CoInvertedImage)와 그 안의 흐려지기 — 넘기면 멈춘다
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

        // 건너뛰기 게이지에 박힌 "ESC" — 패드면 B 다(GameKeys 가 B·Menu 도 Esc 로 받는다).
        TMPro.TMP_Text escLabel = GetImage((int)Images.EscapeGauge).GetComponentInChildren<TMPro.TMP_Text>(true);
        if (escLabel != null)
            PromptText.Set(escLabel, () => GameKeys.UsingPad ? "B" : "ESC");

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

        // 글자 속도(설정)와 타자 소리. 인트로 문구(900000~900006)는 story_gen 이 줄 수를 그대로 두고 고쳐 쓴다.
        StoryUI.ApplyTextSpeed(GetText((int)Texts.SceneText));
    }

    private void Update()
    {
        if (_lock)
            return;

        if (!GetText((int)Texts.SceneText).GetComponent<TextAnimator_TMP>().allLettersShown && (GameKeys.Down(KeyCode.Return) || Input.GetMouseButtonDown(0)))
        {
            GetText((int)Texts.SceneText).GetComponent<TextAnimator_TMP>().SetVisibilityEntireText(true);
        }
        else if (GetText((int)Texts.SceneText).GetComponent<TextAnimator_TMP>().allLettersShown && (GameKeys.Down(KeyCode.Return) || Input.GetMouseButtonDown(0)))
        {
            NextScene();
        }

        //test
        if (GameKeys.Held(KeyCode.Escape))
        {
            isPressing = true;
        }

        if (GameKeys.Up(KeyCode.Escape))
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
                LoadGameSceneOnce();
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

    /// <summary>
    /// 게임 씬 로드는 한 번만. 마지막 장에서 NextScene 이 다시 불리면
    /// idx 가 그대로라 또 로드하는데, 초기화 도중 씬이 다시 깔리면서
    /// 플레이어 배치가 통째로 날아간다 (엔터 연타로도 재현된다).
    /// </summary>
    bool _loadingGameScene;

    void LoadGameSceneOnce()
    {
        if (_loadingGameScene)
            return;
        _loadingGameScene = true;
        Managers.Scene.LoadScene(Define.Scene.GameScene);
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
            LoadGameSceneOnce();
            return;
        }

        // 4번 그림이 뒤집히는 중(2초)에 넘기면 그 코루틴이 다음 그림을 4번으로 덮고 투명도를 마저 바꿨다 — 멈추고 되돌린다.
        if (_inverted != null)
        {
            StopCoroutine(_inverted);
            if (_invertedFade != null)
                StopCoroutine(_invertedFade);
            _inverted = _invertedFade = null;
            GetImage((int)Images.SceneImage).color = Color.white;
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
            _inverted = StartCoroutine(CoInvertedImage());
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
        _invertedFade = StartCoroutine(CoFadeOutImage(GetImage((int)Images.SceneImage)));
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

        LoadGameSceneOnce();
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

        // 초당 화면 높이의 1/12 (1080p 60프레임에서 예전 걸음과 같다). 예전에는 걸음마다 1.5px 이라 해상도·주사율을
        // 따라 훑는 시간이 4초에서 11초까지 달랐다.
        float speed = Screen.height / 12f;

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

            GetImage((int)Images.SceneImage).transform.position -= new Vector3(0, speed * Time.deltaTime, 0);
            yield return null;
        }
        //yield return null;

        //StartCoroutine(CoFadeOutImage(GetImage((int)Images.SceneFrameImage)));


        GetText((int)Texts.SceneText).gameObject.transform.position = Util.WorldToScreenCood(new Vector3(0, 0, 0));

        //GetText((int)Texts.SceneText).text = Managers.GetString(_scripts[totalCount - 1].id);
    }
}
