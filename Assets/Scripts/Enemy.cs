using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHp = 100;
    public float moveSpeed = 4f;

    public float attackRange = 2f;

    private float hp;

    private Transform player;

    private EnemyGravity gravity;
    
    public int scoreValue = 10;

    public ScoreManager scoreManager;

    public void Init(ScoreManager manager)
    {
        scoreManager = manager;
    }

    void Start()
    {
        hp = maxHp;

        gravity = GetComponent<EnemyGravity>();

        GameObject obj =
            GameObject.FindGameObjectWithTag("Player");

        if(obj)
            player = obj.transform;
    }

    void Update()
    {
        if(player == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position);

        if(distance > attackRange)
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
        Vector3 grav =
            gravity.GravityDir;

        Vector3 dir =
            Vector3.ProjectOnPlane(
                player.position -
                transform.position,
                grav).normalized;

        transform.position +=
            dir * moveSpeed * Time.deltaTime;

        if(dir != Vector3.zero)
        {
            Quaternion look =
                Quaternion.LookRotation(
                    dir,
                    -grav);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    look,
                    Time.deltaTime * 8f);
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

        if(hp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        scoreManager.AddScore(scoreValue);

        Destroy(gameObject);
    }

}
