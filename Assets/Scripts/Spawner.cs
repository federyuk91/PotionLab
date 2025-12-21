using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Spawner : MonoBehaviour
{
    public PhaseSettings spawnSettings;
    public PotionScript potion;
    public BoxCollider2D blockCollider;
    private AudioSource audio;
    public GameObject spawnerButton;

    public GameObject currentPot;

    float dropTime = 1.5f;

    public bool stopDrop = false;

    private void Start()
    {
        audio = GetComponent<AudioSource>();
        //spawnerButton.SetActive(false);

        Spawn();
        StartCoroutine(CheckPot());

    }

    private void Update()
    {
        //Qualche evento come bomba o pesce palla ha distrutto la pozione da droppare e la rimpiazzo
        if (potion == null)
        {
            spawnerButton.SetActive(false);
        }
    }

    public void Spawn()
    {
        audio.Play();
        //Instanzio la nuova pozione, scelta casualmente, alla posizione dello spawner
        GameObject potionObj = Instantiate(spawnSettings.PickRandomPotion(), transform.position, Quaternion.identity);
        potion = potionObj.GetComponent<PotionScript>();
        //Riattivo il collider per essere sicuro che la pozione non cada direttamente nel livello
        blockCollider.enabled = true;

        //Questo controllo è probabilmente superfluo, la pozione è appena stata instanziata quindi esiste
        if (potion != null)
        {
            potion.isActive = true;
            GameMan.Instance.spawnedPotion++;
            GameMan.Instance.levelPotions.Add(potion);
            potion.DropPotion();
        }
    }

    public void ActivateButton()
    {
        //Riattivo il bottone
        spawnerButton.SetActive(true);
    }

    public void DropPotion()
    {
        if (!stopDrop && (GameMan.Instance.cc.currentHP != 0))
            DropRoutine();
        else
            Debug.Log("Wait");
    }


    public void DropRoutine()
    {
        //Disabilito il blocco permettendo alla pozione di cadere
        blockCollider.enabled = false;
        //Disattivo il bottone di spawn per evitare venga premuto ripetutamente troppo velocemente 
        spawnerButton.SetActive(false);
        //Aspetto droptime e creo una nuova pozione
        Invoke("Spawn", dropTime);
    }

    public void DeactivateCollider()
    {
        //????? CHE ROBA E'? 
        blockCollider.enabled = true;
    }


    public IEnumerator CheckPot()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            if (potion == null)
            {
                Spawn();
            }

        }
    }






}
