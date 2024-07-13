using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Fade : UI_Base
{
    public float _fadeTime = Define.FADE_DURATION;
    private float _offset = 300;
    void Start()
    {
        Managers.Game.OnFade = true;
        gameObject.GetComponentInChildren<Image>().gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width + _offset, Screen.height);
    }

    public Sequence GetSequence(Define.FadeEvent type)
    {
        switch ((int)type)
        {
            // left -> center
            case 0:
                { 
                    gameObject.GetComponentInChildren<Image>().transform.localScale = Vector3.one;
                    gameObject.GetComponentInChildren<Image>().gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3((-1) * Screen.width, 0);

                    Tween move = gameObject.GetComponentInChildren<Image>().transform.DOMoveX(Util.WorldToScreenCood(Vector3.zero).x + _offset, _fadeTime);

                    Sequence seq = DOTween.Sequence();
                    seq.Append(move);

                    return seq;
                }

            // center -> right
            case 1:
                {
                    gameObject.GetComponentInChildren<Image>().transform.localScale = new Vector3(-1, 1, 1);
                    gameObject.GetComponentInChildren<Image>().gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0);

                    Tween move = gameObject.GetComponentInChildren<Image>().transform.DOMoveX(Util.WorldToScreenCood(new Vector3(Screen.width + _offset, 0)).x, _fadeTime);

                    Sequence seq = DOTween.Sequence();
                    seq.Append(move);

                    return seq;
                }
            // fade in
            case 2:
                {
                    gameObject.GetComponentInChildren<Image>().material = null;
                    gameObject.GetComponentInChildren<Image>().gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0);

                    gameObject.GetComponentInChildren<Image>().DOFade(1, 0);
                    Tween fade = gameObject.GetComponentInChildren<Image>().DOFade(0, _fadeTime);

                    Sequence seq = DOTween.Sequence();
                    seq.Append(fade);

                    return seq;
                }
            // fade out
            case 3:
                {
                    gameObject.GetComponentInChildren<Image>().material = null;
                    gameObject.GetComponentInChildren<Image>().gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0);

                    gameObject.GetComponentInChildren<Image>().DOFade(0, 0);
                    Tween fade = gameObject.GetComponentInChildren<Image>().DOFade(1, _fadeTime);

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
