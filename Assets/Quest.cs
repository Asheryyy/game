using UnityEngine;
using System;

[Serializable]
public class Quest
{
    public string questId;
    public string questName;
    public string description;
    public bool isCompleted;
    public bool isActive;
    public bool isGoalReached;
}
