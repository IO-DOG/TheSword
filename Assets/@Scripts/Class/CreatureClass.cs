using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static CreatureClass;
using static GameManager;

public static class EffectFactory
{
    public static IEffect GetEffect(CreatureData creatureData, UI_BaseCard baseCard = null)
    {
        if (creatureData.Class == Define.Class.Beast.ToString())
        {
            return new BeastEffect();
        }
        else if (creatureData.Class == Define.Class.Magic.ToString())
        {
            return new MagicEffect();
        }
        else if (creatureData.Class == Define.Class.Shield.ToString())
        {
            return new GuardianEffect(baseCard);
        }
        else if (creatureData.Class == Define.Class.Immortal.ToString())
        {
            return new ImmortalEffect();
        }
        else if (creatureData.Class == Define.Class.Knight.ToString())
        {
            return new KnightEffect();
        }
        else if (creatureData.Class == Define.Class.Titan.ToString())
        {
            return new TitanEffect();
        }
        else if (creatureData.Class == Define.Class.Assassin.ToString())
        {
            return new AssassinEffect();
        }
        else if (creatureData.Class == Define.Class.Armor.ToString())
        {
            return new KnightEffect();
        }
        else
        {
            return new DefaultAttackEffect();
        }
    }
}

public class CreatureClass : MonoBehaviour
{
    public interface IEffect
    {
        int ExecuteAttack(CreatureData attacker, CreatureData target);
        void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage);
    }

    public class BeastEffect : IEffect
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

    public class MagicEffect : IEffect
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

    public class GuardianEffect : IEffect
    {
        public GuardianEffect(UI_BaseCard uI_BaseCard)
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

    public class ImmortalEffect : IEffect
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

    public class KnightEffect : IEffect
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

    public class TitanEffect : IEffect
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
                attacker.effect.ExcuteOnHit(creature, attacker, Roar(creature, attacker));
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

    public class AssassinEffect : IEffect
    {
        bool flag = true;

        public void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage)
        {
            if (!attacker.ISCritical) damage = 0;
            else flag = false;

            creature.CurHP -= damage;
            if (creature.CurHP <= 0)
            {
                creature.CurHP = 0;
                Managers.Game.OnDeadMonsterAction[0].Invoke();
            }

            Managers.Game.OnHitMonsterAction[0].Invoke();
        }

        int IEffect.ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.ISCritical) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.ISCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }
    }

    public class ArmorEffect : IEffect
    {
        int shield = 10;

        public void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage)
        {
            shield -= damage;
            if (shield <= 0)
            {
                damage = -shield;
                shield = 0;
            }
            else
            {
                damage = 0;
            }

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

    // 기본 공격 효과 (특정 클래스가 아닐 경우)
    public class DefaultAttackEffect : IEffect
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
