using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;


[SerializeField]
public class ConsumableItem : Item
{
    //[Tooltip("¿­¼èÀÇ »ö")]
    //public Define.KeyColor _keyColor;

    private void Start()
    {
        Managers.Game.ConsumableItemData.id = Managers.Data.ConsumableItemDic[id].id;
        Managers.Game.ConsumableItemData.Name = Managers.Data.ConsumableItemDic[id].Name;
        Managers.Game.ConsumableItemData.Heal = Managers.Data.ConsumableItemDic[id].Heal;
        Managers.Game.ConsumableItemData.AttackUp = Managers.Data.ConsumableItemDic[id].AttackUp;
        Managers.Game.ConsumableItemData.DefenceUp = Managers.Data.ConsumableItemDic[id].DefenceUp;
        Managers.Game.ConsumableItemData.HPUp = Managers.Data.ConsumableItemDic[id].HPUp;
        Managers.Game.ConsumableItemData.Description = Managers.Data.ConsumableItemDic[id].Description;
        Managers.Game.ConsumableItemData.IsActiveIndex = _itemIndex_forActive;
    }

    public void PickUp()
    {
        Debug.Log(gameObject.name + "is Picked up!");
        //Managers.Game.Inventory.AddItem(this);
        gameObject.SetActive(false);
    }
}
