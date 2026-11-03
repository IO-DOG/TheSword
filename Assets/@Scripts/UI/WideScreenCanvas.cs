using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 16:9 보다 넓은 화면(21:9·32:9)에서는 캔버스를 높이 1080 에 맞춘다. 이 프로젝트의 캔버스는 폭과 높이의 중간(맞춤 0.5)으로
/// 커지는데, 넓은 화면에서는 그 배율이 높이 비율보다 커져 세로가 1080 보다 짧아졌다 — 2560x1080 에서 935 라 왼쪽 아래 열쇠 칸이
/// "LEVEL" 을 덮었다. 16:9 이하(16:10 덱·4:3)는 그대로 둔다 — 덱 글자 9px 측정이 그 배율이다. 맞춤 0.5 인 캔버스만 만진다
/// (도감·모드 창은 이미 높이에 맞추고, 고정 픽셀 캔버스는 배율이 없다). 해상도를 바꾸면 따라간다.
/// </summary>
[DefaultExecutionOrder(-1000)]      // CanvasScaler.Update 보다 먼저 — 같은 프레임에 배율이 선다
public class WideScreenCanvas : MonoBehaviour
{
    CanvasScaler _scaler;
    Vector2 _reference;
    float _match = -1f;
    int _width, _height;

    public static void Attach(GameObject go)
    {
        if (go.GetComponent<WideScreenCanvas>() == null)
            go.AddComponent<WideScreenCanvas>();
    }

    void Update()
    {
        if (_match < 0f)
        {
            // 첫 Update 에 원래 값을 잰다 — 붙인 뒤에 기준·맞춤을 고쳐 쓰는 창(StoryUI·도감)이 있어서다.
            _scaler = GetComponent<CanvasScaler>();
            if (_scaler == null || _scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                || Mathf.Abs(_scaler.matchWidthOrHeight - 0.5f) > 0.01f)
            {
                enabled = false;
                return;
            }
            _reference = _scaler.referenceResolution;
            _match = _scaler.matchWidthOrHeight;
        }
        if (_width == Screen.width && _height == Screen.height)
            return;
        _width = Screen.width;
        _height = Screen.height;
        bool wide = _width * 9 > _height * 16;
        _scaler.referenceResolution = wide ? new Vector2(1920f, 1080f) : _reference;
        _scaler.matchWidthOrHeight = wide ? 1f : _match;
    }
}
