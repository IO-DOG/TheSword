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
    public ConsumableItem ChoicePartner;
    LineRenderer _choiceLine;
    SpriteRenderer _sprite;
    Color _baseColor = Color.white;
    float _shownWaste = -1f;

    // 넘치는 몫이 전부일 때의 색. 흰 물약이 이 색까지 어두워진다.
    // 알파는 1 로 둔다 — 물약 셰이더(HalfSpriteShadow)는 알파 클립이라 정점 알파를
    // 읽지 않는다. 투명도로 흐리게 하려 하면 아무 일도 일어나지 않는다.
    static readonly Color OverflowColor = new Color(0.35f, 0.35f, 0.35f, 1f);

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
        GetComponent<Animator>().Play($"ConsumableItem_{id}");
        _sprite = GetComponent<SpriteRenderer>();
        _sprite.material = Managers.Resource.Load<Material>(Managers.Data.ConsumableItemDic[id].Shadow);
        _baseColor = _sprite.color;
    }

    public bool IsPotion => id >= NUM_OF_KEYS && id < NUM_OF_POTIONS;

    /// <summary>지금 MaxHP 로 이 물약이 채우는 양. 물약이 아니면 0.</summary>
    public static float PotionHeal(int id)
    {
        if (id < NUM_OF_KEYS || id >= NUM_OF_POTIONS)
            return 0f;
        if (Managers.Data.ConsumableItemDic.TryGetValue(id, out var data) == false)
            return 0f;
        return Mathf.Round(data.Heal * Managers.Game.PlayerData.MaxHP / 100);
    }

    /// <summary>지금 마시면 MaxHP 에 잘려 버려지는 양.
    ///
    /// 물약은 줍는 순간 마시고 넘친 몫은 영구히 사라진다. 원형 魔塔 는 HP 상한이
    /// 없지만 우리는 있으므로, 이것이 "언제 마시느냐" 를 질문으로 만드는 유일한 값이다.
    /// 그런데 아무 데도 보이지 않았다 — 가득 찬 채로 밟아도 회복 숫자는 온전히 떴다.</summary>
    public static float PotionWaste(int id)
    {
        float heal = PotionHeal(id);
        if (heal <= 0f)
            return 0f;
        var player = Managers.Game.PlayerData;
        return Mathf.Clamp(player.CurHP + heal - player.MaxHP, 0f, heal);
    }

    /// <summary>넘칠 물약을 밟기 전에 알린다 — 버려질 몫만큼 어두워진다.
    /// HP 는 전투마다 바뀌므로 매 프레임 보되, 색은 값이 바뀔 때만 쓴다.</summary>
    private void Update()
    {
        if (_sprite == null || IsPotion == false)
            return;

        float waste = PotionWaste(id);
        if (Mathf.Approximately(waste, _shownWaste))
            return;
        _shownWaste = waste;

        float heal = PotionHeal(id);
        float t = heal > 0f ? Mathf.Clamp01(waste / heal) : 0f;
        _sprite.color = Color.Lerp(_baseColor, _baseColor * OverflowColor, t);
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
            float heal = PotionHeal(id);
            float gained = heal - PotionWaste(id);
            Managers.Game.PlayerData.CurHP += heal;
            if (Managers.Game.PlayerData.CurHP > Managers.Game.PlayerData.MaxHP)
                Managers.Game.PlayerData.CurHP = Managers.Game.PlayerData.MaxHP;

            // 실제로 찬 만큼만 띄운다. 넘친 몫까지 띄우면 버린 것이 보이지 않는다.
            Transform ui_PlayerHpBar = Managers.UI.GetPlayerHpBar();
            Managers.Object.ShowPotionHealingFont(gained, ui_PlayerHpBar);

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
