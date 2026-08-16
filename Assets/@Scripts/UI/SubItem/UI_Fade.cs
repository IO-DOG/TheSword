using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Fade : UI_Base
{
    private float _offset = 300;

    private void Awake()
    {
        Managers.Game.OnFade = true;
    }

    private void OnDestroy()
    {
        // 트윈이 끝나기 전에 이 오브젝트가 사라지면 OnComplete 가 안 돈다.
        // 그러면 OnFade 가 켜진 채 남고, CoUsePortal 은 OnFade 면 그냥
        // 빠져나오기 때문에 계단이 영영 안 먹는다 (9층에서 그렇게 멈췄다).
        if (Managers.Game != null)
            Managers.Game.OnFade = false;
    }

    /// <summary>
    /// 페이드가 제 시간에 안 끝나면 풀어 준다.
    /// 트윈이 중간에 죽거나 대상이 사라지면 OnComplete 가 영영 안 오는데,
    /// 그 한 번이 게임 진행을 통째로 막는다.
    /// </summary>
    IEnumerator CoReleaseFade(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        if (Managers.Game.OnFade)
        {
            Debug.LogWarning($"[Fade] {seconds:0.0}초가 지나도 페이드가 안 끝나 강제로 해제한다");
            Managers.Game.OnFade = false;
            Managers.Resource.Destroy(gameObject);
        }
    }
    void Start()
    {
        gameObject.GetComponentInChildren<Image>().gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width + _offset, Screen.height);
    }

    public void PlayFade(Define.FadeEvent type, float duration, Define.Scene scene = Define.Scene.Unknown)
    {
        StartCoroutine(CoReleaseFade(duration + 3f));

        Sequence sequence = GetSequence(type, duration);
        if (sequence == null)
        {
            // 모르는 연출 종류. 여기서 그냥 두면 OnFade 가 켜진 채 남는다.
            Debug.LogWarning($"[Fade] 알 수 없는 연출({type}) — 페이드를 건너뛴다");
            Managers.Game.OnFade = false;
            Managers.Resource.Destroy(gameObject);
            return;
        }

        sequence.OnComplete(() =>
        {
            if (scene != Define.Scene.Unknown)
            {
                Managers.Scene.LoadScene(scene);
                //Managers.UI.ShowPopupUI<UI_StageNamePopup>();
                return;
            }
            Managers.Game.OnFade = false;
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
}
