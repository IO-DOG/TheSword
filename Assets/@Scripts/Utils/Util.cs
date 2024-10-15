using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;
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
}
