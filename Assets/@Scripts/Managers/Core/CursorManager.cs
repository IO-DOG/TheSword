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
    Directing,
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

    Texture2D _directingCursor = null;

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

    Texture2D _normalCursorSmall0 = null;
    Texture2D _normalCursorSmall1 = null;
    Texture2D _normalCursorSmall2 = null;
    Texture2D _normalCursorSmall3 = null;
    Texture2D _normalCursorSmall4 = null;
    Texture2D _normalCursorSmall5 = null;
    Texture2D _handleCursorSmall0 = null;
    Texture2D _handleCursorSmall1 = null;
    Texture2D _handleCursorSmall2 = null;
    Texture2D _handleCursorSmall3 = null;
    Texture2D _handleCursorSmall4 = null;
    Texture2D _handleCursorSmall5 = null;
    Texture2D _searchCursorSmall0 = null;
    Texture2D _searchCursorSmall1 = null;
    Texture2D _searchCursorSmall2 = null;
    Texture2D _searchCursorSmall3 = null;
    Texture2D _searchCursorSmall4 = null;
    Texture2D _searchCursorSmall5 = null;

    public void Init()
    {
        _init = true;

        _directingCursor = Resources.Load<Texture2D>("Cursor/MouseCursor_Directing");

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

        _normalCursorSmall0 = Resources.Load<Texture2D>("Cursor/MouseCursor_Normal_Small_0");
        _normalCursorSmall1 = Resources.Load<Texture2D>("Cursor/MouseCursor_Normal_Small_1");
        _normalCursorSmall2 = Resources.Load<Texture2D>("Cursor/MouseCursor_Normal_Small_2");
        _normalCursorSmall3 = Resources.Load<Texture2D>("Cursor/MouseCursor_Normal_Small_3");
        _normalCursorSmall4 = Resources.Load<Texture2D>("Cursor/MouseCursor_Normal_Small_4");
        _normalCursorSmall5 = Resources.Load<Texture2D>("Cursor/MouseCursor_Normal_Small_5");
        _handleCursorSmall0 = Resources.Load<Texture2D>("Cursor/MouseCursor_Handle_Small_0");
        _handleCursorSmall1 = Resources.Load<Texture2D>("Cursor/MouseCursor_Handle_Small_1");
        _handleCursorSmall2 = Resources.Load<Texture2D>("Cursor/MouseCursor_Handle_Small_2");
        _handleCursorSmall3 = Resources.Load<Texture2D>("Cursor/MouseCursor_Handle_Small_3");
        _handleCursorSmall4 = Resources.Load<Texture2D>("Cursor/MouseCursor_Handle_Small_4");
        _handleCursorSmall5 = Resources.Load<Texture2D>("Cursor/MouseCursor_Handle_Small_5");
        _searchCursorSmall0 = Resources.Load<Texture2D>("Cursor/MouseCursor_MagnifierGlass_Small_0");
        _searchCursorSmall1 = Resources.Load<Texture2D>("Cursor/MouseCursor_MagnifierGlass_Small_1");
        _searchCursorSmall2 = Resources.Load<Texture2D>("Cursor/MouseCursor_MagnifierGlass_Small_2");
        _searchCursorSmall3 = Resources.Load<Texture2D>("Cursor/MouseCursor_MagnifierGlass_Small_3");
        _searchCursorSmall4 = Resources.Load<Texture2D>("Cursor/MouseCursor_MagnifierGlass_Small_4");
        _searchCursorSmall5 = Resources.Load<Texture2D>("Cursor/MouseCursor_MagnifierGlass_Small_5");
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && _cursor == CursorType.Normal)
        {
            _cursor = CursorType.Press;
        }
        else if (Input.GetMouseButtonUp(0) && _cursor == CursorType.Press)
        {
            _frameTimer = 0;
            _cursor = CursorType.Normal;
        }

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
        if (Managers.Game.GameScene != null && Managers.Game.GameScene.isOpenMenuPopup) return;

        _frameTimer += Time.deltaTime;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(Camera.main.transform.position, ray.direction * 100.0f, Color.red, 1.0f);

        if (Managers.Game.OnDirect)
        {
            _cursor = CursorType.Directing;
        }
        else
        {
            if (Physics.Raycast(ray, out hit, 100.0f, _mask))
            {
                // 몬스터, 장비, 물약
                if (hit.collider.gameObject.layer == (int)Define.Layer.Monster || hit.collider.gameObject.layer == (int)Define.Layer.EItem || hit.collider.gameObject.layer == (int)Define.Layer.CItem)
                {
                    if (_cursor != CursorType.Search)
                    {
                        _cursor = CursorType.Search;
                    }
                }
                // 보스문, 문, 포탈, 레버, 상호작용 물체
                else if (hit.collider.gameObject.layer == (int)Define.Layer.BossDoor || hit.collider.gameObject.layer == (int)Define.Layer.Door || hit.collider.gameObject.layer == (int)Define.Layer.Portal || hit.collider.gameObject.layer == (int)Define.Layer.Lever || hit.collider.gameObject.layer == (int)Define.Layer.InteractObjects)
                {
                    if (_cursor != CursorType.Grap)
                    {
                        _cursor = CursorType.Grap;
                    }
                }
                else
                {
                    if (_cursor != CursorType.Press && _cursor != CursorType.Normal)
                        _cursor = CursorType.Normal;
                }
            }
            else
            {
                if (_cursor != CursorType.Press && _cursor != CursorType.Normal)
                    _cursor = CursorType.Normal;
            }
        }

        if (Managers.Game.ScreenType == Define.ScreenType.Window)
        {
            switch (_cursor)
            {
                case CursorType.Normal:
                    //_frameTimer = 0;
                    if (_frameTimer < 3.500f)
                        Cursor.SetCursor(_normalCursorSmall0, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.550f)
                        Cursor.SetCursor(_normalCursorSmall1, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.600f)
                        Cursor.SetCursor(_normalCursorSmall2, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 4.300f)
                        Cursor.SetCursor(_normalCursorSmall3, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 4.350f)
                        Cursor.SetCursor(_normalCursorSmall4, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 4.400f)
                        Cursor.SetCursor(_normalCursorSmall5, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else
                        _frameTimer = 0;
                    break;
                case CursorType.Search:
                    if (_frameTimer < 3.500f)
                        Cursor.SetCursor(_searchCursorSmall0, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.550f)
                        Cursor.SetCursor(_searchCursorSmall1, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.600f)
                        Cursor.SetCursor(_searchCursorSmall2, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.650f)
                        Cursor.SetCursor(_searchCursorSmall3, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.700f)
                        Cursor.SetCursor(_searchCursorSmall4, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.750f)
                        Cursor.SetCursor(_searchCursorSmall5, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else
                        _frameTimer = 0;
                    break;
                case CursorType.Grap:
                    if (_frameTimer < 3.500f)
                        Cursor.SetCursor(_handleCursorSmall0, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.550f)
                        Cursor.SetCursor(_handleCursorSmall1, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.600f)
                        Cursor.SetCursor(_handleCursorSmall2, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 4.300f)
                        Cursor.SetCursor(_handleCursorSmall3, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 4.350f)
                        Cursor.SetCursor(_handleCursorSmall4, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 4.400f)
                        Cursor.SetCursor(_handleCursorSmall5, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else
                        _frameTimer = 0;
                    break;
                case CursorType.Click:
                    if (_frameTimer < 0.005f)
                        Cursor.SetCursor(_normalCursorSmall3, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 0.105f)
                        Cursor.SetCursor(_normalCursorSmall4, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 0.150f)
                        Cursor.SetCursor(_normalCursorSmall5, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else
                        _cursor = CursorType.Normal;
                    break;
                case CursorType.Press:
                    Cursor.SetCursor(_normalCursorSmall3, new Vector2(0, 0), CursorMode.ForceSoftware);
                    break;
                case CursorType.Directing:
                    Cursor.SetCursor(_directingCursor, new Vector2(0, 0), CursorMode.ForceSoftware);
                    break;
                default:
                    break;
            }
        }
        else
        {
            switch (_cursor)
            {
                case CursorType.Normal:
                    //_frameTimer = 0;
                    if (_frameTimer < 3.500f)
                        Cursor.SetCursor(_normalCursor0, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.550f)
                        Cursor.SetCursor(_normalCursor1, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.600f)
                        Cursor.SetCursor(_normalCursor2, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 4.300f)
                        Cursor.SetCursor(_normalCursor3, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 4.350f)
                        Cursor.SetCursor(_normalCursor4, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 4.400f)
                        Cursor.SetCursor(_normalCursor5, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else
                        _frameTimer = 0;
                    break;
                case CursorType.Search:
                    if (_frameTimer < 3.500f)
                        Cursor.SetCursor(_searchCursor0, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.550f)
                        Cursor.SetCursor(_searchCursor1, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.600f)
                        Cursor.SetCursor(_searchCursor2, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.650f)
                        Cursor.SetCursor(_searchCursor3, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.700f)
                        Cursor.SetCursor(_searchCursor4, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.750f)
                        Cursor.SetCursor(_searchCursor5, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else
                        _frameTimer = 0;
                    break;
                case CursorType.Grap:
                    if (_frameTimer < 3.500f)
                        Cursor.SetCursor(_handleCursor0, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.550f)
                        Cursor.SetCursor(_handleCursor1, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 3.600f)
                        Cursor.SetCursor(_handleCursor2, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 4.300f)
                        Cursor.SetCursor(_handleCursor3, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 4.350f)
                        Cursor.SetCursor(_handleCursor4, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 4.400f)
                        Cursor.SetCursor(_handleCursor5, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else
                        _frameTimer = 0;
                    break;
                case CursorType.Click:
                    if (_frameTimer < 0.005f)
                        Cursor.SetCursor(_normalCursor3, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 0.105f)
                        Cursor.SetCursor(_normalCursor4, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else if (_frameTimer < 0.150f)
                        Cursor.SetCursor(_normalCursor5, new Vector2(0, 0), CursorMode.ForceSoftware);
                    else
                        _cursor = CursorType.Normal;
                    break;
                case CursorType.Press:
                    Cursor.SetCursor(_normalCursor3, new Vector2(0, 0), CursorMode.ForceSoftware);
                    break;
                case CursorType.Directing:
                    Cursor.SetCursor(_directingCursor, new Vector2(0, 0), CursorMode.ForceSoftware);
                    break;
                default:
                    break;
            }
        }
        
    }

}
