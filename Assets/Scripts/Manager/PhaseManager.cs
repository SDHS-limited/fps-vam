using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PhaseManager : MonoBehaviour
{
    public int currentPhase = 1;
    public TextMeshProUGUI text;

    public void StartPhase()
    {
        text.text = "Phase" + currentPhase;

        currentPhase++;
    }
}
