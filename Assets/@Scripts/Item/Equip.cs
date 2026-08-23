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
    }
}
