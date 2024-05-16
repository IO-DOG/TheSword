using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SerializeField]
public class Item : MonoBehaviour
{
    [Header("Information")]

    [Tooltip("아이템 타입")]
    public Define.ItemType ItemType = Define.ItemType.Unknown;

    [Tooltip("아이템 인덱스")]
    public int Index;

    [Tooltip("아이템 이름")]
    public string Name;

    [Tooltip("아이템 설명")]
    public string Description;
}
