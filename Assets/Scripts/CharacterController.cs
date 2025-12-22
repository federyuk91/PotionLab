using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class CharacterController : MonoBehaviour
{
    [Header("Character Attribute")]
    public int staringHP = 10;
    public int currentHP = 10;
    public int startingMagicPoint = 10;
    public int currentMagicPoint = 0;
    public float drinkingTime = 1f;
    [Header("Other Attribute")]
    public bool poisonBlock = false;
    public int algheLevel = 1;
    public int fireLevel = 1;
    public int groundLevel = 1;
    public bool treeBlock = false;

    [Header("Character Status")]

    public Status newStatus;
    public List<Status> currentStatus = new List<Status>();
    [Header("Character Personalization")]

    public Color[] lightColor;

    [Header("Character Reference")]
    public Animator mageAnimator;
    public Image hpBar, magicBar;
    public CameraShake cameraShake;
    public GameObject spellBar;
    public SpellManager spellManager;
    public Text hp, magic;
    public GameObject burningFx, wetFx, iceFx, grassFx, poisonFx, algheFx, groundFx, healUpFX, MagicUpFX, immuneFX, treeShield;//, treeFx;
    [Header("Character Audio")]
    private AudioSource audioSource;
    public AudioSource audioSourceDamage;

    private Coroutine burnRoutine;

    public enum Status
    {
        burned,
        freezed,
        wet,
        grass,
        alghe,
        tree,
        grounded,
        balrog,
        poisoned,
        pupperfish,
        yeti,
        none,
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void Update()
    {
        hpBar.fillAmount = (float)currentHP / (float)staringHP;
        magicBar.fillAmount = (float)currentMagicPoint / (float)startingMagicPoint;
        hp.text = currentHP.ToString();
        magic.text = currentMagicPoint.ToString();

    }

    public void Drunk(PotionScript potion)
    {
        //mageAnimator.SetBool("isDrinking", true);
        mageAnimator.SetTrigger("Drunk");
        StartCoroutine(ApplyEffect(potion));
    }

    public IEnumerator ApplyEffect(PotionScript potion)
    {

        PotionScriptable ps = potion.potion;
        yield return new WaitForSeconds(drinkingTime);
        //mageAnimator.SetBool("isDrinking", false);

        if (currentStatus.Contains(Status.tree))
        {
            StatusOnTree(ps);
        }
        else if (currentStatus.Contains(Status.balrog))
        {
            StatusOnBalrog(ps);
        }
        else if (currentStatus.Contains(Status.pupperfish))
        {

            StatusOnPupperfish(ps);
        }
        else if (currentStatus.Contains(Status.yeti))
        {
            StatusOnYeti(ps);
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
                        GameMan.Instance.PickADialog(ps.dialogs, 1.5f);

                    break;
                case PotionScriptable.EffectType.healing:
                    if (!AchievementManager.d_achievements["Goodnight!"].unlocked)
                        AchievementManager.instance.Achive("Goodnight!");
                    GetHealth(ps.baseValue);
                    PlayClip(ps.none);
                    if (ps.dialogs.Count > 0)
                        GameMan.Instance.PickADialog(ps.dialogs, 1.5f);//"I don't digest the lava before going to sleep"

                    break;
                case PotionScriptable.EffectType.burned:
                    if (burnRoutine != null)
                        StopCoroutine(burnRoutine);
                    burnRoutine = StartCoroutine(GetBurn(ps));
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
                case PotionScriptable.EffectType.magicUP:
                    GetMagic(ps.baseValue);
                    GameMan.Instance.PopDialog("*sparkle*", 1.5f);
                    break;
                case PotionScriptable.EffectType.magicDown:

                    if (currentMagicPoint == 0)
                    {
                        GameMan.Instance.PopDialog("This hurt my health!", 1.5f);
                        TakeDamage(2);
                        if (currentHP <= 0)
                        {
                            AchievementManager.instance.Achive("Necrotic Death!");
                        }
                    }
                    else
                    {
                        LostMagic(ps.baseValue);
                        GameMan.Instance.PopDialog("I lost magic!", 1.5f);
                    }
                    break;
                case PotionScriptable.EffectType.poisoned:
                    StartCoroutine(GetPoisoned(ps));
                    break;
                case PotionScriptable.EffectType.grounded:
                    StartCoroutine(GetGrounded(ps));
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
            if (burnRoutine != null)
                StopCoroutine(burnRoutine);
            burningFx.SetActive(false);
            AddStatus(Status.balrog);
            //GameMan.Instance.OnCharacterDie();

            PlayClip(ps.balrog_audio);
            GameMan.Instance.ChangeLightColor(lightColor[1]);
            GameMan.Instance.PopDialog("ARGGHHHHHH!", 2f);
            spellManager.OnMageMutate(SpellManager.Mutation.Balrog);
            GetMagic(fireLevel);//SwitchSpellSplot(1);
            AchievementManager.instance.Achive("Udun flame");
        }
        else if (currentStatus.Contains(Status.freezed))
        {
            PlayClip(ps.none);
            TakeDamage(ps.baseValue - 1);
            currentStatus.Remove(Status.freezed);
            iceFx.GetComponent<Animator>().SetTrigger("melt");
            GameMan.Instance.PopDialog("ice is melting! OUCH!", 2f);
            if (poisonBlock)
            {
                poisonBlock = false;
            }
            mageAnimator.SetTrigger("lavaDrunked");
            if (ps.dialogs.Count > 0)
                GameMan.Instance.PickADialog(ps.dialogs, 1.5f);
        }
        else if (currentStatus.Contains(Status.grounded))
        {

            if (poisonBlock)
            {
                poisonBlock = false;

                currentStatus.Remove(Status.grounded);
                groundLevel = 1;
                groundFx.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
                groundFx.GetComponent<Animator>().SetTrigger("lava");
                TakeDamage(2);
                PlayClip(ps.freezed_audio);
                if (!currentStatus.Contains(Status.yeti))
                {
                    mageAnimator.SetTrigger("poisonExplosion");
                    GameMan.Instance.PickADialog(ps.dialogs, .5f);
                    GameMan.Instance.PopDialog("Damn! ", 2f);
                    PlayClip(ps.freezed_audio);


                }
                else
                {
                    GameMan.Instance.PopDialog("EH EH! ", 1f);

                }

            }
            else
            {

                currentStatus.Remove(Status.grounded);
                groundLevel = 1;
                PlayClip(ps.none);
                groundFx.GetComponent<Animator>().SetTrigger("lava");
                GameMan.Instance.PopDialog("Ground i Love You", 3f);
            }

        }
        else
        {

            if (!currentStatus.Contains(Status.yeti))
            {

                mageAnimator.SetTrigger("lavaDrunked");
                if (ps.dialogs.Count > 0)
                    GameMan.Instance.PickADialog(ps.dialogs, 1.5f);

            }
            PlayClip(ps.none);
            TakeDamage(ps.baseValue);

        }
    }

    //done
    public void GetWet(PotionScriptable ps)
    {
        if (currentStatus.Contains(Status.burned))
        {
            burningFx.SetActive(false);
            currentStatus.Remove(Status.burned);
            GameMan.Instance.PopDialog(ps.burned, 1.5f);
            fireLevel = 1;
            PlayClip(ps.burned_audio);

            if (currentStatus.Contains(Status.tree))
            {
                currentStatus.Remove(Status.tree);
                mageAnimator.SetTrigger("becomeMage");
                GameMan.Instance.ChangeLightColor(lightColor[0]);
                spellManager.OnMageMutate(SpellManager.Mutation.Mage);
                //spellManager.SwitchSpellSplot(0);
                GameMan.Instance.PopDialog("Back again with leaves in my mouth", 1.5f);
            }
            else
            {
                mageAnimator.SetTrigger("smoking");

                AchievementManager.instance.Achive("Wet wizard, lucky wizard");
            }
        }
        else if (currentStatus.Contains(Status.grass))
        {

            if (!currentStatus.Contains(Status.pupperfish))
            {
                GameMan.Instance.PopDialog(ps.grass, 1.5f);
                Debug.Log("L'erba sul corpo del mago cresce e diventa un albero");
                grassFx.SetActive(false);

                currentStatus.Remove(Status.grass);
                GameMan.Instance.PopDialog("what! a tree?", 1.5f);
                PlayClip(ps.pupperFish_audio);
                AddStatus(Status.tree);
                mageAnimator.SetTrigger("becomeATree");
                GameMan.Instance.ChangeLightColor(lightColor[2]);
                spellManager.OnMageMutate(SpellManager.Mutation.Tree);
                //spellManager.SwitchSpellSplot(2);
                AchievementManager.instance.Achive("Time to think");
            }
            else
            {
                grassFx.SetActive(false);
                currentStatus.Remove(Status.grass);
                algheFx.SetActive(true);
                currentStatus.Add(Status.alghe);
            }


            //Il vecchio tobia->Trasforma il mago in albero, brucialo e lascialo fumante
            /*if (GameMan.Instance.levelPotions.Count == 0)
            {
                GameMan.Instance.OnCharacterDie();
            }*/


        }
        else if (currentStatus.Contains(Status.poisoned) && !poisonBlock)
        {
            Debug.Log("l'acqua lava via il veleno");
            GameMan.Instance.PopDialog("My skin is clear and fresh!", 2f);
            poisonFx.SetActive(false);
            currentStatus.Remove(Status.poisoned);
            AddStatus(Status.wet);
            wetFx.SetActive(true);

            PlayClip(ps.none);

        }
        else if (currentStatus.Contains(Status.freezed))
        {
            TakeDamage(2, null);
            GameMan.Instance.PopDialog("Water on ice? Are you stupid?", 2f);
        }
        else if (currentStatus.Contains(Status.alghe))
        {
            if (algheLevel < 3)
            {
                algheLevel++;
                GameMan.Instance.PopDialog("MORE ALGHE!", .9f);
                algheFx.GetComponent<Animator>().SetInteger("level", algheLevel);

            }
            else
            {
                GameMan.Instance.PopDialog("Maybe to much?", 2f);
            }

        }
        else if (currentStatus.Contains(Status.grounded))
        {
            if (poisonBlock)
            {
                poisonBlock = false;
                groundFx.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
                GameMan.Instance.PopDialog("Stinky poison, go away!", 2f);
            }

            if (groundLevel <= 3)
            {
                groundLevel--;
                PlayClip(ps.none);
                groundFx.GetComponent<Animator>().SetInteger("groundLevel", groundLevel);
                GameMan.Instance.PopDialog("Clean like glass", 2f);
                if (groundLevel == 0)
                {
                    currentStatus.Remove(Status.grounded);
                    groundLevel = 1;
                    groundFx.SetActive(false);
                }
            }
        }
        else if (currentStatus.Contains(Status.wet))
        {
            GameMan.Instance.PopDialog("I'm already wet!", 2f);
        }
        else
        {
            AddStatus(Status.wet);
            wetFx.SetActive(true);
            GameMan.Instance.PickADialog(ps.dialogs, 1.5f);
            PlayClip(ps.none);
        }
    }

    private void GrowingGrass(PotionScriptable ps)
    {
        if (currentStatus.Contains(Status.burned))
        {
            Debug.Log("L'erba non cresce sul fuoco");
            GameMan.Instance.PopDialog("mmh, poor grass!", 1f);
            immuneFX.SetActive(true);
            PlayClip(ps.burned_audio);
        }
        else if (currentStatus.Contains(Status.freezed))
        {
            GameMan.Instance.PopDialog("NO GRASS ON MY ICE", 1f);
            immuneFX.SetActive(true);
            Debug.Log("L'erba non cresce sul ghiaccio");
            PlayClip(ps.grass_audio);
        }
        else if (currentStatus.Contains(Status.alghe))
        {
            GameMan.Instance.PopDialog("Not more grass on my alghe!", 1f);
            immuneFX.SetActive(true);
            Debug.Log("L'erba non cresce vicino alle alghe");

        }
        else if (currentStatus.Contains(Status.wet) && !currentStatus.Contains(Status.tree))
        {
            Debug.Log("l'erba su l'acqua fa crescere delle alghe");
            wetFx.SetActive(false);
            GameMan.Instance.PopDialog("Algae?", 1.5f);
            currentStatus.Remove(Status.wet);
            algheFx.SetActive(true);
            currentStatus.Add(Status.alghe);
        }
        else if (currentStatus.Contains(Status.poisoned))
        {
            immuneFX.SetActive(true);
            GameMan.Instance.PopDialog("My skin is venom, nothing can grow", 1f);
        }
        else if (currentStatus.Contains(Status.grounded))
        {
            if (poisonBlock)
            {
                GameMan.Instance.PopDialog("Grass doesn't grow on poison", 2f);
            }
            else
            {
                GameMan.Instance.PopDialog("I can absorb the life!", 2f);
                GetHealth(2);
            }

        }
        else
        {
            AddStatus(Status.grass);
            grassFx.SetActive(true);
            PlayClip(ps.none);
            GameMan.Instance.PickADialog(ps.dialogs, 1f);

        }
    }
    //done
    private void GetFreezed(PotionScriptable ps)
    {
        if (currentStatus.Contains(Status.wet))
        {
            TakeDamage(3);
            currentStatus.Remove(Status.wet);
            wetFx.SetActive(false);
            PlayClip(ps.wet_audio);
            AddStatus(Status.freezed);
            iceFx.SetActive(true);
        }
        else if (currentStatus.Contains(Status.freezed))
        {
            GameMan.Instance.PopDialog("OOOOUCH!", 3f);
            PlayClip(ps.none);
            AchievementManager.instance.Achive("ICE TW-ICE");
            TakeDamage(5);
        }
        else if (currentStatus.Contains(Status.grass))
        {
            GameMan.Instance.PopDialog("Goodbye plants!", 1.5f);
            Debug.Log("Il ghiaccio distrugge l'erba");
            grassFx.SetActive(false);
            currentStatus.Remove(Status.grass);
            AddStatus(Status.freezed);
            iceFx.SetActive(true);
            PlayClip(ps.none);
        }
        else if (currentStatus.Contains(Status.alghe))
        {
            GameMan.Instance.PopDialog("Freezing!", 1.5f);
            Debug.Log("Il ghiaccio distrugge le alghe");
            algheFx.SetActive(false);
            currentStatus.Remove(Status.alghe);
            AddStatus(Status.freezed);
            iceFx.SetActive(true);
            PlayClip(ps.none);

        }
        else if (currentStatus.Contains(Status.burned))
        {
            if (currentStatus.Contains(Status.tree))
            {
                currentStatus.Remove(Status.tree);
                mageAnimator.SetTrigger("becomeMage");
                spellManager.OnMageMutate(SpellManager.Mutation.Mage);
                //spellManager.SwitchSpellSplot(0);
            }
            else
            {
                mageAnimator.SetTrigger("vaporing");
                AddStatus(Status.wet);
                wetFx.SetActive(true);
                currentStatus.Remove(Status.burned);
                burningFx.SetActive(false);
                fireLevel = 1;
                audioSource.Stop();
                PlayClip(ps.burned_audio);
            }

        }
        else if (currentStatus.Contains(Status.poisoned))
        {
            Debug.Log("il ghiaccio diventa velonoso");
            poisonFx.SetActive(false);
            currentStatus.Remove(Status.poisoned);
            AddStatus(Status.freezed);
            iceFx.SetActive(true);
            PlayClip(ps.none);
            iceFx.GetComponent<SpriteRenderer>().color = new Color(143, 0, 255);
            poisonBlock = true;
        }
        else if (currentStatus.Contains(Status.grounded))
        {
            Debug.Log("il mago si traforma in yeti");
            groundFx.SetActive(false);
            currentStatus.Remove(Status.grounded);
            GetMagic(groundLevel);
            groundLevel = 1;

            
            mageAnimator.SetTrigger("becomeYeti");
            GameMan.Instance.ChangeLightColor(lightColor[4]);
            PlayClip(ps.balrog_audio);
            AddStatus(Status.yeti);
            AchievementManager.instance.Achive("Roar!");
            groundFx.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);

            if (poisonBlock)
            {
                poisonBlock = false;
                StartCoroutine(GetPoisoned(ps));
            }

            //Get Poisoned?
            GameMan.Instance.ChangeLightColor(lightColor[3]);
            spellManager.OnMageMutate(SpellManager.Mutation.Yeti);

        }
        else
        {
            GameMan.Instance.PopDialog("F***!", 1.5f);
            AddStatus(Status.freezed);
            iceFx.SetActive(true);
            PlayClip(ps.none);
            iceFx.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
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
            GameMan.Instance.PopDialog(ps.wet, 3.5f);
            mageAnimator.SetTrigger("smoking");

            PlayClip(ps.wet_audio);


        }
        else if (currentStatus.Contains(Status.freezed))
        {
            if (currentStatus.Contains(Status.tree))
            {
                currentStatus.Remove(Status.tree);
                mageAnimator.SetTrigger("becomeMage");
                spellManager.OnMageMutate(SpellManager.Mutation.Mage);
                //spellManager.SwitchSpellSplot(0);
            }

            mageAnimator.SetTrigger("vaporing");
            currentStatus.Remove(Status.freezed);
            iceFx.GetComponent<Animator>().SetTrigger("melt");
            AddStatus(Status.wet);
            wetFx.SetActive(true);
            PlayClip(ps.freezed_audio);

            if (poisonBlock)
            {
                poisonBlock = false;
                mageAnimator.SetTrigger("poisonExplosion");
                TakeDamage(2 * fireLevel);
                iceFx.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
                PlayClip(ps.burned_audio);
            }

            GameMan.Instance.PopDialog(ps.freezed, 3.5f);
            if (GameMan.Instance.levelPotions.Count == 1)
            {
                //Pizza Express  3000-> Mentre il mago è ghiacciato consuma tutte le pozioni del livello e scongelalo solo con l'ultima
                AchievementManager.instance.Achive("Pizza Express 3000");
            }
            else
            {
                //Scongela il mago
                AchievementManager.instance.Achive("Thank you lara");
            }

            Debug.Log("Scongela il mago ma lo lascia bagnato");

        }
        else if (currentStatus.Contains(Status.poisoned))
        {
            if (!currentStatus.Contains(Status.tree) && !currentStatus.Contains(Status.balrog) && !currentStatus.Contains(Status.pupperfish))
            {
                Debug.Log("il veleno si incendia e si genera una piccola esplosione");
                currentStatus.Remove(Status.poisoned);
                poisonFx.SetActive(false);
                mageAnimator.SetTrigger("poisonExplosion");
                TakeDamage(3 * fireLevel);
                PlayClip(ps.burned_audio);
                //Creare fx per la piccola esplosione velenosa
            }
            else
            {
                //DA PENSARE
            }
        }
        else if (currentStatus.Contains(Status.alghe))
        {
            Debug.Log("Le alghe bruciano e rilasciano magia!");
            algheFx.SetActive(false);
            currentStatus.Remove(Status.alghe);
            if (algheLevel == 3)
            {
                AchievementManager.instance.Achive("Advance knowledge");
            }
            GameMan.Instance.PopDialog("This knowledge could be usefull!", 1f);
            GetMagic(algheLevel * 2);
            algheLevel = 1;
            algheFx.GetComponent<Animator>().SetInteger("level", algheLevel);
            PlayClip(ps.grass_audio);
        }
        else if (currentStatus.Contains(Status.burned))
        {
            if (fireLevel < 3)
            {
                fireLevel++;

                if (fireLevel == 3)
                {
                    AchievementManager.instance.Achive("Burn Baby BURN!");
                }
                GameMan.Instance.PopDialog("AHHHHH I'm burning!!!", 1f);
                burningFx.GetComponent<Animator>().SetInteger("fireLevel", fireLevel);

            }
        }
        else if (currentStatus.Contains(Status.grounded))
        {
            GameMan.Instance.PopDialog("Ground doesn't burn", 1f);
            immuneFX.SetActive(true);
        }
        else if (!currentStatus.Contains(Status.burned))
        {
            fireLevel = 1;
            if (currentStatus.Contains(Status.grass))
            {
                currentStatus.Remove(Status.grass);
                grassFx.SetActive(false);
                Debug.Log("Brucia l'erba che stava crescendo sul mago");
                PlayClip(ps.grass_audio);
                GameMan.Instance.PopDialog("grass is fueling fire", 1f);
                fireLevel++;
            }

            AddStatus(Status.burned);
            burningFx.SetActive(true);
            burningFx.GetComponent<Animator>().SetInteger("fireLevel", fireLevel);

            if (currentStatus.Contains(Status.tree))
            {
                PlayClip(ps.tree_audio);
            }
                       
        }
        
        while (currentStatus.Contains(Status.burned) && currentHP > 0)
        {
            yield return new WaitForSeconds(1);
            if (currentStatus.Contains(Status.tree))
            {
                //burnDelay = ps.baseValue / 2;
                TakeDamage(fireLevel + 1, ps.tree_audio);
                GameMan.Instance.PickADialog(ps.dialogs, 1.5f);
                burnDelay = 4;
            }
            else if (currentStatus.Contains(Status.balrog))
            {
                GetHealth(1);
                GameMan.Instance.PopDialog("Yeah! Burn!", 1);
                burnDelay = 8 - fireLevel;
            }
            else
            {
                mageAnimator.SetTrigger("isDamaged");
                TakeDamage(fireLevel);
                GameMan.Instance.PickADialog(ps.dialogs, 1f);
            }
            yield return new WaitForSeconds(burnDelay-1);

        }
        audioSource.Stop();
        burningFx.SetActive(false);
        fireLevel = 1;
        burningFx.GetComponent<Animator>().SetInteger("fireLevel", fireLevel);
    }

    public IEnumerator GetGrounded(PotionScriptable ps)
    {


        if (currentStatus.Contains(Status.grounded))
        {
            if (groundLevel < 3)
            {
                groundLevel++;
                groundFx.GetComponent<Animator>().SetInteger("groundLevel", groundLevel);
                Debug.Log("a");
                PlayClip(ps.none);
                GameMan.Instance.PickADialog(ps.dialogs, 1.5f);

                if (groundLevel == 3)
                {
                    AchievementManager.instance.Achive("Pile of S...");
                    while (groundLevel == 3 && currentHP > 0)
                    {
                        if (currentStatus.Contains(Status.tree))
                        {
                            AchievementManager.instance.Achive("Holy pile of ground!");
                            GetHealth(1);
                        }
                        else
                        {
                            TakeDamage(ps.baseValue);
                        }

                        yield return new WaitForSeconds(5f);
                    }


                    yield break;
                }
            }
            else
            {
                GameMan.Instance.PopDialog("I'm already full of s...", 2f);
            }

        }
        else if (currentStatus.Contains(Status.burned))
        {

            currentStatus.Remove(Status.burned);
            burningFx.SetActive(false);
            fireLevel = 1;
            GameMan.Instance.PopDialog("NICE trick!", 2f);
            audioSource.Stop();
            AddStatus(Status.grounded);
            groundFx.SetActive(true);
            PlayClip(ps.none);


        }
        else if (currentStatus.Contains(Status.wet))
        {

            currentStatus.Remove(Status.wet);
            wetFx.SetActive(false);
            GameMan.Instance.PopDialog("Ground is absorbing the water", 2.5f);
            AddStatus(Status.grounded);
            groundFx.SetActive(true);
            PlayClip(ps.none);

        }
        else if (currentStatus.Contains(Status.poisoned))
        {

            currentStatus.Remove(Status.poisoned);
            poisonFx.SetActive(false);
            GameMan.Instance.PopDialog("Poisoned ground...strange", 2.5f);
            AddStatus(Status.grounded);
            groundFx.GetComponent<SpriteRenderer>().color = new Color(143, 0, 255);
            groundFx.SetActive(true);
            poisonBlock = true;
            while (poisonBlock && currentHP > 0)
            {
                if (currentStatus.Contains(Status.yeti))
                {
                    TakeDamage(1);
                    Debug.Log("1");
                    PlayClip(ps.none);

                }
                else
                {

                    mageAnimator.SetTrigger("isDamaged");
                    TakeDamage(1);
                    Debug.Log("1");
                    PlayClip(ps.none);
                    GameMan.Instance.PickADialog(ps.dialogs, .5f);
                }
                yield return new WaitForSeconds(4);

            }

        }
        else if (currentStatus.Contains(Status.alghe))
        {

            currentStatus.Remove(Status.alghe);
            algheFx.SetActive(false);
            GameMan.Instance.PopDialog("Bye Algae", 2.5f);
            AddStatus(Status.grounded);
            groundFx.SetActive(true);
            PlayClip(ps.none);

        }
        else if (currentStatus.Contains(Status.grass))
        {

            currentStatus.Remove(Status.grass);
            grassFx.SetActive(false);
            GameMan.Instance.PopDialog("Bye Grass", 1.5f);
            AddStatus(Status.grounded);
            groundFx.SetActive(true);
            PlayClip(ps.none);


        }
        else if (currentStatus.Contains(Status.freezed))
        {
            GameMan.Instance.PopDialog("Ground slides away! ", 2f);
            immuneFX.SetActive(true);
            PlayClip(ps.none);
        }
        else
        {

            AddStatus(Status.grounded);
            groundFx.SetActive(true);
            PlayClip(ps.none);
            GameMan.Instance.PickADialog(ps.dialogs, 1.5f);
            Debug.Log("Applicare Ground");

        }

    }

    public IEnumerator GetPoisoned(PotionScriptable ps)
    {
        int burnDelay = ps.baseValue;
        int poisonTick = 0;

        if (currentStatus.Contains(Status.wet))
        {
            GameMan.Instance.PopDialog("Blob, blob!", 1.5f);
            Debug.Log("La magia trasforma il mago in un pesce palla velenoso");
            poisonFx.SetActive(false);
            wetFx.SetActive(false);
            currentStatus.Remove(Status.poisoned);
            currentStatus.Remove(Status.wet);
            spellManager.OnMageMutate(SpellManager.Mutation.Fish);
            //spellManager.SwitchSpellSplot(3);
            AchievementManager.instance.Achive("Under the sea! Under the sea!");
            GameMan.Instance.ChangeLightColor(lightColor[3]);
            PlayClip(ps.pupperFish_audio);
            AddStatus(Status.pupperfish);
            mageAnimator.SetTrigger("becomeAfish");

            //AchievementManager.instance.Achive("Time to think"); //AGGIUNGERE ACHIVEMENT
        }
        else if (currentStatus.Contains(Status.burned))
        {
            Debug.Log("il veleno si incendia e si genera una piccola esplosione");
            currentStatus.Remove(Status.burned);
            GameMan.Instance.PopDialog("KABOOM!", 1.5f);
            poisonFx.SetActive(false);
            mageAnimator.SetTrigger("poisonExplosion");
            TakeDamage(fireLevel * 2);
            PlayClip(ps.burned_audio);
        }
        else if (currentStatus.Contains(Status.freezed))
        {
            Debug.Log("il ghiaccio diventa velonoso");
            poisonFx.SetActive(false);
            GameMan.Instance.PopDialog("Purple Ice, mmh...", 1.5f);
            currentStatus.Remove(Status.poisoned);
            iceFx.SetActive(true);
            iceFx.GetComponent<SpriteRenderer>().color = new Color(143, 0, 255);
            poisonBlock = true;

            if (poisonBlock)
            {
                GameMan.Instance.PopDialog("Ice protect me from poison!", 1.5f);
            }
        }
        else if (currentStatus.Contains(Status.grass))
        {
            Debug.Log("Avvelena le piante che muoiono");
            currentStatus.Remove(Status.grass);
            GameMan.Instance.PopDialog("Grass protects me! But it is dead :(", 1.5f);
            grassFx.SetActive(false);
            PlayClip(ps.grass_audio);

        }
        else if (currentStatus.Contains(Status.alghe))
        {
            Debug.Log("Avvelena le piante che muoiono");
            GameMan.Instance.PopDialog("Alghe protects me from poison! Bye alghe!", 1.5f);
            currentStatus.Remove(Status.alghe);
            algheFx.SetActive(false);
            PlayClip(ps.grass_audio);

        }
        else if (currentStatus.Contains(Status.grounded))
        {
            if (!poisonBlock)
            {

                AddStatus(Status.grounded);
                groundFx.GetComponent<SpriteRenderer>().color = new Color(143, 0, 255);
                groundFx.SetActive(true);
                poisonBlock = true;

                while (poisonBlock && currentHP > 0)
                {
                    if (currentStatus.Contains(Status.tree))
                    {
                        TakeDamage(1);
                    }
                    else if (currentStatus.Contains(Status.yeti))
                    {
                        TakeDamage(1);
                        Debug.Log("1");
                        PlayClip(ps.none);

                    }
                    else
                    {

                        mageAnimator.SetTrigger("isDamaged");
                        TakeDamage(1);
                        Debug.Log("1");
                        PlayClip(ps.none);
                        GameMan.Instance.PickADialog(ps.dialogs, .5f);
                    }
                    yield return new WaitForSeconds(4);

                }

            }
            else
            {
                immuneFX.SetActive(true);
            }

        }
        else if (!currentStatus.Contains(Status.poisoned))
        {
            AddStatus(Status.poisoned);


            poisonFx.SetActive(true);

            while (poisonTick < 3 && currentStatus.Contains(Status.poisoned) && !poisonBlock)
            {
                if (currentStatus.Contains(Status.tree))
                {
                    // edera velenosa?
                }
                else if (currentStatus.Contains(Status.yeti))
                {
                    TakeDamage(1);
                    Debug.Log("1");
                    PlayClip(ps.none);
                    poisonTick++;
                }

                else
                {

                    mageAnimator.SetTrigger("isDamaged");
                    TakeDamage(1);
                    Debug.Log("1");
                    PlayClip(ps.none);
                    poisonTick++;
                    GameMan.Instance.PickADialog(ps.dialogs, .5f);
                }
                yield return new WaitForSeconds(burnDelay);

            }
            poisonFx.SetActive(false);
            currentStatus.Remove(Status.poisoned);
        }

    }


    public void StatusOnBalrog(PotionScriptable ps)
    {
        switch (ps.effectType)
        {
            case PotionScriptable.EffectType.lava:
                GetHealth(ps.baseValue);
                GameMan.Instance.PopDialog("Lava give me more power!", 3f);
                break;
            case PotionScriptable.EffectType.damage:
                break;
            case PotionScriptable.EffectType.healing:
                GameMan.Instance.PopDialog("This potion is not enough for my power", 4f);
                immuneFX.SetActive(true);
                break;
            case PotionScriptable.EffectType.burned:
                if (burnRoutine != null)
                    StopCoroutine(burnRoutine);
                burnRoutine = StartCoroutine(GetBurn(ps));
                break;
            case PotionScriptable.EffectType.freezed:
                if (currentStatus.Contains(Status.burned))
                {
                    currentStatus.Remove(Status.burned);
                    burningFx.SetActive(false);
                    fireLevel = 1;
                    audioSource.Stop();
                }
                else
                {
                    TakeDamage(4);
                    GameMan.Instance.PopDialog("I hate cold", 2f);
                }

                break;
            case PotionScriptable.EffectType.grass:
                GameMan.Instance.PopDialog("Only my evil can grow here! ", 2f);
                immuneFX.SetActive(true);
                break;
            case PotionScriptable.EffectType.wet:


                if (currentStatus.Contains(Status.burned))
                {
                    currentStatus.Remove(Status.burned);
                    GameMan.Instance.PopDialog("My little flames :( ", 1.5f);
                    fireLevel = 1;
                    burningFx.SetActive(false);
                    audioSource.Stop();
                }
                else
                {
                    GameMan.Instance.PopDialog("Water? You think we are in pokemon? ", 3f);
                    immuneFX.SetActive(true);
                }
                break;
            case PotionScriptable.EffectType.poisoned:
                //GameMan.Instance.PopDialog("Venom? ptz...humans stuff doesn't affect me!", 3f);
                //immuneFX.SetActive(true);
                GameMan.Instance.PopDialog("Human venom? ptz...", 3f);
                TakeDamage(1);
                break;
            case PotionScriptable.EffectType.grounded:
                if (currentStatus.Contains(Status.burned))
                {
                    currentStatus.Remove(Status.burned);
                    GameMan.Instance.PopDialog("My little flames :( ", 1.5f);
                    fireLevel = 1;
                    burningFx.SetActive(false);
                    audioSource.Stop();
                }
                else
                {
                    TakeDamage(2);
                    GameMan.Instance.PopDialog("Damn SHIT!", 2f);
                }

                break;

            case PotionScriptable.EffectType.magicUP:
                GameMan.Instance.PopDialog("What is happening? NOOOOO", 2f);
                mageAnimator.SetTrigger("isDamaged");
                TakeDamage(0);
                currentHP = 1;
                GetMagic(10);
                GameMan.Instance.ChangeLightColor(lightColor[0]);
                spellManager.OnMageMutate(SpellManager.Mutation.Mage);
                //spellManager.SwitchSpellSplot(0);
                currentStatus.Remove(Status.balrog);



                break;
            case PotionScriptable.EffectType.magicDown:
                GameMan.Instance.PopDialog("Liquid Death! mmh mmmmhhhh", 2f);

                GetMagic(2);
                break;


        }
    }

    public void StatusOnTree(PotionScriptable ps)
    {
        switch (ps.effectType)
        {
            case PotionScriptable.EffectType.lava:
                if (treeBlock)
                {
                    GameMan.Instance.PopDialog("Tree's bark fade away", 3f);
                    treeBlock = false;
                    treeShield.SetActive(false);
                }
                else
                {
                    if (currentStatus.Contains(Status.grounded))
                    {
                        GetLava(ps);
                    }
                    else
                    {
                        TakeDamage(ps.baseValue);
                        GameMan.Instance.PopDialog("Trees ate lava! keep attention", 3f);
                    }

                }

                break;
            case PotionScriptable.EffectType.damage:
                break;
            case PotionScriptable.EffectType.healing:
                GameMan.Instance.PopDialog("This things doesn't work on trees", 3f);
                immuneFX.SetActive(true);
                break;
            case PotionScriptable.EffectType.burned:
                if (treeBlock)
                {
                    GameMan.Instance.PopDialog("Tree's bark burn away", 3f);
                    treeBlock = false;
                    treeShield.SetActive(false);
                }
                else
                {
                    if (burnRoutine != null)
                        StopCoroutine(burnRoutine);
                    burnRoutine = StartCoroutine(GetBurn(ps));
                    GameMan.Instance.PopDialog("Not good for plants", 3f);
                }

                break;
            case PotionScriptable.EffectType.freezed:

                if (treeBlock)
                {
                    GameMan.Instance.PopDialog("Tree's bark freeze away", 2.5f);
                    treeBlock = false;
                    PlayClip(ps.none);
                    treeShield.SetActive(false);
                }
                else
                {
                    if (currentStatus.Contains(Status.burned))
                    {
                        GameMan.Instance.PopDialog("Ice, water, health!", 3f);
                        currentStatus.Remove(Status.tree);
                        mageAnimator.SetTrigger("becomeMage");
                        GameMan.Instance.ChangeLightColor(lightColor[0]);
                        spellManager.OnMageMutate(SpellManager.Mutation.Mage);
                        currentStatus.Remove(Status.burned);
                        AchievementManager.instance.Achive("Advance Tatics!");
                        burningFx.SetActive(false);
                        fireLevel = 1;
                        GetHealth(2);

                    }
                    else
                    {
                        TakeDamage(ps.baseValue * 2);
                        GameMan.Instance.PopDialog("Ice is terrible!", 1.5f);
                        PlayClip(ps.none);
                    }
                }


                break;
            case PotionScriptable.EffectType.grass:
                GameMan.Instance.PopDialog("This increase my magic!", 1.5f);
                GetMagic(2);
                break;
            case PotionScriptable.EffectType.poisoned:
                if (currentStatus.Contains(Status.grounded))
                {
                    GameMan.Instance.PopDialog("Tree gains protection from soils", 2f);
                    immuneFX.SetActive(true);
                }
                else
                {
                    GameMan.Instance.PopDialog("Ahhhhh, this is harmfull!", 1.5f);
                    TakeDamage(1);
                }

                break;
            case PotionScriptable.EffectType.wet:
                if (currentStatus.Contains(Status.burned))
                {
                    GetWet(ps);
                }
                else
                {
                    if (currentStatus.Contains(Status.grounded)) { GetHealth(3); GameMan.Instance.PopDialog("more nutrition!", 2f); } else { GetHealth(2); GameMan.Instance.PopDialog("sip... nice", 2f); }


                }
                break;
            case PotionScriptable.EffectType.magicUP:
                GetMagic(ps.baseValue);
                GameMan.Instance.PopDialog(":)", 1.5f);

                break;
            case PotionScriptable.EffectType.magicDown:
                GameMan.Instance.PopDialog("This things doesn't work on trees", 2f);
                immuneFX.SetActive(true);

                break;
            case PotionScriptable.EffectType.grounded:
                StartCoroutine(GetGrounded(ps));
                break;


        }
    }

    public void StatusOnPupperfish(PotionScriptable ps)
    {
        switch (ps.effectType)
        {
            case PotionScriptable.EffectType.lava:
                TakeDamage(ps.baseValue);
                GameMan.Instance.PopDialog("BLOOOB!", 1f);
                break;
            case PotionScriptable.EffectType.damage:
                break;
            case PotionScriptable.EffectType.healing:
                GetHealth(ps.baseValue);
                GameMan.Instance.PopDialog("blob blob blob", 1f);
                break;
            case PotionScriptable.EffectType.burned:
                immuneFX.SetActive(true);
                GameMan.Instance.PopDialog("blob? bloob?", 1f);
                break;
            case PotionScriptable.EffectType.freezed:
                TakeDamage(4);
                GameMan.Instance.PopDialog("B L O B", 1f);
                break;
            case PotionScriptable.EffectType.grass:
                GrowingGrass(ps);
                break;
            case PotionScriptable.EffectType.poisoned:
                GameMan.Instance.PopDialog("Bloooooob blob blob!", 2f);
                GetMagic(1);
                break;
            case PotionScriptable.EffectType.wet:
                GetWet(ps);
                break;
            case PotionScriptable.EffectType.magicUP:
                GetMagic(ps.baseValue);
                GameMan.Instance.PopDialog("*blob*", 1.5f);
                break;
            case PotionScriptable.EffectType.grounded:
                immuneFX.SetActive(true);
                GameMan.Instance.PopDialog("blob? bloob?", 1f);
                break;
            case PotionScriptable.EffectType.magicDown:
                GameMan.Instance.PopDialog("Blooooooooooooob!", 2f);
                mageAnimator.SetTrigger("isDamaged");
                currentStatus.Remove(Status.pupperfish);
                spellManager.OnMageMutate(SpellManager.Mutation.Mage);
                GameMan.Instance.ChangeLightColor(lightColor[0]);
                break;


        }
    }

    public void StatusOnYeti(PotionScriptable ps)
    {

        Debug.Log(ps.effectType.ToString());
        switch (ps.effectType)
        {
            case PotionScriptable.EffectType.lava:
                GetLava(ps);
                GameMan.Instance.PopDialog("AHHHHH", 1f);
                break;
            case PotionScriptable.EffectType.healing:
                GetHealth(ps.baseValue);
                GameMan.Instance.PopDialog("crunch", 1f);
                break;
            case PotionScriptable.EffectType.burned:
                immuneFX.SetActive(true);
                GameMan.Instance.PopDialog("No FLAMES here!", 1.5f);
                break;
            case PotionScriptable.EffectType.freezed:
                GetHealth(3);
                GameMan.Instance.PopDialog("ARGGGH!", 1f);
                break;
            case PotionScriptable.EffectType.grass:
                immuneFX.SetActive(true);
                GameMan.Instance.PopDialog("No GRASS here!", 1.5f);
                break;
            case PotionScriptable.EffectType.poisoned:
                StartCoroutine(GetPoisoned(ps));
                break;
            case PotionScriptable.EffectType.wet:

                if (poisonBlock)
                {
                    poisonBlock = false;
                    groundFx.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
                    GameMan.Instance.PopDialog("Stinky poison, go away!", 2f);
                }
                else
                {
                    immuneFX.SetActive(true);
                    GameMan.Instance.PopDialog("No SHOWER here!", 1.5f);
                }

                break;
            case PotionScriptable.EffectType.magicUP:

                if (currentMagicPoint == 10)
                {
                    /*mageAnimator.SetTrigger("isDamaged");
                    GameMan.Instance.ChangeLightColor(lightColor[0]);
                    spellManager.OnMageMutate(SpellManager.Mutation.Mage);
                    currentStatus.Remove(Status.yeti);
                    Debug.Log("Return mage");*/
                }
                else
                {
                    GetMagic(ps.baseValue);
                    TakeDamage(ps.baseValue);
                    GameMan.Instance.PopDialog("?", 1.5f);
                }

                break;
            case PotionScriptable.EffectType.magicDown:
                if (currentMagicPoint == 0)
                {
                    TakeDamage(2);
                }
                else
                {
                    LostMagic(1);
                    GameMan.Instance.PopDialog("!", 1.5f);
                }



                break;
            case PotionScriptable.EffectType.grounded:
                StartCoroutine(GetGrounded(ps));
                break;


        }
    }

    public void CameraShakeRef(float a, float b)
    {
        StartCoroutine(cameraShake.Shake(a, b));
    }


    public void TakeDamage(int dmg, AudioClip audio = null)
    {
        Debug.Log("taking damage " + dmg);
        string deathDialog = "...the last goodnight potion is unforgettable...";
        currentHP -= dmg;
        GameMan.Instance.PopUpUp("-" + dmg, Color.red);
        StartCoroutine(cameraShake.Shake(.15f, 1));
        audioSourceDamage.Play();
        Debug.Log("current hp " + currentHP);


        if (currentStatus.Contains(Status.yeti) && currentHP == currentMagicPoint)
        {
            mageAnimator.SetTrigger("isDamaged");
            GameMan.Instance.ChangeLightColor(lightColor[0]);
            spellManager.OnMageMutate(SpellManager.Mutation.Mage);
            currentStatus.Remove(Status.yeti);
            Debug.Log("Return mage");
        }

        if (currentHP < 1)
        {
            Debug.Log("hp minori di 0");
            //END GAME
            if (currentStatus.Contains(Status.tree))
            {
                Debug.Log("Suono Albero bruciato");
                PlayClip(audio);
                mageAnimator.SetTrigger("treeBurned");
                AchievementManager.instance.Achive("Old Toby");
                deathDialog = "...you will remain ash throught time...";
            }
            else if (currentStatus.Contains(Status.pupperfish))
            {
                mageAnimator.SetTrigger("pupperDeath");
                AchievementManager.instance.Achive("Flounder is death");
                deathDialog = "...the sea takes back what is its own...";
            }
            else if (currentStatus.Contains(Status.balrog))
            {
                mageAnimator.SetTrigger("balrogDie");
                AchievementManager.instance.Achive("The sacrifice of the gray wizard");
                deathDialog = "...back from where i belong...";
            }
            else if (currentStatus.Contains(Status.yeti))
            {
                mageAnimator.SetTrigger("yetiDie");
                //AchievementManager.instance.Achive("An old legend is death"); AGGIUNGERE ACHIVEMENT YETI
                deathDialog = "...mountains call me back...";
            }
            else
            {
                mageAnimator.SetBool("isDie", true);
            }

            GameMan.Instance.OnCharacterDie(deathDialog);
        }
    }

    public void GetHealth(int i)
    {
        int temp = currentHP;
        currentHP += i;

        if (currentHP >= staringHP) //current hp , max hp, heal         7/10  +5 = +3
        {
            GameMan.Instance.PopUpUp("+" + (staringHP - temp), Color.red);
            currentHP = staringHP;

        }
        else
        {
            GameMan.Instance.PopUpUp("+" + i, Color.red);
        }
        healUpFX.SetActive(true);
        if (currentStatus.Contains(Status.yeti) && currentHP == currentMagicPoint)
        {
            mageAnimator.SetTrigger("isDamaged");
            GameMan.Instance.ChangeLightColor(lightColor[0]);
            spellManager.OnMageMutate(SpellManager.Mutation.Mage);
            currentStatus.Remove(Status.yeti);
            Debug.Log("Return mage");
        }

        

    }

    public void GetMagic(int i)
    {

        int temp = currentMagicPoint;
        currentMagicPoint += i;

        if (currentMagicPoint >= startingMagicPoint)
        {
            GameMan.Instance.PopUpUp("+" + (startingMagicPoint - temp), Color.yellow);
            currentMagicPoint = startingMagicPoint;
        }
        else
        {
            GameMan.Instance.PopUpUp("+" + i, Color.yellow);
        }

        //ACHIVEMENT
        if (currentHP == 10 && currentMagicPoint == 10)
        {
            AchievementManager.instance.Achive("Fresh and Clean");
        }

        MagicUpFX.SetActive(true);

       
        spellManager.OnManaChange();
    }

    public void LostMagic(int i)
    {
        currentMagicPoint -= i;

        GameMan.Instance.PopUpUp("-" + i, Color.yellow);
        if (currentMagicPoint < 1)
        {
            currentMagicPoint = 0;

        }

       

        spellManager.OnManaChange();

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
