
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AchievementManager : MonoBehaviour
{
    public Animator animator;
    public Text name_txt;
    public Text content_txt;
    public Image image;
    public static AchievementManager instance;

    public AchievementData achievementsData;
    public static Dictionary<string, Achievement> d_achievements;


    private void Awake()
    {
        instance = this;
        if (d_achievements == null)
        {
            Debug.Log("Carico achievement database");
            d_achievements = new Dictionary<string, Achievement>();
            foreach (Achievement ach in achievementsData.achievements)
            {
                if (!d_achievements.ContainsKey(ach.name))
                {
                    Achievement toAdd = new Achievement(ach.name, ach.content, ach.sprite, ach.unlocked);
                    d_achievements.Add(ach.name, toAdd);
                }
            }
        }
    }

    /*private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            Achive("Old Toby");
    }*/

    public void Achive(string name)
    {
        if (d_achievements[name].unlocked)
        {
            return;
        }
        d_achievements[name].unlocked = true;
        name_txt.text = d_achievements[name].name;
        content_txt.text = d_achievements[name].content;
        image.sprite = d_achievements[name].sprite;

        animator.SetTrigger("pop");
    }
}

