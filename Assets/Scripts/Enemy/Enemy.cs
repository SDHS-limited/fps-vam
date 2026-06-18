using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHp = 100;
    public float moveSpeed = 4f;
    public float attackRange = 2f;

    [Header("Hit Reaction Settings")]
    public float stunDuration = 0.2f; // 총에 맞았을 때 경직되는 시간 (초)
    public Color hitColor = Color.red; // 피격 시 깜빡일 색상

    [Header("Hit Motion Settings")]
    public float knockbackForce = 0.5f; // 뒤로 밀려나는 힘 (수치가 클수록 멀리 밀려남)
    public Animator animator; // 애니메이터 컴포넌트 (있는 경우 연결)

    private float stunTimer = 0f;
    private Renderer enemyRenderer;
    private Color originalColor;

    private float hp;
    private Transform player;
    private EnemyGravity gravity;
    public int scoreValue = 10;
    public ScoreManager scoreManager;

    void Start()
    {
        hp = maxHp;
        gravity = GetComponent<EnemyGravity>();
        scoreManager = FindObjectOfType<ScoreManager>();

        // 적의 메쉬 렌더러를 찾아 원래 색상을 저장해둡니다.
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

        // 1. 리액션(경직) 상태 체크
        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;

            // 피격 색상에서 원래 색상으로 자연스럽게 돌아오는 연출
            if (enemyRenderer != null)
            {
                enemyRenderer.material.color = Color.Lerp(originalColor, hitColor, stunTimer / stunDuration);
            }

            return; // 경직 타이머가 도는 동안에는 이동 및 공격 로직을 실행하지 않음 (return)
        }
        else if (enemyRenderer != null && enemyRenderer.material.color != originalColor)
        {
            // 경직이 끝나면 원래 색상으로 확실하게 복구
            enemyRenderer.material.color = originalColor;
        }

        // 2. 기본 AI 로직 (경직 상태가 아닐 때만 실행됨)
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
        }
        else
        {
            // 체력이 남아있다면 피격 리액션 실행
            Reaction();
        }
    }

    // 피격 모션과 리액션을 동시에 처리하는 함수
    void Reaction()
    {
        // 1. 기존 리액션 (경직 및 색상 변경)
        stunTimer = stunDuration;
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = hitColor;
        }

        // 2. 애니메이션 모션 (Animator가 연결되어 있을 때만 실행)
        if (animator != null)
        {
            // 애니메이터 컨트롤러에 "Hit"라는 이름의 Trigger 파라미터가 있어야 합니다.
            animator.SetTrigger("Hit");
        }

        // 3. 넉백 모션 (순간적으로 뒤로 밀려남)
        if (player != null)
        {
            Vector3 grav = gravity.GravityDir;

            // 플레이어에서 적을 향하는 방향을 구한 뒤, 중력 평면에 투영하여 바닥을 뚫지 않게 함
            Vector3 knockbackDir = Vector3.ProjectOnPlane(transform.position - player.position, grav).normalized;

            // 적을 계산된 방향으로 밀어냄
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