using UnityEngine;
 
/// <summary>
/// 큐브 면 자동 감지 중력 시스템
/// - 공중에서도 큐브 면에 가까워지면 중력 전환
///
/// [씬 셋업]
/// 1. Player GameObject
///    - Rigidbody (useGravity OFF, Freeze Rotation XYZ 체크)
///    - CapsuleCollider (Radius: 0.4, Height: 1.8)
///    - 이 스크립트 추가
/// 2. Player 자식 Main Camera → Local Position (0, 0.7, 0) → [Camera Transform] 슬롯
/// 3. 큐브 Tag → "Ground"
/// 4. Project Settings → Physics → Gravity → (0, 0, 0)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class GravityPlayerController : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed  = 7f;
    public float jumpForce  = 8f;
    [Range(0f, 1f)]
    public float airControl = 0.3f;
 
    [Header("중력")]
    public float gravStrength    = 25f;
    public float gravRotateSpeed = 8f;
 
    [Header("카메라")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2.5f;
    [Range(10f, 89f)]
    public float pitchLimit = 80f;
 
    [Header("면 감지")]
    public string    groundTag   = "Ground";
    public LayerMask groundLayer = ~0;

    [Header("대쉬")]
    public float dashForce = 20f;
    public float dashCooldown = 1f;

    private bool canDash = true;
 
    // 공중 포함 감지 거리. 클수록 멀리서 전환. 권장 1.0~2.0
    [Range(0.5f, 100.0f)]
    public float faceDetectRange = 1.5f;
 
    // 전환 쿨다운
    [Range(0.1f, 0.5f)]
    public float switchCooldown = 0.25f;
 
    // ── 내부 ──────────────────────────────────────────────────────
 
    Rigidbody       rb;
    CapsuleCollider col;
 
    Vector3 gravDir       = Vector3.down;
    Vector3 targetGravDir = Vector3.down;
 
    bool  isGrounded  = false;
    bool  jumpQueued  = false;
    float pitch       = 0f;
    float cooldown    = 0f;
    float flashTimer  = 0f;
    string faceLabel  = "Bottom";
 
    static readonly Vector3[] SixDirs =
    {
        Vector3.down, Vector3.up,
        Vector3.right, Vector3.left,
        Vector3.forward, Vector3.back
    };
 
    void Awake()
    {
        rb  = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
 
        rb.useGravity             = false;
        rb.freezeRotation         = true;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
 
        if (!cameraTransform)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam) cameraTransform = cam.transform;
        }
 
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }
 
    void Update()
    {
        CameraLook();
 
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            jumpQueued = true;

        if (Input.GetMouseButtonDown(1) && canDash)
        {
            Dash();
        }
 
        if (Input.GetKeyDown(KeyCode.Escape))
        { Cursor.lockState = CursorLockMode.None;   Cursor.visible = true; }
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
 
        if (cooldown   > 0f) cooldown   -= Time.deltaTime;
        if (flashTimer > 0f) flashTimer -= Time.deltaTime;
    }
 
    void FixedUpdate()
    {
        gravDir = Vector3.Slerp(gravDir, targetGravDir,
                                Time.fixedDeltaTime * gravRotateSpeed * 3f).normalized;
 
        CheckGround();
        DetectFace();
        rb.AddForce(gravDir * gravStrength, ForceMode.Acceleration);
        Move();
 
        if (jumpQueued && isGrounded)
        { Jump(); jumpQueued = false; }
 
        AlignBody();
    }
 
    // ── 면 감지 ───────────────────────────────────────────────────
    // 접근 방식: 큐브 중심→플레이어 방향으로 역산하지 않고
    // 플레이어 주변 6방향 SphereCast + OverlapSphere 병행
    // 지면 접촉 여부와 무관하게 항상 실행
 
    void DetectFace()
    {
        if (cooldown > 0f) return;
 
        Vector3 center = transform.TransformPoint(col.center);
        float   r      = col.radius * 0.85f;
 
        Vector3 bestDir  = Vector3.zero;
        float   bestDist = float.MaxValue;  // 가장 가까운 면 우선
 
        foreach (Vector3 dir in SixDirs)
        {
            // 현재 목표 중력과 같은 방향 → 이미 그 면이 바닥 → 스킵
            if (Vector3.Dot(dir, targetGravDir) > 0.9f) continue;
 
            // SphereCast: 캡슐 중심에서 각 방향으로
            if (!Physics.SphereCast(
                    center, r, dir,
                    out RaycastHit hit,
                    faceDetectRange,          // ← 공중 포함 넓은 감지 거리
                    groundLayer,
                    QueryTriggerInteraction.Ignore))
                continue;
 
            if (!hit.collider.CompareTag(groundTag)) continue;
 
            // 가장 가까운 면 선택
            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                bestDir  = -hit.normal;   // 법선 반대 = 새 중력 방향
            }
        }
 
        if (bestDir == Vector3.zero) return;
 
        // 현재 목표 중력과 실질적으로 다를 때만 전환
        if (Vector3.Dot(bestDir, targetGravDir) > 0.95f) return;
 
        SwitchGravity(bestDir);
    }

    void Dash()
    {
        canDash = false;

        // 카메라 기준 방향
        Vector3 forward =
            Vector3.ProjectOnPlane(
                cameraTransform.forward,
                gravDir).normalized;

        Vector3 right =
            Vector3.ProjectOnPlane(
                cameraTransform.right,
                gravDir).normalized;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 dashDir = (forward * v + right * h).normalized;

        // 입력 없으면 바라보는 방향
        if (dashDir == Vector3.zero)
            dashDir = forward;

        // 현재 중력 방향 속도 유지
        Vector3 vel = rb.linearVelocity;
        Vector3 gravVel =
            gravDir * Vector3.Dot(vel, gravDir);

        rb.AddForce(
            dashDir * dashForce,
            ForceMode.VelocityChange);

        Invoke(nameof(ResetDash), dashCooldown);
    }

    void ResetDash()
    {
        canDash = true;
    }
 
    void SwitchGravity(Vector3 newDir)
    {
        // 기존 중력 방향 속도 감쇠 (부드러운 전환)
        Vector3 vel  = rb.linearVelocity;
        float   comp = Vector3.Dot(vel, gravDir);
        rb.linearVelocity = vel - gravDir * comp * 0.6f;
 
        targetGravDir = newDir;
        faceLabel     = GetFaceLabel(newDir);
        cooldown      = switchCooldown;
        flashTimer    = 0.25f;
    }
 
    // ── 카메라 ────────────────────────────────────────────────────
 
    void CameraLook()
    {
        if (!cameraTransform) return;
 
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
 
        transform.Rotate(-gravDir, mx, Space.World);
 
        pitch = Mathf.Clamp(pitch - my, -pitchLimit, pitchLimit);
        cameraTransform.localRotation = Quaternion.Euler(pitch, pitch, 0f);
    }
 
    // ── 이동 ──────────────────────────────────────────────────────
 
    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (h == 0f && v == 0f) return;
 
        Vector3 fwd   = Vector3.ProjectOnPlane(cameraTransform.forward, gravDir).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right,   gravDir).normalized;
        Vector3 dir   = (fwd * v + right * h).normalized;
        float   ctrl  = isGrounded ? 1f : airControl;
 
        Vector3 vel     = rb.linearVelocity;
        Vector3 vertVel = gravDir * Vector3.Dot(vel, gravDir);
        Vector3 horiVel = vel - vertVel;
 
        rb.linearVelocity = Vector3.Lerp(horiVel, dir * moveSpeed * ctrl,
                                         isGrounded ? 0.22f : 0.07f) + vertVel;
    }
 
    // ── 점프 ──────────────────────────────────────────────────────
 
    void Jump()
    {
        Vector3 vel  = rb.linearVelocity;
        float   comp = Vector3.Dot(vel, gravDir);
        if (comp > 0f) vel -= gravDir * comp;
        rb.linearVelocity = vel + (-gravDir) * jumpForce;
    }
 
    // ── 지면 감지 ─────────────────────────────────────────────────
 
    void CheckGround()
    {
        float   r      = col.radius * 0.95f;
        float   hHalf  = col.height * 0.5f - col.radius;
        Vector3 origin = transform.TransformPoint(col.center) + gravDir * hHalf;
 
        isGrounded = Physics.SphereCast(
            origin - gravDir * r, r, gravDir,
            out _, r + 0.12f, groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }
 
    // ── 바디 정렬 ─────────────────────────────────────────────────
 
    void AlignBody()
    {
        Vector3 desiredUp = -gravDir;
        Vector3 currentUp =  transform.up;
        if (Vector3.Angle(currentUp, desiredUp) < 0.3f) return;
 
        Quaternion target  = Quaternion.FromToRotation(currentUp, desiredUp) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, target,
                                              Time.fixedDeltaTime * gravRotateSpeed);
    }
 
    // ── 유틸 ──────────────────────────────────────────────────────
 
    static string GetFaceLabel(Vector3 dir)
    {
        var map = new (Vector3 d, string n)[]
        {
            (Vector3.down, "Bottom"), (Vector3.up, "Top"),
            (Vector3.right, "Right"), (Vector3.left, "Left"),
            (Vector3.forward, "Front"), (Vector3.back, "Back"),
        };
        float best = -1f; string label = "";
        foreach (var (d, n) in map)
        {
            float v = Vector3.Dot(dir, d);
            if (v > best) { best = v; label = n; }
        }
        return label;
    }
 
    // ── HUD ───────────────────────────────────────────────────────
 
    void OnGUI()
    {
        if (flashTimer > 0f)
        {
            GUI.color = new Color(0.5f, 0.8f, 1f, (flashTimer / 0.25f) * 0.18f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
 
        var title = new GUIStyle(GUI.skin.label)
            { fontSize = 13, fontStyle = FontStyle.Bold,
              normal   = { textColor = new Color(0.55f, 0.85f, 1f) } };
        var body = new GUIStyle(GUI.skin.label)
            { fontSize = 13, normal = { textColor = Color.white } };
 
        GUI.Box(new Rect(10, 10, 220, 120), GUIContent.none);
        GUI.Label(new Rect(18, 14, 200, 20), "Gravity System",                                  title);
        GUI.Label(new Rect(18, 34, 200, 20), $"현재 면:  {faceLabel}",                         body);
        GUI.Label(new Rect(18, 54, 200, 20), $"속도:     {rb.linearVelocity.magnitude:F1} m/s", body);
        GUI.Label(new Rect(18, 74, 200, 20), $"지면:     {(isGrounded ? "접지 ✓" : "공중 ✗")}", body);
        GUI.Label(new Rect(18, 94, 200, 20), $"쿨다운:   {Mathf.Max(0f, cooldown):F2}s",        body);
 
        GUI.Box(new Rect(10, 138, 220, 70), GUIContent.none);
        GUI.Label(new Rect(18, 142, 200, 20), "조작법",         title);
        GUI.Label(new Rect(18, 162, 200, 20), "WASD   이동",    body);
        GUI.Label(new Rect(18, 182, 200, 20), "Space  점프",    body);
 
        float cx = Screen.width * .5f, cy = Screen.height * .5f;
        GUI.color = flashTimer > 0f ? new Color(0.4f, 0.8f, 1f) : Color.white;
        GUI.DrawTexture(new Rect(cx - 12, cy - 1, 9, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx +  3, cy - 1, 9, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx -  1, cy -12, 2, 9), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx -  1, cy + 3, 2, 9), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
 
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!col) col = GetComponent<CapsuleCollider>();
        Vector3 center = transform.TransformPoint(col.center);
 
        Gizmos.color = Color.yellow;
        foreach (var dir in SixDirs)
            Gizmos.DrawRay(center, dir * faceDetectRange);
 
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, gravDir * 3f);
 
        // 감지 구 시각화
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(center, faceDetectRange);
    }
#endif
}
 