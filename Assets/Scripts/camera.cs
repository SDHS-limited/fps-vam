using UnityEngine;

public class camera : MonoBehaviour
{
    [Header("Target")]
    public Transform playerBody; // 플레이어 몸통 오브젝트 (부모)

    [Header("Sensitivity")]
    public float mouseSensitivity = 100f;
    public float clampAngle = 85f; // 상하 최대 회전 각도 제한

    private float verticalRotation = 0f;

    void Start()
    {
        // 마우스 커서를 화면 중앙에 고정하고 숨김
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 마우스 입력값 받기
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 1. 좌우 회전 (Yaw)
        // 월드 Y축이 아니라, 플레이어 몸통의 '로컬 Y축(Vector3.up)'을 기준으로 회전시킵니다.
        // 이 한 줄 덕분에 벽에 붙어있든 천장에 붙어있든 정상적으로 좌우를 돌아봅니다.
        playerBody.Rotate(Vector3.up * mouseX);

        // 2. 상하 회전 (Pitch)
        verticalRotation -= mouseY;
        // 카메라가 머리 뒤로 넘어가지 않도록 상하 각도를 제한합니다.
        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

        // 로컬 X축 기준으로 카메라만 상하 회전을 적용합니다.
        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}
