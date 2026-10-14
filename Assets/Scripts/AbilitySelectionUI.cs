using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilitySelectionUI : MonoBehaviour
{
    [Serializable]
    public class AbilitySlot
    {
        public Button button;
        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public Image icon;
    }

    [Header("UI")]
    [SerializeField] private GameObject abilityPanel;

    [SerializeField]
    private AbilitySlot[] slots = new AbilitySlot[3];

    [Header("등장 가능한 능력")]
    [SerializeField] private List<AbilityData> abilities;

    [Header("설정")]
    [SerializeField] private bool pauseGame = true;

    private AbilityData[] currentAbilities = new AbilityData[3];

    private GravityPlayerController currentPlayer;

    private bool isSelecting;

    private float previousTimeScale;
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    private void Awake()
    {
        abilityPanel.SetActive(false);

        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;

            if (slots[i] != null && slots[i].button != null)
            {
                slots[i].button.onClick.AddListener(
                    () => SelectAbility(index)
                );
            }
        }
    }

    public bool Open(GravityPlayerController player)
    {
        if (isSelecting || player == null)
            return false;

        if (abilityPanel == null ||
            slots == null ||
            slots.Length != 3)
        {
            Debug.LogError("능력 UI 설정을 확인하세요.");
            return false;
        }

        // 중복된 능력 데이터 제거
        List<AbilityData> pool = new List<AbilityData>();

        foreach (AbilityData ability in abilities)
        {
            if (ability != null && !pool.Contains(ability))
            {
                pool.Add(ability);
            }
        }

        if (pool.Count < 3)
        {
            Debug.LogWarning("능력을 최소 3개 등록하세요.");
            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            if (slots[i] == null ||
                slots[i].button == null ||
                slots[i].nameText == null ||
                slots[i].descriptionText == null)
            {
                Debug.LogError("능력 카드 UI 연결이 누락되었습니다.");
                return false;
            }
        }

        currentPlayer = player;

        // 랜덤으로 중복 없이 3개 선택
        for (int i = 0; i < 3; i++)
        {
            int randomIndex = UnityEngine.Random.Range(
                i,
                pool.Count
            );

            AbilityData temp = pool[i];
            pool[i] = pool[randomIndex];
            pool[randomIndex] = temp;

            currentAbilities[i] = pool[i];
        }

        // 선택된 능력의 UI 설정
        for (int i = 0; i < 3; i++)
        {
            AbilityData ability = currentAbilities[i];

            slots[i].nameText.text = ability.abilityName;
            slots[i].descriptionText.text = ability.description;

            if (slots[i].icon != null)
            {
                slots[i].icon.sprite = ability.icon;
                slots[i].icon.enabled = ability.icon != null;
            }

            slots[i].button.interactable = true;
        }

        // 기존 상태 저장
        previousTimeScale = Time.timeScale;
        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        isSelecting = true;
        abilityPanel.SetActive(true);

        // 게임 정지
        if (pauseGame)
        {
            Time.timeScale = 0f;
        }

        // 마우스 사용 가능하도록 설정
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        return true;
    }

    private void SelectAbility(int index)
    {
        if (!isSelecting)
            return;

        if (index < 0 || index >= currentAbilities.Length)
            return;

        AbilityData selected = currentAbilities[index];

        if (selected == null)
            return;

        // 선택한 능력 적용
        currentPlayer.ApplyAbility(selected);

        Close();
    }

    private void Close()
    {
        if (!isSelecting)
            return;

        isSelecting = false;

        abilityPanel.SetActive(false);

        // 기존 시간 상태 복구
        if (pauseGame)
        {
            Time.timeScale = previousTimeScale;
        }

        // 기존 마우스 상태 복구
        Cursor.lockState = previousLockState;
        Cursor.visible = previousCursorVisible;

        currentPlayer = null;
    }

    private void OnDisable()
    {
        if (isSelecting)
        {
            Close();
        }
    }
}