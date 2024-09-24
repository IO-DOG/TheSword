using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EventHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Action OnClickHandler = null;
    public Action OnPressedHandler = null;
    public Action OnPointerDownHandler = null;
    public Action OnPointerUpHandler = null;
    public Action OnPointerEnterHandler = null;
    public Action OnPointerExitHandler = null;
    public Action<BaseEventData> OnDragHandler = null;
    public Action<BaseEventData> OnBeginDragHandler = null;
    public Action<BaseEventData> OnEndDragHandler = null;

    bool _pressed = false;
    GameObject _cursorObject = null;

    private void Update()
    {
        if (_pressed)
        {
            OnPressedHandler?.Invoke();

            //_cursorObject = GameObject.Find("@Cursor");
            if (_cursorObject != null)
            {
                if (_cursorObject.GetOrAddComponent<CursorManager>()._cursor != CursorType.Search && _cursorObject.GetOrAddComponent<CursorManager>()._cursor != CursorType.Grap)
                    _cursorObject.GetOrAddComponent<CursorManager>()._cursor = CursorType.Press;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (OnClickHandler != null)
        {
            OnClickHandler.Invoke();

            _cursorObject = GameObject.Find("@Cursor");
            if (_cursorObject != null)
            {
                if (_cursorObject.GetOrAddComponent<CursorManager>()._cursor != CursorType.Search && _cursorObject.GetOrAddComponent<CursorManager>()._cursor != CursorType.Grap)
                    _cursorObject.GetOrAddComponent<CursorManager>()._cursor = CursorType.Click;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
        OnPointerDownHandler?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = true;
        OnPointerUpHandler?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        _pressed = true;
        OnDragHandler?.Invoke(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        OnBeginDragHandler?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnEndDragHandler?.Invoke(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnPointerEnterHandler?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerExitHandler?.Invoke();
    }
}
