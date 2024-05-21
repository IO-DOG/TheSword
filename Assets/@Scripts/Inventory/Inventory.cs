using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Inventory
{
    //const int NUM_OF_KEYS = 3;
    //List<Item> _items = new List<Item>();
    ////Key[] _keys = new Key[NUM_OF_KEYS];

    //public void AddItem(Item item)
    //{
    //    if (item != null)
    //        _items.Add(item);

    //    if (item.GetComponent<Key>() != null)
    //    {
    //        _keys[(int)item.GetComponent<Key>()._keyColor] = item.GetComponent<Key>();
    //    }

    //    ShowKeySlot(Managers.Game.Player._keyInventory);
    //}

    //public bool TryUseKey(GameObject door)
    //{
    //    if (_keys[(int)door.GetComponent<Door>()._doorColor] == null)
    //    {
    //        return false;
    //    }
    //    else
    //    {
    //        door.SetActive(false);
    //        //_keys[(int)door.GetComponent<Door>()._doorColor] = null;
    //        ShowKeySlot(Managers.Game.Player._keyInventory);
    //        return true;
    //    }
    //}

    //public void ShowKeySlot(GameObject keyInventory)
    //{
    //    if (keyInventory != null)
    //    {
    //       for(int i = 0; i < NUM_OF_KEYS; i++)
    //       {
    //            //if (_keys[i] != null)
    //            //    keyInventory.transform.GetChild(i).GetChild(0).GetComponent<Image>().sprite = _keys[i].Icon;
    //            //else
    //            //    keyInventory.transform.GetChild(i).GetChild(0).GetComponent<Image>().sprite = null;
    //        }
    //    }
    //}
}
