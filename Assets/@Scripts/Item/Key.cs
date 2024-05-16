using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;


[SerializeField]
public class Key : Item
{ 
    [Header("Name of the door to unlock")]

    [Tooltip("잠금을 해제할 문의 이름")]
    public string DoorName;

    public void PickUp()
    {
        Debug.Log(gameObject.name + "is Picked up!");
        Managers.Game.Inventory.AddItem(this);
        gameObject.SetActive(false);
    }
}
