using UnityEngine;

public enum AbilityType
{
    DamageUp,
    FireRateUp,
    MaxHpUp,
    MoveSpeedUp,
    DashUp,

    Berserker,      // 공격력↑ 체력↓
    Vampire,        // 흡혈
    GlassCannon,    // 공격력↑↑ 체력↓↓
    GravityBurst,   // 중력 폭발

    None
}

[CreateAssetMenu(menuName = "Ability/New Ability")]
public class Ability : ScriptableObject
{
    public string abilityName;

    [TextArea]
    public string buffText;

    [TextArea]
    public string nuffText;

    public AbilityType type;

    public float value1;
    public float value2;
}