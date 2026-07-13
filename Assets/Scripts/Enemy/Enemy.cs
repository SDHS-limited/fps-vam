using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHp = 100;
    public float moveSpeed = 4f;
    public float attackRange = 2f;

    [Header("Hit Reaction Settings")]
    public float stunDuration = 0.2f; // �ѿ� �¾��� �� �����Ǵ� �ð� (��)
    public Color hitColor = Color.red; // �ǰ� �� ������ ����

    [Header("Hit Motion Settings")]
    public float knockbackForce = 0.5f; // �ڷ� �з����� �� (��ġ�� Ŭ���� �ָ� �з���)
    public Animator animator; // �ִϸ����� ������Ʈ (�ִ� ��� ����)

    private float stunTimer = 0f;
    private Renderer enemyRenderer;
    private Color originalColor;

    private float hp;
    private Transform player;
    private EnemyGravity gravity;
    public int scoreValue = 10;
    public ScoreManager scoreManager;
    public GravityPlayerController gravityPlayerController;

    private Vector3 knockbackVelocity;
    private float knockbackTimer;

    public float knockbackTime = 0.2f;
    public float knockbackDamping = 8f;

    void Start()
    {
        hp = maxHp;
        gravity = GetComponent<EnemyGravity>();
        scoreManager = FindObjectOfType<ScoreManager>();

        // ���� �޽� �������� ã�� ���� ������ �����صӴϴ�.
        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }

        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj)
            player = obj.transform;
    }

    void Update()
    {
        if (player == null)
            return;

        if (knockbackTimer > 0f)
        {
            transform.position += knockbackVelocity * Time.deltaTime;

            knockbackVelocity = Vector3.Lerp(
                knockbackVelocity,
                Vector3.zero,
                knockbackDamping * Time.deltaTime);

            knockbackTimer -= Time.deltaTime;

            return;
        }

        // 1. ���׼�(����) ���� üũ
        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;

            // �ǰ� ���󿡼� ���� �������� �ڿ������� ���ƿ��� ����
            if (enemyRenderer != null)
            {
                enemyRenderer.material.color = Color.Lerp(originalColor, hitColor, stunTimer / stunDuration);
            }

            return; // ���� Ÿ�̸Ӱ� ���� ���ȿ��� �̵� �� ���� ������ �������� ���� (return)
        }
        else if (enemyRenderer != null && enemyRenderer.material.color != originalColor)
        {
            // ������ ������ ���� �������� Ȯ���ϰ� ����
            enemyRenderer.material.color = originalColor;
        }

        // 2. �⺻ AI ���� (���� ���°� �ƴ� ���� �����)
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange)
        {
            MoveToPlayer();
        }
        else
        {
            Attack();
        }
    }

    void MoveToPlayer()
    {
        Vector3 grav = gravity.GravityDir;
        Vector3 dir = Vector3.ProjectOnPlane(player.position - transform.position, grav).normalized;

        transform.position += dir * moveSpeed * Time.deltaTime;

        if (dir != Vector3.zero)
        {
            Quaternion look = Quaternion.LookRotation(dir, -grav);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 8f);
        }
    }   
    public void Knockback(Vector3 direction, float power)
    {
        knockbackVelocity = direction.normalized * power;
        knockbackTimer = knockbackTime;
    }

    void Attack()
    {
        // TODO
        // animator.SetTrigger("Attack");

        // TODO
        // hitBox.SetActive(true);

        // TODO
        // player.TakeDamage(damage);
    }

    public void TakeDamage(float damage)
    {
        hp -= damage;

        if (hp <= 0)
        {
            Die();
            gravityPlayerController.OnEnemyKilled(); // 적을 처치하면 행복도 10 증가
        }
        else
        {
            // ü���� �����ִٸ� �ǰ� ���׼� ����
            Reaction();
        }
    }

    // �ǰ� ��ǰ� ���׼��� ���ÿ� ó���ϴ� �Լ�
    void Reaction()
    {
        // 1. ���� ���׼� (���� �� ���� ����)
        stunTimer = stunDuration;
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = hitColor;
        }

        // 2. �ִϸ��̼� ��� (Animator�� ����Ǿ� ���� ���� ����)
        if (animator != null)
        {
            // �ִϸ����� ��Ʈ�ѷ��� "Hit"��� �̸��� Trigger �Ķ���Ͱ� �־�� �մϴ�.
            animator.SetTrigger("Hit");
        }

        // 3. �˹� ��� (���������� �ڷ� �з���)
        if (player != null)
        {
            Vector3 grav = gravity.GravityDir;

            // �÷��̾�� ���� ���ϴ� ������ ���� ��, �߷� ��鿡 �����Ͽ� �ٴ��� ���� �ʰ� ��
            Vector3 knockbackDir = Vector3.ProjectOnPlane(transform.position - player.position, grav).normalized;

            // ���� ���� �������� �о
            transform.position += knockbackDir * knockbackForce;
        }
    }

    void Die()
    {
        Debug.Log("Enemy.Die called for " + gameObject.name + ", scoreValue=" + scoreValue);

        if (scoreManager == null)
        {
            Debug.LogWarning("Enemy.Die: 'scoreManager' is null. Attempting to find ScoreManager in scene.");
            scoreManager = FindObjectOfType<ScoreManager>();
        }

        if (scoreManager != null)
        {
            scoreManager.AddScore(scoreValue);
        }
        else
        {
            Debug.LogError("Enemy.Die: Could not find ScoreManager; score not awarded.");
        }

        Destroy(gameObject);
    }
}