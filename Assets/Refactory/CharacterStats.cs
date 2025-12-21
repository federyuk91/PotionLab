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

        public int lightLevel = 0, darkLevel = 0;


        public event Action OnHeal, OnLight;

        public void SetHP(int value)
        {
            HP = Mathf.Clamp(value, 0, MaxHP);
            hpPercentage = (float)HP / (float)MaxHP;
            healtFiller.fillAmount = hpPercentage;
            hpText.text = HP.ToString();
        }

        public void TakeDamage(int dmg)
        {
            HP = Mathf.Max(0, HP - dmg);
            hpPercentage = (float)HP / (float)MaxHP;
            healtFiller.fillAmount = hpPercentage;
            hpText.text = HP.ToString();
            StartCoroutine(cameraShake.Shake(.15f, 1));
            PopUp("-" + dmg.ToString(), hpColor);

        }

        public void Heal(int value)
        {
            HP = Mathf.Min(MaxHP, HP + value);
            hpPercentage = (float)HP / (float)MaxHP;
            healtFiller.fillAmount = hpPercentage;
            hpText.text = HP.ToString();
            OnHeal?.Invoke(); 
            PopUp("+" + value.ToString(), hpColor);
        }

        public void SetMP(int value)
        {
            MP = Mathf.Clamp(value, 0, MaxMP);
            mpPercentage = (float)MP / (float)MaxMP;
            manaFiller.fillAmount = mpPercentage;
            mpText.text = MP.ToString();
        }

        public void AddMana(int value)
        {
            if (MP == MaxMP)
            {
                lightLevel++;
            }
            else
            {
                lightLevel = 0;
            }
            MP = Mathf.Min(MaxMP, MP + value);
            mpPercentage = (float)MP / (float)MaxMP;
            manaFiller.fillAmount = mpPercentage;
            mpText.text = MP.ToString();
            OnLight?.Invoke();
            PopUp("+" + value.ToString(), lightColor);
        }
        public void LoseMana(int value)
        {
            if (MP == 0)
            {
                darkLevel++;
            }
            else
            {
                darkLevel = 0;
            }
            MP = Mathf.Max(0, MP - value);
            mpPercentage = (float)MP / (float)MaxMP;
            manaFiller.fillAmount = mpPercentage;
            mpText.text = MP.ToString();
            PopUp("-" + value.ToString(), lightColor);
        }

        public void PopUp(string text, Color col)
        {
            popUpStats.text = text;
            popUpStats.color = col;
            popUpStats.gameObject.SetActive(true);
        }

    }
}