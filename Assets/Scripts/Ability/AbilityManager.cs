using UnityEngine;
using TMPro;

public class AbilityManager : MonoBehaviour
{
    public Ability[] abilities;

    public AbilityPanel panel1;
    public AbilityPanel panel2;
    public AbilityPanel panel3;

    public void GenerateAbilities()
{
    panel1.SetAbility(
        abilities[Random.Range(0, abilities.Length)]
    );

    panel2.SetAbility(
        abilities[Random.Range(0, abilities.Length)]
    );

    panel3.SetAbility(
        abilities[Random.Range(0, abilities.Length)]
    );
}
}