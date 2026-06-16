// DoubleMagnumShooter.cs — 왼쪽/오른쪽 번갈아 발사 + 팔 개별 반동
using UnityEngine;
 
public class DoubleMagnumShooter : MonoBehaviour
{
    [Header("── 히어라키 연결 ──")]
    public Transform barrelLeft;
    public Transform barrelRight;
 
    [Header("── 팔 반동 (각각 연결) ──")]
    public ArmRecoil recoilLeft;    // L 오브젝트의 ArmRecoil
    public ArmRecoil recoilRight;   // shoulder.R 오브젝트의 ArmRecoil
    public CameraShake cameraShake;    // 카메라 흔들림 (양쪽 공통)asd
 
    [Header("── 총알 설정 ──")]
    public GameObject bulletPrefab;
    public float bulletSpeed  = 120f;
    public float bulletDamage = 75f;
 
    [Header("── 발사 설정 ──")]
    public float fireRate = 0.55f;
 
    [Header("── 이펙트 ──")]
    public ParticleSystem muzzleFlashLeft;
    public ParticleSystem muzzleFlashRight;
    public AudioClip      shootSound;
 
    [Header("── 오브젝트 풀 ──")]
    public int poolSize = 20;
 
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
 
    void Shoot()
    {
        nextFireTime = Time.time + fireRate;
 
        bool useLeft = (currentBarrel == 0);
        currentBarrel = 1 - currentBarrel;
 
        Transform barrel = useLeft ? barrelLeft : barrelRight;
 
        SpawnBullet(barrel);
 
        //(useLeft ? muzzleFlashLeft : muzzleFlashRight)?.Play();
        audioSource?.PlayOneShot(shootSound);
 
        // ★ 발사한 쪽 팔만 반동
        if (useLeft)  recoilLeft?.TriggerRecoil();
        else          recoilRight?.TriggerRecoil();
        cameraShake?.Shake(0.1f, 0.1f);
    }
 
    void SpawnBullet(Transform barrel)
    {
        if (bulletPrefab == null || barrel == null) return;
 
        GameObject bulletObj = GetBulletFromPool();
        bulletObj.transform.position = barrel.position;
        bulletObj.transform.rotation = barrel.rotation;
        bulletObj.SetActive(true);
 
        bulletObj.GetComponent<Bullet>()?.Init(barrel.forward, bulletSpeed, bulletDamage);
    }
 
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
 