using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class CharacterController : MonoBehaviour
{

    public int staringHP = 10;
    public int currentHP = 10;

    public float drinkingTime = 1f;

    public Animator mageAnimator;
    public Image hpBar;

    public GameObject burningFx, wetFx, iceFx, grassFx;//, treeFx;

    public AudioSource audioSource;

    public enum Status
    {
        burned,
        freezed,
        wet,
        grass,
        tree,
        balrog,
        none,
    }

    public Status newStatus;
    public List<Status> currentStatus = new List<Status>();
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void Update()
    {
        hpBar.fillAmount = (float)currentHP / (float)staringHP;
    }

    public void Drunk(PotionScript potion)
    {
        mageAnimator.SetBool("isDrinking", true);
        StartCoroutine(ApplyEffect(potion));
    }

    public IEnumerator ApplyEffect(PotionScript potion)
    {

        PotionScriptable ps = potion.potion;
        yield return new WaitForSeconds(drinkingTime);
        mageAnimator.SetBool("isDrinking", false);

        if (currentStatus.Contains(Status.tree))
        {
            StatusOnTree(ps);

        }
        else
        {
            //Effetto pozione
            switch (ps.effectType)
            {
                case PotionScriptable.EffectType.lava:
                    GetLava(ps);
                    break;
                case PotionScriptable.EffectType.damage:
                    mageAnimator.SetTrigger("isDamaged");
                    TakeDamage(ps.baseValue);
                    if (ps.dialogs.Count > 0)
                        GameMan.Instance.PickADialog(ps.dialogs, 3f);

                    break;
                case PotionScriptable.EffectType.healing:
                    GetHealth(ps.baseValue);
                    if (ps.dialogs.Count > 0)
                        GameMan.Instance.PickADialog(ps.dialogs, 3f);//"I don't digest the lava before going to sleep"

                    break;
                case PotionScriptable.EffectType.burned:
                    StartCoroutine(GetBurn(ps));
                    break;
                case PotionScriptable.EffectType.freezed:
                    GetFreezed(ps);
                    break;
                case PotionScriptable.EffectType.grass:
                    GrowingGrass(ps);
                    break;
                case PotionScriptable.EffectType.wet:
                    GetWet(ps);
                    break;
                case PotionScriptable.EffectType.none:
                    Debug.Log("None");
                    break;

            }
        }
        if (currentHP > 0)
            GameMan.Instance.RemovePotion(potion);

    }
    //Effetti

    private void GetLava(PotionScriptable ps)
    {
        if (currentStatus.Contains(Status.burned))
        {
            //Balrog
            mageAnimator.SetTrigger("becomeABalrog");
            currentStatus.Remove(Status.burned);
            AddStatus(Status.balrog);
            GameMan.Instance.OnCharacterDie();
            GameMan.Instance.PopDialog(ps.burned);
            PlayClip(ps.burned_audio);

            AchievementManager.instance.Achive("Udun flame");
        }
        else
        {
            mageAnimator.SetTrigger("lavaDrunked");
            TakeDamage(ps.baseValue);
            if (ps.dialogs.Count > 0)
                GameMan.Instance.PickADialog(ps.dialogs, 3f);
        }
    }

    //done
    private void GetWet(PotionScriptable ps)
    {
        if (currentStatus.Contains(Status.burned))
        {
            burningFx.SetActive(false);
            currentStatus.Remove(Status.burned);
            GameMan.Instance.PopDialog(ps.burned, 3.5f);

            PlayClip(ps.burned_audio);
            if (currentStatus.Contains(Status.tree))
            {
                currentStatus.Remove(Status.tree);
                mageAnimator.SetTrigger("becomeMage");
            }
            else
            {
                mageAnimator.SetTrigger("smoking");
            }
        }
        else if (currentStatus.Contains(Status.grass))
        {
            GameMan.Instance.PopDialog(ps.grass, 3.5f);
            Debug.Log("L'erba sul corpo del mago cresce e diventa un albero");
            grassFx.SetActive(false);
            currentStatus.Remove(Status.grass);

            PlayClip(ps.grass_audio);
            AddStatus(Status.tree);
            mageAnimator.SetTrigger("becomeATree");
            //Il vecchio tobia->Trasforma il mago in albero, brucialo e lascialo fumante
            /*if (GameMan.Instance.levelPotions.Count == 0)
            {
                GameMan.Instance.OnCharacterDie();
            }*/


        }
        else
        {
            AddStatus(Status.wet);
            wetFx.SetActive(true);
        }
    }

    private void GrowingGrass(PotionScriptable ps)
    {
        if (currentStatus.Contains(Status.burned))
        {
            Debug.Log("L'erba non cresce sul fuoco");
            GameMan.Instance.PopDialog(ps.burned);
            PlayClip(ps.burned_audio);
        }
        else if (currentStatus.Contains(Status.freezed))
        {
            GameMan.Instance.PopDialog(ps.freezed, 7f);
            Debug.Log("L'erba non cresce sul ghiaccio");
            PlayClip(ps.freezed_audio);
        }
        else
        {
            AddStatus(Status.grass);
            grassFx.SetActive(true);
            GameMan.Instance.PickADialog(ps.dialogs, 3.5f);
        }
    }
    //done
    private void GetFreezed(PotionScriptable ps)
    {
        if (currentStatus.Contains(Status.wet))
        {
            TakeDamage(3);
            PlayClip(ps.wet_audio);
        }
        if (currentStatus.Contains(Status.burned))
        {
            if (currentStatus.Contains(Status.tree))
            {
                currentStatus.Remove(Status.tree);
                mageAnimator.SetTrigger("becomeMage");
            }
            else
            {
                mageAnimator.SetTrigger("vaporing");
            }

            currentStatus.Remove(Status.burned);
            burningFx.SetActive(false);

            AddStatus(Status.wet);
            wetFx.SetActive(true);

            PlayClip(ps.burned_audio);
        }
        else
        {
            AddStatus(Status.freezed);
            iceFx.SetActive(true);

        }
    }
    //done
    public IEnumerator GetBurn(PotionScriptable ps)
    {
        int burnDelay = ps.baseValue;
        if (currentStatus.Contains(Status.wet))
        {
            currentStatus.Remove(Status.wet);
            wetFx.SetActive(false);
            Debug.Log("Asciuga il mago");

            GameMan.Instance.PopDialog(ps.wet, 3.5f);
            PlayClip(ps.wet_audio);
        }
        else if (currentStatus.Contains(Status.freezed))
        {
            if (currentStatus.Contains(Status.tree))
            {
                currentStatus.Remove(Status.tree);
                mageAnimator.SetTrigger("becomeMage");
            }
            else
            {
                mageAnimator.SetTrigger("vaporing");
            }
            currentStatus.Remove(Status.freezed);
            iceFx.SetActive(false);

            AddStatus(Status.wet);
            wetFx.SetActive(true);

            PlayClip(ps.freezed_audio);



            GameMan.Instance.PopDialog(ps.freezed, 3.5f);
            if (GameMan.Instance.levelPotions.Count == 1) {
                //Pizza Express  3000-> Mentre il mago è ghiacciato consuma tutte le pozioni del livello e scongelalo solo con l'ultima
                AchievementManager.instance.Achive("Pizza Express 3000");
            }

            Debug.Log("Scongela il mago ma lo lascia bagnato");

        }
        else if (!currentStatus.Contains(Status.burned))
        {
            if (currentStatus.Contains(Status.grass))
            {
                currentStatus.Remove(Status.grass);
                grassFx.SetActive(false);
                Debug.Log("Brucia l'erba che stava crescendo sul mago");
                PlayClip(ps.grass_audio);
                GameMan.Instance.PopDialog(ps.grass, 4f);
            }

            AddStatus(Status.burned);



            burningFx.SetActive(true);
            while (currentStatus.Contains(Status.burned))
            {
                if (currentStatus.Contains(Status.tree))
                {
                    burnDelay = burnDelay / 2;
                }
                else
                {
                    mageAnimator.SetTrigger("isDamaged");
                }
                TakeDamage(1);
                yield return new WaitForSeconds(burnDelay);
                GameMan.Instance.PickADialog(ps.dialogs, 1.5f);

            }
            burningFx.SetActive(false);
        }

    }

    public void StatusOnTree(PotionScriptable ps)
    {
        switch (ps.effectType)
        {
            case PotionScriptable.EffectType.lava:
                TakeDamage(ps.baseValue * 2);
                GameMan.Instance.PopDialog("Trees ate lava! keep attention", 3f);
                break;
            case PotionScriptable.EffectType.damage:
                break;
            case PotionScriptable.EffectType.healing:
                GameMan.Instance.PopDialog("This things doesn't work on trees", 3f);
                break;
            case PotionScriptable.EffectType.burned:
                StartCoroutine(GetBurn(ps));
                break;
            case PotionScriptable.EffectType.freezed:
                GetFreezed(ps);
                break;
            case PotionScriptable.EffectType.grass:
                GetHealth(ps.baseValue);
                break;
            case PotionScriptable.EffectType.wet:
                if (currentStatus.Contains(Status.burned))
                {
                    GetWet(ps);
                }
                else {
                    GetHealth(ps.baseValue); 
                }
                break;
            case PotionScriptable.EffectType.none:
                Debug.Log("None");
                break;

        }
    }
    public void TakeDamage(int dmg)
    {
        currentHP -= dmg;
        if (currentHP < 1)
        {
            //END GAME
            if (currentStatus.Contains(Status.tree))
            {
                mageAnimator.SetTrigger("treeBurned");
                AchievementManager.instance.Achive("Old Toby");
            }
            else
            {
                mageAnimator.SetBool("isDie", true);
            }

            GameMan.Instance.OnCharacterDie();
        }
    }

    public void GetHealth(int i)
    {
        currentHP += i;
        if (currentHP >= staringHP)
        {
            currentHP = staringHP;
        }
    }

    public void AddStatus(Status status)
    {
        if (!currentStatus.Contains(status))
        {
            currentStatus.Add(status);
        }

    }

    public void PlayClip(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

}
