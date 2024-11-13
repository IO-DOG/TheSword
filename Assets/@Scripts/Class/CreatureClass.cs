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

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            damage = Mathf.Max(0, damage);
            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                target.CurHP = 0;
                target.OnDeadAction.Invoke();
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
            Debug.Log($"attacker.Attack : {attacker.Attack}");
            int damage = (int)Mathf.Max(0, attacker.Attack);
            Debug.Log($"CriticalAttack : {attacker.CriticalAttack} , {(int)(attacker.CriticalAttack / 100)}");
            Debug.Log($"damage : {damage}");
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
            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical == true) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                target.CurHP = 0;
                target.OnDeadAction.Invoke();
            }

            target.OnHitAction.Invoke();
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

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                target.CurHP = 0;
                target.OnDeadAction.Invoke();
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

    public class ImmortalTrait : ITrait
    {
        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            if (!attacker.IsCritical) damage = (int)(damage * 0.2f);

            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                target.CurHP = 0;
                target.OnDeadAction.Invoke();
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

    public class KnightTrait : ITrait
    {
        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                target.CurHP = 0;
                target.OnDeadAction.Invoke();
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

            // todo
            /// add attack effect

            return damage;
        }
    }

    public class TitanTrait : ITrait
    {
        int hitCount = 0;

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            hitCount++;

            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                target.CurHP = 0;
                target.OnDeadAction.Invoke();
            }

            if (hitCount == 5)
            {
                hitCount = 0;
                attacker.Trait.ExcuteOnHit(target, attacker, Roar(target, attacker));
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

        public int Roar(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack * 0.2f);
            if (attacker.IsCritical) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }
    }

    public class AssassinTrait : ITrait
    {
        bool flag = true;

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            if (!attacker.IsCritical) damage = 0;
            else flag = false;

            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                target.CurHP = 0;
                target.OnDeadAction.Invoke();
            }

            target.OnHitAction.Invoke();
        }

        int ITrait.ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) damage *= (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }
    }

    public class ArmorTrait : ITrait
    {
        int shield = 10;

        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
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

            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                target.CurHP = 0;
                target.OnDeadAction.Invoke();
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

    // 기본 공격 효과 (특정 클래스가 아닐 경우)
    public class DefaultTrait : ITrait
    {
        public void ExcuteOnHit(CreatureData attacker, CreatureData target, int damage)
        {
            damage = Mathf.Max(0, damage);
            target.CurHP -= damage;
            if (target.CurHP <= 0)
            {
                target.CurHP = 0;
                target.OnDeadAction.Invoke();
            }

            target.OnHitAction.Invoke();
        }

        public int ExecuteAttack(CreatureData attacker, CreatureData target)
        {
            int damage = (int)Mathf.Max(0, attacker.Attack);
            if (attacker.IsCritical) damage = damage * (int)(attacker.CriticalAttack / 100);
            damage -= (int)target.Defence;
            damage = (int)Mathf.Max(0, damage);
            if (target.IsDefence && attacker.IsCritical) damage = (int)(damage * 0.25f);
            else if (target.IsDefence) damage = 0;

            return damage;
        }
    }

}
