using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;


[SerializeField]
public class ConsumableItem : MonoBehaviour
{
    public const int NUM_OF_KEYS = 3;
    public const int NUM_OF_POTIONS = NUM_OF_KEYS + 6;
    public const int NUM_OF_RUNES = NUM_OF_POTIONS + 3;
    public int id;
    public int _itemIndex_forActive;

    private void Start()
    {
        GetComponent<Animator>().Play($"ConsumableItem_{id}");
        GetComponent<SpriteRenderer>().material = Managers.Resource.Load<Material>(Managers.Data.ConsumableItemDic[id].Shadow);
    }

    public void PickUp()
    {
        #region Data Loading
        Managers.Game.ConsumableItemData.id = id;
        Managers.Game.ConsumableItemData.Heal = Managers.Data.ConsumableItemDic[id].Heal;
        Managers.Game.ConsumableItemData.AttackUp = Managers.Data.ConsumableItemDic[id].AttackUp;
        Managers.Game.ConsumableItemData.DefenceUp = Managers.Data.ConsumableItemDic[id].DefenceUp;
        Managers.Game.ConsumableItemData.HPUp = Managers.Data.ConsumableItemDic[id].HPUp;
        Managers.Game.ConsumableItemData.Img = Managers.Data.ConsumableItemDic[id].Img;
        Managers.Game.ConsumableItemData.PrefabName = Managers.Data.ConsumableItemDic[id].PrefabName;
        Managers.Game.ConsumableItemData.Shadow = Managers.Data.ConsumableItemDic[id].Shadow;
        Managers.Game.ConsumableItemData.ScriptNameId = Managers.Data.ConsumableItemDic[id].ScriptNameId;
        Managers.Game.ConsumableItemData.ScriptDescriptionId = Managers.Data.ConsumableItemDic[id].ScriptDescriptionId;
        Managers.Game.ConsumableItemData.IsActiveIndex = _itemIndex_forActive;
        #endregion

        Managers.Data.CItemActiveDic[_itemIndex_forActive] = false;
        gameObject.SetActive(false);
        PlayParticle();

        if (id < NUM_OF_KEYS)
        {
            Managers.Game.KeyInventory.AddItem(this);
        }
        else if(id < NUM_OF_POTIONS)
        {
            Managers.Game.CurPlayerData.CurHP += Managers.Game.ConsumableItemData.Heal;
        }
        else if(id < NUM_OF_RUNES)
        {
            Managers.Game.CurPlayerData.Attack += Managers.Game.ConsumableItemData.AttackUp;
            Managers.Game.CurPlayerData.Defence += Managers.Game.ConsumableItemData.DefenceUp;   
            Managers.Game.CurPlayerData.MaxHP += Managers.Game.ConsumableItemData.HPUp;   
        }

        Managers.Game.SaveGame();
    }

    private void PlayParticle()
    {
        switch (id)
        {
            case 0:
            case 1:
            case 2:
                { 
                    GameObject particle = Managers.Resource.Instantiate(Managers.Data.ConsumableItemDic[id].PrefabName, Managers.Game.Player.transform);
                    particle.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);
                    break;
                }
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
                {
                    GameObject particle = Managers.Resource.Instantiate(Managers.Data.ConsumableItemDic[id].PrefabName, Managers.Game.Player.transform);
                    particle.transform.position = this.transform.position; ;
                    particle.transform.localScale = new Vector3(0.25f, 0.25f/3f, 0.25f);
                    break;
                }
        }
    }
}
