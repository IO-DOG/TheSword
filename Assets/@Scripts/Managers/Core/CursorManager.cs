using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    Define.Cursor _cursor = Define.Cursor.Normal;

    Animator _animator;
    SpriteRenderer _renderer;

    public void Init()
    {
        _animator = GetComponent<Animator>();
        _renderer = GetComponent<SpriteRenderer>();
        
    }

    void Update()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.nearClipPlane; // 카메라와의 거리 설정
        transform.position = Camera.main.ScreenToWorldPoint(mousePosition);


    }
}
