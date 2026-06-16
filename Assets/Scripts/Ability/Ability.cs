using UnityEngine;

[System.Serializable]
public class Ability : ScriptableObject
{
    public string abilityName;

    public string buffText;
    public string nuffText;

    public int damageBonus;
    public int hpPenalty;
}