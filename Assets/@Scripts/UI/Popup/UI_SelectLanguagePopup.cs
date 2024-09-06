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
        GetImage((int)Images.BackgroundImage).gameObject.transform.DOMoveX(1240, 0.2f);
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

    void TurnOnCurScriptTypeImage()
    {
        Define.ScriptType curScriptType = Managers.Game.ScriptType;
        switch (curScriptType)
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
            default:
                GetImage((int)Images.KoreanPick).gameObject.SetActive(true);
                break;
        }
    }

    void OnClickKorean()
    {
        Managers.Game.ScriptType = Define.ScriptType.Kr;
        Refresh();
        TurnOnCurScriptTypeImage();

        //ClosePopupUI();
    }

    void OnClickEnglish()
    {
        Managers.Game.ScriptType = Define.ScriptType.En;
        Refresh();
        TurnOnCurScriptTypeImage();

        //ClosePopupUI();
    }

    void OnClickJapan()
    {
        Managers.Game.ScriptType = Define.ScriptType.Jp;
        Refresh();
        TurnOnCurScriptTypeImage();

        //ClosePopupUI();
    }

    void OnClickChina()
    {
        Managers.Game.ScriptType = Define.ScriptType.Cn;
        Refresh();
        TurnOnCurScriptTypeImage();

        //ClosePopupUI();
    }
}
