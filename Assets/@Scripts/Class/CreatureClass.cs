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

            float ratio = target.CurHP / target.MaxHP;
            if (flag == false && ratio <= 0.1f)
            {
                flag = true;
                float heal = target.MaxHP * 0.4f;
                target.CurHP += heal;
            }

            target.OnHitAction.Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) damage *= (int)(attacker.CriticalAttack / 100);
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
            uI_BaseCard.FillDefenceGague();
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

            // todo
            /// add attack effect

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
            if (target.CurHP <= 0)
            {
                ExcuteOnDead(target);
            }

            if (hitCount == 5)
            {
                hitCount = 0;
                int roarDamage = Roar(target, attacker);
                attacker.Trait.ExcuteOnHit(target, attacker, roarDamage);

                //Vector3 pos = GetImage((int)Images.CreatureImage).gameObject.transform.position;
                //Managers.Object.ShowDamageFont(pos, damage, 0, attacker., attacker.IsCritical);
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

        public int Roar(CreatureData attacker, CreatureData target)
        {
            // 기획서 53쪽: 포효는 공격력의 20%. 감쇠 없이 전탄으로 들어가고 있었다.
            float num = (int)Mathf.Max(0, attacker.Attack) * 0.2f;
            if (attacker.IsCritical) num = num * (attacker.CriticalAttack / 100);
            int damage = Mathf.RoundToInt(num);
            damage -= (int)target.Defence;
            damage = (int)Mathf.Max(1, damage);
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 1;

            return damage;
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

    // 물약 특성
    public class PotionTrait : ITrait
    {
        public void ExcuteOnDead(CreatureData creature)
        {
            creature.CurHP = 0;
            // todo
            // 물약 생성해야 함

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
