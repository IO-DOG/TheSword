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
        EnsureKeys();

        // 예전에는 GetComponentInChildren 과 GetComponent 를 섞어 썼다.
        // 생성된 층은 문 타일의 계층이 달라서 후자가 null 이 되고,
        // 그 자리에서 예외가 나 문이 영영 안 열렸다.
        Door d = Door.Find(door);
        if (d == null)
        {
            Debug.LogWarning($"[열쇠] {door.name} 에서 Door 를 못 찾았다");
            return false;
        }

        int idx = d._keyIndex;
        if (idx < 0 || idx >= _keys.Count || _keys[idx] == 0)
            return false;
        int stranded = StrandedFloor(d);
        if (stranded > 0)
        {
            UI_ConfirmPopup.Tell(string.Format(Managers.GetString(Define.VAULT_KEY_NEEDED), stranded));
            return false;
        }

        Managers.Data.DoorActiveDic[d._doorIndex_forActive] = false;
        _keys[idx]--;
        ShowKeySlot(Managers.Game.Player._keyInventory);
        return true;
    }

    /// <summary>
    /// 아래층 금고(네 번째 문)에 이 열쇠를 쓰면 가장 높이 올라온 층의 큰길 문을 열 열쇠가 모자라는가 — 그 층 번호(1 부터), 아니면 0.
    /// 큰길 열쇠는 층마다 문 수만큼만 있다. 위층 첫 구역의 초록 열쇠는 내려가는 계단 옆이고 아래층 금고는 올라오는 계단 옆이라,
    /// 내려가 금고를 열고 오면 그 층을 영영 못 넘었고 오르내릴 때마다 체크포인트도 그 상태로 덮였다.
    /// 같은 층의 금고는 마지막 구역에 있어(layout_gen.check_vault_safe) 그 앞에서는 큰길 문이 다 열려 있다 — 늘 0 이다.
    /// ponytail: 아직 안 주운 여분 열쇠는 세지 않는다 — 줍고 오면 열린다.
    /// </summary>
    int StrandedFloor(Door door)
    {
        GameManager.CurPlayerData p = Managers.Game.PlayerData;
        int floor = p.CurStageid + 1;
        if (floor >= GeneratedStory.VaultDoors.Length || GeneratedStory.VaultDoors[floor] != door._doorIndex_forActive)
            return 0;
        int top = p.FirstEnterMapCheck != null ? p.FirstEnterMapCheck.LastIndexOf(true) : -1;
        if (top <= p.CurStageid || Managers.Data.MapDic.TryGetValue(top, out MapData map) == false || map.Objects == null)
            return 0;

        int topVault = top + 1 < GeneratedStory.VaultDoors.Length ? GeneratedStory.VaultDoors[top + 1] : -1;
        int need = 0;
        foreach (ObjectData obj in map.Objects)
        {
            // 색은 MapBuilder.BuildDoor 와 같은 셈 — 셀 코드 3·6 초록, 4·7 노랑, 5·8 빨강.
            if (obj.ObjectType == (int)Define.ObjectType.Door && obj.Count != topVault && (obj.Id - 3) % NUM_OF_KEYS == door._keyIndex
                && Managers.Data.DoorActiveDic.TryGetValue(obj.Count, out bool closed) && closed)
                need++;
        }
        return _keys[door._keyIndex] - 1 < need ? top + 1 : 0;
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
