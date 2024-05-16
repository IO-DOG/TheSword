using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
    List<Item> _items = new List<Item>();

    public void AddItem(Item item)
    {
        if(item != null)
            _items.Add(item);
    }
}
