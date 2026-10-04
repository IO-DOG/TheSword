using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class UI_BossRoomCheckPopup : UI_Popup
{
    #region Enum
    enum Images
    {
        BossRoomCheckBox,
    }

    enum Texts
    {
        BossRoomCheckText,
    }

    enum Buttons
    {
        YesBtn,
        NoBtn,
    }

    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindButton(typeof(Buttons));
        #endregion


        GetButton((int)Buttons.YesBtn).gameObject.BindEvent(YesPointerEnter, type: Define.UIEvent.PointerEnter);
        GetButton((int)Buttons.NoBtn).gameObject.BindEvent(NoPointerEnter, type: Define.UIEvent.PointerEnter);

        GetButton((int)Buttons.YesBtn).gameObject.BindEvent(YesClick, type: Define.UIEvent.Click);
        GetButton((int)Buttons.NoBtn).gameObject.BindEvent(NoClick, type: Define.UIEvent.Click);

        GetButton((int)Buttons.YesBtn).gameObject.BindEvent(YesPointerExit, type: Define.UIEvent.PointerExit);
        GetButton((int)Buttons.NoBtn).gameObject.BindEvent(NoPointerExit, type: Define.UIEvent.PointerExit);

        #region Popup init
        GetImage((int)Images.BossRoomCheckBox).color = new Color(1f, 1f, 1f, 0f);
        GetText((int)Texts.BossRoomCheckText).gameObject.SetActive(false);
        GetButton((int)Buttons.YesBtn).gameObject.SetActive(false);
        GetButton((int)Buttons.NoBtn).gameObject.SetActive(false);
        GetText((int)Texts.BossRoomCheckText).text = Managers.GetString(Managers.Data.ScriptDic[Define.BOSS_ALERT].id);
        #endregion

        StartCoroutine(PopupAnimation());

        Managers.Game.OnConversation = true;
        _locked = true;

        return true;
    }

    // Esc 는 아니오(X) — 확인 창(UI_ConfirmPopup)과 같다. 닫는 길(NoClick)로 닫아야 대화 잠금이 풀린다.
    // 버튼이 뜨기 전에는 마우스로도 못 고르니 삼키기만 한다.
    public override bool OnEscape()
    {
        if (_locked && GetButton((int)Buttons.NoBtn).gameObject.activeSelf)
            NoClick();
        return true;
    }

    // Enter·Space 는 예(O) — 마우스 없이(스팀 덱의 A) 고를 수 있어야 한다. 버튼이 떠야 받는다(마우스와 같다).
    // 맨 위일 때만, 이번 프레임에 창이 닫히지 않았을 때만 — 그 키는 닫힌 창의 것이다.
    void Update()
    {
        if (_locked == false || Managers.UI.TopPopup != this || Managers.UI.ClosedThisFrame
            || GetButton((int)Buttons.YesBtn).gameObject.activeSelf == false)
            return;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            YesClick();
    }

    // 이 창이 건 대화 잠금. 어떤 길로 사라지든 풀고 간다. (확인 창이 이 프리팹을 빌려 쓸 때는
    // 이 스크립트를 Init 전에 떼므로 잠금을 건 적이 없다 — 그때는 건드리지 않는다)
    bool _locked;

    void OnDestroy()
    {
        if (_locked)
            Managers.Game.OnConversation = false;
    }

    IEnumerator PopupAnimation()
    {
        GetImage((int)Images.BossRoomCheckBox).DOFade(1f, 0.5f).SetLink(gameObject);
        yield return new WaitForSeconds(0.5f);

        GetText((int)Texts.BossRoomCheckText).gameObject.SetActive(true);
        yield return new WaitForSeconds(2.5f);

        GetButton((int)Buttons.YesBtn).gameObject.SetActive(true);
        GetButton((int)Buttons.NoBtn).gameObject.SetActive(true);

        YesPointerExit();
        NoPointerExit();
    }

    #region Pointer interaction
    void YesPointerEnter()
    {
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Choice_SFX");

        GetButton((int)Buttons.YesBtn).gameObject.GetComponent<Animator>().Play("YesMouseOver");
    }

    void NoPointerEnter()
    {
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Choice_SFX");

        GetButton((int)Buttons.NoBtn).gameObject.GetComponent<Animator>().Play("NoMouseOver");
    }

    void YesClick()
    {
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Ok_SFX");

        Managers.Game.ParentMap.GetComponentInChildren<BossDoor>().CoStartPlayEffect();
        Managers.Game.OnConversation = false;
        _locked = false;
        ClosePopupUI();
    }

    void NoClick()
    {
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_No_SFX");

        Managers.Game.OnConversation = false;
        _locked = false;
        ClosePopupUI();
    }

    void YesPointerExit()
    {
        GetButton((int)Buttons.YesBtn).gameObject.GetComponent<Animator>().Play("YesIdle");
    }

    void NoPointerExit()
    {
        GetButton((int)Buttons.NoBtn).gameObject.GetComponent<Animator>().Play("NoIdle");
    }
    #endregion
}
