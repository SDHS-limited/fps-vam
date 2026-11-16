using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// REVFIELD - Gravity Breaker
/// 적 조준 -> 돌진하며 양팔 뒤로 당기기 -> 짧은 정지 -> 두 팔 동시 전방 밀치기 -> 넉백 -> 복귀.
/// 플레이어 이동 잠금과 FOV는 GravityPlayerController에서 연동합니다.
/// Unity 6 / Rigidbody.linearVelocity / 기존 Enemy.ApplyGravityPulse() 기반.
/// v4: 양팔 동시 밀치기, 양팔 충격 반동, 기존 돌진/FOV/명중 UI/넉백 유지.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GravityBreaker : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private GravityPlayerController player;
    [SerializeField] private Camera playerCamera;
    [Tooltip("화면 왼쪽 팔의 어깨 Transform 또는 전용 Pivot")]
    [SerializeField] private Transform leftArmPivot;
    [Tooltip("화면 오른쪽 팔의 어깨 Transform 또는 전용 Pivot")]
    [SerializeField] private Transform rightArmPivot;
    [Tooltip("공격 도중 발사를 막을 무기 스크립트들. 기존 ArmRecoil은 넣지 마세요.")]
    [SerializeField] private Behaviour[] disableDuringAttack;

    [Header("입력")]
    [SerializeField] private bool useKeyManagerPushBinding = true;
    [SerializeField] private KeyCode fallbackKey = KeyCode.E;
    [Min(0f)] [SerializeField] private float cooldown = 3f;

    [Header("타겟 탐색")]
    [SerializeField] private LayerMask enemyLayers = ~0;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [Min(1f)] [SerializeField] private float lockRange = 9f;
    [Min(0.1f)] [SerializeField] private float aimAssistRadius = 0.85f;
    [Tooltip("값이 클수록 조준 범위가 좁아집니다. 0.65는 전방 약 ±49도, 1은 정중앙만 허용합니다.")]
    [Range(0f, 1f)] [SerializeField] private float minAimDot = 0.65f;
    [Tooltip("다른 면에 있는 적까지 직선 돌진하지 않도록 현재 면과의 높이 차이를 제한")]
    [Min(0.1f)] [SerializeField] private float maxSurfaceHeightDifference = 2.0f;

    [Header("자동 접근")]
    [Min(1f)] [SerializeField] private float rushSpeed = 25f;
    [Tooltip("돌진 안전 제한의 최소값. 전체 탐색 거리를 이동할 시간은 자동으로 확보합니다.")]
    [Min(0.05f)] [SerializeField] private float maxRushDuration = 0.26f;
    [Min(0.1f)] [SerializeField] private float stopDistance = 1.35f;
    [Min(0.1f)] [SerializeField] private float strikeRange = 2.6f;
    [Min(0.05f)] [SerializeField] private float obstacleCheckRadius = 0.27f;

    [Header("양팔 밀치기 - 모션 타이밍")]
    [Tooltip("돌진하는 동안 양팔을 몸쪽으로 당기는 최소 시간")]
    [Min(0.05f)] [SerializeField] private float minimumWindupTime = 0.15f;
    [Tooltip("양팔을 당긴 자세를 유지하는 짧은 시간")]
    [Min(0f)] [SerializeField] private float raisedHoldDuration = 0.03f;
    [Tooltip("두 팔을 동시에 앞으로 내미는 시간. 기존 필드명을 유지해 Inspector 값을 보존합니다.")]
    [Min(0.05f)] [SerializeField] private float slamDuration = 0.10f;
    [Tooltip("밀치기 자세가 이 비율에 도달하면 실제 피해와 넉백을 적용")]
    [Range(0.1f, 0.95f)] [SerializeField] private float hitTimeRatio = 0.85f;
    [Tooltip("밀치기 이후 두 팔을 원위치시키는 시간")]
    [Min(0.05f)] [SerializeField] private float recoveryDuration = 0.18f;
    [Min(0f)] [SerializeField] private float directDamage = 50f;
    [Min(0f)] [SerializeField] private float wallDamage = 70f;
    [Min(1f)] [SerializeField] private float launchSpeed = 22f;
    [Range(-0.5f, 0.5f)] [SerializeField] private float upwardLaunchBias = 0.12f;

    [Header("양팔 준비 자세 - 카메라 방향 기준 (미터/도)")]
    [Tooltip("왼손을 몸쪽으로 당김. Z 음수는 카메라 뒤쪽")]
    [SerializeField] private Vector3 leftPullbackPosition = new Vector3(-0.01f, -0.01f, -0.05f);
    [SerializeField] private Vector3 leftPullbackRotation = new Vector3(6f, -8f, 0f);
    [Tooltip("오른손을 몸쪽으로 당김")]
    [SerializeField] private Vector3 rightPullbackPosition = new Vector3(0.01f, -0.01f, -0.05f);
    [SerializeField] private Vector3 rightPullbackRotation = new Vector3(6f, 8f, 0f);

    [Header("양팔 밀치기 자세 - 카메라 방향 기준 (미터/도)")]
    [Tooltip("왼팔을 카메라 정면(+Z)으로 뻗으며 화면 중앙 쪽으로 모음")]
    [SerializeField] private Vector3 leftPushPosition = new Vector3(0.015f, 0f, 0.10f);
    [SerializeField] private Vector3 leftPushRotation = new Vector3(-8f, 10f, 0f);
    [Tooltip("오른팔을 카메라 정면(+Z)으로 뻗으며 화면 중앙 쪽으로 모음")]
    [SerializeField] private Vector3 rightPushPosition = new Vector3(-0.015f, 0f, 0.10f);
    [SerializeField] private Vector3 rightPushRotation = new Vector3(-8f, -10f, 0f);

    [Header("팔 화면 이탈 방지")]
    [Tooltip("이전 팔 사라짐 문제 방지를 위해 공격 중 추가 이동과 회전 크기를 제한")]
    [SerializeField] private bool useArmMotionLimits = true;
    [Min(0f)] [SerializeField] private float maxArmPositionOffset = 0.16f;
    [Range(0f, 90f)] [SerializeField] private float maxArmAngleOffset = 30f;

    [Header("FOV / 타격 효과")]
    [SerializeField] private float rushFovOffset = 13f;
    [SerializeField] private float impactFovOffset = -5f;
    [SerializeField] private ParticleSystem impactVfx;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip rushClip;
    [SerializeField] private AudioClip slamClip;
    [SerializeField] private AudioClip hitClip;

    [Header("타격감 보강 (신규)")]
    [Tooltip("두 팔이 적중했을 때 양팔 자세만 잠깐 강조합니다. 게임 시간은 멈추지 않습니다.")]
    [Min(0f)] [SerializeField] private float impactPoseHold = 0.028f;
    [Tooltip("명중 시 양팔이 몸쪽으로 잠깐 밀리는 카메라 기준 오프셋")]
    [SerializeField] private Vector3 impactRecoilPosition = new Vector3(0f, 0f, -0.018f);
    [Tooltip("명중 시 양팔에 추가하는 카메라 기준 회전 오프셋")]
    [SerializeField] private Vector3 impactRecoilRotation = new Vector3(5f, 0f, 0f);
    [Tooltip("기존 GravityPlayerController.ShowHitMarker(false)를 명중 시 호출합니다.")]
    [SerializeField] private bool usePlayerHitMarker = true;
    [Tooltip("낮은 충격음을 추가로 겹쳐 재생합니다. hitClip 또는 플레이어 히트 사운드와 볼륨을 비교하세요.")]
    [SerializeField] private AudioClip impactBassClip;
    [Range(0f, 1f)] [SerializeField] private float impactBassVolume = 0.45f;
    [Min(0f)] [SerializeField] private float impactShakeDuration = 0.09f;
    [Min(0f)] [SerializeField] private float impactShakeStrength = 0.12f;

    [Header("디버그 / 팔 보이기 테스트")]
    [Tooltip("적이 없어도 공격 모션을 실행해 팔이 보이는지 확인합니다. 테스트 후 끄세요.")]
    [SerializeField] private bool previewMotionWithoutTarget = false;
    [Tooltip("참조가 없거나 타겟을 못 찾았을 때 Console에 원인을 출력합니다.")]
    [SerializeField] private bool logAttackFailures = true;

    public bool IsActive { get; private set; }
    public float FovOffset { get; private set; }
    public bool BlocksMovement { get; private set; }
    public float CooldownRemaining => Mathf.Max(0f, nextUseTime - Time.time);
    public bool IsRushing => IsActive && rushing;
    private Rigidbody body;
    private Enemy lockedEnemy;
    private Collider lockedCollider;
    private bool rushing;
    private bool rushBlocked;
    private float nextUseTime;
    private Coroutine attackRoutine;
    private Vector3 leftDefaultPosition;
    private Vector3 rightDefaultPosition;
    private Quaternion leftDefaultRotation;
    private Quaternion rightDefaultRotation;
    private readonly List<Behaviour> suspendedScripts = new List<Behaviour>();
    private readonly List<bool> previousEnabledStates = new List<bool>();
    private Vector3 currentLeftPosition;
    private Vector3 currentLeftAngles;
    private Vector3 currentRightPosition;
    private Vector3 currentRightAngles;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (player == null) player = GetComponent<GravityPlayerController>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        // AudioSource를 연결하지 않았을 때도 효과음이 묵음이 되지 않도록 전용 소스를 만듭니다.
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        if (leftArmPivot != null)
        {
            leftDefaultPosition = leftArmPivot.localPosition;
            leftDefaultRotation = leftArmPivot.localRotation;
        }
        if (rightArmPivot != null)
        {
            rightDefaultPosition = rightArmPivot.localPosition;
            rightDefaultRotation = rightArmPivot.localRotation;
        }

        if (leftArmPivot != null && leftArmPivot == rightArmPivot)
            Debug.LogWarning("[GravityBreaker] 왼팔/오른팔 Pivot이 같은 Transform입니다. 서로 다른 부모 오브젝트를 연결하세요.", this);
    }

    private void LateUpdate()
    {
        // Apply after recoil/animation updates, in camera space rather than the twisted bone axes.
        if (IsActive) ApplyArmPose();
    }

    private void Update()
    {
        if (player != null && player.IsDead)
        {
            if (IsActive) CancelAttack();
            return;
        }
        if (Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked) return;
        if (Input.GetKeyDown(GetAttackKey())) TryStartAttack();
    }

    private KeyCode GetAttackKey()
    {
        if (useKeyManagerPushBinding && KeyManager.instance != null &&
            KeyManager.instance.keys.TryGetValue("Push", out KeyCode key))
            return key;
        return fallbackKey;
    }

    public bool TryStartAttack()
    {
        if (!isActiveAndEnabled || IsActive || Time.timeScale == 0f || Time.time < nextUseTime) return false;
        if (player == null || player.IsDead || playerCamera == null ||
            leftArmPivot == null || rightArmPivot == null)
        {
            if (logAttackFailures)
                Debug.LogWarning("[GravityBreaker] 시작 실패: Player/Camera/LeftArmPivot/RightArmPivot 연결을 확인하세요.", this);
            return false;
        }

        bool hasTarget = FindTarget(out lockedEnemy, out lockedCollider);
        if (!hasTarget && !previewMotionWithoutTarget)
        {
            if (logAttackFailures)
                Debug.Log("[GravityBreaker] 전방 사거리 안에 같은 중력면의 보이는 적이 없습니다.", this);
            return false;
        }

        nextUseTime = Time.time + cooldown;
        IsActive = true;
        BlocksMovement = true;
        rushBlocked = false;
        ToggleWeaponScripts(false);
        attackRoutine = StartCoroutine(AttackSequence());
        return true;
    }

    private bool FindTarget(out Enemy bestEnemy, out Collider bestCollider)
    {
        bestEnemy = null;
        bestCollider = null;
        float bestScore = float.MaxValue;
        Vector3 origin = playerCamera.transform.position;
        Vector3 forward = playerCamera.transform.forward;

        // 조준선의 좁은 SphereCast 통로 밖에 있는 적도 전방 각도 범위에서 탐색합니다.
        // EvaluateCandidate에서 거리/각도/중력면/벽 가림을 검사하고 조준에 가까운 적을 선택합니다.
        Collider[] candidates = Physics.OverlapSphere(origin, lockRange, enemyLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in candidates)
            EvaluateCandidate(hit, origin, forward, ref bestEnemy, ref bestCollider, ref bestScore);

        RaycastHit[] hits = Physics.SphereCastAll(origin, aimAssistRadius, forward,
            lockRange, enemyLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
            EvaluateCandidate(hit.collider, origin, forward, ref bestEnemy, ref bestCollider, ref bestScore);

        return bestEnemy != null;
    }

    private void EvaluateCandidate(Collider hit, Vector3 origin, Vector3 forward,
        ref Enemy bestEnemy, ref Collider bestCollider, ref float bestScore)
    {
        if (hit == null || hit.transform.IsChildOf(transform)) return;
        Enemy candidate = hit.GetComponentInParent<Enemy>();
        if (candidate == null || !candidate.isActiveAndEnabled) return;

        Vector3 aimPoint = hit.bounds.center;
        Vector3 offset = aimPoint - origin;
        float distance = offset.magnitude;
        if (distance < 0.001f || distance > lockRange) return;

        float dot = Vector3.Dot(forward, offset / distance);
        if (dot < minAimDot) return;

        float surfaceDelta = Mathf.Abs(Vector3.Dot(
            candidate.transform.position - transform.position, transform.up));
        if (surfaceDelta > maxSurfaceHeightDifference) return;
        if (Blocked(origin, aimPoint, hit)) return;

        float score = (1f - dot) * 12f + distance * 0.15f;
        if (score >= bestScore) return;

        bestScore = score;
        bestEnemy = candidate;
        bestCollider = hit;
    }

    // 내 콜라이더와 목표 적의 콜라이더를 제외한 장애물 검사.
    private bool Blocked(Vector3 from, Vector3 to, Collider allowedCollider)
    {
        Vector3 delta = to - from;
        float length = delta.magnitude;
        if (length < 0.01f) return false;

        RaycastHit[] hits = Physics.RaycastAll(from, delta / length, length,
            obstacleLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == allowedCollider || hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<Enemy>() != null) continue;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 1) 돌진 중 두 팔을 몸쪽으로 당김
    /// 2) 양팔을 짧게 고정하여 힘을 모음
    /// 3) 양팔을 동시에 전방으로 밀면서 실제 피해/넉백 판정 (1회)
    /// 4) 명중하면 양팔에만 충격 반동 -> 자연스러운 원위치 복귀
    /// </summary>
    private IEnumerator AttackSequence()
    {
        PlaySound(rushClip);
        bool hasTarget = lockedEnemy != null && lockedCollider != null;
        rushing = hasTarget;
        float elapsed = 0f;
        float prepDuration = Mathf.Max(0.05f, minimumWindupTime);
        float rushTimeout = Mathf.Max(maxRushDuration,
            lockRange / Mathf.Max(1f, rushSpeed) + 0.2f);

        // 돌진과 양팔 뒤로 당기기를 병렬 처리
        while (rushing || elapsed < prepDuration)
        {
            if (hasTarget && (lockedEnemy == null || lockedCollider == null ||
                !lockedEnemy.isActiveAndEnabled || !lockedCollider.enabled || rushBlocked))
                break;
            if (rushing && elapsed >= rushTimeout)
            {
                rushBlocked = true;
                break;
            }
            ApplyWindupPose(elapsed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rushing = false;
        FinishRushMomentum();
        if (hasTarget && (rushBlocked || lockedEnemy == null || lockedCollider == null ||
            !lockedEnemy.isActiveAndEnabled || !lockedCollider.enabled ||
            DistanceToTarget() > stopDistance + 0.15f))
        {
            if (logAttackFailures)
                Debug.Log("[GravityBreaker] 접근 중 장애물/타겟 소실/시간 초과로 공격을 취소합니다.", this);
            yield return RecoverArms();
            FinishAttack();
            yield break;
        }

        SetArms(leftPullbackPosition, leftPullbackRotation,
            rightPullbackPosition, rightPullbackRotation);
        FovOffset = rushFovOffset;
        float holdElapsed = 0f;
        while (holdElapsed < raisedHoldDuration)
        {
            holdElapsed += Time.deltaTime;
            yield return null;
        }

        PlaySound(slamClip); // 기존 Slam 사운드 필드 = 양팔 내미는 휘두름 소리
        bool strikeChecked = false;
        float pushElapsed = 0f;
        float pushSeconds = Mathf.Max(0.05f, slamDuration);

        while (pushElapsed < pushSeconds)
        {
            float t = Mathf.Clamp01(pushElapsed / pushSeconds);
            float eased = t * t; // 서서히 시작했다가 전방으로 가속
            SetArms(
                Vector3.Lerp(leftPullbackPosition, leftPushPosition, eased),
                Vector3.Lerp(leftPullbackRotation, leftPushRotation, eased),
                Vector3.Lerp(rightPullbackPosition, rightPushPosition, eased),
                Vector3.Lerp(rightPullbackRotation, rightPushRotation, eased));
            FovOffset = Mathf.Lerp(rushFovOffset, impactFovOffset, eased);

            // 시간 비율이 아닌 실제 두 팔 자세의 전진 비율에 맞춰 타격
            if (!strikeChecked && eased >= hitTimeRatio)
            {
                strikeChecked = true;
                bool success = ApplyStrike();
                if (success && impactPoseHold > 0f)
                {
                    Vector3 contactLeftPosition = currentLeftPosition;
                    Vector3 contactLeftAngles = currentLeftAngles;
                    Vector3 contactRightPosition = currentRightPosition;
                    Vector3 contactRightAngles = currentRightAngles;
                    float contactTimer = 0f;
                    while (contactTimer < impactPoseHold)
                    {
                        SetArms(
                            contactLeftPosition + impactRecoilPosition,
                            contactLeftAngles + impactRecoilRotation,
                            contactRightPosition + impactRecoilPosition,
                            contactRightAngles + impactRecoilRotation);
                        FovOffset = impactFovOffset;
                        contactTimer += Time.deltaTime;
                        yield return null;
                    }
                }
            }

            pushElapsed += Time.deltaTime;
            yield return null;
        }

        SetArms(leftPushPosition, leftPushRotation, rightPushPosition, rightPushRotation);
        FovOffset = impactFovOffset;
        if (!strikeChecked) ApplyStrike(); // 프레임 건너뛰어도 1회 타격 체크
        yield return RecoverArms();
        FinishAttack();
    }

    private IEnumerator RecoverArms()
    {
        // 기존 Player Move() / IsRushing 연동은 유지. 양팔을 동시에 자연스럽게 복원
        BlocksMovement = false;
        Vector3 leftStartPos = currentLeftPosition;
        Vector3 leftStartAngles = currentLeftAngles;
        Vector3 rightStartPos = currentRightPosition;
        Vector3 rightStartAngles = currentRightAngles;
        float fovStart = FovOffset;
        float elapsed = 0f;
        while (elapsed < recoveryDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f,
                elapsed / Mathf.Max(0.001f, recoveryDuration));
            SetArms(
                Vector3.Lerp(leftStartPos, Vector3.zero, t),
                Vector3.Lerp(leftStartAngles, Vector3.zero, t),
                Vector3.Lerp(rightStartPos, Vector3.zero, t),
                Vector3.Lerp(rightStartAngles, Vector3.zero, t));
            FovOffset = Mathf.Lerp(fovStart, 0f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetArms(Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
    }

    private void ApplyWindupPose(float elapsed)
    {
        float t = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(elapsed / Mathf.Max(0.001f, minimumWindupTime)));
        SetArms(
            Vector3.Lerp(Vector3.zero, leftPullbackPosition, t),
            Vector3.Lerp(Vector3.zero, leftPullbackRotation, t),
            Vector3.Lerp(Vector3.zero, rightPullbackPosition, t),
            Vector3.Lerp(Vector3.zero, rightPullbackRotation, t));
        FovOffset = Mathf.Lerp(0f, rushFovOffset, t);
    }

    private void FixedUpdate()
    {
        if (!rushing || !IsActive) return;
        if (lockedEnemy == null || lockedCollider == null || !lockedCollider.enabled)
        {
            rushBlocked = true;
            rushing = false;
            StopHorizontalVelocity();
            return;
        }

        float remaining = DistanceToTarget() - stopDistance;
        if (remaining <= 0.03f)
        {
            rushing = false;
            // 근접 성공 시 속도를 0으로 만들지 않아 공격 후 이동이 뚝 끊기지 않습니다.
            FinishRushMomentum();
            return;
        }

        Vector3 gravityDirection = -transform.up;
        Vector3 planar = Vector3.ProjectOnPlane(
            lockedCollider.bounds.center - body.position, gravityDirection);
        if (planar.sqrMagnitude < 0.01f)
        {
            rushBlocked = true;
            rushing = false;
            StopHorizontalVelocity();
            return;
        }

        Vector3 direction = planar.normalized;
        float speed = Mathf.Min(rushSpeed, remaining / Time.fixedDeltaTime);
        float travelDistance = speed * Time.fixedDeltaTime + 0.12f;
        Vector3 origin = body.worldCenterOfMass;
        RaycastHit[] obstacles = Physics.SphereCastAll(origin, obstacleCheckRadius, direction,
            travelDistance, obstacleLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in obstacles)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<Enemy>() != null) continue;
            rushBlocked = true;
            rushing = false;
            StopHorizontalVelocity();
            return;
        }

        Vector3 gravityVelocity = Vector3.Project(body.linearVelocity, gravityDirection);
        body.linearVelocity = direction * speed + gravityVelocity;
    }

    private float DistanceToTarget()
    {
        return lockedCollider == null ? float.MaxValue :
            Vector3.Distance(body.position, lockedCollider.ClosestPoint(body.position));
    }

    private void StopHorizontalVelocity()
    {
        if (body == null) return;
        Vector3 gravityDirection = -transform.up;
        body.linearVelocity = Vector3.Project(body.linearVelocity, gravityDirection);
    }

    private void FinishRushMomentum()
    {
        if (body == null) return;

        Vector3 up = transform.up;

        // 중력 방향 속도 유지
        Vector3 verticalVelocity =
            Vector3.Project(body.linearVelocity, up);

        // 수평 돌진 속도
        Vector3 horizontalVelocity =
            Vector3.ProjectOnPlane(body.linearVelocity, up);

        // 돌진 후 일반 이동 속도로 부드럽게 전환
        float exitSpeed = player != null
            ? player.moveSpeed
            : 7f;

        horizontalVelocity = Vector3.ClampMagnitude(
            horizontalVelocity,
            exitSpeed
        );

        body.linearVelocity =
            horizontalVelocity + verticalVelocity;
    }

    private bool ApplyStrike()
    {
        if (lockedEnemy == null || lockedCollider == null || playerCamera == null) return false;

        Vector3 origin = playerCamera.transform.position;
        Vector3 point = lockedCollider.ClosestPoint(origin);
        Vector3 towardEnemy = point - origin;
        if (towardEnemy.sqrMagnitude < 0.0001f)
            towardEnemy = lockedCollider.bounds.center - origin;
        float distance = towardEnemy.magnitude;

        if (distance > strikeRange || distance < 0.001f) return false;
        if (Vector3.Dot(playerCamera.transform.forward, towardEnemy / distance) < 0.30f) return false;
        if (Blocked(origin, lockedCollider.bounds.center, lockedCollider)) return false;

        // 적이 놓인 중력면과 상관없이 전방으로 강하게 날리고, 약간 위쪽 성분을 섞음.
        Vector3 push = (towardEnemy.normalized + transform.up * upwardLaunchBias).normalized;
        float multiplier = player != null ? player.abilityDamageMultiplier : 1f;
        bool success = lockedEnemy.ApplyGravityPulse(push, launchSpeed,
            directDamage * multiplier, wallDamage * multiplier);
        if (!success) return false;

        // 기존 체력/명중 피드백 시스템에 연결. Enemy.ApplyGravityPulse가 true를 돌려준 경우만 출력합니다.
        if (usePlayerHitMarker && player != null)
            player.ShowHitMarker(false);
        PlaySound(hitClip);
        if (impactBassClip != null && audioSource != null)
            audioSource.PlayOneShot(impactBassClip, impactBassVolume);

        if (impactVfx != null)
        {
            ParticleSystem fx = Instantiate(impactVfx, point, Quaternion.LookRotation(-push));
            fx.Play();
            // 일회성 VFX가 씬에 누적되지 않도록 자동 제거합니다.
            ParticleSystem.MainModule main = fx.main;
            if (!main.loop)
                Destroy(fx.gameObject, Mathf.Max(0.3f, main.duration + main.startLifetime.constantMax + 0.15f));
        }

        CameraShake cameraShake = playerCamera.GetComponent<CameraShake>();
        if (cameraShake != null)
            cameraShake.Shake(impactShakeDuration, impactShakeStrength);
        return true;
    }

    private void SetArms(Vector3 leftPosition, Vector3 leftAngles,
        Vector3 rightPosition, Vector3 rightAngles)
    {
        currentLeftPosition = leftPosition;
        currentLeftAngles = leftAngles;
        currentRightPosition = rightPosition;
        currentRightAngles = rightAngles;
        ApplyArmPose();
    }

    private void ApplyArmPose()
    {
        // 기존 Pivot의 회전/위치를 기준으로 카메라 좌표계에서만 추가 이동/회전.
        // ArmRecoil은 공격 중 잠시 비활성화하므로 서로 덮어쓰지 않음.
        Quaternion frame = playerCamera != null ? playerCamera.transform.rotation : transform.rotation;
        PoseArm(leftArmPivot, leftDefaultPosition, leftDefaultRotation,
            currentLeftPosition, currentLeftAngles, frame);
        PoseArm(rightArmPivot, rightDefaultPosition, rightDefaultRotation,
            currentRightPosition, currentRightAngles, frame);
    }

    private void PoseArm(Transform pivot, Vector3 defaultPosition, Quaternion defaultRotation,
        Vector3 offset, Vector3 angles, Quaternion frame)
    {
        if (pivot == null) return;
        Transform parent = pivot.parent;
        Vector3 origin = parent != null ? parent.TransformPoint(defaultPosition) : defaultPosition;
        Quaternion rest = parent != null ? parent.rotation * defaultRotation : defaultRotation;
        pivot.position = origin + frame * LimitPosition(offset);
        pivot.rotation = frame * Quaternion.Euler(LimitAngles(angles)) * Quaternion.Inverse(frame) * rest;
    }

    private Vector3 LimitPosition(Vector3 v)
    {
        return useArmMotionLimits ? Vector3.ClampMagnitude(v, maxArmPositionOffset) : v;
    }

    private Vector3 LimitAngles(Vector3 v)
    {
        if (!useArmMotionLimits) return v;
        float a = maxArmAngleOffset;
        return new Vector3(Mathf.Clamp(v.x, -a, a),
            Mathf.Clamp(v.y, -a, a), Mathf.Clamp(v.z, -a, a));
    }

    private void ToggleWeaponScripts(bool enable)
    {
        if (!enable)
        {
            suspendedScripts.Clear();
            previousEnabledStates.Clear();
            if (disableDuringAttack != null)
                foreach (Behaviour script in disableDuringAttack) SuspendScript(script);
            if (leftArmPivot != null)
                foreach (ArmRecoil recoil in leftArmPivot.GetComponentsInChildren<ArmRecoil>(true))
                    SuspendScript(recoil);
            if (rightArmPivot != null)
                foreach (ArmRecoil recoil in rightArmPivot.GetComponentsInChildren<ArmRecoil>(true))
                    SuspendScript(recoil);
        }
        else
        {
            for (int i = 0; i < suspendedScripts.Count; i++)
                if (suspendedScripts[i] != null)
                    suspendedScripts[i].enabled = previousEnabledStates[i];
            suspendedScripts.Clear();
            previousEnabledStates.Clear();
        }
    }

    private void SuspendScript(Behaviour script)
    {
        if (script == null || script == this || script == player || suspendedScripts.Contains(script)) return;
        suspendedScripts.Add(script);
        previousEnabledStates.Add(script.enabled);
        script.enabled = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    private void FinishAttack()
    {
        rushing = false;
        IsActive = false;
        BlocksMovement = false;
        FovOffset = 0f;
        SetArms(Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
        ToggleWeaponScripts(true);
        lockedEnemy = null;
        lockedCollider = null;
        attackRoutine = null;
    }

    public void CancelAttack()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        if (rushing) StopHorizontalVelocity();
        FinishAttack();
    }

    private void OnDisable()
    {
        if (IsActive) CancelAttack();
    }
}
