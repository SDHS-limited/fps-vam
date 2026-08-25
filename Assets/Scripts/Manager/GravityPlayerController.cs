using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEngine.UI;
using System.Collections;
 
/// <summary>
/// 큐브 면 자동 감지 중력 시스템 + 피버타임(행복지수) 시스템 (커스텀 키 설정 적용됨)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class GravityPlayerController : MonoBehaviour
{
    public AudioSource audioSource;
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

    [Header("카메라 연출 (FOV)")]
    public float defaultFOV = 90f;
    public float actionFOV = 115f;
    public float fovLerpSpeed = 15f;
    private Camera playerCam;
    private float targetFOV = 90f;
 
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
    public Bullet bullet;
 
    [Header("UI 연결")]
    public TextMeshProUGUI hpText;
    public Image image;
    public TimerUI timerUI;
    private bool isDie = false;
 
    // ─────────────────────────────────────────────────────────────
    // 피버타임(행복지수) 시스템
    // ─────────────────────────────────────────────────────────────
    [Header("피버 - 행복지수")]
    [Tooltip("현재 행복지수")]
    [Range(0, 100)]
    public int happiness = 0;

    [Tooltip("최대 행복지수")]
    public int maxHappiness = 100;

    [Tooltip("적 하나 처치 시 증가하는 행복지수")]
    public int happinessPerKill = 20;

    [Header("행복지수 UI")]
    [SerializeField] private TMP_Text happinessText;
 
    [Header("피버 - 지속효과")]
    public float feverDuration = 10f;
    [Tooltip("피버 중 이동속도 배율")]
    public float feverMoveSpeedMultiplier = 1.3f;
    [Tooltip("피버 중 초당 회복량")]
    public float feverHealPerSecond = 8f;
 
    [Header("피버 - 시작 연출")]
    [Tooltip("화면을 밝게 페이드시킬 풀스크린 흰색 Image (알파 0으로 시작)")]
    public Image feverFadeImage;
    public float feverFadeInTime = 0.15f;
    public float feverFadeOutTime = 0.6f;
    public float feverFadeMaxAlpha = 0.85f;
 
    [Header("피버 - 밀치기(크리티컬) 공격")]
    [Tooltip("밀치기를 사용할 수 있는 최대 거리")]
    public float pushRange = 8f;
    [Tooltip("적을 밀쳐내는 힘")]
    public float pushForce = 18f;
    [Tooltip("기본 데미지")]
    public float pushBaseDamage = 15f;
    [Tooltip("크리티컬 배율")]
    public float pushCriticalMultiplier = 3f;
    public float pushCooldown = 0.6f;
    public LayerMask enemyLayer;

    bool isPouncing = false;
    private bool hasHitEnemy = false;

    [Header("Pounce")]
    public float pounceSpeed = 45f;
    public float pounceDuration = 0.22f;
    public float pounceDamage = 80f;
    public float enemyKnockback = 35f;
    public float enemyUpForce = 6f;
 
    public bool IsFeverActive { get; private set; } = false;
    private float feverTimer = 0f;
    private float baseMoveSpeedCache;
    private float pushCooldownTimer = 0f;
    private Coroutine feverFadeRoutine;
 
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
        audioSource.Play();
 
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
 
        if (!cameraTransform)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam) cameraTransform = cam.transform;
        }

        if (cameraTransform != null)
        {
            playerCam = cameraTransform.GetComponent<Camera>();
        }
        if (playerCam == null) playerCam = GetComponentInChildren<Camera>();
        
        targetFOV = defaultFOV;
        if (playerCam != null) playerCam.fieldOfView = defaultFOV;
 
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
 
        currentHp = maxHp;
        UpdateHpUI();
 
        baseMoveSpeedCache = moveSpeed;
 
        if (feverFadeImage != null)
        {
            var c = feverFadeImage.color;
            c.a = 0f;
            feverFadeImage.color = c;
            feverFadeImage.gameObject.SetActive(true);
        }
 
        if (globalVolume != null && globalVolume.profile.TryGet(out Vignette v))
        {
            vignette = v;
            vignette.intensity.value = 0f;
        }
    }
 
    void Update()
    {
        // ── ⚠️ KeyManager 로드가 끝날 때까지 대기 ──
        if (KeyManager.instance == null) return;

        UpdateHappinessUI();
        if (Time.timeScale == 0f) return;
 
        // 1. 점프 (Space -> 커스텀 키)
        if (Input.GetKeyDown(KeyManager.instance.keys["Jump"]) && isGrounded)
            jumpQueued = true;
 
        // 2. 대쉬 (Shift -> 커스텀 키)
        if (Input.GetKeyDown(KeyManager.instance.keys["Dash"]) && canDash)
        {
            Dash();
        }

        // 3. 밀치기 (일반 상태 Push)
        if(Input.GetKeyDown(KeyManager.instance.keys["Push"]))
        {
            TryPushAttack();
        }
        //치트키
        if (Input.GetKeyDown(KeyCode.F1))
        {
            happiness = maxHappiness;
        }

        if(currentHp <= 0)
        {
            isDie = true;
        }
        else
        {
            isDie = false;
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
 
        if (Input.GetKeyDown(KeyManager.instance.keys["Attack"]) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }
 
        if (cooldown > 0f) cooldown -= Time.deltaTime;
        if (flashTimer > 0f) flashTimer -= Time.deltaTime;

        if (playerCam != null)
        {
            playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);
        }
 
        UpdateFever();
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
 
    public void OnEnemyKilled()
    {
        if (IsFeverActive) return;

        happiness = Mathf.Clamp(happiness + happinessPerKill, 0, maxHappiness);
        UpdateHappinessUI();

        if (happiness >= maxHappiness) StartFever();
    }
 
    void StartFever()
    {
        IsFeverActive = true;
        feverTimer = feverDuration;
        moveSpeed = baseMoveSpeedCache * feverMoveSpeedMultiplier;
 
       timerUI.bonusTimeAnimator.SetTrigger("open");
       timerUI.bonusTimeText.text = "Fever Time!";
    }
 
    void EndFever()
    {
        bullet.damage = 75;
        IsFeverActive = false;
        moveSpeed = baseMoveSpeedCache;

        happiness = 0;
        UpdateHappinessUI();

        if (feverFadeRoutine != null) StopCoroutine(feverFadeRoutine);

        if (feverFadeImage != null)
        {
            feverFadeRoutine = StartCoroutine(
                FadeImage(feverFadeImage, feverFadeImage.color.a, 0f, feverFadeOutTime));
        }
        isPouncing = false;
    }
 
    void UpdateFever()
    {
        bullet.damage = 999;
        if (pushCooldownTimer > 0f) pushCooldownTimer -= Time.deltaTime;
 
        if (!IsFeverActive) return;
 
        if (currentHp < maxHp) Heal(feverHealPerSecond * Time.deltaTime);
 
        // 피버 상태에서의 Pounce 공격 (우클릭 -> 커스텀 키로 통일)
        if (Input.GetKeyDown(KeyManager.instance.keys["Push"]) && pushCooldownTimer <= 0f)
        {
            if (!isPouncing) StartCoroutine(Pounce());
        }
 
        feverTimer -= Time.deltaTime;
        if (feverTimer <= 0f) EndFever();
    }
 
    IEnumerator FeverFadeRoutine()
    {
        if (feverFadeImage != null)
        {
            yield return FadeImage(feverFadeImage, feverFadeImage.color.a, feverFadeMaxAlpha, feverFadeInTime);
            yield return FadeImage(feverFadeImage, feverFadeImage.color.a, 0f, feverFadeOutTime);
        }
    }
 
    IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            var c = img.color;
            c.a = a;
            img.color = c;
            yield return null;
        }
        var final = img.color;
        final.a = to;
        img.color = final;
    }
 
    void TryPushAttack()
    {
        if (!cameraTransform) return;
 
        pushCooldownTimer = pushCooldown;
 
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward,
                             out RaycastHit hit, pushRange, enemyLayer,
                             QueryTriggerInteraction.Ignore))
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if(enemy != null)
            {
                Vector3 dir = Vector3.ProjectOnPlane(cameraTransform.forward, gravDir).normalized;
                dir += (-gravDir) * 0.2f;
                enemy.Knockback(dir, enemyKnockback);
            }
 
            var enemyHealth = hit.collider.GetComponentInParent<MonoBehaviour>() as IDamageable;
            if (enemyHealth != null) enemyHealth.TakeDamage(pushBaseDamage * pushCriticalMultiplier);
 
            flashTimer = 0.15f;
        }
    }
 
    void UpdateHappinessUI()
    {
        if (happinessText != null) happinessText.text = $"{happiness} / {maxHappiness}";
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
        if (isPouncing) return;
        canDash = false;
    
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, gravDir).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, gravDir).normalized;
    
        // ── 변경: GetAxisRaw 대신 커스텀 키보드 입력 매핑 ──
        float h = 0f;
        if (Input.GetKey(KeyManager.instance.keys["Right"])) h += 1f;
        if (Input.GetKey(KeyManager.instance.keys["Left"])) h -= 1f;

        float v = 0f;
        if (Input.GetKey(KeyManager.instance.keys["Forward"])) v += 1f;
        if (Input.GetKey(KeyManager.instance.keys["Backward"])) v -= 1f;
    
        Vector3 dashDir = (forward * v + right * h).normalized;
        if (dashDir == Vector3.zero) dashDir = forward;
    
        rb.AddForce(dashDir * dashForce, ForceMode.VelocityChange);
        Invoke(nameof(ResetDash), dashCooldown);
    }
 
    void ResetDash() { canDash = true; }

    void ResetFOV()
    {
        if (!isPouncing) targetFOV = defaultFOV;
    }
 
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
        // (기존 코드 유지)
    }
 
    void Move()
    {
        if (isPouncing) return;

        // ── 변경: GetAxisRaw 대신 커스텀 키 입력 감지 ──
        float h = 0f;
        if (Input.GetKey(KeyManager.instance.keys["Right"])) h += 1f;
        if (Input.GetKey(KeyManager.instance.keys["Left"])) h -= 1f;

        float v = 0f;
        if (Input.GetKey(KeyManager.instance.keys["Forward"])) v += 1f;
        if (Input.GetKey(KeyManager.instance.keys["Backward"])) v -= 1f;

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
        if (collision.gameObject.CompareTag("Enemy") && !isPouncing)
        {
            TakeDamage(10f);
            return;
        }

        if (isPouncing && collision.gameObject.CompareTag("Enemy"))
        {
            hasHitEnemy = true;
            isPouncing = false;
            flashTimer = 0.15f;

            IDamageable hp = collision.collider.GetComponentInParent<IDamageable>();
            if (hp != null) hp.TakeDamage(pounceDamage);

            Rigidbody enemyRb = collision.collider.GetComponentInParent<Rigidbody>();
            if (enemyRb != null)
            {
                Vector3 dir = (collision.transform.position - transform.position).normalized;
                dir += Vector3.up * 0.2f;

                enemyRb.linearVelocity = Vector3.zero;
                enemyRb.AddForce(dir.normalized * enemyKnockback + Vector3.up * enemyUpForce, ForceMode.VelocityChange);
            }

            rb.linearVelocity *= 0.2f;
            StartCoroutine(HitStop(0.03f));
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
        UpdateHpUI();
        vignetteTimer = 0.4f;
 
        if (currentHp <= 0)
        {
            image.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
 
    public void Heal(float amount)
    {
        currentHp = Mathf.Min(currentHp + amount, maxHp);
        UpdateHpUI();
        vignetteTimer = 0.5f;
 
        if (vignette != null)
        {
            vignette.color.value = Color.green;
            vignette.intensity.value = 0.4f;
        }
    }
 
    private void UpdateHpUI()
    {
        if (hpText != null) hpText.text = $"HP: {Mathf.CeilToInt(currentHp)}";
    }
 
    void OnGUI()
    {
        if (isDie) return;

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
        // (기존 GUI 코드 유지)
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

    IEnumerator Pounce()
    {
        isPouncing = true;
        hasHitEnemy = false;
        pushCooldownTimer = pushCooldown;
        targetFOV = actionFOV;
        float timer = 0f;

        while (timer < pounceDuration && !hasHitEnemy)
        {
            Vector3 dir = Vector3.ProjectOnPlane(cameraTransform.forward, gravDir).normalized;
            rb.linearVelocity = dir * pounceSpeed;
            timer += Time.deltaTime;
            yield return null;
        }

        isPouncing = false;
        targetFOV = defaultFOV;
    }

    IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0.03f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}
 
public interface IDamageable
{
    void TakeDamage(float amount);
}