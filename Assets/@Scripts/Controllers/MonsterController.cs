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

    /// <summary>이 자리의 몬스터가 이미 죽었는가. 죽는 연출이 도는 동안 다시 걸리지 않게.</summary>
    bool _dead = false;

    public void MarkDead()
    {
        _dead = true;
    }
    public void SetMonster()
    {
        // 이미 죽어서 꺼진 몬스터로는 전투를 열 수 없다. 꺼진 오브젝트에서는
        // 코루틴이 시작되지 않는데, OnBattle 만 먼저 켜지면 전투는 열리지 않고
        // 그 플래그가 참인 채로 남아 플레이어가 영영 움직이지 못한다.
        if (gameObject.activeInHierarchy == false)
            return;

        // 방금 잡은 놈과는 다시 싸우지 않는다.
        //
        // 보통 몬스터는 죽으면 곧바로 꺼져서 위 검사에 걸리는데, 보스는
        // 죽고 나서도 연출(폭발 -> 하얗게 -> 빛)이 끝날 때까지 몇 초 켜져 있다.
        // 그 사이에 다시 부딪히면 같은 보스와 두 번 싸우고, 경험치도 보상도
        // 두 번 받았다 (60층/80층에서 실제로 났다).
        //
        // MonsterActiveDic 으로 검사하면 안 된다 - 1~4층의 프리팹 몬스터는
        // 그 인덱스를 나눠 쓰기 때문에, 하나가 죽으면 킹 슬라임까지 막힌다.
        if (_dead)
            return;

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
            // 전투마다 화면을 통째로 한 장(1080p 면 8MB) 찍는다. 지난 전투의 것을 버리지 않으면
            // 씬이 바뀔 때까지 쌓인다 — 100층 한 판이 전투 500번 남짓이다.
            Sprite old = Managers.Game._screenShot2;
            if (old != null)
            {
                Destroy(old.texture);
                Destroy(old);
            }
            Managers.Game._screenShot2 = screenShot;
            if (gameObject.GetComponent<BossMonsterController>() != null)
            {
                // 보스는 전투창을 바로 열지 않고 등장 연출을 거친다.
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
        // 볼륨이나 그 안의 효과가 없어도 전투창은 연다. 예전에는 여기서 널참조가 나면 OnBattle 만 켜진 채 전투창이
        // 안 열려 영영 굳었고, 색수차만 빠진 프로필이면 끝의 널참조로 렌즈 왜곡이 -1 인 채 남았다. 효과는 있으면 쓴다.
        Volume postProcessingVolume = Managers.Game.MainCamera != null ? Managers.Game.MainCamera.GetComponent<Volume>() : null;
        VolumeProfile profile = postProcessingVolume != null ? postProcessingVolume.profile : null;
        ChromaticAberration chromaticAberration = null;
        if (profile != null && profile.TryGet<ChromaticAberration>(out chromaticAberration))
        {
            chromaticAberration.intensity.value = 1;
        }

        LensDistortion lensDistortion = null;

        // 볼록 렌즈 효과
        float plusTime = 0.5f;
        if (profile != null && profile.TryGet<LensDistortion>(out lensDistortion))
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
        if (lensDistortion != null)
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

        if (chromaticAberration != null)
            chromaticAberration.intensity.value = 0;
        if (lensDistortion != null)
            lensDistortion.active = false;
    }
}
