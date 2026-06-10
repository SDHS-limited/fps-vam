using UnityEngine;
using TMPro;

public class AbilityManager : MonoBehaviour
{
    public TextMeshProUGUI abilityName;
    public TextMeshProUGUI buffText;
    public TextMeshProUGUI nuffText;

    public Ability[] abilities;

    public Ability currentAbility;

    public void GenerateRandomAbility()
    {
        currentAbility =
            abilities[Random.Range(0, abilities.Length)];

        abilityName.text = currentAbility.abilityName;
        buffText.text = currentAbility.buffText;
        nuffText.text = currentAbility.nuffText;
    }
}