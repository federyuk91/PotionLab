using System;
using UnityEngine;
using UnityEngine.UI;

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

        private float hpPercentage, mpPercentage;
        public Image healtFiller, manaFiller;
        public Text hpText, popUpStats, mpText;



        public event Action OnHeal, OnTakeDamage, OnLight, OnLoseLight;

        public void TakeDamage(int value)
        {
            ModifyHP(-value);
            PopUp("-" + value, hpColor);
            StartCoroutine(cameraShake.Shake(.15f, 1));
            OnTakeDamage?.Invoke();
        }

        public void Heal(int value)
        {
            ModifyHP(value);
            PopUp("+" + value, hpColor);
            OnHeal?.Invoke();
        }

        public void AddMana(int value)
        {
            ModifyMP(value);
            PopUp("+" + value, lightColor);
            OnLight?.Invoke();
        }

        public void LoseMana(int value)
        {
            ModifyMP(-value);
            PopUp("-" + value, lightColor);
            OnLoseLight?.Invoke();
        }

        public void PopUp(string text, Color col)
        {
            popUpStats.text = text;
            popUpStats.color = col;
            popUpStats.gameObject.SetActive(true);
        }

        private void ModifyHP(int delta)
        {
            HP = Mathf.Clamp(HP + delta, 0, MaxHP);
            healtFiller.fillAmount = (float)HP / MaxHP;
            hpText.text = HP.ToString();
        }

        private void ModifyMP(int delta)
        {
            MP = Mathf.Clamp(MP + delta, 0, MaxMP);
            manaFiller.fillAmount = (float)MP / MaxMP;
            mpText.text = MP.ToString();
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