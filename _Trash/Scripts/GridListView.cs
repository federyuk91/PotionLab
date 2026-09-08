using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GridListView : MonoBehaviour
{

    public enum ViewType
    {
        Potion,
        Achievement,
        Night,
        Spells, 
        Familiar
    }

    public Text viewName;
    public ViewType view = ViewType.Potion;
    public int page = 0;
    public Element lockedElement;
    public List<Element> achievements = new List<Element>();
    public List<Element> nights = new List<Element>();
    public List<Element> potions = new List<Element>();
    public List<Element> spells = new List<Element>();
    public List<Element> familiar = new List<Element>();
    public List<Element> currentView = new List<Element>();

    public List<ElementUI> elementUIList;

    public Animator[] tabs;

    private void OnEnable()
    {/*
        for(int i=0; i<nights.Count; i++)
        {
            if (i <= GameMan.maxLevelReached)
            {
                nights[i].unlocked = true;
            }
            else
            {
                nights[i].unlocked = false;
            }
        }
        CompileView();*/
        SetView((int)view);
        
    }

    public void SetView(int newView)
    {
        if((int)view!=newView)
            tabs[(int)view].SetBool("Selected", false);
        tabs[newView].SetBool("Selected", true);
        view = (ViewType)newView;
        page = 0;
        
        CompileView();

    }

    public void NextPage()
    {
        int maxPage = currentView.Count % 5 == 0 ? (currentView.Count / 5) - 1 : (currentView.Count / 5);
        if (page < maxPage)
        {
            page++;
        }
        CompileView();
    }
    public void PrevPage()
    {
        if (page > 0)
        {
            page--;
        }
        CompileView();
    }

    public void LoadLevel(int i)
    {
        SceneManager.LoadScene(i);
    }

    public void CompileView()
    {

        switch (view)
        {
            case ViewType.Potion:
                viewName.text = "Potions";
                Debug.Log("View Potion");
                currentView = potions;
                break;
            case ViewType.Achievement:
                viewName.text = "Achievements";
                achievements = new List<Element>();
                foreach (Achievement achievement in AchievementManager.instance.achievementsData.achievements)
                {
                    achievements.Add(new Element(achievement.name, achievement.content, achievement.sprite, AchievementManager.d_achievements[achievement.name].unlocked));
                }
                Debug.Log("View Achievement");
                currentView = achievements;
                break;
            case ViewType.Night:
                viewName.text = "Nights";
                for (int i = 0; i < nights.Count; i++)
                {
                    if (i <= DataSaver.instance.maxLevelReached)
                    {
                        nights[i].unlocked = true;
                    }
                    else
                    {
                        nights[i].unlocked = false;
                    }
                }
                nights[26].unlocked = true;
                currentView = nights;
                break;
            case ViewType.Spells:
                viewName.text = "Spells";
                currentView = spells;
                break;
            case ViewType.Familiar:
                viewName.text = "Familiars";
                currentView = familiar;
                break;
        }

        Debug.Log(elementUIList.Count);
        int lastPage = currentView.Count / 5;
        for (int i = 0; i < elementUIList.Count; i++)
        {
            if (i+page*5 < currentView.Count)
            {
                Debug.Log(i + "need visualization");
                elementUIList[i].gameObject.SetActive(true);
                if (view.Equals(ViewType.Night))
                {
                    int val = 5 * page + i;
                    if (i + page * 5 <= DataSaver.instance.maxLevelReached)
                    {
                        //currentView[5*page+i].unlocked = true;
                        elementUIList[i].click.AddListener(delegate { LoadLevel(val); });
                    }
                    else
                    {
                        elementUIList[i].click.RemoveAllListeners();
                    }
                    /*else
                        currentView[5 * page + i].unlocked = false;*/
                }
                else
                {
                    elementUIList[i].click.RemoveAllListeners();
                }


                if (currentView[5 * page + i].unlocked)
                {
                    Debug.Log(i + "unloocked");
                    elementUIList[i].title = currentView[5 * page + i].name;
                    elementUIList[i].info = currentView[5 * page + i].description;
                    elementUIList[i].image.sprite = currentView[5 * page + i].sprite;
                    elementUIList[i].Show();
                }
                else
                {
                    Debug.Log(i + "loocked");
                    elementUIList[i].title = lockedElement.name;
                    elementUIList[i].info = lockedElement.description;
                    elementUIList[i].image.sprite = lockedElement.sprite;
                    elementUIList[i].Show();
                }
            }
            else
            {
                Debug.Log(i + "don't need visualization");
                elementUIList[i].gameObject.SetActive(false);
            }
        }

    }


}

[Serializable]
public class Element
{
    public string name;
    [TextArea(2, 5)]
    public string description;
    public Sprite sprite;
    public bool unlocked;

    public Element(string name, string description, Sprite sprite, bool unlocked)
    {
        this.name = name;
        this.description = description;
        this.sprite = sprite;
        this.unlocked = unlocked;
    }
}
