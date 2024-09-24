using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CursorType
{
    Normal,
    Search,
    Grap,
    Click,
    Press,
}

public class CursorManager : MonoBehaviour
{
    public CursorType _cursor = CursorType.Normal;
    bool _init = false;

    int _frameCount = 0;
    float _frameRate = 0f;
    int _currentFrame = 0;
    float _frameTimer = 0f;

    Texture2D _normalCursor0 = null;
    Texture2D _normalCursor1 = null;
    Texture2D _normalCursor2 = null;
    Texture2D _normalCursor3 = null;
    Texture2D _normalCursor4 = null;
    Texture2D _normalCursor5 = null;
    Texture2D _handleCursor0 = null;
    Texture2D _handleCursor1 = null;
    Texture2D _handleCursor2 = null;
    Texture2D _handleCursor3 = null;
    Texture2D _handleCursor4 = null;
    Texture2D _handleCursor5 = null;
    Texture2D _searchCursor0 = null;
    Texture2D _searchCursor1 = null;
    Texture2D _searchCursor2 = null;
    Texture2D _searchCursor3 = null;
    Texture2D _searchCursor4 = null;
    Texture2D _searchCursor5 = null;

    public void Init()
    {
        _init = true;

        _normalCursor0 = Resources.Load<Texture2D>("Cursor/MouseCursor_Normal_0");
        _normalCursor1 = Resources.Load<Texture2D>("Cursor/MouseCursor_Normal_1");
        _normalCursor2 = Resources.Load<Texture2D>("Cursor/MouseCursor_Normal_2");
        _normalCursor3 = Resources.Load<Texture2D>("Cursor/MouseCursor_Normal_3");
        _normalCursor4 = Resources.Load<Texture2D>("Cursor/MouseCursor_Normal_4");
        _normalCursor5 = Resources.Load<Texture2D>("Cursor/MouseCursor_Normal_5");
        _handleCursor0 = Resources.Load<Texture2D>("Cursor/MouseCursor_Handle_0");
        _handleCursor1 = Resources.Load<Texture2D>("Cursor/MouseCursor_Handle_1");
        _handleCursor2 = Resources.Load<Texture2D>("Cursor/MouseCursor_Handle_2");
        _handleCursor3 = Resources.Load<Texture2D>("Cursor/MouseCursor_Handle_3");
        _handleCursor4 = Resources.Load<Texture2D>("Cursor/MouseCursor_Handle_4");
        _handleCursor5 = Resources.Load<Texture2D>("Cursor/MouseCursor_Handle_5");
        _searchCursor0 = Resources.Load<Texture2D>("Cursor/MouseCursor_MagnifierGlass_0");
        _searchCursor1 = Resources.Load<Texture2D>("Cursor/MouseCursor_MagnifierGlass_1");
        _searchCursor2 = Resources.Load<Texture2D>("Cursor/MouseCursor_MagnifierGlass_2");
        _searchCursor3 = Resources.Load<Texture2D>("Cursor/MouseCursor_MagnifierGlass_3");
        _searchCursor4 = Resources.Load<Texture2D>("Cursor/MouseCursor_MagnifierGlass_4");
        _searchCursor5 = Resources.Load<Texture2D>("Cursor/MouseCursor_MagnifierGlass_5");
    }

    void Update()
    {
        UpdateMousePosition();
        UpdateMouseCursor();
    }

    void UpdateMousePosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.nearClipPlane; // 카메라와의 거리 설정
        transform.position = Camera.main.ScreenToWorldPoint(mousePosition);
    }

    void UpdateMouseCursor()
    {
        if (!_init) return;

        _frameTimer += Time.deltaTime;
        Debug.Log(_frameTimer);

        switch (_cursor)
        {
            case CursorType.Normal:
                _frameTimer = 0;
                while (_cursor == CursorType.Normal)
                {
                    if (_frameTimer < 3500)
                        Cursor.SetCursor(_normalCursor0, new Vector2(0, 0), CursorMode.Auto);
                    else if (_frameTimer < 3550)
                        Cursor.SetCursor(_normalCursor1, new Vector2(0, 0), CursorMode.Auto);
                    else if (_frameTimer < 3600)
                        Cursor.SetCursor(_normalCursor2, new Vector2(0, 0), CursorMode.Auto);
                    else if (_frameTimer < 4300)
                        Cursor.SetCursor(_normalCursor3, new Vector2(0, 0), CursorMode.Auto);
                    else if (_frameTimer < 4350)
                        Cursor.SetCursor(_normalCursor4, new Vector2(0, 0), CursorMode.Auto);
                    else if (_frameTimer < 4400)
                        Cursor.SetCursor(_normalCursor5, new Vector2(0, 0), CursorMode.Auto);
                    else
                        _frameTimer = 0;
                }
                break;
            case CursorType.Search:
                _frameTimer = 0;

                break;
            case CursorType.Grap:
                _frameTimer = 0;

                break;
            case CursorType.Click:
                _frameTimer = 0;
                while (_frameTimer < 150)
                {
                    if (_frameTimer < 5)
                        Cursor.SetCursor(_normalCursor3, new Vector2(0, 0), CursorMode.Auto);
                    else if (_frameTimer < 105)
                        Cursor.SetCursor(_normalCursor4, new Vector2(0, 0), CursorMode.Auto);
                    else
                        Cursor.SetCursor(_normalCursor5, new Vector2(0, 0), CursorMode.Auto);
                }

                _cursor = CursorType.Normal;
                break;
            case CursorType.Press:
                Cursor.SetCursor(_normalCursor3, new Vector2(0, 0), CursorMode.Auto);
                break;
            default:
                break;
        }
    }
}
