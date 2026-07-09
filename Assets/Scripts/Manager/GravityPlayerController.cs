using UnityEngine;
using UnityEngine.Rendering;       // 추가
using UnityEngine.Rendering.Universal; // 추가 (URP용)
using TMPro;
using UnityEngine.UI;                       // UI 텍스트 처리를 위해 추가!

/// <summary>
/// 큐브 면 자동 감지 중력 시스템
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class GravityPlayerController : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 7f;
    public float jumpForce = 8f;
    [Range(0f, 1f)]
    public float airControl = 0.3f;

    [Header("중력")]
    public float gravStrength = 25f;
    public float gravRotateSpeed = 8f;

    [Header("카메라")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2.5f;
    [Range(10f, 89f)]
    public float pitchLimit = 80f;

    [Header("면 감지")]
    public string groundTag = "Ground";
    public LayerMask groundLayer = ~0;

    [Header("대쉬")]
    public float dashForce = 20f;
    public float dashCooldown = 1f;

    private bool canDash = true;

    [Range(0.5f, 100.0f)]
    public float faceDetectRange = 1.5f;

    [Range(0.1f, 0.5f)]
    public float switchCooldown = 0.25f;

    [Header("데미지 및 연출")]
    public float maxHp = 100f;
    public float currentHp;
    public Volume globalVolume;
    private Vignette vignette;
    private float vignetteTimer = 0f;

    [Header("UI 연결")]
    public TextMeshProUGUI hpText; // 체력을 띄워줄 UI 텍스트
    public Image image;

    // ── 내부 ──────────────────────────────────────────────────────
    Rigidbody rb;
    CapsuleCollider col;

    Vector3 gravDir = Vector3.down;
    Vector3 targetGravDir = Vector3.down;

    bool isGrounded = false;
    bool jumpQueued = false;
    float pitch = 0f;
    float cooldown = 0f;
    float flashTimer = 0f;
    string faceLabel = "Bottom";

    static readonly Vector3[] SixDirs =
    {
        Vector3.down, Vector3.up,
        Vector3.right, Vector3.left,
        Vector3.forward, Vector3.back
    };

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        image.gameObject.SetActive(false);

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (!cameraTransform)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam) cameraTransform = cam.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentHp = maxHp; // 체력 초기화
        UpdateHpUI();      // 시작할 때 체력 텍스트 갱신

        // URP 볼륨에서 Vignette 찾기
        if (globalVolume != null && globalVolume.profile.TryGet(out Vignette v))
        {
            vignette = v;
            vignette.intensity.value = 0f;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        //CameraLook();

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            jumpQueued = true;

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            Dash();
        }

        if (vignetteTimer > 0f)
        {
            vignetteTimer -= Time.deltaTime;
            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(0f, 0.4f, vignetteTimer / 0.5f);
            }
        }
        else
        {
            if (vignette != null) vignette.color.value = Color.black;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (cooldown > 0f) cooldown -= Time.deltaTime;
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

    void DetectFace()
    {
        if (cooldown > 0f) return;

        Vector3 center = transform.TransformPoint(col.center);
        float r = col.radius * 0.85f;

        Vector3 bestDir = Vector3.zero;
        float bestDist = float.MaxValue;

        foreach (Vector3 dir in SixDirs)
        {
            if (Vector3.Dot(dir, -gravDir) > 0.8f) continue;

            if (!Physics.SphereCast(
                    center, r, dir,
                    out RaycastHit hit,
                    faceDetectRange,
                    groundLayer,
                    QueryTriggerInteraction.Ignore))
                continue;

            if (!hit.collider.CompareTag(groundTag)) continue;

            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                bestDir = -hit.normal;
            }
        }

        if (bestDir == Vector3.zero) return;
        if (Vector3.Dot(bestDir, targetGravDir) > 0.95f) return;

        SwitchGravity(bestDir);
    }

    void Dash()
    {
        canDash = false;

        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, gravDir).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, gravDir).normalized;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 dashDir = (forward * v + right * h).normalized;

        if (dashDir == Vector3.zero) dashDir = forward;

        Vector3 vel = rb.linearVelocity;
        Vector3 gravVel = gravDir * Vector3.Dot(vel, gravDir);

        rb.AddForce(dashDir * dashForce, ForceMode.VelocityChange);
        Invoke(nameof(ResetDash), dashCooldown);
    }

    void ResetDash() { canDash = true; }

    void SwitchGravity(Vector3 newDir)
    {
        Vector3 vel = rb.linearVelocity;
        float comp = Vector3.Dot(vel, gravDir);
        rb.linearVelocity = vel - gravDir * comp * 0.6f;

        targetGravDir = newDir;
        faceLabel = GetFaceLabel(newDir);
        cooldown = switchCooldown;
        flashTimer = 0.25f;
    }

    void CameraLook()
    {
        if (!cameraTransform) return;

        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(-gravDir, mx, Space.World);

        pitch = Mathf.Clamp(pitch - my, -pitchLimit, pitchLimit);
        cameraTransform.localRotation = Quaternion.Euler(pitch, pitch, 0f);
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (h == 0f && v == 0f) return;

        Vector3 fwd = Vector3.ProjectOnPlane(cameraTransform.forward, gravDir).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, gravDir).normalized;
        Vector3 dir = (fwd * v + right * h).normalized;
        float ctrl = isGrounded ? 1f : airControl;

        Vector3 vel = rb.linearVelocity;
        Vector3 vertVel = gravDir * Vector3.Dot(vel, gravDir);
        Vector3 horiVel = vel - vertVel;

        rb.linearVelocity = Vector3.Lerp(horiVel, dir * moveSpeed * ctrl,
                                         isGrounded ? 0.22f : 0.07f) + vertVel;
    }

    void Jump()
    {
        Vector3 vel = rb.linearVelocity;
        float comp = Vector3.Dot(vel, gravDir);
        if (comp > 0f) vel -= gravDir * comp;
        rb.linearVelocity = vel + (-gravDir) * jumpForce;
    }

    void CheckGround()
    {
        float r = col.radius * 0.95f;
        float hHalf = col.height * 0.5f - col.radius;
        Vector3 origin = transform.TransformPoint(col.center) + gravDir * hHalf;

        isGrounded = Physics.SphereCast(
            origin - gravDir * r, r, gravDir,
            out _, r + 0.12f, groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    void AlignBody()
    {
        Vector3 desiredUp = -gravDir;
        Vector3 currentUp = transform.up;
        if (Vector3.Angle(currentUp, desiredUp) < 0.3f) return;

        Quaternion target = Quaternion.FromToRotation(currentUp, desiredUp) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, target,
                                              Time.fixedDeltaTime * gravRotateSpeed);
    }

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

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(10f);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            vignetteTimer = 0.4f;
            vignette.color.value = Color.red;
            vignette.intensity.value = 0.4f;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHp -= amount;
        UpdateHpUI(); // 데미지 입은 후 UI 갱신

        vignetteTimer = 0.4f;

        if (currentHp <= 0)
        {
            Debug.Log("플레이어 사망!");
            // TODO: 게임 오버 로직
            image.gameObject.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(currentHp + amount, maxHp);
        UpdateHpUI(); // 힐 받은 후 UI 갱신

        vignetteTimer = 0.5f;

        if (vignette != null)
        {
            vignette.color.value = Color.green;
            vignette.intensity.value = 0.4f;
        }
    }

    // 체력 텍스트를 최신화하는 함수
    private void UpdateHpUI()
    {
        if (hpText != null)
        {
            // 원하시는 텍스트 형식에 맞게 변경 가능합니다. 예: "100 / 100"
            hpText.text = $"HP: {Mathf.CeilToInt(currentHp)}";
        }
    }

    void OnGUI()
    {
        if (flashTimer > 0f)
        {
            GUI.color = new Color(0.5f, 0.8f, 1f, (flashTimer / 0.25f) * 0.18f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        float cx = Screen.width * .5f, cy = Screen.height * .5f;
        GUI.color = flashTimer > 0f ? new Color(0.4f, 0.8f, 1f) : Color.white;
        GUI.DrawTexture(new Rect(cx - 12, cy - 1, 9, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx + 3, cy - 1, 9, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1, cy - 12, 2, 9), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1, cy + 3, 2, 9), Texture2D.whiteTexture);
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

        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(center, faceDetectRange);
    }
#endif
}