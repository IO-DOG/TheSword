using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum CursorType
{
    None,
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

    float _frameTimer = 0f;
    int _mask = (1 << (int)Define.Layer.Monster) | (1 << (int)Define.Layer.EItem) | (1 << (int)Define.Layer.CItem) |
        (1 << (int)Define.Layer.BossDoor) | (1 << (int)Define.Layer.Door) | (1 << (int)Define.Layer.Portal) | 
        (1 << (int)Define.Layer.Lever) | (1 << (int)Define.Layer.InteractObjects) | (1 << (int)Define.Layer.Default) |
        (1 << (int)Define.Layer.Wall) | (1 << (int)Define.Layer.Player);

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
        if (Input.GetMouseButton(0) && _cursor == CursorType.Normal)
        {
            _cursor = CursorType.Press;
        }
        else if (Input.GetMouseButtonUp(0) && _cursor == CursorType.Press)
        {
            _frameTimer = 0;
            _cursor = CursorType.Normal;
        }
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

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000.0f, _mask))
        {
            // 몬스터, 장비, 물약
            if (hit.collider.gameObject.layer == (int)Define.Layer.Monster || hit.collider.gameObject.layer == (int)Define.Layer.EItem || hit.collider.gameObject.layer == (int)Define.Layer.CItem)
            {
                if (_cursor != CursorType.Search)
                {
                    Debug.Log("Search");
                    _cursor = CursorType.Search;
                }
            }
            // 보스문, 문, 포탈, 레버, 상호작용 물체
            else if (hit.collider.gameObject.layer == (int)Define.Layer.BossDoor || hit.collider.gameObject.layer == (int)Define.Layer.Door || hit.collider.gameObject.layer == (int)Define.Layer.Portal || hit.collider.gameObject.layer == (int)Define.Layer.Lever || hit.collider.gameObject.layer == (int)Define.Layer.InteractObjects)
            {
                if (_cursor != CursorType.Grap)
                {
                    Debug.Log("Grap");
                    _cursor = CursorType.Grap;
                }
            }
            else
            {
                if (_cursor != CursorType.Normal && _cursor != CursorType.Press)
                    _cursor = CursorType.Normal;
            }
        }
        //Debug.Log(_frameTimer);

        switch (_cursor)
        {
            case CursorType.Normal:
                //_frameTimer = 0;
                if (_frameTimer < 3.500f)
                    Cursor.SetCursor(_normalCursor0, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 3.550f)
                    Cursor.SetCursor(_normalCursor1, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 3.600f)
                    Cursor.SetCursor(_normalCursor2, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 4.300f)
                    Cursor.SetCursor(_normalCursor3, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 4.350f)
                    Cursor.SetCursor(_normalCursor4, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 4.400f)
                    Cursor.SetCursor(_normalCursor5, new Vector2(0, 0), CursorMode.Auto);
                else
                    _frameTimer = 0;
                break;
            case CursorType.Search:
                if (_frameTimer < 3.500f)
                    Cursor.SetCursor(_searchCursor0, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 3.550f)
                    Cursor.SetCursor(_searchCursor1, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 3.600f)
                    Cursor.SetCursor(_searchCursor2, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 4.300f)
                    Cursor.SetCursor(_searchCursor3, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 4.350f)
                    Cursor.SetCursor(_searchCursor4, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 4.400f)
                    Cursor.SetCursor(_searchCursor5, new Vector2(0, 0), CursorMode.Auto);
                else
                    _frameTimer = 0;
                break;
            case CursorType.Grap:
                if (_frameTimer < 3.500f)
                    Cursor.SetCursor(_handleCursor0, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 3.550f)
                    Cursor.SetCursor(_handleCursor1, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 3.600f)
                    Cursor.SetCursor(_handleCursor2, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 4.300f)
                    Cursor.SetCursor(_handleCursor3, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 4.350f)
                    Cursor.SetCursor(_handleCursor4, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 4.400f)
                    Cursor.SetCursor(_handleCursor5, new Vector2(0, 0), CursorMode.Auto);
                else
                    _frameTimer = 0;
                break;
            case CursorType.Click:
                if (_frameTimer < 0.005f)
                    Cursor.SetCursor(_normalCursor3, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 0.105f)
                    Cursor.SetCursor(_normalCursor4, new Vector2(0, 0), CursorMode.Auto);
                else if (_frameTimer < 0.150f)
                    Cursor.SetCursor(_normalCursor5, new Vector2(0, 0), CursorMode.Auto);
                else
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
