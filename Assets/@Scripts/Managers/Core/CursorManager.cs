using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    Define.Cursor _cursor = Define.Cursor.Normal;

    public void Init()
    {

    }

    void Update()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.nearClipPlane; // 카메라와의 거리 설정
        transform.position = Camera.main.ScreenToWorldPoint(mousePosition);


    }
}
