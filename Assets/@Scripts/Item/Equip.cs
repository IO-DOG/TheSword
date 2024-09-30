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
        Define.Types type = (Define.Types)Managers.Data.EquipDic[Id].Type;
        Managers.Game.CurPlayerData.Inventory[(int)type].Add(Id);
        Managers.Data.EItemActiveDic[_itemIndex_forActive] = false;

        gameObject.SetActive(false);

        Managers.Game.SaveGame();
        Managers.Game.SwapEquip(Id);
        Managers.Game.GameScene.Refresh();
    }
}
