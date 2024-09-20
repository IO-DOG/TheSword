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
    }

    public void PickUp()
    {
        #region Data Loading
        Managers.Game.ConsumableItemData.id = id;
        Managers.Game.ConsumableItemData.Name = Managers.Data.ConsumableItemDic[id].Name;
        Managers.Game.ConsumableItemData.Heal = Managers.Data.ConsumableItemDic[id].Heal;
        Managers.Game.ConsumableItemData.AttackUp = Managers.Data.ConsumableItemDic[id].AttackUp;
        Managers.Game.ConsumableItemData.DefenceUp = Managers.Data.ConsumableItemDic[id].DefenceUp;
        Managers.Game.ConsumableItemData.HPUp = Managers.Data.ConsumableItemDic[id].HPUp;
        Managers.Game.ConsumableItemData.Description = Managers.Data.ConsumableItemDic[id].Description;
        Managers.Game.ConsumableItemData.IsActiveIndex = _itemIndex_forActive;
        #endregion

        Debug.Log(Managers.Game.ConsumableItemData.Name + "is Picked up!");
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
                {
                    GameObject particle = Managers.Resource.Instantiate("FX_Key_Green");
                    particle.transform.position = this.transform.position;
                    particle.transform.localScale = Vector3.one * 0.3f;
                    break;
                }
            case 1:
                {
                    GameObject particle = Managers.Resource.Instantiate("FX_Key_Red");
                    particle.transform.position = this.transform.position;
                    particle.transform.localScale = Vector3.one * 0.3f;
                    break;
                }
            case 2:
                {
                    GameObject particle = Managers.Resource.Instantiate("FX_Key_Yellow");
                    particle.transform.position = this.transform.position;
                    particle.transform.localScale = Vector3.one * 0.3f;
                    break;
                }
            case 3:
            case 4:
                {
                    GameObject particle = Managers.Resource.Instantiate("FX_Potion_B");
                    particle.transform.position = this.transform.position; ;
                    particle.transform.localScale = Vector3.one * 0.1f;
                }
                break;
            case 5:
            case 6:
                {
                    GameObject particle = Managers.Resource.Instantiate("FX_Potion_C");
                    particle.transform.position = this.transform.position;
                    particle.transform.localScale = Vector3.one * 0.1f;
                }
                break;
            case 7:
            case 8:
                {
                    GameObject particle = Managers.Resource.Instantiate("FX_Potion_D");
                    particle.transform.position = this.transform.position;
                    particle.transform.localScale = Vector3.one * 0.1f;
                }
                break;
        }
    }
}
