using UnityEngine;

public class AdvancedGravitySystem : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public float rotationSpeed = 10f;

    [Header("Dash to Wall Settings")]
    public float dashSpeed = 40f; // 벽으로 날아가는 속도
    public float playerCenterOffset = 1.0f; // 캐릭터 중심점 오프셋 (캡슐 콜라이더 높이의 절반)

    [Header("Gravity Settings")]
    public float gravityStrength = 20f;
    public Transform cameraTransform;

    private Rigidbody rb;
    private Vector3 currentGravityDir = Vector3.down;
    private bool isGrounded;
    
    // 상태 체크용 변수
    private bool isDashing = false;
    private Vector3 dashTargetPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // 기본 중력 끄기
        rb.freezeRotation = true;
    }

    void Update()
    {
        // 대시 중일 때는 입력을 받지 않음
        if (!isDashing)
        {
            HandleInput();
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            // 1. 대시 중: 중력 무시, 목표 지점으로 직선 이동
            PerformDash();
            // 대시하는 동안 몸통도 새 중력에 맞춰 회전
            ApplyRotation(); 
        }
        else
        {
            // 2. 평상시: 중력 적용 및 이동
            ApplyGravity();
            ApplyRotation();
            HandleMovement();
            CheckGrounded();
        }
    }

    private void HandleInput()
    {
        // 타겟팅 중력 전환 (좌클릭)
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                StartDashToWall(hit);
            }
        }

        // 점프
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(-currentGravityDir * jumpForce, ForceMode.VelocityChange);
        }
    }

    private void StartDashToWall(RaycastHit hit)
    {
        isDashing = true;
        currentGravityDir = -hit.normal; // 새로운 중력(바닥) 방향 설정

        // 목표 위치 계산: 벽에 파묻히지 않도록, 맞은 지점에서 법선(Normal) 방향으로 플레이어 반경만큼 띄움
        dashTargetPosition = hit.point + (hit.normal * playerCenterOffset);

        // 기존에 가지고 있던 속도(가속도, 관성)를 완전히 0으로 만들어 추락 방지
        rb.linearVelocity = Vector3.zero;
    }

    private void PerformDash()
    {
        // 현재 위치에서 목표 위치로 일정한 속도(dashSpeed)로 직선 이동
        Vector3 newPos = Vector3.MoveTowards(rb.position, dashTargetPosition, dashSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        // 목표 지점에 거의 도달했는지 체크
        if (Vector3.Distance(rb.position, dashTargetPosition) < 0.1f)
        {
            isDashing = false; // 대시 종료, 일반 물리 상태로 복귀
            rb.linearVelocity = Vector3.zero; // 착지 시 미끄러짐 방지
        }
    }

    private void ApplyGravity()
    {
        rb.AddForce(currentGravityDir * gravityStrength, ForceMode.Acceleration);
    }

    private void ApplyRotation()
    {
        Vector3 newUp = -currentGravityDir;
        Vector3 newForward = Vector3.ProjectOnPlane(transform.forward, newUp).normalized;

        if (newForward.sqrMagnitude < 0.01f)
        {
            newForward = Vector3.ProjectOnPlane(transform.up, newUp).normalized;
        }

        Quaternion targetRotation = Quaternion.LookRotation(newForward, newUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = transform.TransformDirection(new Vector3(x, 0, y).normalized);
        Vector3 targetVelocity = moveDir * moveSpeed;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        Vector3 targetLocalVel = transform.InverseTransformDirection(targetVelocity);
        
        localVel.x = targetLocalVel.x;
        localVel.z = targetLocalVel.z;
        
        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    private void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, currentGravityDir, playerCenterOffset + 0.2f);
    }
}
