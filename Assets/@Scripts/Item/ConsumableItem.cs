using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;


public class ConsumableItem : MonoBehaviour
{
    public const int NUM_OF_KEYS = 3;
    public const int NUM_OF_POTIONS = NUM_OF_KEYS + 6;
    // 룬: 9~11 계단 룬(+1), 12~ 크기 룬(금고·둘 중 하나 — generate_content.SIZED_RUNES 의 행 수).
    // 표에 행이 늘면 여기도 늘린다 — validate_content 가 대조한다.
    public const int NUM_OF_RUNES = NUM_OF_POTIONS + 3 + 7;
    public int id;
    public int _itemIndex_forActive;
    public ConsumableItem ChoicePartner;
    LineRenderer _choiceLine;

    private void Start()
    {
        if (ChoicePartner != null && _itemIndex_forActive < ChoicePartner._itemIndex_forActive)
        {
            _choiceLine = gameObject.AddComponent<LineRenderer>();
            _choiceLine.sharedMaterial = GetComponent<SpriteRenderer>().sharedMaterial;
            _choiceLine.positionCount = 2;
            _choiceLine.startWidth = _choiceLine.endWidth = 0.015f;
            _choiceLine.startColor = _choiceLine.endColor = new Color(0.5f, 1f, 1f, 0.8f);
            _choiceLine.SetPosition(0, transform.position + Vector3.up * 0.06f);
            _choiceLine.SetPosition(1, ChoicePartner.transform.position + Vector3.up * 0.06f);
        }
        int art = ArtId(id);
        GetComponent<Animator>().Play($"ConsumableItem_{art}");
        GetComponent<SpriteRenderer>().material = Managers.Resource.Load<Material>(Managers.Data.ConsumableItemDic[id].Shadow);
        // 크기 룬은 기본 룬 그림을 빌려 써서 맵에서 계단 룬(+1)과 똑같아 보였다 — 금고를 열어야
        // 할지 겉보기로 알 수 없다. 제 능력치 색으로 더 진하게 칠한다(몬스터처럼 "진하면 세다").
        // 룬 클립은 m_Sprite 만 움직이므로 색이 덮이지 않는다.
        // ponytail: 임시 표시다. 등급별 룬 그림이 오면 ItemAnimator 상태로 바꾸고 이 줄을 지운다.
        if (art != id)
            GetComponent<SpriteRenderer>().color = SizedRuneTint[art - NUM_OF_POTIONS];
    }

    static readonly Color[] SizedRuneTint =
    {
        new Color(1f, 0.55f, 0.55f),    // 공격 — 더 붉게
        new Color(0.55f, 0.7f, 1f),     // 방어 — 더 푸르게
        new Color(0.55f, 1f, 0.55f),    // 체력 — 더 짙은 초록
    };

    /// <summary>맵 위 그림의 애니메이션 상태. 크기 룬(12~)은 그림이 따로 없어서 같은 능력치의
    /// 기본 룬(9 공격 · 10 방어 · 11 체력) 것을 튼다 — ItemAnimator 에 12~ 상태가 없고,
    /// 없는 상태를 틀면 경고만 찍히고 그림이 안 나온다(보이지 않는 룬이 길을 막는다).</summary>
    static int ArtId(int id)
    {
        Data.ConsumableItemData d;
        if (id < NUM_OF_POTIONS + 3 || Managers.Data.ConsumableItemDic.TryGetValue(id, out d) == false)
            return id;
        return d.AttackUp > 0 ? NUM_OF_POTIONS : d.DefenceUp > 0 ? NUM_OF_POTIONS + 1 : NUM_OF_POTIONS + 2;
    }

    public void PickUp()
    {
        if (!gameObject.activeInHierarchy) return;
        // Consume the pair together before presentation callbacks can run again.
        if (ChoicePartner != null)
        {
            Managers.Data.CItemActiveDic[ChoicePartner._itemIndex_forActive] = false;
            ChoicePartner.gameObject.SetActive(false);
        }
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

        // 물약이 실제로 채운 HP 와, 최대치에 막혀 버려진 HP. 물약이 아니면 둘 다 0.
        int healed = 0, overflow = 0;

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
            float before = Managers.Game.PlayerData.CurHP;
            Managers.Game.PlayerData.CurHP += heal;

            // Show Healing Font
            Transform ui_PlayerHpBar = Managers.UI.GetPlayerHpBar();
            Managers.Object.ShowPotionHealingFont(heal, ui_PlayerHpBar);

            if (Managers.Game.PlayerData.CurHP > Managers.Game.PlayerData.MaxHP)
                Managers.Game.PlayerData.CurHP = Managers.Game.PlayerData.MaxHP;

            healed = Mathf.Max(0, Mathf.RoundToInt(Managers.Game.PlayerData.CurHP - before));
            overflow = Mathf.RoundToInt(heal) - healed;

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

        GameEvents.RaiseItemPicked(id, healed, overflow);
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
