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
        // 새 게임에서는 이 함수가 LoadGame 끝에서만 불려서 아예 안 돌 때가 있다.
        // 그러면 _keys 가 빈 채로 남고, ShowKeySlot 이 그걸 인덱싱하다 터져서
        // UI_GameScene.Init 이 통째로 끊긴다 — 맵도 플레이어 배치도 안 된다.
        // 여러 번 불려도 칸이 늘어나지 않도록 개수를 맞춰서 채운다.
        List<int> saved = Managers.Game.PlayerData.KeyInventory;
        if (saved == null)
        {
            saved = new List<int>();
            Managers.Game.PlayerData.KeyInventory = saved;
        }
        while (saved.Count < ConsumableItem.NUM_OF_KEYS)
            saved.Add(0);

        _keys = saved;
    }

    /// <summary>열쇠 칸이 아직 없으면 채운다. 어디서 불려도 안전하다.</summary>
    public void EnsureKeys()
    {
        if (_keys == null || _keys.Count < NUM_OF_KEYS)
            InitKeyInventory();
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
        if (keyInventory == null)
            return;

        EnsureKeys();

        int slots = Mathf.Min(NUM_OF_KEYS, keyInventory.transform.childCount);
        for (int i = 0; i < slots && i < _keys.Count; i++)
        {
            TMP_Text text = keyInventory.transform.GetChild(i).GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = _keys[i].ToString();
        }
    }
}
