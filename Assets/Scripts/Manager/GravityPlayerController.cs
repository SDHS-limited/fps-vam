using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// 큐브 면 자동 감지 중력 + 커스텀 키 + 피버타임 + 능력 강화 시스템.
/// AbilityData.cs의 AbilityData / AbilityType을 사용합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class GravityPlayerController : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("이동")]
    public float moveSpeed = 7f;
    public float jumpForce = 8f;
    [Range(0f, 1f)] public float airControl = 0.3f;

    [Header("중력")]
    public float gravStrength = 25f;
    public float gravRotateSpeed = 8f;

    [Header("카메라")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2.5f;
    [Range(10f, 89f)] public float pitchLimit = 80f;

    [Header("카메라 연출 (FOV)")]
    public float defaultFOV = 90f;
    public float actionFOV = 115f;
    public float fovLerpSpeed = 15f;
    private Camera playerCam;
    private float targetFOV = 90f;

    [Header("면 감지")]
    public string groundTag = "Ground";
    public LayerMask groundLayer = ~0;
    [Range(0.5f, 100f)] public float faceDetectRange = 1.5f;
    [Range(0.1f, 0.5f)] public float switchCooldown = 0.25f;

    [Header("대쉬")]
    public float dashForce = 20f;
    public float dashCooldown = 1f;
    private bool canDash = true;

    [Header("데미지 및 연출")]
    public float maxHp = 100f;
    public float currentHp;
    public Volume globalVolume;
    private Vignette vignette;
    private float vignetteTimer;
    public Bullet bullet;

    [Header("UI 연결")]
    public TextMeshProUGUI hpText;
    public Image image;
    public TimerUI timerUI;
    private bool isDie;
    
    [Header("피버타임 텍스트")]
    [SerializeField] private FeverWaveText feverWaveText;

    [Header("능력 강화 시스템")]
    [Tooltip("1 = 기본, 1.2 = 이동속도 20% 증가")]
    public float abilityMoveSpeedMultiplier = 1f;
    [Tooltip("1 = 기본, 1.25 = 공격력 25% 증가")]
    public float abilityDamageMultiplier = 1f;
    [Tooltip("피버가 아닐 때의 기본 총알 공격력")]
    public int normalBulletDamage = 75;
    [Tooltip("피버 중의 기본 총알 공격력")]
    public int feverBulletDamage = 999;

    [Header("피버 - 행복지수")]
    [Range(0, 100)] public int happiness = 0;
    public int maxHappiness = 100;
    public int happinessPerKill = 20;

    [Header("행복지수 UI")]
    [SerializeField] private TMP_Text happinessText;

    [Header("피버 - 지속효과")]
    public float feverDuration = 10f;
    public float feverMoveSpeedMultiplier = 1.3f;
    public float feverHealPerSecond = 8f;

    [Header("피버 - 시작 연출")]
    public Image feverFadeImage;
    public float feverFadeInTime = 0.15f;
    public float feverFadeOutTime = 0.6f;
    public float feverFadeMaxAlpha = 0.85f;

    [Header("피버 - 밀치기(크리티컬) 공격")]
    public float pushRange = 8f;
    public float pushForce = 18f;
    public float pushBaseDamage = 15f;
    public float pushCriticalMultiplier = 3f;
    public float pushCooldown = 0.6f;
    public LayerMask enemyLayer;

    private bool isPouncing;
    private bool hasHitEnemy;

    [Header("Pounce")]
    public float pounceSpeed = 45f;
    public float pounceDuration = 0.22f;
    public float pounceDamage = 80f;
    public float enemyKnockback = 35f;
    public float enemyUpForce = 6f;

    public bool IsFeverActive { get; private set; }
    private float feverTimer;
    private float baseMoveSpeedCache;
    private float pushCooldownTimer;
    private Coroutine feverFadeRoutine;

    private Rigidbody rb;
    private CapsuleCollider col;
    private Vector3 gravDir = Vector3.down;
    private Vector3 targetGravDir = Vector3.down;
    private bool isGrounded;
    private bool jumpQueued;
    private float pitch;
    private float cooldown;
    private float flashTimer;
    private string faceLabel = "Bottom";

    private static readonly Vector3[] SixDirs =
    {
        Vector3.down, Vector3.up,
        Vector3.right, Vector3.left,
        Vector3.forward, Vector3.back
    };

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        if (image != null) image.gameObject.SetActive(false);
        if (audioSource != null) audioSource.Play();

        if (feverWaveText != null)
        {
            feverWaveText.gameObject.SetActive(false);
        }

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }

        if (cameraTransform != null)
            playerCam = cameraTransform.GetComponent<Camera>();
        if (playerCam == null)
            playerCam = GetComponentInChildren<Camera>();

        targetFOV = defaultFOV;
        if (playerCam != null) playerCam.fieldOfView = defaultFOV;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentHp = maxHp;
        UpdateHpUI();
        UpdateHappinessUI();
        baseMoveSpeedCache = moveSpeed;

        if (feverFadeImage != null)
        {
            Color color = feverFadeImage.color;
            color.a = 0f;
            feverFadeImage.color = color;
            feverFadeImage.gameObject.SetActive(true);
        }

        if (globalVolume != null &&
            globalVolume.profile != null &&
            globalVolume.profile.TryGet(out Vignette v))
        {
            vignette = v;
            vignette.intensity.value = 0f;
        }

        // 최초 총알 데미지에도 능력 배율 적용
        UpdateBulletDamage();
    }

    private void Update()
    {
        UpdateHappinessUI();

        // 능력 선택 UI 등으로 게임이 정지했으면 플레이어 입력도 정지
        if (Time.timeScale == 0f) return;
        if (KeyManager.instance == null) return;

        if (Input.GetKeyDown(KeyManager.instance.keys["Jump"]) && isGrounded)
            jumpQueued = true;

        if (Input.GetKeyDown(KeyManager.instance.keys["Dash"]) && canDash)
            Dash();

        // 일반 상태: 밀치기 / 피버 상태: Pounce (UpdateFever에서 처리)
        if (!IsFeverActive && Input.GetKeyDown(KeyManager.instance.keys["Push"]))
            TryPushAttack();

        if (Input.GetKeyDown(KeyCode.F1))
            happiness = maxHappiness;

        isDie = currentHp <= 0f;

        if (vignetteTimer > 0f)
        {
            vignetteTimer -= Time.deltaTime;
            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(0f, 0.4f, vignetteTimer / 0.5f);
        }
        else if (vignette != null)
        {
            vignette.intensity.value = 0f;
            vignette.color.value = Color.black;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetKeyDown(KeyManager.instance.keys["Attack"]) &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }

        if (cooldown > 0f) cooldown -= Time.deltaTime;
        if (flashTimer > 0f) flashTimer -= Time.deltaTime;

        if (playerCam != null)
            playerCam.fieldOfView = Mathf.Lerp(
                playerCam.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);

        UpdateFever();
    }

    private void FixedUpdate()
    {
        gravDir = Vector3.Slerp(
            gravDir, targetGravDir,
            Time.fixedDeltaTime * gravRotateSpeed * 3f).normalized;

        CheckGround();
        DetectFace();
        rb.AddForce(gravDir * gravStrength, ForceMode.Acceleration);
        Move();

        if (jumpQueued && isGrounded)
        {
            Jump();
            jumpQueued = false;
        }

        AlignBody();
    }

    // ─────────────────────────────────────────────────────────
    // 능력 선택 UI가 호출하는 함수
    // ─────────────────────────────────────────────────────────
    public void ApplyAbility(AbilityData ability)
    {
        if (ability == null) return;

        float value = Mathf.Max(0f, ability.value);

        switch (ability.abilityType)
        {
            case AbilityType.MoveSpeed:
                abilityMoveSpeedMultiplier += value;
                break;

            case AbilityType.AttackDamage:
                abilityDamageMultiplier += value;
                UpdateBulletDamage();
                break;

            case AbilityType.MaxHealth:
                maxHp += value;
                Heal(value);
                break;

            case AbilityType.Heal:
                Heal(value);
                break;
        }

        Debug.Log($"[능력 획득] {ability.abilityName} / 강화 수치: {value}");
    }

    private void UpdateBulletDamage()
    {
        if (bullet == null) return;

        int baseDamage = IsFeverActive
            ? feverBulletDamage
            : normalBulletDamage;

        bullet.damage = Mathf.RoundToInt(baseDamage * abilityDamageMultiplier);
    }

    public void OnEnemyKilled()
    {
        if (IsFeverActive) return;

        happiness = Mathf.Clamp(happiness + happinessPerKill, 0, maxHappiness);
        UpdateHappinessUI();

        if (happiness >= maxHappiness) StartFever();
    }

    private void StartFever()
    {
        IsFeverActive = true;
        feverTimer = feverDuration;

        // 이동 강화는 Move()에서 별도로 곱해지므로 피버와 함께 유지됨
        moveSpeed = baseMoveSpeedCache * feverMoveSpeedMultiplier;

        // 피버 텍스트 표시 + 파도 애니메이션 실행
        if (feverWaveText != null)
        {
            feverWaveText.gameObject.SetActive(true);
            feverWaveText.Play();
        }

        UpdateBulletDamage();

        if (timerUI != null)
        {
            if (timerUI.bonusTimeAnimator != null)
                timerUI.bonusTimeAnimator.SetTrigger("open");
            if (timerUI.bonusTimeText != null)
                timerUI.bonusTimeText.text = "Fever Time!";
        }
    }

    private void EndFever()
    {
        IsFeverActive = false;
        moveSpeed = baseMoveSpeedCache;
        UpdateBulletDamage();

        happiness = 0;
        UpdateHappinessUI();

        if (feverFadeRoutine != null)
            StopCoroutine(feverFadeRoutine);

        if (feverWaveText != null)
        {
            feverWaveText.Stop();
            feverWaveText.gameObject.SetActive(false);
        }

        if (feverFadeImage != null)
        {
            feverFadeRoutine = StartCoroutine(
                FadeImage(feverFadeImage, feverFadeImage.color.a,
                          0f, feverFadeOutTime));
        }

        isPouncing = false;
    }

    private void UpdateFever()
    {
        // 기존 코드의 bullet.damage = 999를 제거함.
        // 총알 데미지는 Awake/StartFever/EndFever/ApplyAbility에서 갱신.
        if (pushCooldownTimer > 0f)
            pushCooldownTimer -= Time.deltaTime;

        if (!IsFeverActive) return;

        if (currentHp < maxHp)
            Heal(feverHealPerSecond * Time.deltaTime);



        if (Input.GetKeyDown(KeyManager.instance.keys["Push"]) &&
            pushCooldownTimer <= 0f && !isPouncing)
        {
            StartCoroutine(Pounce());
        }

        feverTimer -= Time.deltaTime;
        if (feverTimer <= 0f) EndFever();
    }

    private IEnumerator FeverFadeRoutine()
    {
        if (feverFadeImage != null)
        {
            yield return FadeImage(feverFadeImage, feverFadeImage.color.a,
                                   feverFadeMaxAlpha, feverFadeInTime);
            yield return FadeImage(feverFadeImage, feverFadeImage.color.a,
                                   0f, feverFadeOutTime);
        }
    }

    private IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            Color c = img.color;
            c.a = a;
            img.color = c;
            yield return null;
        }

        Color final = img.color;
        final.a = to;
        img.color = final;
    }

    private void TryPushAttack()
    {
        if (cameraTransform == null) return;

        pushCooldownTimer = pushCooldown;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward,
                            out RaycastHit hit, pushRange, enemyLayer,
                            QueryTriggerInteraction.Ignore))
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                Vector3 dir = Vector3.ProjectOnPlane(
                    cameraTransform.forward, gravDir).normalized;
                dir += -gravDir * 0.2f;
                enemy.Knockback(dir, enemyKnockback);
            }

            IDamageable enemyHealth = FindDamageable(hit.collider);
            if (enemyHealth != null)
            {
                float finalDamage = pushBaseDamage * pushCriticalMultiplier
                                    * abilityDamageMultiplier;
                enemyHealth.TakeDamage(finalDamage);
            }

            flashTimer = 0.15f;
        }
    }

    private static IDamageable FindDamageable(Collider targetCollider)
    {
        if (targetCollider == null) return null;

        MonoBehaviour[] behaviours =
            targetCollider.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IDamageable damageable)
                return damageable;
        }

        return null;
    }

    private void UpdateHappinessUI()
    {
        if (happinessText != null)
            happinessText.text = $"{happiness} / {maxHappiness}";
    }

    private void DetectFace()
    {
        if (cooldown > 0f) return;

        Vector3 center = transform.TransformPoint(col.center);
        float r = col.radius * 0.85f;
        Vector3 bestDir = Vector3.zero;
        float bestDist = float.MaxValue;

        foreach (Vector3 dir in SixDirs)
        {
            if (Vector3.Dot(dir, -gravDir) > 0.8f) continue;

            if (!Physics.SphereCast(center, r, dir,
                                    out RaycastHit hit, faceDetectRange,
                                    groundLayer, QueryTriggerInteraction.Ignore))
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

    private void Dash()
    {
        if (isPouncing || cameraTransform == null) return;
        canDash = false;

        Vector3 forward = Vector3.ProjectOnPlane(
            cameraTransform.forward, gravDir).normalized;
        Vector3 right = Vector3.ProjectOnPlane(
            cameraTransform.right, gravDir).normalized;

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

    private void ResetDash()
    {
        canDash = true;
    }

    private void ResetFOV()
    {
        if (!isPouncing) targetFOV = defaultFOV;
    }

    private void SwitchGravity(Vector3 newDir)
    {
        Vector3 vel = rb.linearVelocity;
        float comp = Vector3.Dot(vel, gravDir);
        rb.linearVelocity = vel - gravDir * comp * 0.6f;

        targetGravDir = newDir;
        faceLabel = GetFaceLabel(newDir);
        cooldown = switchCooldown;
        flashTimer = 0.25f;
    }

    private void CameraLook()
    {
        // 원본 코드에서 비어 있던 부분은 그대로 두었습니다.
        // 실제 카메라 회전 코드가 다른 스크립트에 있다면 그대로 사용하세요.
    }

    private void Move()
    {
        if (isPouncing || cameraTransform == null || KeyManager.instance == null)
            return;

        float h = 0f;
        if (Input.GetKey(KeyManager.instance.keys["Right"])) h += 1f;
        if (Input.GetKey(KeyManager.instance.keys["Left"])) h -= 1f;

        float v = 0f;
        if (Input.GetKey(KeyManager.instance.keys["Forward"])) v += 1f;
        if (Input.GetKey(KeyManager.instance.keys["Backward"])) v -= 1f;

        if (h == 0f && v == 0f) return;

        Vector3 fwd = Vector3.ProjectOnPlane(
            cameraTransform.forward, gravDir).normalized;
        Vector3 right = Vector3.ProjectOnPlane(
            cameraTransform.right, gravDir).normalized;
        Vector3 dir = (fwd * v + right * h).normalized;
        float ctrl = isGrounded ? 1f : airControl;

        Vector3 vel = rb.linearVelocity;
        Vector3 vertVel = gravDir * Vector3.Dot(vel, gravDir);
        Vector3 horiVel = vel - vertVel;

        // 능력 강화와 피버 배율이 모두 적용됨.
        float finalMoveSpeed = moveSpeed * abilityMoveSpeedMultiplier;
        rb.linearVelocity = Vector3.Lerp(
            horiVel, dir * finalMoveSpeed * ctrl,
            isGrounded ? 0.22f : 0.07f) + vertVel;
    }

    private void Jump()
    {
        Vector3 vel = rb.linearVelocity;
        float comp = Vector3.Dot(vel, gravDir);
        if (comp > 0f) vel -= gravDir * comp;
        rb.linearVelocity = vel + -gravDir * jumpForce;
    }

    private void CheckGround()
    {
        float r = col.radius * 0.95f;
        float hHalf = col.height * 0.5f - col.radius;
        Vector3 origin = transform.TransformPoint(col.center) + gravDir * hHalf;

        isGrounded = Physics.SphereCast(
            origin - gravDir * r, r, gravDir,
            out _, r + 0.12f, groundLayer,
            QueryTriggerInteraction.Ignore);
    }

    private void AlignBody()
    {
        Vector3 desiredUp = -gravDir;
        Vector3 currentUp = transform.up;
        if (Vector3.Angle(currentUp, desiredUp) < 0.3f) return;

        Quaternion target = Quaternion.FromToRotation(currentUp, desiredUp)
                            * transform.rotation;
        transform.rotation = Quaternion.Slerp(
            transform.rotation, target,
            Time.fixedDeltaTime * gravRotateSpeed);
    }

    private static string GetFaceLabel(Vector3 dir)
    {
        var map = new (Vector3 d, string n)[]
        {
            (Vector3.down, "Bottom"), (Vector3.up, "Top"),
            (Vector3.right, "Right"), (Vector3.left, "Left"),
            (Vector3.forward, "Front"), (Vector3.back, "Back")
        };

        float best = -1f;
        string label = "";
        foreach (var (d, n) in map)
        {
            float v = Vector3.Dot(dir, d);
            if (v > best)
            {
                best = v;
                label = n;
            }
        }
        return label;
    }

    private void OnCollisionEnter(Collision collision)
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

            IDamageable hp = FindDamageable(collision.collider);
            if (hp != null)
                hp.TakeDamage(pounceDamage * abilityDamageMultiplier);

            Rigidbody enemyRb =
                collision.collider.GetComponentInParent<Rigidbody>();
            if (enemyRb != null)
            {
                Vector3 dir = (collision.transform.position -
                               transform.position).normalized;
                dir += Vector3.up * 0.2f;

                enemyRb.linearVelocity = Vector3.zero;
                enemyRb.AddForce(
                    dir.normalized * enemyKnockback +
                    Vector3.up * enemyUpForce,
                    ForceMode.VelocityChange);
            }

            rb.linearVelocity *= 0.2f;
            StartCoroutine(HitStop(0.03f));
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Enemy")) return;

        vignetteTimer = 0.4f;
        if (vignette != null)
        {
            vignette.color.value = Color.red;
            vignette.intensity.value = 0.4f;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHp = Mathf.Max(0f, currentHp - amount);
        UpdateHpUI();
        vignetteTimer = 0.4f;

        if (currentHp <= 0f && image != null)
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
        if (hpText != null)
            hpText.text = $"HP: {Mathf.CeilToInt(currentHp)}";
    }

    private void OnGUI()
    {
        if (isDie || Time.timeScale == 0f) return;

        if (flashTimer > 0f)
        {
            GUI.color = new Color(
                0.5f, 0.8f, 1f,
                (flashTimer / 0.25f) * 0.18f);
            GUI.DrawTexture(
                new Rect(0, 0, Screen.width, Screen.height),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        float cx = Screen.width * .5f;
        float cy = Screen.height * .5f;
        GUI.color = flashTimer > 0f
            ? new Color(0.4f, 0.8f, 1f)
            : Color.white;

        GUI.DrawTexture(new Rect(cx - 12, cy - 1, 9, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx + 3, cy - 1, 9, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1, cy - 12, 2, 9), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1, cy + 3, 2, 9), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<CapsuleCollider>();
        if (col == null) return;

        Vector3 center = transform.TransformPoint(col.center);
        Gizmos.color = Color.yellow;
        foreach (Vector3 dir in SixDirs)
            Gizmos.DrawRay(center, dir * faceDetectRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, gravDir * 3f);
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(center, faceDetectRange);
    }
#endif

    private IEnumerator Pounce()
    {
        isPouncing = true;
        hasHitEnemy = false;
        pushCooldownTimer = pushCooldown;
        targetFOV = actionFOV;
        float timer = 0f;

        while (timer < pounceDuration && !hasHitEnemy)
        {
            Vector3 dir = Vector3.ProjectOnPlane(
                cameraTransform.forward, gravDir).normalized;
            rb.linearVelocity = dir * pounceSpeed;
            timer += Time.deltaTime;
            yield return null;
        }

        isPouncing = false;
        targetFOV = defaultFOV;
    }

    private IEnumerator HitStop(float duration)
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
