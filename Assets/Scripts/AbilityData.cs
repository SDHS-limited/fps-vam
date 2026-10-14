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
    [Header("기본 정보")]
    public string abilityName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    [Header("능력 설정")]
    public AbilityType abilityType;

    [Min(0)]
    public float value = 0.2f;
}