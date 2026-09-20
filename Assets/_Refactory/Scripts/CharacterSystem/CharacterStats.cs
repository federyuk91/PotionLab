using System;
using UnityEngine;

namespace CharacterSystem
{
    public class CharacterStats : MonoBehaviour
    {
        public Color lightColor, hpColor;
        public int MaxHP;
        public int HP;

        public int MaxMP;
        public int MP;

        public event Action OnHealtUp, OnHealtDown, OnManaUp, OnManaDown;
        public event Action<int, int> HPChanged;
        public event Action<int, int> MPChanged;
        public event Action<string, Color> HPPopupRequested;
        public event Action<string, Color> MPPopupRequested;
        public event Action<int> DamageTaken;
        public event Action OnDeath;

        private bool deathNotified;
        private bool deferChangedEvents;

        public void TakeDamage(int value)
        {
            ModifyHP(-value);
            DamageTaken?.Invoke(value);
            OnHealtDown?.Invoke();
        }

        public void Heal(int value)
        {
            ModifyHP(value);
            OnHealtUp?.Invoke();
        }

        public void AddMana(int value)
        {
            ModifyMP(value);
            OnManaUp?.Invoke();
        }

        public void LoseMana(int value)
        {
            ModifyMP(-value);
            OnManaDown?.Invoke();
        }

        public void ModifyHPAndMP(int hpDelta, int mpDelta)
        {
            deferChangedEvents = true;

            try
            {
                ApplyHPDelta(hpDelta);
                ApplyMPDelta(mpDelta);
            }
            finally
            {
                deferChangedEvents = false;
            }

            HPChanged?.Invoke(HP, MaxHP);
            MPChanged?.Invoke(MP, MaxMP);
        }

        public bool HasMana(int value)
        {
            return MP >= value;
        }
        public bool HasHealt(int value)
        {
            return HP >= value;
        }

        private void RequestPopup(int delta, Color color, Action<string, Color> popupRequested)
        {
            if (delta == 0)
            {
                return;
            }

            string sign = delta > 0 ? "+" : string.Empty;
            popupRequested?.Invoke(sign + delta, color);
        }

        private void ModifyHP(int delta)
        {
            int previousHP = HP;
            HP = Mathf.Clamp(HP + delta, 0, MaxHP);
            RequestPopup(HP - previousHP, hpColor, HPPopupRequested);
            if (!deferChangedEvents)
            {
                HPChanged?.Invoke(HP, MaxHP);
            }

            if (HP > 0)
            {
                deathNotified = false;
                return;
            }

            if (previousHP > 0 && !deathNotified)
            {
                deathNotified = true;
                OnDeath?.Invoke();
            }
        }

        private void ModifyMP(int delta)
        {
            int previousMP = MP;
            MP = Mathf.Clamp(MP + delta, 0, MaxMP);
            RequestPopup(MP - previousMP, lightColor, MPPopupRequested);
            if (!deferChangedEvents)
            {
                MPChanged?.Invoke(MP, MaxMP);
            }
        }

        private void ApplyHPDelta(int delta)
        {
            if (delta > 0)
            {
                Heal(delta);
            }
            else if (delta < 0)
            {
                TakeDamage(-delta);
            }
        }

        private void ApplyMPDelta(int delta)
        {
            if (delta > 0)
            {
                AddMana(delta);
            }
            else if (delta < 0)
            {
                LoseMana(-delta);
            }
        }

        public void SetHP(int value)
        {
            if (value > HP)
            {
                Heal(value - HP);
            }
            else if (value < HP)
            {
                TakeDamage(HP - value);
            }
        }
        public void SetMP(int value)
        {
            if (value > MP)
            {
                AddMana(value - MP);
            }
            else if (value < MP)
            {
                LoseMana(MP - value);
            }
        }

    }
}
