using UnityEngine;

public enum AbilityType
{
    MoveSpeed,
    AttackDamage,
    MaxHealth,
    Heal,
    
}

[CreateAssetMenu(
    fileName = "NewAbility",
    menuName = "Roguelike/Ability"
)]
public class AbilityData : ScriptableObject
{

    [TextArea(2, 4)]
    public string Buffdescription;
    public string Nuffdescription;

    [Header("능력 설정")]
    public AbilityType abilityType;

    [Min(0)]
    public float value = 0.2f;
}