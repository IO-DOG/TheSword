using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;


[SerializeField]
public class Key : Item
{
    [Tooltip("¿­¼èÀÇ »ö")]
    public Define.KeyColor _keyColor;

    public void PickUp()
    {
        Debug.Log(gameObject.name + "is Picked up!");
        Managers.Game.Inventory.AddItem(this);
        gameObject.SetActive(false);
    }
}
