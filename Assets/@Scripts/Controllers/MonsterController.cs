using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static GameManager;
using static Unity.VisualScripting.Member;

public class MonsterController : MonoBehaviour
{
    [HideInInspector]
    public int id = 0;
    [HideInInspector]
    public int _monsterIndex_forActive = 0;
    public void SetMonster()
    {
        Managers.Game.OnBattle = true;
        Managers.Game.Player.SetIdleState(Managers.Game.Player._moveDir);
        Managers.Game.MonsterData.Clear();

        Managers.Game.MonsterData.Add(new GameManager.CurMonsterData());

        int stageId = Managers.Game.PlayerData.CurStageid;

        Managers.Game.MonsterData[0].id = Managers.Data.MonsterDic[id].id;
        Managers.Game.MonsterData[0].Chapter = Managers.Data.MonsterDic[id].Chapter;
        Managers.Game.MonsterData[0].Ability = Managers.Data.MonsterDic[id].Ability;
        Managers.Game.MonsterData[0].Name = Managers.GetString(Managers.Data.MonsterDic[id].MonsterNameId);
        Managers.Game.MonsterData[0].MaxHP = Managers.Data.MonsterDic[id].MaxHP;
        Managers.Game.MonsterData[0].CurHP = Managers.Data.MonsterDic[id].MaxHP;
        
        Managers.Game.MonsterData[0].Attack = Managers.Data.StageInfoDic[stageId].ATK * Managers.Data.MonsterDic[id].Attack;
        Managers.Game.MonsterData[0].Defence = Managers.Data.StageInfoDic[stageId].DEF * Managers.Data.MonsterDic[id].Defence;

        Managers.Game.MonsterData[0].AttackSpeed = Managers.Data.MonsterDic[id].AttackSpeed;
        Managers.Game.MonsterData[0].DefenceSpeed = Managers.Data.MonsterDic[id].DefenceSpeed;
        Managers.Game.MonsterData[0].Critical = Managers.Data.MonsterDic[id].Critical;
        Managers.Game.MonsterData[0].CriticalAttack = Managers.Data.MonsterDic[id].CriticalAttack;
        Managers.Game.MonsterData[0].RewardExp = Managers.Data.StageInfoDic[stageId].EXP / (float)100 * Managers.Data.MonsterDic[id].RewardExp;
        Managers.Game.MonsterData[0].RewardItem = Managers.Data.MonsterDic[id].RewardItem;
        Managers.Game.MonsterData[0].IdleAnimStr = Managers.Data.MonsterDic[id].IdleAnimStr;
        Managers.Game.MonsterData[0].AttackAnimStr = Managers.Data.MonsterDic[id].AttackAnimStr;
        Managers.Game.MonsterData[0].BattleParticleAttack = Managers.Data.MonsterDic[id].BattleParticleAttack;
        Managers.Game.MonsterData[0].BattleParticleHit = Managers.Data.MonsterDic[id].BattleParticleHit;
        Managers.Game.MonsterData[0].MonsterNameId = Managers.Data.MonsterDic[id].MonsterNameId;
        Managers.Game.MonsterData[0].MonsterDescId = Managers.Data.MonsterDic[id].MonsterDescId;
        Managers.Game.MonsterData[0].IsDefence = false;
        Managers.Game.MonsterData[0].IsActiveIndex = _monsterIndex_forActive;
        //Managers.Game.MonsterData.Image = Managers.Data.MonsterDic[id].Image;

        Managers.Game.Monster = this;
        //Util.Screenshot((screenShot) => {Managers.Game._screenShot = screenShot; });
        StartCoroutine(Util.Screenshot2((screenShot) =>
        {
            Managers.Game._screenShot2 = screenShot;
            if (gameObject.GetComponent<BossMonsterController>() != null)
            {
                // todo
                // 보스면 연출 있다가 배틀로
                StartCoroutine(CoBossEnter());
            }
            else
            {
                Managers.UI.ShowPopupUI<UI_BattlePopup>();
            }
        }));
        //Util.Screenshot2((screenShot) => {Managers.Game._screenShot2 = screenShot; });
    }

    public void PromoteToBoss(Type bossType)
    {
        var bossController = gameObject.AddComponent(bossType) as BossMonsterController;
        bossController.id = id;

        DestroyImmediate(this);
    }

    protected virtual void Init()
    {
        GetComponent<Animator>().Play($"{Managers.Data.MonsterDic[id].IdleAnimStr}");
        GetComponent<SpriteRenderer>().material = Managers.Resource.Load<Material>(Managers.Data.MonsterDic[id].Shadow);
        //id = 1;
    }

    private void Start()
    {
        Init();
    }

    IEnumerator CoBossEnter()
    {
        yield return null;

        Managers.Sound.Play(Define.Sound.Effect, "BossBattleStart_Event");
        Volume postProcessingVolume = Managers.Game.MainCamera.GetComponent<Volume>();
        ChromaticAberration chromaticAberration;
        if (postProcessingVolume.profile.TryGet<ChromaticAberration>(out chromaticAberration))
        {
            chromaticAberration.intensity.value = 1;
        }

        LensDistortion lensDistortion;

        // 볼록 렌즈 효과
        float plusTime = 0.5f;
        if (postProcessingVolume.profile.TryGet<LensDistortion>(out lensDistortion))
        {
            lensDistortion.active = true;
            float originalIntensity = lensDistortion.intensity.value;
            float targetIntensity = 0.8f;
            float elapsedTime = 0f;
            while (elapsedTime < plusTime)
            {
                elapsedTime += Time.deltaTime;
                lensDistortion.intensity.value = Mathf.Lerp(originalIntensity, targetIntensity, elapsedTime / plusTime);
                yield return null;
            }

            lensDistortion.intensity.value = targetIntensity;
        }

        // 오목 렌즈 효과
        float minusTime = 0.1f;
        if (postProcessingVolume.profile.TryGet<LensDistortion>(out lensDistortion))
        {
            float originalIntensity = lensDistortion.intensity.value;
            float targetIntensity = -1f;
            float elapsedTime = 0f;
            while (elapsedTime < minusTime)
            {
                elapsedTime += Time.deltaTime;
                lensDistortion.intensity.value = Mathf.Lerp(originalIntensity, targetIntensity, elapsedTime / minusTime);
                yield return null;
            }

            lensDistortion.intensity.value = targetIntensity;
        }

        yield return new WaitForSeconds(0.05f);

        Managers.UI.ShowPopupUI<UI_BattlePopup>();

        chromaticAberration.intensity.value = 0;
        lensDistortion.active = false;
    }
}
