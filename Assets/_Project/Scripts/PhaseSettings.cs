using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PhaseSettings", menuName = "ScriptableObjects/PhaseSettings", order = 2), Serializable]
public class PhaseSettings : ScriptableObject
{
    public enum EventType
    {
        None,
        LightVariation,
        Obstacle,
        ChangeSpeedUpperSlider,
        ChangeSpeedBottomSlider,
        SpawnFamiliar,
        Bomb,
    }

    public EventType eventType;
    public float eventValue = 0;

    public int nextSetupAfterSpawnedPotion = 0;
    public float spawnSpeedIncrement;
    public float platformSpeedIncrement;
    public List<SpawnChanche> spawnChanches;

    public float total = 0;

    public GameObject PickRandomPotion()
    {
        float chance = UnityEngine.Random.Range(0f, 100f);
        float minChance = 0f;

        foreach (SpawnChanche sc in spawnChanches)
        {
            if (chance < sc.chance + minChance)
            {
                return sc.potion.gameObject;
            }
            else
            {
                minChance += sc.chance;
            }
        }
        //Se il for sopra funziona bene, qui non si arriva mai
        return spawnChanches[0].potion.gameObject;
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        total = 0;
        foreach(SpawnChanche sc in spawnChanches)
        {
            total += sc.chance;
            if (sc.potion != null)
                sc.name = sc.potion.name.Replace("Potion","")+" "+sc.chance+"%";
        }
    }
#endif

}



[Serializable]
public class SpawnChanche
{
    public string name;
    public PotionScript potion;
    public float chance = 10f;

    
}
