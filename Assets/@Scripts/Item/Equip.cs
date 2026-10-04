using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Equip : MonoBehaviour
{
    public Define.Types _type = Define.Types.None;
    public int _id = 0;
    public int _itemIndex_forActive;

    public Define.Types Types 
    { 
        get 
        { 
            return _type; 
        }
        set 
        { 
            _type = value;
        }
    }

    public int Id
    {
        get 
        { 
            return _id; 
        }
        set
        {
            _id = value;
        }
    }

    private void Start()
    {
        // 프리팹 콜라이더가 0.5 라 옆 칸 탐침(PathMover)까지 걸려 둘레 3x3 이 막혔다 — 클릭 이동이 떨군 장비 옆 칸에 서지 못해
        // 40층 둘 중 하나 룬에 못 갔다. 두 칸 밖에서 민 광선(0.416)도 겉면(0.25)에 닿는다. 가로·깊이만 한 칸으로 — 높이는 미는 광선 몫이라 그대로.
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
            box.size = new Vector3(Define.TILE_SIZE, box.size.y, Define.TILE_SIZE);
        GetComponent<Animator>().Play($"EquipItem_{Id}");
        GetComponent<SpriteRenderer>().material = Managers.Resource.Load<Material>(Managers.Data.EquipDic[Id].Shadow);
    }

    public void PickUp()
    {
        Debug.Log("Pickup");
        Define.Types type = (Define.Types)Managers.Data.EquipDic[Id].Type;
        Managers.Game.PlayerData.Inventory[(int)type].Add(Id);

        // 몬스터가 떨군 것은 맵 데이터에 없다. 인덱스가 없는데 0 번을 지우면
        // 그 층에 원래 있던 다른 장비가 이미 주운 것으로 기록된다.
        if (_itemIndex_forActive >= 0)
            Managers.Data.EItemActiveDic[_itemIndex_forActive] = false;

        gameObject.SetActive(false);

        //Managers.Game.SaveGame();
        // 무조건 갈아입지 않는다. 더 나을 때만 착용하고, 아니면 인벤토리에 남는다.
        Managers.Game.EquipIfBetter(Id);
        Managers.Game.GameScene.Refresh();
        // 워프석 반지(32)를 주우면 반지에 새긴 글을 읽는 장면이 뜬다 (StoryDirector, 바이블 R14).
        GameEvents.RaiseEquipPicked(Id);
    }
}
