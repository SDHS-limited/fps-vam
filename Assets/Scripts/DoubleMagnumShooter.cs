// DoubleMagnumShooter.cs — 기본 발사 (반동 없음)
using UnityEngine;
using System.Collections;

public class DoubleMagnumShooter : MonoBehaviour
{
    [Header("── 히어라키 연결 ──")]
    public Transform barrelLeft;      // Pistol_D 하위 빈 오브젝트 (총구)
    public Transform barrelRight;     // Pistol_D (1) 하위 빈 오브젝트 (총구)

    [Header("── 총알 설정 ──")]
    public GameObject bulletPrefab;
    public float bulletSpeed  = 120f;
    public float bulletDamage = 75f;

    [Header("── 발사 설정 ──")]
    public float fireRate = 0.55f;    // 발사 간격 (초)

    [Header("── 이펙트 ──")]
    public ParticleSystem muzzleFlashLeft;
    public ParticleSystem muzzleFlashRight;
    public AudioClip      shootSound;

    [Header("── 오브젝트 풀 ──")]
    public int poolSize = 20;

    // 내부 상태
    private float        nextFireTime  = 0f;
    private int          currentBarrel = 0;   // 0=왼쪽, 1=오른쪽
    private GameObject[] pool;
    private int          poolIndex = 0;
    private AudioSource  audioSource;

    void Start()
    {
        audioSource = GetComponentInParent<AudioSource>();
        InitPool();
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
            Shoot();
    }

    // ── 발사
    void Shoot()
    {
        nextFireTime = Time.time + fireRate;

        bool useLeft = (currentBarrel == 0);
        currentBarrel = 1 - currentBarrel;

        Transform barrel = useLeft ? barrelLeft : barrelRight;

        // 총알 스폰
        SpawnBullet(barrel);

        // 머즐 플래시
//        (useLeft ? muzzleFlashLeft : muzzleFlashRight)?.Play();

        // 발사음
        audioSource?.PlayOneShot(shootSound);
    }

    // ── 총구 방향으로 총알 생성
    void SpawnBullet(Transform barrel)
    {
        if (bulletPrefab == null || barrel == null) return;

        GameObject bulletObj = GetBulletFromPool();
        bulletObj.transform.position = barrel.position;
        bulletObj.transform.rotation = barrel.rotation;
        bulletObj.SetActive(true);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        bullet?.Init(barrel.forward, bulletSpeed, bulletDamage);
    }

    // ── 오브젝트 풀
    void InitPool()
    {
        if (poolSize <= 0 || bulletPrefab == null) return;
        pool = new GameObject[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            pool[i] = Instantiate(bulletPrefab);
            pool[i].SetActive(false);
        }
    }

    GameObject GetBulletFromPool()
    {
        if (pool == null) return Instantiate(bulletPrefab);
        for (int i = 0; i < poolSize; i++)
        {
            int idx = (poolIndex + i) % poolSize;
            if (!pool[idx].activeInHierarchy)
            {
                poolIndex = (idx + 1) % poolSize;
                return pool[idx];
            }
        }
        var obj = pool[poolIndex];
        obj.SetActive(false);
        poolIndex = (poolIndex + 1) % poolSize;
        return obj;
    }
}
