using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static CreatureClass;
using static GameManager;

public static class EffectFactory
{
    public static ITrait GetTrait(CreatureData creatureData, UI_BaseCard baseCard = null)
    {
        switch (creatureData.Ability)
        {
            case (int)Define.Trait.Beast:
                return new BeastTrait();
            case (int)Define.Trait.Magic:
                return new MagicTrait();
            case (int)Define.Trait.Guardian:
                return new GuardianTrait(baseCard);
            case (int)Define.Trait.Immortal:
                return new ImmortalTrait();
            case (int)Define.Trait.Knight:
                return new KnightTrait();
            case (int)Define.Trait.Titan:
                return new TitanTrait();
            case (int)Define.Trait.Assassin:
                return new AssassinTrait();
            case (int)Define.Trait.Armor:
                return new ArmorTrait();
            case (int)Define.Trait.KingSlime:
                return new SplitTrait();
            default:
                return new DefaultTrait();
        };
    }
}

public class CreatureClass : MonoBehaviour
{
    public interface ITrait
    {
        int ExecuteAttack(CreatureData attacker, CreatureData target);
        void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage);
        void ExcuteOnDead(CreatureData creature);
    }

    public class BeastTrait : ITrait
    {
        bool flag = false;

        public void ExcuteOnDead(CreatureData creature)
        {
            creature.CurHP = 0;
            creature.OnDeadAction.Invoke();
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            damage = Mathf.Max(0, damage);
            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                ExcuteOnDead(target);
            }

            // 살아 있을 때만 광폭한다 (thesword_balance 도 hp > 0 을 본다).
            // 죽은 뒤에 40% 가 차서 쓰러진 야수의 체력 막대가 도로 차올랐다.
            float ratio = target.CurHP / target.MaxHP;
            if (flag == false && target.CurHP > 0 && ratio <= 0.1f)
            {
                flag = true;
                float heal = target.MaxHP * 0.4f;
                target.CurHP += heal;
            }

            target.OnHitAction.Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            // 치명 배율은 다른 특성처럼 실수로 곱한다. (int) 로 자르면 치명공격력 250 이 2배,
            // 199 가 1배였다 — 야수의 치명타가 사실상 없었다.
            float num = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) num = num * (attacker.CriticalAttack / 100);
            int damage = Mathf.RoundToInt(num);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }
    }

    public class MagicTrait : ITrait
    {
        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            attacker.IsCritical = true;
            float num = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) num = num * (attacker.CriticalAttack / 100);
            int damage = Mathf.RoundToInt(num);
            damage -= (int)target.Defence;
            damage = (int)Mathf.Max(1, damage);
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 1;

            return damage;
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                ExcuteOnDead(target);
            }

            target.OnHitAction.Invoke();
        }

        public void ExcuteOnDead(CreatureData creature)
        {
            creature.CurHP = 0;
            creature.OnDeadAction.Invoke();
        }
    }

    public class GuardianTrait : ITrait
    {
        public System.Action OnGuardianAction;

        public GuardianTrait(UI_BaseCard uI_BaseCard)
        {
            Debug.Log(uI_BaseCard);
            //OnGuardianAction -= uI_BaseCard.FillDefenceGague;
            //OnGuardianAction += uI_BaseCard.FillDefenceGague;
            //Managers.Event.Unsubscribe(Define.GameEvent.FillDefenceGague, uI_BaseCard.FillDefenceGague);
            //Managers.Event.Subscribe(Define.GameEvent.FillDefenceGague, uI_BaseCard.FillDefenceGague);
            if (uI_BaseCard != null) uI_BaseCard.FillDefenceGague();
        }

        ~GuardianTrait()
        {
            //Managers.Event.DeleteEvent(Define.GameEvent.FillDefenceGague);
        }

        public void ExcuteOnDead(CreatureData creature)
        {
            creature.CurHP = 0;
            creature.OnDeadAction.Invoke();
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                ExcuteOnDead(target);
            }

            target.OnHitAction.Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            float num = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) num = num * (attacker.CriticalAttack / 100);
            int damage = Mathf.RoundToInt(num);
            damage -= (int)target.Defence;
            damage = (int)Mathf.Max(1, damage);
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 1;

            return damage;
        }
    }

    public class ImmortalTrait : ITrait
    {
        public void ExcuteOnDead(CreatureData creature)
        {
            creature.CurHP = 0;
            creature.OnDeadAction.Invoke();
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            if (!attacker.IsCritical) damage = (int)(damage * 0.2f);

            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                ExcuteOnDead(target);
            }

            target.OnHitAction.Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            float num = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) num = num * (attacker.CriticalAttack / 100);
            int damage = Mathf.RoundToInt(num);
            damage -= (int)target.Defence;
            damage = (int)Mathf.Max(1, damage);
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 1;

            return damage;
        }
    }

    public class KnightTrait : ITrait
    {
        public void ExcuteOnDead(CreatureData creature)
        {
            creature.CurHP = 0;
            creature.OnDeadAction.Invoke();
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                ExcuteOnDead(target);
            }

            target.OnHitAction.Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            float num = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) num = num * (attacker.CriticalAttack / 100);
            int damage = Mathf.RoundToInt(num);
            damage -= (int)target.Defence;
            damage = (int)Mathf.Max(1, damage);
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 1;

            return damage;
        }
    }

    public class TitanTrait : ITrait
    {
        int hitCount = 0;

        public void ExcuteOnDead(CreatureData creature)
        {
            creature.CurHP = 0;
            creature.OnDeadAction.Invoke();
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            hitCount++;

            target.CurHP -= damage;

            // 5번째 대에 쓰러져도 포효한다 (thesword_balance.apply_hit 과 같다).
            // 쓰러짐 처리보다 먼저 한다. 쓰러짐(UI_MonsterCard.Dead)이 경험치를 주고 레벨업이 플레이어의 HP·방어를
            // 올리는데, 그 뒤에 포효하면 오른 방어로 깎고 늘어난 HP 에 넣어 실제 전투가 예측(BattleForecast)·
            // 시뮬레이터보다 덜 아팠다 — 같이 쓰러지는 싸움을 이긴 적도 있다.
            if (hitCount == 5)
            {
                hitCount = 0;
                int roarDamage = Roar(target, attacker);
                if (roarDamage > 0)
                    attacker.Trait.ExcuteOnHit(target, attacker, roarDamage);
            }

            if (target.CurHP <= 0)
            {
                ExcuteOnDead(target);
            }

            target.OnHitAction.Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            float num = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) num = num * (attacker.CriticalAttack / 100);
            int damage = Mathf.RoundToInt(num);
            damage -= (int)target.Defence;
            damage = (int)Mathf.Max(1, damage);
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 1;

            return damage;
        }

        /// <summary>
        /// 기획서 53쪽: 포효는 공격력의 20%. 명세는 thesword_balance.apply_hit —
        /// 치명 아닌 보통 한 대의 20%: round(0.2 * max(1, ATK - DEF)), 상대가 방어 중이면 1 의 20% = 0.
        /// 예전에는 공격력의 20% 에서 방어력을 빼서, 층 설계 레벨에서는 거대 41마리가 전부 1 이었다.
        /// </summary>
        public int Roar(CreatureData attacker, CreatureData target)
        {
            int blow = Mathf.Max(1, (int)Mathf.Max(0, attacker.Attack) - (int)target.Defence);
            if (target.IsDefence) blow = 1;

            return Mathf.RoundToInt(blow * 0.2f);
        }
    }

    public class AssassinTrait : ITrait
    {
        bool flag = true;

        public void ExcuteOnDead(CreatureData creature)
        {
            creature.CurHP = 0;
            creature.OnDeadAction.Invoke();
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            // 기획서 53쪽: 일반 공격은 회피하지만, 치명 공격을 맞으면 은신이 풀린다.
            // flag 를 내려놓고 쓰지 않아서, 은신이 영영 풀리지 않고 있었다 —
            // 치명 주기가 긴 후반에는 사실상 못 죽이는 상대가 된다.
            if (flag && !attacker.IsCritical) damage = 0;
            else if (attacker.IsCritical) flag = false;

            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                ExcuteOnDead(target);
            }

            target.OnHitAction.Invoke();
        }

        int ITrait.ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            float num = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) num = num * (attacker.CriticalAttack / 100);
            int damage = Mathf.RoundToInt(num);
            damage -= (int)target.Defence;
            damage = (int)Mathf.Max(1, damage);
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 1;

            return damage;
        }
    }

    public class ArmorTrait : ITrait
    {
        // 기획서 53쪽: 피격 시 방어 게이지가 깎이며 모든 공격을 흡수하고,
        // 게이지가 다 사라지면 껍질이 해제된다.
        // 10 으로 고정돼 있어서 후반에는 첫 공격에 그대로 뚫렸다 — 게이지가
        // 체력에 비례해야 층이 올라가도 "껍질을 깨는" 상대로 남는다.
        const float SHIELD_RATIO = 0.3f;
        float shield = -1f;

        public void ExcuteOnDead(CreatureData creature)
        {
            creature.CurHP = 0;
            creature.OnDeadAction.Invoke();
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            if (shield < 0f)
                shield = target.MaxHP * SHIELD_RATIO;

            shield -= damage;
            if (shield <= 0f)
            {
                damage = (int)(-shield);
                shield = 0f;
            }
            else
            {
                damage = 0;
            }

            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                ExcuteOnDead(target);
            }

            target.OnHitAction.Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            float num = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) num = num * (attacker.CriticalAttack / 100);
            int damage = Mathf.RoundToInt(num);
            damage -= (int)target.Defence;
            damage = (int)Mathf.Max(1, damage);
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 1;

            return damage;
        }
    }

    // 분열 특성
    public class SplitTrait : ITrait
    {
        public void ExcuteOnDead(CreatureData creature)
        {
            creature.CurHP = 0;
            creature.OnDeadAction.Invoke();
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            damage = Mathf.Max(0, damage);
            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                ExcuteOnDead(target);
            }

            target.OnHitAction.Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            float num = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) num = num * (attacker.CriticalAttack / 100);
            int damage = Mathf.RoundToInt(num);
            damage -= (int)target.Defence;
            damage = (int)Mathf.Max(1, damage);
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 1;

            return damage;
        }

        //public void ExcuteOnDead(CreatureData creature)
        //{
        //    Transform boss = Managers.Game.GetBoss().gameObject.transform;
        //    creature.CurHP = 0;
        //    Vector3 pos = boss.localPosition;
        //    float size = Define.TILE_SIZE;

        //    int[] dx = { -1, 0, 1/*, 1, 1, 0, -1, -1*/ };
        //    int[] dy = { 1, 2, 1/*, 0, -1, -1, -1, 0*/ };
        //    bool[] ch = { false, false, false, false, false, false, false, false };
        //    List<int> s = new List<int>();
        //    int cnt = 0;

        //    while (cnt < 3)
        //    {
        //        int randValue = UnityEngine.Random.Range(0, dx.Length);
        //        if (ch[randValue] == false)
        //        {
        //            cnt++;
        //            ch[randValue] = true;
        //            s.Add(randValue);
        //        }
        //    }

        //    for (int i = 0; i < s.Count; i++)
        //    {
        //        int idx = s[i];
        //        Vector3 vector = new Vector3(pos.x + dx[idx] * size * 4, 2f, pos.z + dy[idx] * size * 4);
        //        Debug.Log($"vector : {vector.x}, {vector.y}, {vector.z}");

        //        GameObject monster = Managers.Resource.Instantiate("Monster", boss.parent);
        //        monster.GetOrAddComponent<MonsterController>().id = 6 + i;
        //        monster.GetComponent<BoxCollider>().size = new Vector3(1.5f, 2f, 0.2f);
        //        monster.transform.localPosition += vector;
        //        monster.transform.localScale = new Vector3(1, 2, 1);
        //        monster.name = $"KingSlimeSplitMonster";
        //    }
        //    creature.OnDeadAction.Invoke();
        //}
    }

    // 기본 공격 효과 (특정 클래스가 아닐 경우)
    public class DefaultTrait : ITrait
    {
        public void ExcuteOnDead(CreatureData creature)
        {
            creature.CurHP = 0;
            creature.OnDeadAction.Invoke();
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            damage = Mathf.Max(0, damage);
            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                ExcuteOnDead(target);
            }

            target.OnHitAction.Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            float num = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) num = num * (attacker.CriticalAttack / 100);
            int damage = Mathf.RoundToInt(num);
            damage -= (int)target.Defence;
            damage = (int)Mathf.Max(1, damage);
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 1;

            return damage;
        }
    }

}
