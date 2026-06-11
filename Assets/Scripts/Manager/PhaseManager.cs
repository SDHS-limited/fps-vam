using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PhaseManager : MonoBehaviour
{
    public int currentPhase = 1;
    public TextMeshProUGUI text;
    public Animator animator;

    void Start()
    {
        text.text = "Phase" + currentPhase;
    }

    public void StartPhase()
    {
        currentPhase++;
        animator.SetBool("clear", true);
        //페이즈 시작
    }
}
