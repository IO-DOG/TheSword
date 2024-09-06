using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Fade : UI_Base
{
    private float _offset = 300;
    void Start()
    {
        Managers.Game.OnFade = true;
        gameObject.GetComponentInChildren<Image>().gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width + _offset, Screen.height);
    }

    public void PlayFade(Define.FadeEvent type, float duration, Define.Scene scene = Define.Scene.Unknown)
    {
        GetSequence(type, duration).OnComplete(() =>
        {
            if (scene != Define.Scene.Unknown)
            {
                Managers.Scene.LoadScene(scene);
                //Managers.UI.ShowPopupUI<UI_StageNamePopup>();
                return;
            }

            Managers.Resource.Destroy(this.gameObject);
        });
    }

    Sequence GetSequence(Define.FadeEvent type, float duration)
    {
        switch ((int)type)
        {
            // left -> center
            case 0:
                { 
                    gameObject.GetComponentInChildren<Image>().transform.localScale = Vector3.one;
                    gameObject.GetComponentInChildren<Image>().gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3((-1) * Screen.width, 0);

                    Tween move = gameObject.GetComponentInChildren<Image>().transform.DOMoveX(Util.WorldToScreenCood(Vector3.zero).x + _offset, duration);

                    Sequence seq = DOTween.Sequence();
                    seq.Append(move);

                    return seq;
                }

            // center -> right
            case 1:
                {
                    gameObject.GetComponentInChildren<Image>().transform.localScale = new Vector3(-1, 1, 1);
                    gameObject.GetComponentInChildren<Image>().gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0);

                    Tween move = gameObject.GetComponentInChildren<Image>().transform.DOMoveX(Util.WorldToScreenCood(new Vector3(Screen.width + _offset, 0)).x, duration);

                    Sequence seq = DOTween.Sequence();
                    seq.Append(move);

                    return seq;
                }
            // fade in
            case 2:
                {
                    gameObject.GetComponentInChildren<Image>().material = null;
                    gameObject.GetComponentInChildren<Image>().gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0);

                    gameObject.GetComponentInChildren<Image>().color = new Color(0, 0, 0, 1);
                    Tween fade = gameObject.GetComponentInChildren<Image>().DOFade(0, duration);

                    Sequence seq = DOTween.Sequence();
                    seq.Append(fade);

                    return seq;
                }
            // fade out
            case 3:
                {
                    gameObject.GetComponentInChildren<Image>().material = null;
                    gameObject.GetComponentInChildren<Image>().gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0);

                    gameObject.GetComponentInChildren<Image>().color = new Color(0, 0, 0, 0);
                    Tween fade = gameObject.GetComponentInChildren<Image>().DOFade(1, duration);

                    Sequence seq = DOTween.Sequence();
                    seq.Append(fade);

                    return seq;
                }
        }

        return null;
    }

    private void OnDestroy()
    {
        Managers.Game.OnFade = false;
    }
}
