using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_GuidePopup : UI_Popup
{
    #region Enum
    enum Images
    {
        GuideImage,
        OnlyYestButton,
    }

    enum Texts
    {
        GuideText
    }
    #endregion

    public Animator guideAnim = null;
    public Animator btnAnim = null;
    public TMP_Text guideText = null;

    // 뜬 직후의 Enter·Space·Esc 는 이 창을 띄운 쪽(대사 넘기기·전투 끝·열쇠 줍기)의 것이다 — 읽기도 전에 닫히지 않게.
    const float KeyGuard = 0.4f;
    float _openedAt;

    private void Awake()
    {
        Init();
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        _openedAt = Time.unscaledTime;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        Managers.Game.OnInputLock = true;
        _locked = true;

        //guideAnim = GetImage((int)Images.GuideImage).gameObject.GetComponent<Animator>();
        //btnAnim = GetImage((int)Images.OnlyYestButton).gameObject.GetComponent<Animator>();
        //guideText = GetText((int)Texts.GuideText);

        GetImage((int)Images.OnlyYestButton).gameObject.GetComponent<Animator>().Play("OnlyYesIdle");
        GetImage((int)Images.OnlyYestButton).gameObject.BindEvent(YesPointerEnter, type: Define.UIEvent.PointerEnter);
        GetImage((int)Images.OnlyYestButton).gameObject.BindEvent(YesClick, type: Define.UIEvent.Click);
        GetImage((int)Images.OnlyYestButton).gameObject.BindEvent(YesPointerExit, type: Define.UIEvent.PointerExit);

        Managers.Sound.Play(Define.Sound.Effect, "PopUpUI_TutorialGuide");

        return true;
    }

    public void SetInfo(int index)
    {
        GetText((int)Texts.GuideText).text = Managers.GetString(index);

        if (index == Define.GUIDE_BATTLE)
            GetImage((int)Images.GuideImage).gameObject.GetComponent<Animator>().Play("UI_GuideBattle");
        else if (index == Define.GUIDE_RECOVERY)
            GetImage((int)Images.GuideImage).gameObject.GetComponent<Animator>().Play("UI_GuideRecovery");
        else if (index == Define.GUIDE_LEVER)
            GetImage((int)Images.GuideImage).gameObject.GetComponent<Animator>().Play("UI_GuideLever");
        else if (index == Define.GUIDE_KEY)
            GetImage((int)Images.GuideImage).gameObject.GetComponent<Animator>().Play("UI_GuideKey");
    }

    #region Pointer Interaction
    void YesPointerEnter()
    {
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Choice_SFX");

        GetImage((int)Images.OnlyYestButton).gameObject.GetComponent<Animator>().Play("OnlyYesMouseOver");
    }

    void YesClick()
    {
        Managers.Game.OnInputLock = false;
        _locked = false;
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Ok_SFX");

        ClosePopupUI();
    }

    void YesPointerExit()
    {
        GetImage((int)Images.OnlyYestButton).gameObject.GetComponent<Animator>().Play("OnlyYesIdle");
    }
    #endregion

    // Enter·Space 도 확인(O)과 같다. 맨 위일 때만 — 위에 뜬 창을 누른 키다.
    void Update()
    {
        if (Managers.UI.TopPopup != this || Time.unscaledTime - _openedAt < KeyGuard)
            return;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            YesClick();
    }

    // Esc 도 확인과 같다 — 확인 길(YesClick)로 닫아야 입력 잠금이 풀린다. 뜬 직후의 Esc 는 삼키기만 한다.
    public override bool OnEscape()
    {
        if (Time.unscaledTime - _openedAt >= KeyGuard)
            YesClick();
        return true;
    }

    // 이 창이 건 입력 잠금. 어떤 길로 사라지든(연출이 창을 모두 걷는 등) 풀고 간다 —
    // 예전에는 Esc 로 걷히면 잠금이 켜진 채 남아 플레이어가 영영 못 움직였다.
    bool _locked;

    void OnDestroy()
    {
        if (_locked)
            Managers.Game.OnInputLock = false;
    }
}
