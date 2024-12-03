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
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        #endregion

        GetImage((int)Images.LetterBoxTop).GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Define.LETTER_BOX_HEIGHT);
        GetImage((int)Images.LetterBoxBottom).GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Define.LETTER_BOX_HEIGHT);
        GetImage((int)Images.LetterBoxTop).GetComponent<RectTransform>().position = Util.WorldToScreenCood(new Vector3(0f, Screen.height / 2 + Define.LETTER_BOX_HEIGHT, 0f));
        GetImage((int)Images.LetterBoxBottom).GetComponent<RectTransform>().position = Util.WorldToScreenCood(new Vector3(0f, -Screen.height / 2 - Define.LETTER_BOX_HEIGHT, 0f));
        GetImage((int)Images.LetterBoxTop).gameObject.SetActive(false);
        GetImage((int)Images.LetterBoxBottom).gameObject.SetActive(false);


        return true;
    }
    public void StartLetterBox()
    {
        GetImage((int)Images.LetterBoxTop).gameObject.SetActive(true);
        GetImage((int)Images.LetterBoxBottom).gameObject.SetActive(true);
        GetImage((int)Images.LetterBoxTop).GetComponent<RectTransform>().DOMove(Util.WorldToScreenCood(new Vector3(0f, Screen.height / 2, 0f)), 1f);
        GetImage((int)Images.LetterBoxBottom).GetComponent<RectTransform>().DOMove(Util.WorldToScreenCood(new Vector3(0f, -Screen.height / 2, 0f)), 1f);
    }

    public void StopLetterBox()
    {
        Tween twe1 = GetImage((int)Images.LetterBoxTop).GetComponent<RectTransform>().DOMove(Util.WorldToScreenCood(new Vector3(0f, Screen.height / 2 + Define.LETTER_BOX_HEIGHT, 0f)), 1f);
        Tween twe2 = GetImage((int)Images.LetterBoxBottom).GetComponent<RectTransform>().DOMove(Util.WorldToScreenCood(new Vector3(0f, -Screen.height / 2 - Define.LETTER_BOX_HEIGHT, 0f)), 1f);

        Sequence seq = DOTween.Sequence();
        seq.Append(twe1);
        seq.Join(twe2).OnComplete(() =>
        {
            GetImage((int)Images.LetterBoxTop).gameObject.SetActive(false);
            GetImage((int)Images.LetterBoxBottom).gameObject.SetActive(false);
            Managers.UI.ClosePopupUI();
        });
    }
}
