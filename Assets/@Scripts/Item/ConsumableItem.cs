using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;


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

    /// <summary>획득 이펙트.
    ///
    /// 파티클이 없어도 획득은 끝나야 한다. 어드레서블에 없는 키를 만나면
    /// Instantiate 가 null 을 주는데, 그대로 transform 을 만지면 PickUp 이 중간에
    /// 끊긴다 — 아이템이 꺼지지 않아 줍지도 못하는데 길은 막는 물건이 되고,
    /// 그 칸에서 진행이 끝난다. 실제로 룬(FX_RunStone_*)이 그래서 5층을 막았다.</summary>
    private void PlayParticle()
    {
        if (id < 0 || id >= ConsumableItem.NUM_OF_RUNES)
            return;

        GameObject particle = Managers.Resource.Instantiate(
            Managers.Data.ConsumableItemDic[id].PrefabName, Managers.Game.Player.transform);
        if (particle == null)
            return;

        if (id >= NUM_OF_KEYS && id < NUM_OF_POTIONS)
        {
            // 물약은 아이템이 놓인 자리 위에서 터진다.
            particle.transform.position = new Vector3(transform.position.x,
                                                      transform.position.y + 0.5f,
                                                      transform.position.z);
            particle.transform.localScale = new Vector3(0.25f, 0.25f / 3f, 0.25f);
            return;
        }

        // 열쇠와 룬은 캐릭터에 붙어서 터진다.
        particle.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);
    }
}
