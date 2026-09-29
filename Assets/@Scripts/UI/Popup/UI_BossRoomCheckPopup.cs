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

    // Esc 로는 닫히지 않는다. 예/아니오 중 하나를 골라야 한다.
    public override bool OnEscape() => true;

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
