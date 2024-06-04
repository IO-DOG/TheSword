using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Inventory
{
    const int NUM_OF_KEYS = ConsumableItem.NUM_OF_KEYS;
    List<Item> _items = new List<Item>();
    int[] _keys = new int[NUM_OF_KEYS];

    public void AddItem(Item item)
    {
        if (item != null)
            _items.Add(item);

        if (item.GetComponent<ConsumableItem>().id < NUM_OF_KEYS)
        {
            _keys[item.GetComponent<ConsumableItem>().id]++;
        }

        ShowKeySlot(Managers.Game.Player._keyInventory);
    }

    public bool TryUseKey(GameObject door)
    {
        if (_keys[door.GetComponentInChildren<Door>()._keyIndex] == 0)
        {
            return false;
        }
        else
        {
            // TODO Save
            _keys[door.GetComponentInChildren<Door>()._keyIndex]--;
            ShowKeySlot(Managers.Game.Player._keyInventory);
            return true;
        }
    }

    public void ShowKeySlot(GameObject keyInventory)
    {
        if (keyInventory != null)
        {
            for (int i = 0; i < NUM_OF_KEYS; i++)
            {
               keyInventory.transform.GetChild(i).GetChild(0).GetComponent<Image>().sprite = Managers.Game.KeyIcon[i];
               keyInventory.transform.GetChild(i).GetComponentInChildren<TMP_Text>().text = _keys[i].ToString();
            }
        }
    }
}
