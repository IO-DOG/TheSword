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
            Managers.Game.PlayerData.KeyInventory.Add(0);
        }

        for (int i = 0; i < NUM_OF_KEYS; i++)
        {
            _keys.Add(0);
        }

        _keys = Managers.Game.PlayerData.KeyInventory;
    }

    public void AddItem(ConsumableItem item)
    {
        if (item != null)
            _items.Add(item);

        if (item.GetComponent<ConsumableItem>().id < NUM_OF_KEYS)
        {
            _keys[item.GetComponent<ConsumableItem>().id]++;
            if (item.GetComponent<ConsumableItem>().id == 0)
                PlayerPrefs.SetInt("ISOPENGREENKEY", 1);
            if (item.GetComponent<ConsumableItem>().id == 1)
                PlayerPrefs.SetInt("ISOPENYELLOWKEY", 1);
            if (item.GetComponent<ConsumableItem>().id == 2)
                PlayerPrefs.SetInt("ISOPENREDKEY", 1);
            if (Managers.Game.Player._keyInventory.transform.GetChild(item.GetComponent<ConsumableItem>().id).gameObject.activeSelf == false)
            {
                Managers.Game.Player._keyInventory.transform.GetChild(item.GetComponent<ConsumableItem>().id).gameObject.SetActive(true);
            }
            ShowKeySlot(Managers.Game.Player._keyInventory);
            Managers.Game.PlayerData.KeyInventory = _keys;
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
            Managers.Data.DoorActiveDic[door.GetComponent<Door>()._doorIndex_forActive] = false;
            //Managers.Game.SaveGame();
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
