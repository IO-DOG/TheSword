using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;

public class Inventory
{
    List<Item> _items = new List<Item>();
    List<Key> _keys = new List<Key>();

    public void AddItem(Item item)
    {
        if(item != null)
            _items.Add(item);

        if (item.GetComponent<Key>() != null)
            _keys.Add(item.GetComponent<Key>());
    }

    public bool HasKey(string doorName)
    {
        if (_keys.Count == 0)
            return false;

        for(int i = 0; i < _keys.Count; i++)
        {
            if (_keys[i].DoorName == doorName)
            {
                return true;
            }
        }

        return false;
    }

    public void UseKey(GameObject door)
    {
        door.SetActive(false);

        for (int i = 0; i < _keys.Count; i++)
        {
            if (_keys[i].DoorName == door.name)
            {
                _keys.RemoveAt(i);
            }
        }
    }
}
