using System;
using UnityEngine;

namespace CharacterSystem
{
    public class CharacterStats : MonoBehaviour
    {
        public Color lightColor, hpColor;
        public CameraShake cameraShake;
        public int MaxHP;
        public int HP;

        public int MaxMP;
        public int MP;

        public event Action OnHealtUp, OnHealtDown, OnManaUp, OnManaDown;
        public event Action<int, int> HPChanged;
        public event Action<int, int> MPChanged;
        public event Action<string, Color> StatPopupRequested;
        public event Action OnDeath;

        private bool deathNotified;

        public void TakeDamage(int value)
        {
            ModifyHP(-value);
            PopUp("-" + value, hpColor);
            StartCoroutine(cameraShake.Shake(.15f, 1));
            OnHealtDown?.Invoke();
        }

        public void Heal(int value)
        {
            ModifyHP(value);
            PopUp("+" + value, hpColor);
            OnHealtUp?.Invoke();
        }

        public void AddMana(int value)
        {
            ModifyMP(value);
            PopUp("+" + value, lightColor);
            OnManaUp?.Invoke();
        }

        public void LoseMana(int value)
        {
            ModifyMP(-value);
            PopUp("-" + value, lightColor);
            OnManaDown?.Invoke();
        }

        public bool HasMana(int value)
        {
            return MP >= value;
        }
        public bool HasHealt(int value)
        {
            return HP >= value;
        }

        public void PopUp(string text, Color col)
        {
            StatPopupRequested?.Invoke(text, col);
        }

        private void ModifyHP(int delta)
        {
            int previousHP = HP;
            HP = Mathf.Clamp(HP + delta, 0, MaxHP);
            HPChanged?.Invoke(HP, MaxHP);

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
            MP = Mathf.Clamp(MP + delta, 0, MaxMP);
            MPChanged?.Invoke(MP, MaxMP);
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
