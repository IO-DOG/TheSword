using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        GetImage((int)Images.BackgroundImage).gameObject.transform.DOMoveX(dist, 0.2f).SetLink(gameObject);   // 곧장 닫혀도 경고 없이
        //GetImage((int)Images.BackgroundImage).gameObject.transform.DOMoveX(1240, 0.2f);
        //GetImage((int)Images.BackgroundImage).gameObject.transform.DOScale(1, 0.2f);

        GetButton((int)Buttons.Korean).gameObject.BindEvent(OnClickKorean);
        GetButton((int)Buttons.English).gameObject.BindEvent(OnClickEnglish);
        GetButton((int)Buttons.Japan).gameObject.BindEvent(OnClickJapan);
        GetButton((int)Buttons.China).gameObject.BindEvent(OnClickChina);

        OnEnterExitImage();
        LabelFlags();

        Refresh();
        TurnOnCurScriptTypeImage();
        return true;
    }

    // 국기만 한 줄 떠 있고 판의 아래 3분의 2가 비어 있었다. 국기는 언어가 아니다(영어 = 성조기) — 그 언어로 쓴 이름을
    // 달고, 판을 국기 줄에 맞춰 줄인다. 이름은 번역하지 않는다: 어느 언어로 두어도 제 이름은 읽을 수 있다.
    static readonly string[] Names = { "한국어", "English", "日本語", "中文" };   // Buttons 순서

    void LabelFlags()
    {
        for (int i = 0; i < Names.Length; i++)
        {
            // 국기 단추(60x44, 배율 2)가 든 칸(90x66) 밑에 단다. 칸은 스크롤 뷰의 격자라 칸째로 따라간다.
            Transform slot = GetButton(i).transform.parent;
            TextMeshProUGUI label = CodeUI.NewText(slot, "Name", CodeUI.ProseFont, 28f, Color.white, TextAlignmentOptions.Top);
            CodeUI.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(120f, 34f));
            label.text = Names[i];
        }

        Image board = GetImage((int)Images.BackgroundImage);
        board.type = Image.Type.Sliced;
        board.rectTransform.sizeDelta = new Vector2(board.rectTransform.sizeDelta.x, 210f);
    }

    // Esc 는 한 칸 뒤로 — 메뉴로 돌아간다. 비켜 있던 메뉴 버튼은 메뉴가 제자리로 돌린다.
    public override bool OnEscape()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Back_SFX");
        ClosePopupUI();
        return true;
    }

    // 키보드·패드: A/D(←/→)로 고르고 Enter·Space 로 바꾼다. 고른 칸에는 마우스를 올린 것과 같은 테두리가 선다.
    // 처음 누른 방향키는 지금 언어(표시가 붙은 칸)에서 한 칸 간다. 방향키를 누르기 전의 Enter 는 아무것도 안 고른다 —
    // 이 창을 연 메뉴의 Enter 가 같은 프레임에 여기서 한 번 더 먹힐 수 있다.
    static readonly Define.ScriptType[] Languages = { Define.ScriptType.Kr, Define.ScriptType.En, Define.ScriptType.Jp, Define.ScriptType.Cn };
    static readonly Images[] Choices = { Images.KoreanChoice, Images.EnglishChoice, Images.JapanChoice, Images.ChinaChoice };
    int _cursor = -1;

    void Update()
    {
        if (Managers.UI.TopPopup != this || Managers.UI.ClosedThisFrame)
            return;
        int step = GameKeys.Nav(KeyCode.A) || GameKeys.Nav(KeyCode.LeftArrow) ? -1
                 : GameKeys.Nav(KeyCode.D) || GameKeys.Nav(KeyCode.RightArrow) ? 1 : 0;
        int from = _cursor >= 0 ? _cursor : Mathf.Max(0, System.Array.IndexOf(Languages, GameSettings.Language));
        if (step != 0)
            Point(Mathf.Clamp(from + step, 0, Languages.Length - 1));
        else if (_cursor >= 0 && (GameKeys.Down(KeyCode.Return) || GameKeys.Down(KeyCode.KeypadEnter) || GameKeys.Down(KeyCode.Space)))
        {
            Select(Languages[_cursor]);
            Point(_cursor);     // 바꾸면 표시를 다 걷는다(Refresh) — 고른 칸을 다시 세운다
        }
    }

    void Point(int index)
    {
        if (index != _cursor)
            Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_SFX");
        _cursor = index;
        for (int i = 0; i < Choices.Length; i++)
            GetImage((int)Choices[i]).gameObject.SetActive(i == index);
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
