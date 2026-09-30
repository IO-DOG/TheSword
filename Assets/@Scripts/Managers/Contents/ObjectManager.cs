using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class ObjectManager
{
    // 대역 소리 (Assets/@Resources/Sounds, 어드레서블 PreLoad). 레벨 업 징글·룬 차임이 새로 들어오면 키만 바꾼다
    // (기획 §8: 레벨 업 1.2초, 룬 0.8초 이하). 둘 다 지금 판에서 따로 들리는 곳이 없다 — HeroReady 는 1층 도입에서
    // 한 번, 축복의 물약(ConsumableItem 8번, FX_Potion_B)은 어느 층에도 놓이지 않는다.
    public const string LevelUpSound = "HeroReady_SFX";   // 불기둥 프리팹(LevelUp)이 없을 때만 — 프리팹이 제 소리를 낸다(CoLevelUp)
    public const string RuneSound = "ItemGet_BlessPotion_SFX";

    static readonly Color HealColor = new Color32(78, 238, 111, 255);    // DamageFont 의 회복 숫자와 같은 초록
    static readonly Color AtkColor = new Color32(255, 140, 110, 255);    // 룬 그림(FX_RunStone_Red/Blue/Green)의 색
    static readonly Color DefColor = new Color32(130, 200, 255, 255);
    static readonly Color HpColor = new Color32(130, 255, 150, 255);
    static readonly Color MixColor = new Color32(255, 216, 74, 255);     // 둘 이상 오르면 레벨 업 표시(★)와 같은 금색

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

    /// <summary>
    /// 물약의 회복 숫자. 최대 체력에 막혀 버려진 몫이 있으면 "40 (넘침 80)" 으로 그것까지 적는다 — 물약은 층의
    /// 예산이라 넘긴 만큼이 손해다. 숫자는 넘치지 않을 때(PotionHealingFont 의 "120")처럼 부호 없이, 괄호는 툴팁
    /// (ForecastUI.HealOverflow) 과 같은 말로. 체력 막대(UI_PlayerHPBar)를 못 찾으면 건너뛴다. 예전에는 UIManager.GetPlayerHpBar 가
    /// 거기서 널참조를 내고 줍기가 도중에 끊겼다(ConsumableItem.PickUp 의 알림·HUD 갱신까지).
    /// 이 글과 ShowRuneGain 은 줍기 한가운데서 불린다. 번역문의 자리표시가 어긋나면 string.Format 이 던지므로 예외는 여기서
    /// 삼킨다 — 떠오르는 글 하나가 줍기(와 그것을 부른 PlayerController 의 한 걸음)를 끊으면 안 된다.
    /// </summary>
    public void ShowHealing(float heal, int healed, int overflow)
    {
        try
        {
            if (overflow > 0)
            {
                string wasted = string.Format(Managers.GetString(ForecastUI.Wasted), overflow);
                ShowPlayerText($"{healed} <color=#FFA040>{wasted}</color>", HealColor);
                return;
            }
            GameObject bar = GameObject.Find("UI_PlayerHPBar");
            if (bar != null)
                ShowPotionHealingFont(heal, bar.transform);
        }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    /// <summary>
    /// 룬: 오른 능력치를 머리 위에 띄우고 차임을 낸다. 양은 표의 값(AttackUp·DefenceUp·HPUp) 그대로라 크기가 다른
    /// 룬도 같은 길로 온다. 예전에는 소리도 글자도 없어서 무엇이 올랐는지 HUD 숫자를 견줘야 알았다. 예외는 ShowHealing 처럼 삼킨다.
    /// </summary>
    public void ShowRuneGain(float attack, float defence, float hp)
    {
        try
        {
            List<string> parts = new List<string>(3);
            if (attack > 0f)
                parts.Add(string.Format(Managers.GetString(ForecastUI.AtkUp), Mathf.RoundToInt(attack)));
            if (defence > 0f)
                parts.Add(string.Format(Managers.GetString(ForecastUI.DefUp), Mathf.RoundToInt(defence)));
            if (hp > 0f)
                parts.Add(string.Format(Managers.GetString(ForecastUI.MaxHpUp), Mathf.RoundToInt(hp)));
            if (parts.Count == 0)
                return;

            Color color = parts.Count > 1 ? MixColor : attack > 0f ? AtkColor : defence > 0f ? DefColor : HpColor;
            ShowPlayerText(string.Join("  ", parts), color);
            Managers.Sound.Play(Define.Sound.Effect, RuneSound);
        }
        catch (System.Exception e) { Debug.LogException(e); }
    }

    /// <summary>
    /// 플레이어 머리 위(체력 막대)에서 떠올라 옅어지는 한 줄. 물약의 회복 숫자(PotionHealingFont)와 같은 자리·글꼴·
    /// 움직임(DamageFont.DoPotionHealingAnimation)이고 글과 색만 다르다. 막대나 프리팹이 없으면 조용히 건너뛴다 —
    /// 글자 하나 때문에 줍기가 끊기면 안 된다.
    /// </summary>
    public void ShowPlayerText(string text, Color color)
    {
        GameObject bar = GameObject.Find("UI_PlayerHPBar");
        GameObject go = bar != null ? Managers.Resource.Instantiate("PotionHealingFont", bar.transform) : null;
        TMP_Text label = go != null ? go.GetComponent<TMP_Text>() : null;
        if (label == null)
            return;

        label.text = text;
        label.color = color;
        DOTween.Sequence().SetLink(go)
            .Append(label.rectTransform.DOLocalMoveY(25f, 1f))
            .Join(label.DOFade(0f, 1f))
            .OnComplete(() => Managers.Resource.Destroy(go));
    }

    /// <summary>
    /// 레벨 업 불기둥과 소리. 경험치는 전투창이 떠 있는 동안(UI_MonsterCard.Dead) 들어와서, 예전에는 불기둥이 전투창
    /// (찍어 둔 화면) 밑의 맵에서 터져 아무도 못 봤다 — 전투 중이면 창이 닫힐 때까지 기다렸다가 맵 위에서 튼다.
    /// 전투 밖(치트 등)이면 바로. 한 번에 여러 레벨이 올라도 한 번이다(CurExp 세터가 한 번 부른다).
    /// </summary>
    public void ShowLevelUp()
    {
        CoroutineManager.StartCoroutine(CoLevelUp(Managers.Game.PlayerData.Level));
    }

    IEnumerator CoLevelUp(int level)
    {
        while (Managers.Game.OnBattle)
            yield return null;

        // 같은 교환에서 같이 쓰러졌으면(진 전투 — 체크포인트가 레벨을 되돌린다), 전투 도중 메뉴로 체크포인트에
        // 돌아갔으면(레벨이 되돌아가 있다) 없던 일이다.
        PlayerController player = Managers.Game.Player;
        if (player == null || Managers.Game.IsPlayerDead || Managers.Game.PlayerData.Level < level)
            yield break;

        // 불기둥 프리팹 안의 "Sweet" 가 제 소리(retro_great, 효과음 볼륨을 따른다)를 낸다 — 여기서 또 틀면 두 소리가 겹쳤다.
        // 대역 소리는 프리팹이 없을 때만. 파티클이 멈춰도 오브젝트는 남아(stopAction 없음) 레벨마다 플레이어 밑에 쌓였다 —
        // 가장 긴 파티클(2.1초)과 소리(1초)가 끝난 뒤 치운다.
        GameObject fx = Managers.Resource.Instantiate("LevelUp", player.transform);
        if (fx != null)
            Managers.Resource.Destroy(fx, 3f);
        else
            Managers.Sound.Play(Define.Sound.Effect, LevelUpSound);
    }
}
