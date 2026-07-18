using System.Collections;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public PhaseSettings spawnSettings;
    public PotionScript potion;
    public BoxCollider2D blockCollider;
    private AudioSource audioSource;
    public GameObject spawnerButton;

    public GameObject currentPot;

    float dropTime = 1.5f;

    public bool stopDrop = false;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
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
        if (audioSource != null)
        {
            audioSource.Play();
        }

        //Instanzio la nuova pozione, scelta casualmente, alla posizione dello spawner
        GameObject potionObj = Instantiate(spawnSettings.PickRandomPotion(), transform.position, Quaternion.identity);
        potion = potionObj.GetComponent<PotionScript>();
        //Riattivo il collider per essere sicuro che la pozione non cada direttamente nel livello
        blockCollider.enabled = true;

        //Questo controllo è probabilmente superfluo, la pozione è appena stata instanziata quindi esiste
        if (potion != null)
        {
            potion.isActive = true;
            RegisterSpawnedPotion(potion);
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
        if (!stopDrop && IsCharacterAlive())
        {
            DropRoutine();
            return;
        }

        Debug.Log("Wait");
    }


    public void DropRoutine()
    {
        //Disabilito il blocco permettendo alla pozione di cadere
        blockCollider.enabled = false;
        //Disattivo il bottone di spawn per evitare venga premuto ripetutamente troppo velocemente 
        spawnerButton.SetActive(false);
        //Aspetto droptime e creo una nuova pozione
        Invoke(nameof(Spawn), dropTime);
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

    private void RegisterSpawnedPotion(PotionScript spawnedPotion)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSpawnedPotion(spawnedPotion);
            return;
        }

        if (GameMan.Instance != null)
        {
            GameMan.Instance.spawnedPotion++;
            GameMan.Instance.levelPotions.Add(spawnedPotion);
        }
    }

    private bool IsCharacterAlive()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.IsCharacterAlive();
        }

        if (GameMan.Instance != null && GameMan.Instance.cc != null)
        {
            return GameMan.Instance.cc.currentHP != 0;
        }

        return true;
    }

}
