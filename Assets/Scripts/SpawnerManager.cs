using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnerManager : MonoBehaviour
{
    public List<PhaseSettings> phases;
    public float spawnSeconds = 5f;
    public int phase = 0;
    public bool spawning = true;
    public bool hyperMode = false;
    public Image lightBar;

    private AudioSource phaseSound;
    private EventListener eventListener;
    public EventType current, last;

    public float timer = 0f;
    private float interval = 43f;

    private void Start()
    {
        phaseSound = GetComponent<AudioSource>();
        eventListener = GetComponent<EventListener>();
        StartCoroutine(Spawn());
        GameMan.Instance.nightCount.text = "Phase: " + phase;
        hyperMode = GameMan.Instance.hyperMode;
        if (hyperMode) { spawnSeconds = 3; } else if (GameMan.Instance.hyperHyperMode) { spawnSeconds = 2f; } else { spawnSeconds = 5; }
    }

    void Update()
    {
        timer += Time.deltaTime;
      
        if (timer >= interval)
        {
            eventListener.SetSceneLightsLevel(GameMan.Instance.lightIntensity - 1);
            timer = 0f;
        }

        lightBar.fillAmount = timer / interval;
    }


    IEnumerator Spawn()
    {

      
        if (GameMan.Instance.hyperHyperMode) { spawnSeconds = 2; } else
        {
            spawnSeconds -= phases[phase].spawnSpeedIncrement;
        }
        //spawnSeconds = Mathf.Clamp(spawnSeconds, 1f, 5f);
        int spawnCount = 0;

        while (spawnCount < phases[phase].nextSetupAfterSpawnedPotion)
        {
            yield return new WaitForSeconds(spawnSeconds);
            GameObject potionObj = Instantiate(phases[phase].PickRandomPotion(), transform.position, Quaternion.identity);
            PotionScript potion = potionObj.GetComponent<PotionScript>();
            potion.isActive = true;
            potion.DropPotion();
            GameMan.Instance.levelPotions.Add(potion);
            if (spawnSeconds < 1) { spawnSeconds = 1; }
            GameMan.Instance.spawnedPotion++;

            if (GameMan.Instance.levelPotions.Count > 30)
            {

                StartEvent(PhaseSettings.EventType.Bomb, 0);

            }
            spawnCount++;
        }

        StartEvent(phases[phase].eventType, phases[phase].eventValue);

        phase++;
        phaseSound.Play();
        GameMan.Instance.nightCount.text = "Phase: " + phase;

       

        if (phase < phases.Count)
        {
            Debug.Log("Starting spawning phase " + phase);
            StartCoroutine(Spawn());
        }
        else
        {
            Debug.LogWarning("End spawn or restart from phase 0");
            phase = 0;
            StartCoroutine(Spawn());
        }
        
    }

    public void StartEvent(PhaseSettings.EventType eventType, float value)
    {
        switch (eventType)
        {
            case PhaseSettings.EventType.None:
                break;

            case PhaseSettings.EventType.Obstacle:
                eventListener.RandomObstacle();
                break;

            case PhaseSettings.EventType.LightVariation:
                eventListener.SetSceneLightsLevel(value);
                break;

            case PhaseSettings.EventType.ChangeSpeedBottomSlider:
                eventListener.SetBottomPlatformSpeed(value);
                break;

            case PhaseSettings.EventType.ChangeSpeedUpperSlider:
                eventListener.SetUpperPlatformSpeed(value);
                break;

            case PhaseSettings.EventType.SpawnFamiliar:
                eventListener.SpawnFamiliar();
                break;
            case PhaseSettings.EventType.Bomb:
                Debug.Log("spawnbomb");
                eventListener.SpawnFamiliarBomb();
                break;
        }
    }



}
