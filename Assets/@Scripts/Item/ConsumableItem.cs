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

            // 최초 문인지 확인
            if (PlayerPrefs.GetInt("ISFIRSTKEY") == 0)
            {
                PlayerPrefs.SetInt("ISFIRSTKEY", 1);
                UI_GuidePopup guidePopup = Managers.UI.ShowPopupUI<UI_GuidePopup>();
                guidePopup.SetInfo(Define.GUIDE_KEY);
            }
        }
        else if(id < NUM_OF_POTIONS)
        {
            float heal = Managers.Game.ConsumableItemData.Heal * Managers.Game.PlayerData.MaxHP / 100;
            heal = Mathf.Round(heal);
            Managers.Game.PlayerData.CurHP += heal;

            // Show Healing Font
            Transform ui_PlayerHpBar = Managers.UI.GetPlayerHpBar();
            Managers.Object.ShowPotionHealingFont(heal, ui_PlayerHpBar);

            if (Managers.Game.PlayerData.CurHP > Managers.Game.PlayerData.MaxHP)
                Managers.Game.PlayerData.CurHP = Managers.Game.PlayerData.MaxHP;

            // 최초 포션인지 확인
            if (PlayerPrefs.GetInt("ISFIRSTRECOVERY") == 0)
            {
                PlayerPrefs.SetInt("ISFIRSTRECOVERY", 1);
                UI_GuidePopup guidePopup = Managers.UI.ShowPopupUI<UI_GuidePopup>();
                guidePopup.SetInfo(Define.GUIDE_RECOVERY);
            }
        }
        else if(id < NUM_OF_RUNES)
        {
            Managers.Game.PlayerData.Attack += Managers.Game.ConsumableItemData.AttackUp;
            Managers.Game.PlayerData.Defence += Managers.Game.ConsumableItemData.DefenceUp;
            Managers.Game.PlayerData.CurHP += Managers.Game.ConsumableItemData.HPUp;
            Managers.Game.PlayerData.MaxHP += Managers.Game.ConsumableItemData.HPUp;

            if (Managers.Game.PlayerData.CurHP > Managers.Game.PlayerData.MaxHP)
                Managers.Game.PlayerData.CurHP = Managers.Game.PlayerData.MaxHP;
        }

        if (Managers.Game.GameScene != null)
        {
            Managers.Game.GameScene.Refresh();
        }
        //Managers.Game.SaveGame();
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
                    particle.transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z); ;
                    particle.transform.localScale = new Vector3(0.25f, 0.25f/3f, 0.25f);
                    break;
                }
        }
    }
}
