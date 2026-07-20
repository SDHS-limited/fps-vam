using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KeyBindUI : MonoBehaviour
{
    [Header("버튼 안의 텍스트들을 연결해주세요")]
    public Text movementText; // ★ 기존 4방향 텍스트를 하나로 통합!
    public Text jumpText;
    public Text attackText;
    public Text pushText;
    public Text dashText;
    public Text grenadeText;

    private string actionToRebind = null;
    private int movementBindStep = 0; // ★ 4방향 연속 입력을 위한 스텝 변수

    void Start()
    {
        UpdateAllTexts();
    }

    public void StartRebind(string actionName)
    {
        actionToRebind = actionName;

        // ★ 움직임(Movement) 버튼을 클릭했을 때의 특별 처리
        if (actionName == "Movement")
        {
            movementBindStep = 1; // 1단계: 앞으로 가기부터 시작
            movementText.text = "앞으로(W) 입력 대기중...";
            actionToRebind = "Forward";
            return;
        }

        // 일반 단일 버튼 처리
        GetTextForAction(actionName).text = "입력 대기중...";
    }

    void Update()
    {
        if (actionToRebind != null && Input.anyKeyDown)
        {
            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    // ESC 누르면 취소 및 초기화
                    if (keyCode == KeyCode.Escape)
                    {
                        UpdateAllTexts();
                        actionToRebind = null;
                        movementBindStep = 0;
                        return;
                    }

                    // 감지된 키를 KeyManager에 저장
                    KeyManager.instance.SaveKey(actionToRebind, keyCode);

                    // ★ 움직임 다중 바인딩 로직 (W -> A -> S -> D 순서로 요구)
                    if (movementBindStep > 0)
                    {
                        movementBindStep++;
                        
                        if (movementBindStep == 2)
                        {
                            actionToRebind = "Left";
                            movementText.text = "왼쪽으로(A) 입력 대기중...";
                        }
                        else if (movementBindStep == 3)
                        {
                            actionToRebind = "Backward";
                            movementText.text = "뒤로(S) 입력 대기중...";
                        }
                        else if (movementBindStep == 4)
                        {
                            actionToRebind = "Right";
                            movementText.text = "오른쪽으로(D) 입력 대기중...";
                        }
                        else
                        {
                            // 4방향 모두 설정 완료
                            movementBindStep = 0;
                            actionToRebind = null;
                            UpdateAllTexts();
                        }
                    }
                    else
                    {
                        // 단일 키 버튼 설정 완료
                        UpdateAllTexts();
                        actionToRebind = null;
                    }
                    break;
                }
            }
        }
    }

    public void UpdateAllTexts()
    {
        if (KeyManager.instance == null) return;
        
        // ★ 4방향 키를 W / A / S / D 형태로 예쁘게 묶어서 출력
        movementText.text = $"{KeyManager.instance.keys["Forward"]} / {KeyManager.instance.keys["Left"]} / {KeyManager.instance.keys["Backward"]} / {KeyManager.instance.keys["Right"]}";
        
        jumpText.text = KeyManager.instance.keys["Jump"].ToString();
        attackText.text = KeyManager.instance.keys["Attack"].ToString();
        pushText.text = KeyManager.instance.keys["Push"].ToString();
        dashText.text = KeyManager.instance.keys["Dash"].ToString();
        grenadeText.text = KeyManager.instance.keys["Grenade"].ToString();
    }

    Text GetTextForAction(string action)
    {
        switch (action)
        {
            case "Jump": return jumpText;
            case "Attack": return attackText;
            case "Push": return pushText;
            case "Dash": return dashText;
            case "Grenade": return grenadeText;
            default: return null;
        }
    }
}