using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WhiteFlashEffect : MonoBehaviour
{
    // 플래시를 담당할 Image 컴포넌트
    private Image flashImage;

    // 플래시 지속 시간
    public float flashDuration = 0.5f;

    // 플래시의 최대 알파값 (보통 1)
    public float maxAlpha = 1f;

    void Awake()
    {
        flashImage = GetComponent<Image>();
        if (flashImage == null)
        {
            Debug.LogError("WhiteFlashEffect: not find image");
        }

        // 초기 상태는 투명하게 설정
        SetAlpha(0f);
    }

    /// <summary>
    /// 화이트 플래시를 실행합니다.
    /// </summary>
    public void TriggerFlash()
    {
        StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        // 처음에 흰색을 최대 알파로 설정
        yield return StartCoroutine(FadeTo(maxAlpha, flashDuration / 2));

        // 다시 투명하게 페이드 아웃
        yield return StartCoroutine(FadeTo(0f, flashDuration / 2));
    }

    /// <summary>
    /// 알파값을 부드럽게 변경하는 코루틴
    /// </summary>
    /// <param name="targetAlpha">목표 알파값</param>
    /// <param name="duration">지속 시간</param>
    /// <returns></returns>
    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = flashImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            SetAlpha(newAlpha);
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    /// <summary>
    /// Image의 알파값을 설정합니다.
    /// </summary>
    /// <param name="alpha">설정할 알파값</param>
    private void SetAlpha(float alpha)
    {
        Color color = flashImage.color;
        color.a = alpha;
        flashImage.color = color;
    }
}
