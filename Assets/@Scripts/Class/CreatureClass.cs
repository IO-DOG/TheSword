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
        if (creatureData.Class == Define.Trait.Beast.ToString())
        {
            return new BeastTrait();
        }
        else if (creatureData.Class == Define.Trait.Magic.ToString())
        {
            return new MagicTrait();
        }
        else if (creatureData.Class == Define.Trait.Guardian.ToString())
        {
            return new GuardianTrait(baseCard);
        }
        else if (creatureData.Class == Define.Trait.Immortal.ToString())
        {
            return new ImmortalTrait();
        }
        else if (creatureData.Class == Define.Trait.Knight.ToString())
        {
            return new KnightTrait();
        }
        else if (creatureData.Class == Define.Trait.Titan.ToString())
        {
            return new TitanTrait();
        }
        else if (creatureData.Class == Define.Trait.Assassin.ToString())
        {
            return new AssassinTrait();
        }
        else if (creatureData.Class == Define.Trait.Armor.ToString())
        {
            return new ArmorTrait();
        }
        else
        {
            return new DefaultTrait();
        }
    }
}

public class CreatureClass : MonoBehaviour
{
    public interface ITrait
    {
        int ExecuteAttack(CreatureData attacker, CreatureData target);
        void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage);
    }

    public class BeastTrait : ITrait
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

    public class MagicTrait : ITrait
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

    public class GuardianTrait : ITrait
    {
        public GuardianTrait(UI_BaseCard uI_BaseCard)
        {
            Managers.Event.Unsubscribe(Define.GameEvent.FillDefenceGague, uI_BaseCard.FillDefenceGague);
            Managers.Event.Subscribe(Define.GameEvent.FillDefenceGague, uI_BaseCard.FillDefenceGague);
        }

        ~GuardianTrait()
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

    public class ImmortalTrait : ITrait
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

    public class KnightTrait : ITrait
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

    public class TitanTrait : ITrait
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
                attacker.Trait.ExcuteOnHit(creature, attacker, Roar(creature, attacker));
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

    public class AssassinTrait : ITrait
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

        int ITrait.ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.ISCritical) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.ISCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }
    }

    public class ArmorTrait : ITrait
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
    public class DefaultTrait : ITrait
    {
        public void ExcuteOnHit(CreatureData attacker, CreatureData creature, int damage)
        {
            damage = Mathf.Max(0, damage);
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
