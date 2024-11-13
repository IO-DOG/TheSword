using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static CreatureClass;
using static GameManager;

public static class AttackEffectFactory
{
    public static IAttackEffect GetAttackEffect(CreatureData creatureData)
    {
        if (creatureData.Class == Define.Class.Beast.ToString())
        {

        }
        else if (creatureData.Class == Define.Class.Magic.ToString())
        {

        }
        else if (creatureData.Class == Define.Class.Shield.ToString())
        {

        }
        else if (creatureData.Class == Define.Class.Immortal.ToString())
        {

        }
        else if (creatureData.Class == Define.Class.Knight.ToString())
        {
            return new KnightAttackEffect();
        }
        else if (creatureData.Class == Define.Class.Titan.ToString())
        {

        }
        else if (creatureData.Class == Define.Class.Assassin.ToString())
        {
            return new AssassinAttackEffect();
        }
        else if (creatureData.Class == Define.Class.Armor.ToString())
        {

        }
        else
        {
            return new DefaultAttackEffect();
        }
    }
}



public class CreatureClass : MonoBehaviour
{
    public interface IAttackEffect
    {
        int ExecuteAttack(CreatureData attacker, CreatureData target);
    }

    public interface IOnHitEffect
    {
        void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage);
    }

    public class BeastEffect : IAttackEffect, IOnHitEffect
    {
        bool flag = false;

        public void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage)
        {
            creature.CurHP -= damage;
            if (creature.CurHP <= 0)
            {
                creature.CurHP = 0;
                Managers.Game.OnDeadMonsterAction[0].Invoke();
            }

            float ratio = creature.CurHP / creature.MaxHP;
            if (flag == false && ratio <= 0.1f)
            {
                flag = true;
                float heal = creature.MaxHP * 0.4f;
                creature.CurHP += heal;
            }

            Managers.Game.OnHitMonsterAction[0].Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.ISCritical) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.ISCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }
    }

    public class MagicEffect : IAttackEffect, IOnHitEffect
    {
        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            attacker.ISCritical = true;

            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.ISCritical) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.ISCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage)
        {
            creature.CurHP -= damage;
            if (creature.CurHP <= 0)
            {
                creature.CurHP = 0;
                Managers.Game.OnDeadMonsterAction[0].Invoke();
            }

            Managers.Game.OnHitMonsterAction[0].Invoke();
        }
    }

    public class GuardianEffect : IAttackEffect, IOnHitEffect
    {
        GuardianEffect(UI_BaseCard uI_BaseCard)
        {
            Managers.Event.Unsubscribe(Define.GameEvent.FillDefenceGague, uI_BaseCard.FillDefenceGague);
            Managers.Event.Subscribe(Define.GameEvent.FillDefenceGague, uI_BaseCard.FillDefenceGague);
        }

        ~GuardianEffect()
        {
            Managers.Event.DeleteEvent(Define.GameEvent.FillDefenceGague);
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage)
        {
            creature.CurHP -= damage;
            if (creature.CurHP <= 0)
            {
                creature.CurHP = 0;
                Managers.Game.OnDeadMonsterAction[0].Invoke();
            }

            Managers.Game.OnHitMonsterAction[0].Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.ISCritical) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.ISCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }
    }

    public class ImmortalEffect : IAttackEffect, IOnHitEffect
    {
        public void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage)
        {
            if (!attacker.ISCritical) damage = (int)(damage * 0.2f);

            creature.CurHP -= damage;
            if (creature.CurHP <= 0)
            {
                creature.CurHP = 0;
                Managers.Game.OnDeadMonsterAction[0].Invoke();
            }

            Managers.Game.OnHitMonsterAction[0].Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.ISCritical) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.ISCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }
    }

    public class KnightEffect : IAttackEffect, IOnHitEffect
    {
        public void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage)
        {
            creature.CurHP -= damage;
            if (creature.CurHP <= 0)
            {
                creature.CurHP = 0;
                Managers.Game.OnDeadMonsterAction[0].Invoke();
            }

            Managers.Game.OnHitMonsterAction[0].Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.ISCritical) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.ISCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            // todo
            /// add attack effect

            return damage;
        }
    }

    public class TitanEffect : IAttackEffect, IOnHitEffect
    {
        int hitCount = 0;

        public void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage)
        {
            hitCount++;

            creature.CurHP -= damage;
            if (creature.CurHP <= 0)
            {
                creature.CurHP = 0;
                Managers.Game.OnDeadMonsterAction[0].Invoke();
            }

            if (hitCount == 5)
            {
                hitCount = 0;
                Roar(creature, attacker);
            }

            Managers.Game.OnHitMonsterAction[0].Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.ISCritical) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.ISCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }

        public int Roar(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack * 0.2f);
            if (attacker.ISCritical) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.ISCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }
    }

    public class AssassinEffect : IAttackEffect, IOnHitEffect
    {
        public void ExecuteAttack(CreatureData attacker, CreatureData target)
        {

            // 추가적인 치명타 효과
            damage *= 2; // 예: 데미지 2배
            attacker._forAssassin = false;
            Debug.Log("암살 효과 발동! 데미지가 2배로 증가했습니다.");


            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                target.CurHP = 0;
                Managers.Game.OnDeadMonsterAction[0].Invoke();
            }

            Managers.Game.OnHitMonsterAction[0].Invoke();
        }
    }

    // 기본 공격 효과 (특정 클래스가 아닐 경우)
    public class DefaultAttackEffect : IAttackEffect, IOnHitEffect
    {
        public void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage)
        {
            creature.CurHP -= damage;
            if (creature.CurHP <= 0)
            {
                creature.CurHP = 0;
                Managers.Game.OnDeadMonsterAction[0].Invoke();
            }

            Managers.Game.OnHitMonsterAction[0].Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.ISCritical) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.ISCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }
    }

}
