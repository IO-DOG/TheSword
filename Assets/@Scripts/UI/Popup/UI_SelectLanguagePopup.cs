using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_SelectLanguagePopup : UI_Popup
{

    #region Enum
    enum Buttons
    {
        Korean,
        English,
        Japan,
        China,
    }

    enum Images
    {
        BackgroundImage,
        KoreanChoice,
        KoreanPick,
        EnglishChoice,
        EnglishPick,
        JapanChoice,
        JapanPick,
        ChinaChoice,
        ChinaPick,
    }
    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButton(typeof(Buttons));
        BindImage(typeof(Images));

        //GetImage((int)Images.BackgroundImage).gameObject.transform.localScale = new Vector3(0, 0, 0);
        float dist = Screen.width * 0.645f;

        GetImage((int)Images.BackgroundImage).gameObject.transform.DOMoveX(dist, 0.2f);
        //GetImage((int)Images.BackgroundImage).gameObject.transform.DOMoveX(1240, 0.2f);
        //GetImage((int)Images.BackgroundImage).gameObject.transform.DOScale(1, 0.2f);

        GetButton((int)Buttons.Korean).gameObject.BindEvent(OnClickKorean);
        GetButton((int)Buttons.English).gameObject.BindEvent(OnClickEnglish);
        GetButton((int)Buttons.Japan).gameObject.BindEvent(OnClickJapan);
        GetButton((int)Buttons.China).gameObject.BindEvent(OnClickChina);

        OnEnterExitImage();

        Refresh();
        TurnOnCurScriptTypeImage();
        return true;
    }

    // Esc 는 한 칸 뒤로 — 메뉴로 돌아간다. 비켜 있던 메뉴 버튼은 메뉴가 제자리로 돌린다.
    public override bool OnEscape()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Back_SFX");
        ClosePopupUI();
        return true;
    }

    void Refresh()
    {
        GetImage((int)Images.KoreanChoice).gameObject.SetActive(false);
        GetImage((int)Images.KoreanPick).gameObject.SetActive(false);
        GetImage((int)Images.EnglishChoice).gameObject.SetActive(false);
        GetImage((int)Images.EnglishPick).gameObject.SetActive(false);
        GetImage((int)Images.JapanChoice).gameObject.SetActive(false);
        GetImage((int)Images.JapanPick).gameObject.SetActive(false);
        GetImage((int)Images.ChinaChoice).gameObject.SetActive(false);
        GetImage((int)Images.ChinaPick).gameObject.SetActive(false);
    }

    void OnEnterExitImage()
    {
        GetButton((int)Buttons.Korean).gameObject.BindEvent(() => {
            GetImage((int)Images.KoreanChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetButton((int)Buttons.English).gameObject.BindEvent(() => {
            GetImage((int)Images.EnglishChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetButton((int)Buttons.Japan).gameObject.BindEvent(() => {
            GetImage((int)Images.JapanChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetButton((int)Buttons.China).gameObject.BindEvent(() => {
            GetImage((int)Images.ChinaChoice).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);

        GetButton((int)Buttons.Korean).gameObject.BindEvent(() => {
            GetImage((int)Images.KoreanChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetButton((int)Buttons.English).gameObject.BindEvent(() => {
            GetImage((int)Images.EnglishChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetButton((int)Buttons.Japan).gameObject.BindEvent(() => {
            GetImage((int)Images.JapanChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetButton((int)Buttons.China).gameObject.BindEvent(() => {
            GetImage((int)Images.ChinaChoice).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
    }

    // 저장된 언어를 그대로 보여 준다. 예전에는 고른 적이 없으면(None) 한국어에 표시가 붙었지만
    // 글자는 영어로 나왔다.
    void TurnOnCurScriptTypeImage()
    {
        switch (GameSettings.Language)
        {
            case Define.ScriptType.Kr:
                GetImage((int)Images.KoreanPick).gameObject.SetActive(true);
                break;
            case Define.ScriptType.En:
                GetImage((int)Images.EnglishPick).gameObject.SetActive(true);
                break;
            case Define.ScriptType.Jp:
                GetImage((int)Images.JapanPick).gameObject.SetActive(true);
                break;
            case Define.ScriptType.Cn:
                GetImage((int)Images.ChinaPick).gameObject.SetActive(true);
                break;
        }
    }

    void OnClickKorean() => Select(Define.ScriptType.Kr);
    void OnClickEnglish() => Select(Define.ScriptType.En);
    void OnClickJapan() => Select(Define.ScriptType.Jp);
    void OnClickChina() => Select(Define.ScriptType.Cn);

    // 저장하고 GameSettings.Changed 를 울린다 — 떠 있는 글자는 그걸 듣고 다시 칠한다.
    void Select(Define.ScriptType language)
    {
        GameSettings.Language = language;
        Refresh();
        TurnOnCurScriptTypeImage();
    }
}
