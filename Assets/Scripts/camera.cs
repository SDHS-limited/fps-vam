using UnityEngine;

public class camera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform playerBody; // 플레이어 몸통 오브젝트 (부모)
    [SerializeField] Transform arm;

    [Header("Sensitivity")]
    [SerializeField] float mouseSensitivity = 100f;
    [SerializeField] float clampAngle = 85f; // 상하 최대 회전 각도 제한

    private float verticalRotation = 0f;

    void Start()
    {
        // 마우스 커서를 화면 중앙에 고정하고 숨김
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ★ 게임 시작 시 저장된 마우스 감도 불러오기 (저장된 값이 없으면 인스펙터의 기본값 사용)
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", mouseSensitivity);
    }

    void Update()
    {
        // ★ 설정 창(일시정지)이 열려있을 때는 시점 이동을 막음
        if (Time.timeScale == 0f) return;

        // 마우스 입력값 받기
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 1. 좌우 회전 (Yaw)
        playerBody.Rotate(Vector3.up * mouseX);

        // 2. 상하 회전 (Pitch)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

        // 로컬 X축 기준으로 카메라만 상하 회전을 적용합니다.
        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        
        // arm.localRotation = transform.localRotation;
    }

    // ★ 설정(UI)에서 슬라이더를 움직일 때 즉각적으로 감도를 변경하기 위한 함수
    public void UpdateSensitivity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
    }
}