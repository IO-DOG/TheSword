using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;


[SerializeField]
public class ConsumableItem : Item
{
    public const int NUM_OF_KEYS = 3;
    public const int NUM_OF_POTIONS = NUM_OF_KEYS + 6;
    public const int NUM_OF_RUNES = NUM_OF_POTIONS + 3;
    public int _consumableItemIndex;

    public void PickUp()
    {
        #region Data Loading
        _consumableItemIndex = id;
        Managers.Game.ConsumableItemData.id = id;
        Managers.Game.ConsumableItemData.Name = Managers.Data.ConsumableItemDic[id].Name;
        Managers.Game.ConsumableItemData.Heal = Managers.Data.ConsumableItemDic[id].Heal;
        Managers.Game.ConsumableItemData.AttackUp = Managers.Data.ConsumableItemDic[id].AttackUp;
        Managers.Game.ConsumableItemData.DefenceUp = Managers.Data.ConsumableItemDic[id].DefenceUp;
        Managers.Game.ConsumableItemData.HPUp = Managers.Data.ConsumableItemDic[id].HPUp;
        Managers.Game.ConsumableItemData.Description = Managers.Data.ConsumableItemDic[id].Description;
        Managers.Game.ConsumableItemData.IsActiveIndex = _itemIndex_forActive;
        #endregion

        Debug.Log(Managers.Game.ConsumableItemData.Name + "is Picked up!");
        Managers.Data.ItemActiveDic[_itemIndex_forActive] = false;
        gameObject.SetActive(false);

        Debug.Log(Managers.Game.CurPlayerData.Attack);

        if(_consumableItemIndex < NUM_OF_KEYS)
        {
            Managers.Game.Inventory.AddItem(this);
        }
        else if(_consumableItemIndex < NUM_OF_POTIONS)
        {
            Managers.Game.CurPlayerData.CurHP += Managers.Game.ConsumableItemData.Heal;
        }
        else if(_consumableItemIndex < NUM_OF_RUNES)
        {
            Managers.Game.CurPlayerData.Attack += Managers.Game.ConsumableItemData.AttackUp;
            Debug.Log(Managers.Game.CurPlayerData.Attack); ;
            Managers.Game.CurPlayerData.Defence += Managers.Game.ConsumableItemData.DefenceUp;   
            Managers.Game.CurPlayerData.MaxHP += Managers.Game.ConsumableItemData.HPUp;   
        }

        Managers.Game.SaveGame();
    }
}
