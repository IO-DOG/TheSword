using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager
{
    public void ShowDamageFont(Vector2 pos, float damage, float healAmount, Transform parent, bool isCritical = false, bool isDefence = false)
    {
        string prefabName;
        if (isCritical)
            prefabName = "CriticalDamageFont";
        else if (isDefence)
            prefabName = "DefenceDamageFont";
        else
            prefabName = "DamageFont";

        GameObject ui_BattlePopup = GameObject.Find("UI_BattlePopup");
        if (ui_BattlePopup != null)
        {
            GameObject go = Managers.Resource.Instantiate(prefabName, ui_BattlePopup.transform);
            DamageFont damageText = go.GetOrAddComponent<DamageFont>();
            damageText.SetInfo(pos, damage, healAmount, parent, isCritical, isDefence);
        }
    }

    public void ShowPotionHealingFont(float healAmount, Transform parentUI)
    {
        if (parentUI != null)
        {
            GameObject go = Managers.Resource.Instantiate("PotionHealingFont", parentUI);
            DamageFont damageText = go.GetOrAddComponent<DamageFont>();
            damageText.SetPotionHealingInfo(healAmount, parentUI);
        }
    }
}
