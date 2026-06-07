using UnityEngine;

public class PlayerController : MonoBehaviour
{
[Header("Movement")]
    public float moveSpeed = 10f;
    public float jumpForce = 10f;
    
    [Header("Gravity")]
    public float gravityStrength = 15f;
    public float rotationSpeed = 10f;

    private Rigidbody rb;
    private Vector3 currentGravityDir = Vector3.down; // 초기 중력은 아래쪽
    private Vector2 input;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Unity 기본 중력 끄기
        rb.freezeRotation = true; // 물리 충돌로 인한 엉뚱한 굴러감 방지
    }

    void Update()
    {
        // 1. 이동 입력 받기
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        // 2. 점프
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // 현재 중력의 반대 방향(위쪽)으로 힘을 가함
            rb.AddForce(-currentGravityDir * jumpForce, ForceMode.VelocityChange);
        }

        // 3. 중력 전환 (예: 마우스 왼쪽 클릭 시 바라보는 벽으로 중력 전환)
        if (Input.GetMouseButtonDown(0))
        {
            ChangeGravityToAimedWall();
        }
    }

    void FixedUpdate()
    {
        // 1. 커스텀 중력 지속 적용
        rb.AddForce(currentGravityDir * gravityStrength, ForceMode.Acceleration);

        // 2. 플레이어 이동 (로컬 좌표 기준)
        Vector3 moveDir = transform.TransformDirection(new Vector3(input.x, 0, input.y).normalized);
        Vector3 targetVelocity = moveDir * moveSpeed;
        
        // 현재 속도에서 수직(현재의 위아래) 속도는 유지하고, 수평 이동 속도만 덮어씌움
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        localVelocity.x = transform.InverseTransformDirection(targetVelocity).x;
        localVelocity.z = transform.InverseTransformDirection(targetVelocity).z;
        rb.linearVelocity = transform.TransformDirection(localVelocity);

        // 3. 중력 방향에 맞춰 캐릭터 기준축 회전
        AlignToGravity();
        
        // 4. 바닥 닿음 체크
        CheckGrounded();
    }

    void AlignToGravity()
    {
        // 현재 캐릭터의 '아래(-up)'가 '새로운 중력 방향'이 되도록 목표 회전값 계산
        Quaternion targetRotation = Quaternion.FromToRotation(-transform.up, currentGravityDir) * transform.rotation;
        
        // Slerp를 이용해 부드럽게 회전 적용
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }

    void ChangeGravityToAimedWall()
    {
        // 화면 중앙(크로스헤어)에서 Ray 발사
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, 50f))
        {
            // 맞은 표면의 수직 방향(Normal)의 반대 방향을 새로운 중력으로 설정하여 해당 벽으로 끌려가게 함
            currentGravityDir = -hit.normal;
        }
    }

    void CheckGrounded()
    {
        // 캐릭터 중심에서 중력 방향으로 살짝 Ray를 쏴서 바닥 체크
        isGrounded = Physics.Raycast(transform.position, currentGravityDir, 1.2f);
    }
}
