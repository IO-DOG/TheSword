using Data;
using DG.Tweening;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class KeyInventory
{
    const int NUM_OF_KEYS = ConsumableItem.NUM_OF_KEYS;
    List<ConsumableItem> _items = new List<ConsumableItem>();
    public List<int> _keys = new List<int>(NUM_OF_KEYS);
    public void InitKeyInventory()
    {
        for (int i = 0; i < ConsumableItem.NUM_OF_KEYS; ++i)
        {
            Managers.Game.CurPlayerData.KeyInventory.Add(0);
        }

        for (int i = 0; i < NUM_OF_KEYS; i++)
        {
            _keys.Add(0);
        }

        _keys = Managers.Game.CurPlayerData.KeyInventory;
    }

    public void AddItem(ConsumableItem item)
    {
        if (item != null)
            _items.Add(item);

        if (item.GetComponent<ConsumableItem>().id < NUM_OF_KEYS)
        {
            _keys[item.GetComponent<ConsumableItem>().id]++;
            if (Managers.Game.Player._keyInventory.transform.GetChild(item.GetComponent<ConsumableItem>().id).gameObject.activeSelf == false)
            {
                Managers.Game.Player._keyInventory.transform.GetChild(item.GetComponent<ConsumableItem>().id).gameObject.SetActive(true);
            }
            ShowKeySlot(Managers.Game.Player._keyInventory);
            Managers.Game.CurPlayerData.KeyInventory = _keys;
        }
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
                keyInventory.transform.GetChild(i).GetComponentInChildren<TMP_Text>().text = _keys[i].ToString();
            }
        }
    }
}
