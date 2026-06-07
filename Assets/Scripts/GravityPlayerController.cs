using UnityEngine;
 
/// <summary>
/// 큐브 면 자동 감지 중력 시스템 - 개선판
///
/// [씬 셋업]
/// 1. Player GameObject
///    - Rigidbody (useGravity OFF, Freeze Rotation XYZ 체크)
///    - CapsuleCollider (Radius: 0.4, Height: 1.8)
///    - 이 스크립트 추가
///
/// 2. Player 자식으로 Main Camera
///    - Local Position: (0, 0.7, 0)
///    - [Camera Transform] 슬롯에 드래그
///
/// 3. 큐브에 "Ground" Tag 설정
///
/// 4. Edit → Project Settings → Physics → Gravity → (0, 0, 0)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class GravityPlayerController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────
 
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
    public string    groundTag     = "Ground";
    public LayerMask groundLayer   = ~0;
 
    // 감지 거리: 캡슐 반지름 + 이 값만큼 앞을 본다
    // 너무 크면 멀리서 전환, 너무 작으면 늦게 전환 → 0.3~0.6 권장
    [Range(0.1f, 1.0f)]
    public float faceCheckExtra  = 0.45f;
 
    // 면 전환 쿨다운 (너무 짧으면 진동, 너무 길면 둔함)
    [Range(0.1f, 0.6f)]
    public float faceCooldown    = 0.25f;
 
    // ── 내부 상태 ─────────────────────────────────────────────────
 
    Rigidbody       rb;
    CapsuleCollider col;
 
    Vector3 gravDir       = Vector3.down;
    Vector3 targetGravDir = Vector3.down;
 
    bool  isGrounded = false;
    bool  jumpQueued = false;
    float pitch      = 0f;
 
    float  cooldownTimer = 0f;
    string faceLabel     = "Bottom";
    float  flashTimer    = 0f;
 
    // ── 초기화 ────────────────────────────────────────────────────
 
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
 
    // ── Update ────────────────────────────────────────────────────
 
    void Update()
    {
        CameraLook();
 
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            jumpQueued = true;
 
        if (Input.GetKeyDown(KeyCode.Escape))
        { Cursor.lockState = CursorLockMode.None;   Cursor.visible = true; }
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
 
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (flashTimer    > 0f) flashTimer    -= Time.deltaTime;
    }
 
    // ── FixedUpdate ───────────────────────────────────────────────
 
    void FixedUpdate()
    {
        gravDir = Vector3.Slerp(gravDir, targetGravDir,
                                Time.fixedDeltaTime * gravRotateSpeed * 3f).normalized;
 
        CheckGround();
        DetectFace();   // ← FixedUpdate에서 매 물리 프레임 감지
        rb.AddForce(gravDir * gravStrength, ForceMode.Acceleration);
        Move();
 
        if (jumpQueued && isGrounded)
        { Jump(); jumpQueued = false; }
 
        AlignBody();
    }
 
    // ── 면 감지 (핵심 개선) ───────────────────────────────────────
    //
    // 개선 포인트:
    //  1) FixedUpdate에서 호출 → 물리 프레임마다 체크
    //  2) 캡슐 하단(발끝) + 캡슐 중심 두 곳에서 Ray 발사
    //  3) SphereCast로 감지 면적 확대 (점이 아닌 구)
    //  4) 현재 중력 방향 dot > 0.7 이상이면 스킵 (반대 방향만 스킵 제거)
    //  5) 감지된 법선 중 현재 중력과 가장 다른 방향을 우선 선택
 
    void DetectFace()
    {
        if (cooldownTimer > 0f) return;
 
        float   r         = col.radius;
        float   extraDist = r + faceCheckExtra;
        Vector3 center    = transform.TransformPoint(col.center);
        // 캡슐 하단 (발끝 근처)
        Vector3 bottom    = center + gravDir * (col.height * 0.5f - r);
 
        Vector3 bestNormal  = Vector3.zero;
        float   bestDot     = 0.7f;   // 이 값보다 클 때만 전환 (노이즈 방지)
 
        // 검사 출발점 2개: 중심 + 발끝
        Vector3[] origins = { center, bottom };
 
        // 검사 방향: 6방향 모두 + 현재 중력 방향(아래쪽)도 포함
        Vector3[] dirs = {
            Vector3.down, Vector3.up,
            Vector3.right, Vector3.left,
            Vector3.forward, Vector3.back
        };
 
        foreach (Vector3 origin in origins)
        {
            foreach (Vector3 dir in dirs)
            {
                // 현재 목표 중력과 거의 같은 방향이면 스킵 (이미 그 면이 바닥)
                if (Vector3.Dot(dir, targetGravDir) > 0.95f) continue;
 
                // SphereCast: 구 반지름 r * 0.8으로 넓게 감지
                if (!Physics.SphereCast(
                        origin, r * 0.8f, dir,
                        out RaycastHit hit, extraDist,
                        groundLayer, QueryTriggerInteraction.Ignore))
                    continue;
 
                if (!hit.collider.CompareTag(groundTag)) continue;
 
                // 법선 반대가 새 중력 방향
                Vector3 newGrav = -hit.normal;
 
                // 현재 목표 중력과 얼마나 다른지 (다를수록 전환 의미 있음)
                float diff = 1f - Vector3.Dot(newGrav, targetGravDir);
                if (diff > bestDot)
                {
                    bestDot    = diff;
                    bestNormal = newGrav;
                }
            }
        }
 
        // 가장 유의미한 새 중력 방향으로 전환
        if (bestNormal != Vector3.zero)
            SwitchGravity(bestNormal);
    }
 
    void SwitchGravity(Vector3 newDir)
    {
        // 기존 수직 속도 감쇠
        Vector3 vel  = rb.linearVelocity;
        float   comp = Vector3.Dot(vel, gravDir);
        rb.linearVelocity = vel - gravDir * comp * 0.55f;
 
        targetGravDir = newDir;
        faceLabel     = GetFaceLabel(newDir);
        cooldownTimer = faceCooldown;
        flashTimer    = 0.25f;
    }
 
    // ── 카메라 룩 ─────────────────────────────────────────────────
 
    void CameraLook()
    {
        if (!cameraTransform) return;
 
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
 
        transform.Rotate(-gravDir, mx, Space.World);
 
        pitch = Mathf.Clamp(pitch - my, -pitchLimit, pitchLimit);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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
        GUI.Label(new Rect(18, 14, 200, 20), "Gravity System",                             title);
        GUI.Label(new Rect(18, 34, 200, 20), $"현재 면:  {faceLabel}",                    body);
        GUI.Label(new Rect(18, 54, 200, 20), $"속도:     {rb.linearVelocity.magnitude:F1} m/s", body);
        GUI.Label(new Rect(18, 74, 200, 20), $"지면:     {(isGrounded ? "접지 ✓" : "공중 ✗")}", body);
        GUI.Label(new Rect(18, 94, 200, 20), $"쿨다운:   {Mathf.Max(0f, cooldownTimer):F2}s", body);
 
        GUI.Box(new Rect(10, 138, 220, 70), GUIContent.none);
        GUI.Label(new Rect(18, 142, 200, 20), "조작법",              title);
        GUI.Label(new Rect(18, 162, 200, 20), "WASD      이동",      body);
        GUI.Label(new Rect(18, 182, 200, 20), "Space     점프",      body);
 
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
        Vector3 bottom = center + gravDir * (col.height * 0.5f - col.radius);
 
        // 감지 구 범위
        Gizmos.color = Color.yellow;
        foreach (var dir in new[]{ Vector3.down, Vector3.up,
                                   Vector3.right, Vector3.left,
                                   Vector3.forward, Vector3.back })
        {
            Gizmos.DrawRay(center, dir * (col.radius + faceCheckExtra));
            Gizmos.DrawRay(bottom, dir * (col.radius + faceCheckExtra));
        }
 
        // 현재 중력
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, gravDir * 3f);
    }
#endif
}