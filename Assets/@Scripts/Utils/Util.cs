using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Define;
using static UnityEngine.ParticleSystem;
using static UnityEngine.RuleTile.TilingRuleOutput;
using Random = UnityEngine.Random;
using Transform = UnityEngine.Transform;

public static class Util
{
    public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
            component = go.AddComponent<T>();
        return component;
    }

    public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
    {
        Transform transform = FindChild<Transform>(go, name, recursive);
        if (transform == null)
            return null;

        return transform.gameObject;
    }

    public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Object
    {
        if (go == null)
            return null;

        if (recursive == false)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform transform = go.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || transform.name == name)
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }
        }
        else
        {
            foreach (T component in go.GetComponentsInChildren<T>())
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                    return component;
            }
        }

        return null;
    }

    public static Transform FindChildByName(Transform transform, string childName)
    {
        foreach (Transform child in transform)
        {
            if (child.name == childName)
            {
                return child;
            }
        }
        return null;
    }

    public static Vector2 RandomPointInAnnulus(Vector2 origin, float minRadius = 6, float maxRadius = 12)
    {
        float randomDist = UnityEngine.Random.Range(minRadius, maxRadius);

        Vector2 randomDir = new Vector2(UnityEngine.Random.Range(-100, 100), UnityEngine.Random.Range(-100, 100)).normalized;
        //Debug.Log(randomDir);
        var point = origin + randomDir * randomDist;
        return point;
    }

    public static Color HexToColor(string color)
    {
        Color parsedColor;
        ColorUtility.TryParseHtmlString("#" + color, out parsedColor);

        return parsedColor;
    }

    //string값 으로 Enum값 찾기
    public static T ParseEnum<T>(string value)
    {
        return (T)Enum.Parse(typeof(T), value, true);
    }

    public static Vector3 ScreenToWorldCood(Vector3 input)
    {
        int width = Screen.width;
        int height = Screen.height;

        return new Vector3(input.x - width / 2, input.y - height / 2, input.z);
    }

    public static Vector3 WorldToScreenCood(Vector3 input)
    {
        int width = Screen.width;
        int height = Screen.height;

        return new Vector3(input.x + width / 2, input.y + height / 2, input.z);
    }

    public static Color DamagedColor()
    {
        return new Color((float)190 / 255, (float)38 / 255, (float)51 / 255);
    }

    public static Color DefenceColor()
    {
        return new Color((float)0, (float)140 / 255, (float)255 / 255);
    }

    /// <summary>
    /// 스크린샷 저장
    /// </summary>
    /// <param name="onFinished">텍스쳐 생성 콜백</param>
    /// <returns></returns>
    public static IEnumerator Screenshot(Action<Texture2D> onFinished)
    {
        yield return new WaitForEndOfFrame();
        // 텍스쳐 생성
        Texture2D screenTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        // 스크린샷 영역 설정
        Rect area = new Rect(0f, 0f, Screen.width, Screen.height);

        // 현재 화면의 픽셀을 읽어온다.
        screenTex.ReadPixels(area, 0, 0);

        // byte[]로 변환 뒤, 이미지를 읽어온다.
        screenTex.LoadImage(screenTex.EncodeToPNG());

        onFinished?.Invoke(screenTex);
    }

    public static IEnumerator Screenshot2(Action<Sprite> onFinished)
    {
        yield return new WaitForEndOfFrame();
        Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        onFinished?.Invoke(sprite);
    }

    public static IEnumerator CoMoveObjectForTime(Transform transform, Vector3 original, Vector3 target, float time)
    {
        float totalTime = 0f;

        while (totalTime <= time)
        {
            float delta = totalTime / time;
            float x = original.x + (target.x - original.x) * delta;
            float y = original.y + (target.y - original.y) * delta;
            float z = original.z + (target.z - original.z) * delta;
            transform.position = new Vector3(x, y, z);
            totalTime += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// 화이트 플래시를 실행합니다.
    /// </summary>
    public static void TriggerFlash(Image image, float maxAlpha, float duration)
    {
        Debug.Log("start flash bang");

        CoroutineManager.StartCoroutine(FlashCoroutine(image, maxAlpha, duration));
    }

    public static IEnumerator FlashCoroutine(Image image, float maxAlpha, float duration)
    {
        // 처음에 흰색을 최대 알파로 설정
        CoroutineManager.StartCoroutine(FadeTo(image, maxAlpha, duration / 2));
        yield return new WaitForSeconds(duration / 2);

        // 다시 투명하게 페이드 아웃
        CoroutineManager.StartCoroutine(FadeTo(image, 0f, duration / 2));
        yield return new WaitForSeconds(duration / 2);
    }

    /// <summary>
    /// 알파값을 부드럽게 변경하는 코루틴
    /// </summary>
    /// <param name="targetAlpha">목표 알파값</param>
    /// <param name="duration">지속 시간</param>
    /// <returns></returns>
    public static IEnumerator FadeTo(Image image, float targetAlpha, float duration)
    {
        float startAlpha = image.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            SetAlpha(image, newAlpha);
            yield return null;
        }

        SetAlpha(image, targetAlpha);
    }

    /// <summary>
    /// Image의 알파값을 설정합니다.
    /// </summary>
    /// <param name="alpha">설정할 알파값</param>
    public static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }


    /// <summary>
    /// flag는 true가 기본 상태
    /// true면 0에서 1로 alpha가 변하고 flase면 1에서 0으로 변한다.
    /// </summary>
    /// <param name="image">alpha값이 변할 이미지</param>
    /// <param name="time">연출 총 시간</param>
    /// <param name="flag"></param>
    /// <returns></returns>
    public static IEnumerator CoFade(Image image, float time, bool flag = true)
    {
        Debug.Log("Start CoFade");
        image.color = new Color(1, 1, 1, 0);

        float total = 0f;

        while (total <= time)
        {
            float delta = total / time;
            if (flag)
                image.color = new Color(1, 1, 1, delta);
            else
                image.color = new Color(1, 1, 1, 1 - delta);
            total += Time.deltaTime;
            yield return null;
        }

        if (flag == true)
            image.color = new Color(1, 1, 1, 1);
        else
            image.color = new Color(1, 1, 1, 0);
    }

    /// <summary>
    /// 코루틴 딜레이만 하는 역할
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static IEnumerator CoDelay(float time)
    {
        yield return new WaitForSeconds(time);
    }
}
