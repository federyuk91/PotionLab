
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
    public bool mainMenu = false;
    public GameObject[] achivement;
    public Text title, subtitle;
    public AchievementData achievementsData;
    public static Dictionary<string, Achievement> d_achievements;

    public Text t_unlockedAchievementCount;
    public static int UnlockedAchievementsCount
    {
        get
        {
            int count = 0;
            foreach (Achievement ach in d_achievements.Values)
            {
                if (ach.unlocked)
                    count++;
            }
            return count;
        }
    }


    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            foreach (Achievement ach in d_achievements.Values)
            {
                if (ach.name != "The Mage")
                {
                    Achive(ach.name);
                }
            }
        }
    }

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
                    if (PlayerPrefs.HasKey(ach.name))
                    {
                        //se diverso da 0 allora è true
                        toAdd.unlocked = PlayerPrefs.GetInt(ach.name) != 0;
                    }
                    else
                    {
                        //Se non è presente lo inizializzo a false
                        PlayerPrefs.SetInt(ach.name, 0);
                    }
                    d_achievements.Add(ach.name, toAdd);


                }
            }
        }
        if (mainMenu)
        {
            int i = 0;
            foreach (Achievement ach in d_achievements.Values)
            {

                achivement[i].GetComponent<Button>().interactable = ach.unlocked;
                achivement[i].name = ach.name;

                achivement[i].GetComponent<Button>().onClick.RemoveAllListeners();
                achivement[i].GetComponent<Button>().onClick.AddListener(delegate { ShowAchiv(ach.name); });
                //achivement[i].GetComponentInChildren<Image>().color = Color.white;
                if (ach.unlocked)
                    achivement[i].transform.GetChild(0).GetComponent<Image>().sprite = ach.sprite;
                i++;
            }
            t_unlockedAchievementCount.text = "" + UnlockedAchievementsCount + "/" + d_achievements.Values.Count;
        }


    }


    public void Achive(string name)
    {
        if (d_achievements[name].unlocked)
        {
            return;
        }
        //Salvo l'achievement come sbloccato
        PlayerPrefs.SetInt(name, 1);
        d_achievements[name].unlocked = true;
        name_txt.text = d_achievements[name].name;
        content_txt.text = d_achievements[name].content;
        image.sprite = d_achievements[name].sprite;

        animator.SetTrigger("pop");

        if (name != "The Mage")
        {
            bool isTheMage = true;
            foreach (Achievement ach in d_achievements.Values)
            {
                if (ach.name != "The Mage")
                {
                    isTheMage = ach.unlocked;
                }
                if (!isTheMage)
                {
                    Debug.Log("Not the mage");
                    break;
                }
            }
            if (isTheMage)
            {
                Debug.Log("The Mage is here");
                Achive("The Mage");
            }
        }
    }


    public void ShowAchiv(string achivName)
    {
        Debug.Log(achivName);
        title.text = d_achievements[achivName].name;
        subtitle.text = d_achievements[achivName].content;
    }

    public void CancelAchiv()
    {
        title.text = "ACHIVEMENT";
        subtitle.text = "Have fun!";
    }

    public void ResetAchievement()
    {
        foreach (Achievement ach in d_achievements.Values)
        {
            ach.unlocked = false;
        }
    }
}

