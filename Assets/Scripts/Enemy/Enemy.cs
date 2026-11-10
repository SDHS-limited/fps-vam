using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHp = 100f;
    public float moveSpeed = 4f;
    public float attackRange = 2f;

    [Header("Hit Reaction")]
    public float stunDuration = 0.12f;
    public Color hitColor = Color.red;
    public float knockbackForce = 0.5f;
    public Animator animator;
    public float knockbackTime = 0.2f;
    public float knockbackDamping = 8f;

    [Header("Gravity Pulse")]
    public bool isBrute;
    [Range(0.05f, 1f)] public float brutePulseForceMultiplier = 0.25f;
    [Min(0f)] public float brutePulseStunDuration = 0.35f;
    [Min(0.1f)] public float pulseTravelDuration = 0.65f;
    [Min(0f)] public float minimumWallImpactSpeed = 6f;

    public int scoreValue = 10;
    public ScoreManager scoreManager;
    public GravityPlayerController gravityPlayerController;

    private float hp;
    private float stunTimer;
    private float knockbackTimer;
    private float nextBulletEffectAt;
    private float nextReactionAt;
    private Vector3 knockbackVelocity;
    private Transform player;
    private EnemyGravity gravity;
    private Renderer enemyRenderer;
    private Color originalColor;
    private MaterialPropertyBlock hitProperties;
    private int colorProperty;
    private bool showingHitColor;
    private bool dying;
    private Rigidbody body;
    private CollisionDetectionMode normalCollisionMode;
    private bool pulseFlying;
    private bool pulseWallHit;
    private Vector3 pulseDirection;
    private float pulseEndsAt;
    private float pulseWallDamage;
    private float pulseLaunchSpeed;

    void Awake()
    {
        hp = maxHp;
        gravity = GetComponent<EnemyGravity>();
        body = GetComponent<Rigidbody>();
        if (body != null) normalCollisionMode = body.collisionDetectionMode;
    }

    void Start()
    {
        scoreManager = scoreManager != null ? scoreManager : FindObjectOfType<ScoreManager>();
        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null && enemyRenderer.sharedMaterial != null)
        {
            var material = enemyRenderer.sharedMaterial;
            colorProperty = material.HasProperty("_BaseColor") ? Shader.PropertyToID("_BaseColor")
                          : material.HasProperty("_Color") ? Shader.PropertyToID("_Color") : 0;
            if (colorProperty != 0)
            {
                originalColor = material.GetColor(colorProperty);
                hitProperties = new MaterialPropertyBlock();
            }
        }

        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj == null) return;
        player = obj.transform;
        if (gravityPlayerController == null)
            gravityPlayerController = obj.GetComponent<GravityPlayerController>();
    }

    void Update()
    {
        bool stunned = stunTimer > 0f;
        if (stunned)
        {
            stunTimer = Mathf.Max(0f, stunTimer - Time.deltaTime);
            SetHitColor(Color.Lerp(originalColor, hitColor,
                Mathf.Clamp01(stunTimer / Mathf.Max(0.01f, stunDuration))));
        }
        else if (showingHitColor)
        {
            SetHitColor(originalColor);
            showingHitColor = false;
        }

        if (pulseFlying)
        {
            if (Time.time < pulseEndsAt) return;
            EndPulseFlight();
        }

        if (knockbackTimer > 0f)
        {
            transform.position += knockbackVelocity * Time.deltaTime;
            knockbackVelocity *= Mathf.Exp(-knockbackDamping * Time.deltaTime);
            knockbackTimer -= Time.deltaTime;
            return;
        }
        if (stunned || dying || player == null) return;

        if (Vector3.Distance(transform.position, player.position) > attackRange)
            MoveToPlayer();
        else
            Attack();
    }

    void MoveToPlayer()
    {
        Vector3 grav = gravity != null ? gravity.GravityDir : Vector3.down;
        Vector3 dir = Vector3.ProjectOnPlane(player.position - transform.position, grav).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(dir, -grav);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 8f);
        }
    }

    public void Knockback(Vector3 direction, float power)
    {
        if (direction.sqrMagnitude < 0.0001f) return;
        knockbackVelocity = direction.normalized * power;
        knockbackTimer = knockbackTime;
    }

    public bool ApplyGravityPulse(Vector3 direction, float speed, float directDamage, float wallDamage)
    {
        if (dying || body == null || direction.sqrMagnitude < 0.0001f) return false;
        ApplyDamage(directDamage, direction);
        if (dying) return true;

        pulseDirection = direction.normalized;
        pulseLaunchSpeed = speed * (isBrute ? brutePulseForceMultiplier : 1f);
        pulseWallDamage = wallDamage * (isBrute ? 0.4f : 1f);
        pulseEndsAt = Time.time + (isBrute ? Mathf.Min(0.35f, pulseTravelDuration) : pulseTravelDuration);
        pulseWallHit = false;
        pulseFlying = true;
        knockbackTimer = 0f;
        knockbackVelocity = Vector3.zero;
        stunTimer = Mathf.Max(stunTimer, isBrute ? brutePulseStunDuration : 0.22f);
        SetHitColor(hitColor);
        showingHitColor = true;
        if (animator != null) animator.SetTrigger("Hit");

        Vector3 grav = gravity != null ? gravity.GravityDir : Vector3.down;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = Vector3.Project(body.linearVelocity, grav) +
                              pulseDirection * pulseLaunchSpeed;
        body.WakeUp();
        return true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckPulseWallImpact(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        CheckPulseWallImpact(collision);
    }

    private void CheckPulseWallImpact(Collision collision)
    {
        if (!pulseFlying || pulseWallHit || pulseLaunchSpeed < minimumWallImpactSpeed ||
            !collision.collider.CompareTag("Ground")) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Dot(pulseDirection, -collision.GetContact(i).normal) < 0.55f)
                continue;
            pulseWallHit = true;
            EndPulseFlight();
            ApplyDamage(pulseWallDamage, pulseDirection);
            break;
        }
    }

    private void EndPulseFlight()
    {
        pulseFlying = false;
        if (body == null) return;
        body.collisionDetectionMode = normalCollisionMode;
        Vector3 grav = gravity != null ? gravity.GravityDir : Vector3.down;
        body.linearVelocity = Vector3.Project(body.linearVelocity, grav) +
                              Vector3.ProjectOnPlane(body.linearVelocity, grav) * 0.2f;
    }

    void Attack()
    {
        // Attack behavior is defined separately from hit feedback.
    }

    // Returns whether this pellet should spawn an impact effect. Damage always applies.
    public bool TakeBulletHit(float damage, Vector3 shotDirection)
    {
        if (dying || damage <= 0f) return false;
        bool showEffect = Time.time >= nextBulletEffectAt;
        if (showEffect) nextBulletEffectAt = Time.time + 0.05f;
        ApplyDamage(damage, shotDirection);
        return showEffect;
    }

    public void TakeDamage(float damage)
    {
        ApplyDamage(damage, Vector3.zero);
    }

    void ApplyDamage(float damage, Vector3 hitDirection)
    {
        if (dying || damage <= 0f) return;
        hp -= damage;
        bool lethal = hp <= 0f;
        if (gravityPlayerController != null) gravityPlayerController.ShowHitMarker(lethal);
        if (lethal)
        {
            Die();
            if (gravityPlayerController != null) gravityPlayerController.OnEnemyKilled();
        }
        else
        {
            React(hitDirection);
        }
    }

    void React(Vector3 hitDirection)
    {
        if (Time.time < nextReactionAt) return;
        nextReactionAt = Time.time + 0.05f;
        stunTimer = stunDuration;
        SetHitColor(hitColor);
        showingHitColor = true;
        if (animator != null) animator.SetTrigger("Hit");

        Vector3 grav = gravity != null ? gravity.GravityDir : Vector3.down;
        Vector3 direction = Vector3.ProjectOnPlane(hitDirection, grav);
        if (direction.sqrMagnitude < 0.0001f && player != null)
            direction = Vector3.ProjectOnPlane(transform.position - player.position, grav);
        if (direction.sqrMagnitude < 0.0001f) return;

        float speed = knockbackForce / Mathf.Max(0.05f, knockbackTime);
        if (knockbackTimer <= 0f || knockbackVelocity.sqrMagnitude < speed * speed)
            Knockback(direction, speed);
    }

    void Die()
    {
        dying = true;
        if (scoreManager == null) scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager != null) scoreManager.AddScore(scoreValue);
        Destroy(gameObject);
    }

    void SetHitColor(Color color)
    {
        if (enemyRenderer == null || hitProperties == null) return;
        enemyRenderer.GetPropertyBlock(hitProperties);
        hitProperties.SetColor(colorProperty, color);
        enemyRenderer.SetPropertyBlock(hitProperties);
    }
}
