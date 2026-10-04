using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_MagicalSwordCheckPopup : UI_Popup
{
    #region Enum
    enum Images
    {
        MagicalSwordCheckBox,
    }

    enum Texts
    {
        MagicalSwordCheckText,
    }

    enum Buttons
    {
        YesBtn,
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
        GetButton((int)Buttons.YesBtn).gameObject.BindEvent(YesClick, type: Define.UIEvent.Click);
        GetButton((int)Buttons.YesBtn).gameObject.BindEvent(YesPointerExit, type: Define.UIEvent.PointerExit);

        GetText((int)Texts.MagicalSwordCheckText).text = Managers.GetString(Managers.Data.ScriptDic[Define.SWOARD_ALERT].id);

        Managers.Game.OnConversation = true;
        _locked = true;

        YesPointerExit();

        return true;
    }

    // Esc 로는 닫히지 않는다. 계약(O)을 눌러야 연출이 이어진다.
    public override bool OnEscape() => true;

    // Enter·Space 도 계약(O)과 같다 — 마우스 없이(스팀 덱의 A) 넘어가야 한다. 맨 위일 때만, 그리고 이번 프레임에
    // 창이 닫히지 않았을 때만: 이 창은 마지막 대사를 넘긴 그 Enter 로 뜬다(PopupAction). 그 키가 계약까지 누르면 안 된다.
    void Update()
    {
        if (_locked == false || Managers.UI.TopPopup != this || Managers.UI.ClosedThisFrame)
            return;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            YesClick();
    }

    // 이 창이 건 대화 잠금. 어떤 길로 사라지든 풀고 간다.
    bool _locked;

    void OnDestroy()
    {
        if (_locked)
            Managers.Game.OnConversation = false;
    }

    #region Pointer Interaction
    void YesPointerEnter()
    {
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Choice_SFX");

        GetButton((int)Buttons.YesBtn).gameObject.GetComponent<Animator>().Play("OnlyYesMouseOver");
    }

    void YesClick()
    {
        Managers.Game.OnDirect = false;
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Ok_SFX");

        Managers.Game.OnConversation = false;
        _locked = false;
        Managers.Directing.Events.CoStartContractSword();
        ClosePopupUI();
    }

    void YesPointerExit()
    {
        GetButton((int)Buttons.YesBtn).gameObject.GetComponent<Animator>().Play("OnlyYesIdle");
    }
    #endregion
}
