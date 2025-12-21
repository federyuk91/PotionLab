using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellManager : MonoBehaviour
{
    public enum Mutation
    {
        Mage,
        Balrog,
        Tree,
        Fish,
        Yeti,
    }

    public enum TerrainStatus
    {
        none,
        fire,
        water,
        grass,
        ice,
    }
    [Header("Spell Status")]
    public Mutation mutation = Mutation.Mage;
    public TerrainStatus terrainStatus = TerrainStatus.none;
    public int indexForSpell = 0;
    private int transformationThisRun = 0;

    [Header("Spell Bool")]
    public bool bloom = false;
    public Transform flowerPosition;

    [Header("Spell Ref")]
    //public GameObject spellInterface;
    public CharacterController cc;
    public GameMan gameManager;
    public Text magictext;
    public List<Transform> areaFx;
    public Animator[] spellAnimators;
    public Text[] SpellButtonsCost;
    public List<GameObject> tempAreaFx;
    public AudioSource spellAudio;
    public AudioClip[] audioSpell;


    [Header("Spell Prefab")]
    public GameObject fireFX;
    public GameObject WaterFx;
    public GameObject grassFX;
    public GameObject iceFX;

    public GameObject calderonePrefab;
    public GameObject pupperfishPrefab;
    public GameObject seedPrefab;
    public GameObject yetiPunch;

    [Header("Spell Cost/Spell Image")]

    public Image[] spellImages;

    public int[] mageSpellCost;
    public Sprite[] mageSpellSprites;

    public int[] balrogSpellCost;
    public Sprite[] balrogSpellSprites;

    public int[] treeSpellCost;
    public Sprite[] treeSpellSprites;

    public int[] pupperfishSpellCost;
    public Sprite[] fishSpellSprites;

    public int[] yetiSpellCost;
    public Sprite[] yetiSpellSprites;

    private void Start()
    {
        OnMageMutate(Mutation.Mage);
        spellAudio = GetComponent<AudioSource>();
    }


    public void CastSpell(int level)
    {
        switch (mutation)
        {
            case Mutation.Mage:
                MageCastSpell(level);
                break;
            case Mutation.Balrog:
                BalrogCastSpell(level);
                break;
            case Mutation.Tree:
                TreeCastSpell(level);
                break;
            case Mutation.Fish:
                FishCastSpell(level);
                break;
            case Mutation.Yeti:
                YetiCastSpell(level);
                break;
        }
    }

    public void MageCastSpell(int level)
    {
        if (cc.currentMagicPoint < mageSpellCost[level])
        {
            gameManager.PopDialog("I need more magic for this spell", 3f);
            Debug.Log("Magia non sufficiente");
            return;
        }
        spellAudio.clip = audioSpell[0];
        spellAudio.Play();
        switch (level)
        {
            case 0:
                if(GameMan.Instance.lightIntensity == 3)
                {
                    gameManager.PopDialog("maximum lumen", 2f);
                } else
                {
                    cc.LostMagic(mageSpellCost[level]);
                    cc.mageAnimator.SetTrigger("cast");
                    cc.mageAnimator.SetInteger("castInt", 1);

                    gameManager.SetUpLightLevel();
                    ChangeSpellText("LIGHT!");
                    Debug.Log("Magia LUCE!");
                }
               
                break;
            case 1:
                if (cc.currentHP < cc.staringHP)
                {
                    //Da pensare
                    cc.LostMagic(mageSpellCost[level]);
                    Debug.Log("Magia CURA!");
                    cc.mageAnimator.SetTrigger("cast");
                    cc.mageAnimator.SetInteger("castInt", 2);
                    ChangeSpellText("HEAL!"); 

                    if(gameManager.lightIntensity == 3)
                    {
                        cc.GetHealth(4);
                        gameManager.PopDialog("Amazing heal!", 2f);
                    } else
                    {
                        cc.GetHealth(3);
                    }
                    
                }
                else
                {
                    gameManager.PopDialog("No need for this now!", 2f);
                }
                break;
            case 2:
                if (cc.currentStatus.Contains(CharacterController.Status.grass) || cc.currentStatus.Contains(CharacterController.Status.wet) || cc.currentStatus.Contains(CharacterController.Status.burned)
                    || cc.currentStatus.Contains(CharacterController.Status.freezed) || cc.currentStatus.Contains(CharacterController.Status.poisoned) || cc.currentStatus.Contains(CharacterController.Status.grounded))
                {
                    if (cc.currentStatus.Contains(CharacterController.Status.freezed) && cc.poisonBlock)
                    {
                        AchievementManager.instance.Achive("Mastro Lindo");
                    }
                    cc.LostMagic(mageSpellCost[level]);
                    cc.currentStatus.Remove(CharacterController.Status.burned);
                    cc.burningFx.SetActive(false);
                    cc.fireLevel = 1;
                    cc.groundLevel = 1;
                    cc.currentStatus.Remove(CharacterController.Status.grass);
                    cc.grassFx.SetActive(false);
                    cc.currentStatus.Remove(CharacterController.Status.wet);
                    cc.wetFx.SetActive(false);
                    cc.currentStatus.Remove(CharacterController.Status.freezed);
                    cc.iceFx.SetActive(false);
                    cc.currentStatus.Remove(CharacterController.Status.poisoned);
                    cc.poisonFx.SetActive(false);

                    if (cc.poisonBlock)
                    {
                        cc.poisonBlock = false;
                    }

                    if (gameManager.lightIntensity == 3)
                    {
                        gameManager.PopDialog("A bit of health too!", 2f);
                        cc.GetHealth(1);
                    }
                    cc.mageAnimator.SetTrigger("cast");
                    cc.mageAnimator.SetInteger("castInt", 3);
                    ChangeSpellText("Cleanse!");
                    Debug.Log("Magia Cleanse!");
                }
                else
                {
                    gameManager.PopDialog("No base status to remove!", 2f);
                }
                break;
        }
    }
    public void BalrogCastSpell(int level)
    {
        if (cc.currentMagicPoint < balrogSpellCost[level])
        {
            gameManager.PopDialog("Ohoh, not enough magic!", 3f);
            Debug.Log("Magia non sufficiente");
            return;
        }
        spellAudio.clip = audioSpell[1];
        spellAudio.Play();
        switch (level)
        {
            case 0:
                cc.LostMagic(balrogSpellCost[level]);
                if (tempAreaFx.Count >= 1)
                {
                    ClearAreaFx("Goodbye heat ):");
                    terrainStatus = TerrainStatus.none;
                }
                else
                {

                    foreach (Transform t in areaFx)
                    {
                        GameObject g = Instantiate(fireFX, t.position, t.rotation);
                        tempAreaFx.Add(g);
                    }
                    ChangeSpellText("FIRE ZONE!");
                    cc.mageAnimator.SetTrigger("cast");
                    cc.mageAnimator.SetInteger("castInt", 1);
                    terrainStatus = TerrainStatus.fire;
                    gameManager.PopDialog("Burn baby burn! Disco INFERNO!", 2f);
                    Debug.Log("MagiaFire!");
                }
                break;
            case 1:
                cc.LostMagic(balrogSpellCost[level]);

                if (terrainStatus.Equals(TerrainStatus.fire))
                {
                    Debug.Log("Fiamme Attive!");
                    cc.TakeDamage(3);
                    cc.GetMagic(2);
                }
                else
                {
                    cc.TakeDamage(3);
                    cc.GetMagic(1);
                }


                Debug.Log("Magia Balance!");
                cc.mageAnimator.SetTrigger("cast");
                cc.mageAnimator.SetInteger("castInt", 2);
                ChangeSpellText("BALANCEEEEEEEEEE");

                break;
            case 2:
                cc.LostMagic(balrogSpellCost[level]);
                ChangeSpellText("CALDDDDEROOONE");
                cc.mageAnimator.SetTrigger("cast");
                cc.mageAnimator.SetInteger("castInt", 3);

                calderonePrefab.SetActive(true);
                if (terrainStatus.Equals(TerrainStatus.fire))
                {
                    calderonePrefab.transform.localScale = new Vector3(2, 2, 2);
                    
                    AchievementManager.instance.Achive("Cooking Mama!"); 
                }
                else
                {
                    calderonePrefab.transform.localScale = new Vector3(1, 1, 1);
                }
                break;
        }
    }
    public void TreeCastSpell(int level)
    {
        if (cc.currentMagicPoint < treeSpellCost[level])
        {
            gameManager.PopDialog("Ohoh, not enough magic!", 3f);
            Debug.Log("Magia non sufficiente");
            return;
        }
        spellAudio.clip = audioSpell[2];
        spellAudio.Play();
        switch (level)
        {
            case 0:
                cc.LostMagic(treeSpellCost[level]);
                if (tempAreaFx.Count >= 1)
                {
                    terrainStatus = TerrainStatus.none;
                    ClearAreaFx("Goodbye grassss!");
                }
                else
                {

                    foreach (Transform t in areaFx)
                    {
                        GameObject g = Instantiate(grassFX, t.position, t.rotation);
                        tempAreaFx.Add(g);
                    }
                    ChangeSpellText("GRASS ZONE!");
                    cc.mageAnimator.SetTrigger("cast");
                    cc.mageAnimator.SetInteger("castInt", 1);
                    gameManager.PopDialog("Grass everywhere!", 2f);
                    terrainStatus = TerrainStatus.grass;
                    Debug.Log("Magia Grass zone!");
                }
                break;
            case 1:
                if (!cc.treeBlock)
                {
                    if (cc.currentStatus.Contains(CharacterController.Status.burned))
                    {
                        GameMan.Instance.PopDialog("Barks can't form any shield with flames", 3f);
                        AchievementManager.instance.Achive("Exotic Interaction");
                    }
                    else
                    {
                        cc.LostMagic(treeSpellCost[level]);
                        Debug.Log("Magia Shield!");
                        cc.mageAnimator.SetTrigger("cast");
                        cc.mageAnimator.SetInteger("castInt", 1);
                        ChangeSpellText("TREE BARK");
                        cc.treeShield.SetActive(true);
                        cc.treeBlock = true;

                        if (terrainStatus.Equals(TerrainStatus.grass))
                        {
                            cc.GetHealth(2);
                        }

                    }
                }
                else
                {
                    GameMan.Instance.PopDialog("I already have shield", 3f);

                }
                break;
            case 2:

                if (!bloom)
                {
                    bloom = true;
                    cc.LostMagic(treeSpellCost[level]);
                    Debug.Log("Magia 3 albero!");
                    cc.mageAnimator.SetTrigger("cast");
                    cc.mageAnimator.SetInteger("castInt", 1);
                    ChangeSpellText("Overgrow!");


                    GameObject s = Instantiate(seedPrefab, flowerPosition.position, Quaternion.identity);
                    if (terrainStatus.Equals(TerrainStatus.grass))
                    {
                        s.GetComponent<FlowerScript>().Grow();
                        AchievementManager.instance.Achive("Sylvanus Blessing");
                    }

                } else
                {
                    GameMan.Instance.PopDialog("I already spawn my child...", 1.5f);
                }




                /* //OLD SPELL
                List<PotionScript> tempList = new List<PotionScript>();
                List<FlowerScript> seedList = new List<FlowerScript>();
                foreach (PotionScript g in gameManager.levelPotions)
                {
                    if (g.potion.effectType.Equals(PotionScriptable.EffectType.wet))
                    {
                        GameObject s = Instantiate(seedPrefab, g.gameObject.transform.position, Quaternion.identity);
                        seedList.Add(s.GetComponent<FlowerScript>());
                        tempList.Add(g);

                    }
                }

                PotionScriptable c = tempList[0].potion;
                foreach (PotionScript p in tempList)
                {
                    Debug.Log(p);
                    gameManager.RemovePotion(p, true);
                    Destroy(p.gameObject);

                }



                if (terrainStatus.Equals(TerrainStatus.grass))
                {
                    foreach (FlowerScript p in seedList)
                    {
                        p.Grow();
                        cc.GetMagic(1);
                    }



                }

                tempList.Clear();
                seedList.Clear();
                bloom = false;*/
                break;
        }

    }
    public void FishCastSpell(int level)
    {
        if (cc.currentMagicPoint < pupperfishSpellCost[level])
        {
            gameManager.PopDialog("Ohoh, bloblob!", 3f);
            Debug.Log("Magia non sufficiente");
            return;
        }

        spellAudio.clip = audioSpell[3];
        spellAudio.Play();
        switch (level)
        {
            case 0:
                cc.LostMagic(pupperfishSpellCost[level]);
                if (tempAreaFx.Count >= 1)
                {
                    terrainStatus = TerrainStatus.none;
                    ClearAreaFx("Goodbye water blob blob!");
                }
                else
                {

                    foreach (Transform t in areaFx)
                    {
                        GameObject g = Instantiate(WaterFx, t.position, t.rotation);
                        tempAreaFx.Add(g);
                    }
                    ChangeSpellText("WATER ZONE!");
                    cc.mageAnimator.SetTrigger("cast");
                    cc.mageAnimator.SetInteger("castInt", 1);
                    gameManager.PopDialog("Water BLO BLOB!", 2f);
                    terrainStatus = TerrainStatus.water;
                    Debug.Log("Magia Water zone!");
                }

                break;
            case 1:

                if (cc.currentStatus.Contains(CharacterController.Status.alghe))
                {
                    cc.LostMagic(pupperfishSpellCost[level]);
                    cc.GetMagic(cc.algheLevel * 2);

                    Debug.Log("Magia EAT ALGHE!");
                    cc.mageAnimator.SetTrigger("cast");
                    cc.mageAnimator.SetInteger("castInt", 2);
                    ChangeSpellText("GNOM GNOM GNOM");
                    cc.algheLevel = 1;
                    cc.algheFx.SetActive(false);
                    cc.currentStatus.Remove(CharacterController.Status.alghe);
                    if (terrainStatus.Equals(TerrainStatus.water))
                    {
                        cc.GetHealth(2);
                    }
                }
                else
                {
                    GameMan.Instance.PopDialog("I need some alghe to eat", 3f);
                }


                break;
            case 2:
                cc.LostMagic(pupperfishSpellCost[level]);
                Debug.Log("Magia EAT ALGHE!");
                cc.mageAnimator.SetTrigger("cast");
                cc.mageAnimator.SetInteger("castInt", 1);
                pupperfishPrefab.SetActive(true);
                if (terrainStatus.Equals(TerrainStatus.water))
                {
                    cc.GetMagic(3);
                    AchievementManager.instance.Achive("BLOB!");
                }
                break;
        }

    }

    public void YetiCastSpell(int level) {

        if (cc.currentMagicPoint < yetiSpellCost[level])
        {
            gameManager.PopDialog("eh?", 1f);
            Debug.Log("Magia non sufficiente");
            return;
        }

        spellAudio.clip = audioSpell[4];
        spellAudio.Play();
        switch (level)
        {
            case 0:
                cc.LostMagic(yetiSpellCost[level]);
                if (tempAreaFx.Count >= 1)
                {
                    terrainStatus = TerrainStatus.none;
                    ClearAreaFx("nooooooo");
                }
                else
                {

                    foreach (Transform t in areaFx)
                    {
                        GameObject g = Instantiate(iceFX, t.position, t.rotation);
                        tempAreaFx.Add(g);
                    }
                    ChangeSpellText("ICE ZONE!");
                    cc.mageAnimator.SetTrigger("cast");
                    cc.mageAnimator.SetInteger("castInt", 1);
                    gameManager.PopDialog("Dance Move!", 2f);
                    terrainStatus = TerrainStatus.ice;
                    Debug.Log("Magia Ice zone!");
                }

                break;
            case 1:

                

                if(!(cc.currentHP == cc.staringHP))
                {
                    cc.LostMagic(yetiSpellCost[level]);
                    if (terrainStatus.Equals(TerrainStatus.ice))
                    {
                        Debug.Log("Ghiaccio Attivo!");
                        //cc.LostMagic(2);
                        cc.GetHealth(4);
                    }
                    else
                    {
                        //cc.LostMagic(2);
                        cc.GetHealth(3);
                    }
                    Debug.Log("Magia yeti 2!");
                    cc.mageAnimator.SetTrigger("cast");
                    cc.mageAnimator.SetInteger("castInt", 2);
                    ChangeSpellText("CONVERT");

                } else
                {
                    AchievementManager.instance.Achive("Smart but fart!"); 
                    GameMan.Instance.PopDialog("FULL", 1f);
                } 

                break;
            case 2:
                
                ChangeSpellText("PUNCH!");
                cc.mageAnimator.SetTrigger("cast");
                cc.mageAnimator.SetInteger("castInt", 3);
                if (terrainStatus.Equals(TerrainStatus.ice))
                {
                    cc.TakeDamage(1);
                }
                else
                {
                    cc.TakeDamage(2);
                }
                if (yetiPunch.activeSelf)
                {
                    yetiPunch.SetActive(false);
                    yetiPunch.SetActive(true);
                } else
                {
                    yetiPunch.SetActive(true);
                }

                
                break;
        }
    }
    public void ClearAreaFx(string s)
    {
        if (!tempAreaFx.Count.Equals(0))
        {
            foreach (GameObject g in tempAreaFx)
            {
                Destroy(g);

                gameManager.PopDialog(s, 2f);

            }
            tempAreaFx.Clear();

        }
    }





    public void OnMageMutate(Mutation mut)
    {
        mutation = mut;
        transformationThisRun++;
        ClearAreaFx("strange");
        if (transformationThisRun == 8) { AchievementManager.instance.Achive("Shapeshifter!"); }
        if (transformationThisRun == 14) { AchievementManager.instance.Achive("Ditto's Follower"); }

        //Update magic cost
        for (int i = 0; i < SpellButtonsCost.Length; i++)
        {
            switch (mutation)
            {
                case Mutation.Mage:
                    SpellButtonsCost[i].text = mageSpellCost[i].ToString();
                    terrainStatus = TerrainStatus.none;
                    break;
                case Mutation.Balrog:
                    SpellButtonsCost[i].text = balrogSpellCost[i].ToString();
                    GameMan.Instance.mutationCounter++;
                    break;
                case Mutation.Tree:
                    SpellButtonsCost[i].text = treeSpellCost[i].ToString();
                    GameMan.Instance.mutationCounter++;
                    break;
                case Mutation.Fish:
                    SpellButtonsCost[i].text = pupperfishSpellCost[i].ToString();
                    GameMan.Instance.mutationCounter++;
                    break;
                case Mutation.Yeti:
                    SpellButtonsCost[i].text = yetiSpellCost[i].ToString();
                    GameMan.Instance.mutationCounter++;
                    break;

            }
        }
        OnManaChange();
    }
    public void ChangeSpellText(string s)
    {
        magictext.text = s;
        magictext.gameObject.SetActive(true);
    }

    public void OnManaChange()
    {
        for (int i = 0; i < spellAnimators.Length; i++)
        {
            switch (mutation)
            {
                case Mutation.Mage:
                    spellAnimators[i].SetBool("isOpen", mageSpellCost[i] <= cc.currentMagicPoint);
                    spellImages[i].gameObject.SetActive(mageSpellCost[i] <= cc.currentMagicPoint);
                    spellImages[i].sprite = mageSpellSprites[i];
                    
                    break;
                case Mutation.Balrog:
                    spellAnimators[i].SetBool("isOpen", balrogSpellCost[i] <= cc.currentMagicPoint);
                    spellImages[i].gameObject.SetActive(balrogSpellCost[i] <= cc.currentMagicPoint);
                    spellImages[i].sprite = balrogSpellSprites[i];
                    break;
                case Mutation.Tree:

                    spellAnimators[i].SetBool("isOpen", treeSpellCost[i] <= cc.currentMagicPoint);
                    spellImages[i].gameObject.SetActive(treeSpellCost[i] <= cc.currentMagicPoint);
                    spellImages[i].sprite = treeSpellSprites[i];
                    break;
                case Mutation.Fish:
                    spellAnimators[i].SetBool("isOpen", pupperfishSpellCost[i] <= cc.currentMagicPoint);
                    spellImages[i].gameObject.SetActive(pupperfishSpellCost[i] <= cc.currentMagicPoint);
                    spellImages[i].sprite = fishSpellSprites[i];
                    break;
                case Mutation.Yeti:
                    spellAnimators[i].SetBool("isOpen", yetiSpellCost[i] <= cc.currentMagicPoint);
                    spellImages[i].gameObject.SetActive(yetiSpellCost[i] <= cc.currentMagicPoint);
                    spellImages[i].sprite = yetiSpellSprites[i];
                    break;

            }
        }
    }

}
