using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Achievement Database", order = 1)]
public class AchievementData : ScriptableObject
{
    public List<Achievement> achievements = new List<Achievement>();
}

[Serializable]
public class Achievement
{
    public string name;
    public string content;
    public Sprite sprite;
    public bool unlocked = false;

    public Achievement(string name, string content, Sprite sprite, bool unlocked)
    {
        this.name = name;
        this.content = content;
        this.sprite = sprite;
        this.unlocked = unlocked;
    }

    public override bool Equals(object obj)
    {
        return obj is Achievement achievement &&
               name == achievement.name;
    }
}