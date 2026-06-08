// DoubleMagnumAnimator.cs
using UnityEngine;
using System.Collections;

public class DoubleMagnumAnimator : MonoBehaviour
{
    [Header("── 히어라키 연결 ──")]
    public Transform arm;
    public Transform armRIG;
    public Transform sleeve;
    public Transform gunPivot;
    public Transform pistolLeft;
    public Transform pistolRight;
    public Transform barrelLeft;
    public Transform barrelRight;
 
    [Header("── 총알 설정 ──")]
    public GameObject bulletPrefab;
    public float      bulletSpeed  = 120f;
    public float      bulletDamage = 75f;
    public float      spreadAngle  = 1.2f;
 
    [Header("── 오브젝트 풀 ──")]
    public int poolSize = 20;
    private GameObject[] pool;
    private int poolIndex = 0;
 
    [Header("── 총기 설정 ──")]
    public float fireRate   = 0.55f;
    public int   maxAmmo    = 12;
    public float reloadTime = 2.5f;
 
    [Header("── ArmRIG 자세 ──")]
    public Vector3 armRigRestRot   = new Vector3(-10f, 0f, 0f);
    public Vector3 armRigReloadRot = new Vector3( 30f, 0f, 0f);
    public Vector3 armRigAdsRot    = new Vector3( -5f, 0f, 0f);
 
    [Header("── GunPivot 반동 ──")]
    public Vector3 gunPivotRecoilRot    = new Vector3(-12f, 0f, 0f);
    public float   gunPivotRecoilSpeed  = 25f;
    public float   gunPivotRecoverSpeed = 9f;
 
    [Header("── 개별 총기 반동 ──")]
    public Vector3 pistolRecoilPos    = new Vector3(0f, 0.005f, -0.06f);
    public Vector3 pistolRecoilRot    = new Vector3(-8f, 0f, 2f);
    public float   pistolRecoilSpeed  = 30f;
    public float   pistolRecoverSpeed = 12f;
 
    [Header("── Sleeve 흔들림 ──")]
    public float sleeveFollowSpeed = 6f;
 
    [Header("── 걷기 흔들림 ──")]
    public float bobSpeed   = 7f;
    public float bobAmountX = 0.012f;
    public float bobAmountY = 0.008f;
 
    [Header("── 이펙트 ──")]
    public ParticleSystem muzzleFlashLeft;
    public ParticleSystem muzzleFlashRight;
    public AudioClip      shootSound;
    public AudioClip      reloadSound;
    public AudioClip      emptySound;
 
    // 내부 상태
    private int   currentAmmo;
    private bool  isReloading  = false;
    private bool  isADS        = false;
    private float nextFireTime = 0f;
    private int   currentBarrel = 0;
 
    private Vector3 armRigCurrentRot, armRigTargetRot;
    private Vector3 gunPivotCurrentRot, gunPivotTargetRot;
 
    private Vector3 pistolLeftBasePos,  pistolRightBasePos;
    private Vector3 pistolLeftBaseRot,  pistolRightBaseRot;
    private Vector3 pistolLeftCurPos,   pistolRightCurPos;
    private Vector3 pistolLeftCurRot,   pistolRightCurRot;
    private Vector3 pistolLeftTgtPos,   pistolRightTgtPos;
    private Vector3 pistolLeftTgtRot,   pistolRightTgtRot;
 
    private float bobTimer = 0f;
 
    private Camera              fpsCam;
    private AudioSource         audioSource;
    private CharacterController charCtrl;
 
    void Start()
    {
        fpsCam      = Camera.main;
        audioSource = GetComponentInParent<AudioSource>();
        charCtrl    = GetComponentInParent<CharacterController>();
        currentAmmo = maxAmmo;
 
        armRigCurrentRot = armRigRestRot;
        armRigTargetRot  = armRigRestRot;
 
        if (pistolLeft)  { pistolLeftBasePos  = pistolLeft.localPosition;  pistolLeftBaseRot  = pistolLeft.localEulerAngles; }
        if (pistolRight) { pistolRightBasePos = pistolRight.localPosition; pistolRightBaseRot = pistolRight.localEulerAngles; }
 
        InitPool();
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
            if (!pool[idx].activeInHierarchy) { poolIndex = (idx + 1) % poolSize; return pool[idx]; }
        }
        // 풀이 꽉 찼으면 강제 재사용
        var obj = pool[poolIndex];
        obj.SetActive(false);
        poolIndex = (poolIndex + 1) % poolSize;
        return obj;
    }
 
    void Update()
    {
        HandleInput();
        UpdateArmRIG();
        UpdateGunPivot();
        UpdatePistols();
        UpdateBobbing();
    }
 
    void HandleInput()
    {
        if (isReloading) return;
        isADS = Input.GetButton("Fire2");
 
        if (Input.GetButtonDown("Fire1"))
        {
            if (currentAmmo > 0 && Time.time >= nextFireTime) Shoot();
            else if (currentAmmo <= 0) audioSource?.PlayOneShot(emptySound);
        }
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo)
            StartCoroutine(Reload());
    }
 
    void Shoot()
    {
        nextFireTime = Time.time + fireRate;
        currentAmmo--;
 
        bool useLeft = (currentBarrel == 0);
        currentBarrel = 1 - currentBarrel;
 
        Transform barrel = useLeft ? barrelLeft : barrelRight;
 
        SpawnBullet(barrel);
 
        (useLeft ? muzzleFlashLeft : muzzleFlashRight)?.Play();
        audioSource?.PlayOneShot(shootSound);
 
        gunPivotTargetRot += gunPivotRecoilRot;
 
        if (useLeft)  { pistolLeftTgtPos  = pistolRecoilPos; pistolLeftTgtRot  = pistolLeftBaseRot  + pistolRecoilRot; }
        else          { pistolRightTgtPos = pistolRecoilPos; pistolRightTgtRot = pistolRightBaseRot + pistolRecoilRot; }
    }
 
    // ── 총알 스폰
    void SpawnBullet(Transform barrel)
    {
        if (bulletPrefab == null || barrel == null) return;
 
        GameObject bulletObj = GetBulletFromPool();
        bulletObj.transform.position = barrel.position;
        bulletObj.transform.rotation = barrel.rotation;
        bulletObj.SetActive(true);
 
        // 카메라 조준 방향 + 퍼짐
        Vector3 aimDir = fpsCam.transform.forward;
        aimDir += new Vector3(
            Random.Range(-spreadAngle, spreadAngle) * 0.01f,
            Random.Range(-spreadAngle, spreadAngle) * 0.01f,
            0f
        );
 
        bulletObj.GetComponent<Bullet>()?.Init(aimDir.normalized, bulletSpeed, bulletDamage);
    }
 
    void UpdateArmRIG()
    {
        if (!isReloading)
        {
            Vector3 rest = isADS ? armRigAdsRot : armRigRestRot;
            armRigTargetRot = Vector3.MoveTowards(armRigTargetRot, rest, Time.deltaTime * 60f);
        }
        armRigCurrentRot = Vector3.Lerp(armRigCurrentRot, armRigTargetRot, Time.deltaTime * gunPivotRecoilSpeed);
        if (armRIG) armRIG.localEulerAngles = armRigCurrentRot;
        if (sleeve) sleeve.localEulerAngles = Vector3.Lerp(sleeve.localEulerAngles, armRigCurrentRot, Time.deltaTime * sleeveFollowSpeed);
    }
 
    void UpdateGunPivot()
    {
        gunPivotTargetRot  = Vector3.Lerp(gunPivotTargetRot,  Vector3.zero,       Time.deltaTime * gunPivotRecoverSpeed);
        gunPivotCurrentRot = Vector3.Lerp(gunPivotCurrentRot, gunPivotTargetRot,  Time.deltaTime * gunPivotRecoilSpeed);
        if (gunPivot) gunPivot.localEulerAngles = gunPivotCurrentRot;
    }
 
    void UpdatePistols()
    {
        pistolLeftTgtPos = Vector3.Lerp(pistolLeftTgtPos, Vector3.zero,       Time.deltaTime * pistolRecoverSpeed);
        pistolLeftTgtRot = Vector3.Lerp(pistolLeftTgtRot, pistolLeftBaseRot,  Time.deltaTime * pistolRecoverSpeed);
        pistolLeftCurPos = Vector3.Lerp(pistolLeftCurPos, pistolLeftTgtPos,   Time.deltaTime * pistolRecoilSpeed);
        pistolLeftCurRot = Vector3.Lerp(pistolLeftCurRot, pistolLeftTgtRot,   Time.deltaTime * pistolRecoilSpeed);
        if (pistolLeft)  { pistolLeft.localPosition = pistolLeftBasePos + pistolLeftCurPos; pistolLeft.localEulerAngles = pistolLeftCurRot; }
 
        pistolRightTgtPos = Vector3.Lerp(pistolRightTgtPos, Vector3.zero,        Time.deltaTime * pistolRecoverSpeed);
        pistolRightTgtRot = Vector3.Lerp(pistolRightTgtRot, pistolRightBaseRot,  Time.deltaTime * pistolRecoverSpeed);
        pistolRightCurPos = Vector3.Lerp(pistolRightCurPos, pistolRightTgtPos,   Time.deltaTime * pistolRecoilSpeed);
        pistolRightCurRot = Vector3.Lerp(pistolRightCurRot, pistolRightTgtRot,   Time.deltaTime * pistolRecoilSpeed);
        if (pistolRight) { pistolRight.localPosition = pistolRightBasePos + pistolRightCurPos; pistolRight.localEulerAngles = pistolRightCurRot; }
    }
 
    void UpdateBobbing()
    {
        bool moving = charCtrl != null && charCtrl.velocity.magnitude > 0.1f;
        if (moving && !isReloading)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bx = Mathf.Sin(bobTimer)            * bobAmountX;
            float by = Mathf.Abs(Mathf.Sin(bobTimer)) * bobAmountY;
            if (arm) arm.localPosition += new Vector3(bx, by, 0f);
        }
    }
 
    IEnumerator Reload()
    {
        isReloading = true;
        audioSource?.PlayOneShot(reloadSound);
 
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime / (reloadTime * 0.35f); armRigTargetRot = Vector3.Lerp(armRigRestRot, armRigReloadRot, t); yield return null; }
        yield return new WaitForSeconds(reloadTime * 0.35f);
        t = 0f;
        while (t < 1f) { t += Time.deltaTime / (reloadTime * 0.3f);  armRigTargetRot = Vector3.Lerp(armRigReloadRot, armRigRestRot, t); yield return null; }
 
        currentAmmo   = maxAmmo;
        currentBarrel = 0;
        isReloading   = false;
    }
 
    public int  GetCurrentAmmo() => currentAmmo;
    public int  GetMaxAmmo()     => maxAmmo;
    public bool IsReloading()    => isReloading;
}