using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_LetterBox : UI_Popup
{
    enum Images
    {
        LetterBoxTop,
        LetterBoxBottom,
    }

    // 막대는 캔버스 위·아래 끝에 가로로 늘여 건다. 예전에는 화면 픽셀로 폭(Screen.width)과 자리를 줬는데 캔버스는 가로
    // 1080 에 맞춰 늘고 줄어서, 1080 보다 좁은 창에서는 막대가 화면 양끝에 모자랐다(960x540 에서 폭의 89%).
    // 막대 가운데가 화면 끝에 서면 절반이 보인다 — 예전 모습 그대로다. 숨은 자리는 막대 높이만큼 바깥.
    RectTransform Top => GetImage((int)Images.LetterBoxTop).rectTransform;
    RectTransform Bottom => GetImage((int)Images.LetterBoxBottom).rectTransform;
    static float Height => Define.LETTER_BOX_HEIGHT;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        #endregion

        Hang(Top, 1f);
        Hang(Bottom, 0f);
        Top.gameObject.SetActive(false);
        Bottom.gameObject.SetActive(false);


        return true;
    }

    static void Hang(RectTransform bar, float edge)
    {
        bar.anchorMin = new Vector2(0f, edge);
        bar.anchorMax = new Vector2(1f, edge);
        bar.pivot = new Vector2(0.5f, 0.5f);
        bar.sizeDelta = new Vector2(0f, Height);
        bar.anchoredPosition = new Vector2(0f, edge > 0f ? Height : -Height);
    }

    // 트윈은 이 창에 묶는다(SetLink). 다 움직이기 전에 창이 걷히면 부서진 막대를 계속 만지며 경고를 찍었다.
    public void StartLetterBox()
    {
        Top.gameObject.SetActive(true);
        Bottom.gameObject.SetActive(true);
        Top.DOAnchorPosY(0f, 1f).SetLink(gameObject);
        Bottom.DOAnchorPosY(0f, 1f).SetLink(gameObject);
    }

    public void StopLetterBox()
    {
        // 내려오던 중에 걷으면 두 트윈이 한 막대를 두고 싸운다.
        Top.DOKill();
        Bottom.DOKill();

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(Top.DOAnchorPosY(Height, 1f));
        seq.Join(Bottom.DOAnchorPosY(-Height, 1f)).OnComplete(() =>
        {
            Top.gameObject.SetActive(false);
            Bottom.gameObject.SetActive(false);
            // 위에 대화창이 막 올라왔을 수 있다 — 맨 위가 아니라 자기 자신을 닫는다.
            Managers.UI.ClosePopupUI(this);
        });
    }
}
