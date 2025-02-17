using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageFont : UI_Base
{
    TextMeshProUGUI _damageText;

    public void SetInfo(Vector2 pos, float damage = 0, float healAmount = 0, Transform parent = null, bool isCritical = false, bool isDefence = false)
    {
        _damageText = GetComponent<TextMeshProUGUI>();
        transform.position = pos;

        if (healAmount > 0)
        {
            _damageText.text = $"{Mathf.RoundToInt(healAmount)}";
            _damageText.color = Util.HexToColor("4EEE6F");
        }
        else if (isCritical)
        {
            _damageText.text = $"{Mathf.RoundToInt(damage)}";
            _damageText.color = Util.HexToColor("F23EFF");
        }
        else if (isDefence)
        {
            _damageText.text = $"{Mathf.RoundToInt(damage)}";
            _damageText.color = Util.HexToColor("01C7ED");
        }
        else
        {
            _damageText.text = $"{Mathf.RoundToInt(damage)}";
            _damageText.color = Util.HexToColor("FF3E3E");
        }
        _damageText.alpha = 1;
        DoAnimation();
    }

    public void SetPotionHealingInfo(float healAmount, Transform parentUI)
    {
        _damageText = GetComponent<TextMeshProUGUI>();

        _damageText.text = $"{Mathf.RoundToInt(healAmount)}";
        _damageText.color = Util.HexToColor("4EEE6F");
        _damageText.alpha = 1;
        DoPotionHealingAnimation();
    }

    //private void DoAnimation()
    //{
    //    Sequence seq = DOTween.Sequence();

    //    transform.localScale = new Vector3(0, 0, 0);

    //    seq.Append(transform.DOScale(1.3f, 0.3f).SetEase(Ease.InOutBounce))
    //        .Join(transform.DOMove(transform.position + Vector3.up, 0.3f).SetEase(Ease.Linear))
    //        .Append(transform.DOScale(1.0f, 0.3f).SetEase(Ease.InOutBounce))
    //        .Join(transform.GetComponent<TMP_Text>().DOFade(0, 0.3f).SetEase(Ease.InQuint))
    //        //.Append(GetComponent<TextMeshPro>().DOFade(0, 1f).SetEase(Ease.InBounce))
    //        .OnComplete(() =>
    //        {
    //            Managers.Resource.Destroy(gameObject);
    //        });

    //}

    private void DoAnimation()
    {
        // -30도 ~ +30도 사이의 랜덤 각도를 구합니다.
        float randomAngle = Random.Range(-30f, 30f);
        // Vector3.up 방향을 randomAngle만큼 회전시켜 랜덤한 사선 방향을 구합니다.
        Vector3 randomDir = Quaternion.Euler(0, 0, randomAngle) * Vector3.up;

        // 각 단계에서 이동할 거리를 설정합니다.
        float firstMoveDistance = 100.5f;   // 첫 번째 애니메이션에서 이동할 거리
        float secondMoveDistance = 100.5f;  // 두 번째 애니메이션에서 추가로 이동할 거리

        Sequence seq = DOTween.Sequence();

        // 시작 전에 스케일을 0으로 초기화합니다.
        transform.localScale = Vector3.zero;

        // 첫 번째 단계: 스케일을 0에서 1.3으로 키우면서 동시에 랜덤한 방향으로 이동
        seq.Append(transform.DOScale(1.3f, 0.3f).SetEase(Ease.InOutBounce))
           .Join(transform.DOMove(randomDir * firstMoveDistance, 0.3f)
                .SetRelative(true)
                .SetEase(Ease.Linear));

        // 두 번째 단계: 스케일을 1.3에서 1.0으로 줄이고, 텍스트를 페이드아웃하며, 추가로 이동
        seq.Append(transform.DOScale(1.0f, 0.3f).SetEase(Ease.InOutBounce))
           .Join(transform.GetComponent<TMP_Text>().DOFade(0, 0.3f).SetEase(Ease.InQuint))
           .Join(transform.DOMove(randomDir * secondMoveDistance, 0.3f)
                .SetRelative(true)
                .SetEase(Ease.Linear));

        seq.OnComplete(() =>
        {
            Managers.Resource.Destroy(gameObject);
        });
    }

    private void DoPotionHealingAnimation()
    {
        Color transparentColot = new Color(_damageText.color.r, _damageText.color.g, _damageText.color.b, 0);

        Sequence seq = DOTween.Sequence();

        // 위로 올라가면서 투명해짐
        seq.Append(GetComponent<RectTransform>().DOLocalMoveY(25f, 1f));
        seq.Join(_damageText.DOColor(transparentColot, 1f));
        seq.OnComplete(() =>
        {
            Managers.Resource.Destroy(gameObject);
        });
    }

}
