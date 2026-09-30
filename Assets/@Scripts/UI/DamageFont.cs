using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageFont : UI_Base
{
    TextMeshProUGUI _damageText;

    // 치명타 숫자는 1.6배. 색만 달라서는 치명이 났는지 모르고 지나갔다 — 전투에서 세는 자원이 치명타뿐인데.
    const float CriticalSize = 1.6f;
    float _size = 1f;

    public void SetInfo(Vector2 pos, float damage = 0, float healAmount = 0, Transform parent = null, bool isCritical = false, bool isDefence = false)
    {
        _damageText = GetComponent<TextMeshProUGUI>();
        _size = isCritical && healAmount <= 0 ? CriticalSize : 1f;
        // 검은 테두리. 빨간 숫자가 주황 폭발 위에서 사라졌다. 재질은 글꼴마다 하나를 나눠 쓴다.
        _damageText.fontSharedMaterial = CodeUI.Outlined(_damageText.font, 0.4f);
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

        // 각 단계에서 이동할 거리. 캔버스 단위라(부모가 전투창 캔버스다) 화면 크기를 따라 늘고 준다 — 예전에는 화면
        // 픽셀이라 어느 해상도에서나 200px 를 날아, 960x540 에서는 화면 높이의 37% 를 올라갔다. 1080p 에서는 그대로다.
        float firstMoveDistance = 100.5f;   // 첫 번째 애니메이션에서 이동할 거리
        float secondMoveDistance = 100.5f;  // 두 번째 애니메이션에서 추가로 이동할 거리

        // 트윈 수명을 이 오브젝트에 묶는다.
        // 전투가 끝나면 전투창과 함께 데미지 숫자도 사라지는데, 시퀀스는 계속 돌다가
        // 완료 콜백에서 이미 파괴된 gameObject 를 만진다. DOTween 안전모드가 삼켜서
        // 게임이 죽지는 않지만, 한 판에 널참조 경고가 수천 건씩 쌓였다.
        Sequence seq = DOTween.Sequence().SetLink(gameObject);

        // 시작 전에 스케일을 0으로 초기화합니다.
        transform.localScale = Vector3.zero;

        // 첫 번째 단계: 스케일을 0에서 1.3으로 키우면서 동시에 랜덤한 방향으로 이동 (치명타는 _size 만큼 더 크게)
        seq.Append(transform.DOScale(1.3f * _size, 0.3f).SetEase(Ease.InOutBounce))
           .Join(transform.DOLocalMove(randomDir * firstMoveDistance, 0.3f)
                .SetRelative(true)
                .SetEase(Ease.Linear));

        // 두 번째 단계: 스케일을 1.3에서 1.0으로 줄이고, 텍스트를 페이드아웃하며, 추가로 이동
        seq.Append(transform.DOScale(1.0f * _size, 0.3f).SetEase(Ease.InOutBounce))
           .Join(transform.GetComponent<TMP_Text>().DOFade(0, 0.3f).SetEase(Ease.InQuint))
           .Join(transform.DOLocalMove(randomDir * secondMoveDistance, 0.3f)
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

        Sequence seq = DOTween.Sequence().SetLink(gameObject);

        // 위로 올라가면서 투명해짐
        seq.Append(GetComponent<RectTransform>().DOLocalMoveY(25f, 1f));
        seq.Join(_damageText.DOColor(transparentColot, 1f));
        seq.OnComplete(() =>
        {
            Managers.Resource.Destroy(gameObject);
        });
    }

}
