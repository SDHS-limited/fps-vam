
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// REVFIELD - Gravity Grab
///
/// G : 오브젝트 잡기 / 취소
/// 좌클릭 : 오브젝트 던지기
///
/// isClick  : 잡기 애니메이션
/// isGrab   : 던지기 애니메이션
/// isReturn : 복귀 애니메이션
/// </summary>
[DefaultExecutionOrder(-100)]
public class GravityGrabController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Material blueOutlineMaterial;

    [Header("Arm Animation")]
    [SerializeField] private Animator armAnimator;

    [Header("Animation Timing")]
    [SerializeField, Min(0f)]
    private float throwReleaseDelay = 0.12f;

    [SerializeField, Min(0f)]
    private float returnStartDelay = 0.20f;

    [Header("Input")]
    [SerializeField] private KeyCode grabKey = KeyCode.G;
    [SerializeField] private LayerMask aimMask = ~0;
    [SerializeField] private float grabRange = 8f;
    [SerializeField] private float maximumGrabMass = 100f;

    [Header("Holding")]
    [SerializeField] private float holdDistance = 2.8f;
    [SerializeField] private float followStrength = 12f;
    [SerializeField] private float followResponse = 20f;
    [SerializeField] private float maxFollowSpeed = 24f;
    [SerializeField] private float maxFollowAcceleration = 150f;
    [SerializeField] private float angularDamping = 12f;
    [SerializeField] private float breakDistance = 10f;
    [SerializeField] private LayerMask obstructionMask = ~0;

    [Header("Throw")]
    [SerializeField] private float throwSpeed = 32f;

    private static readonly int GrabTrigger =
        Animator.StringToHash("isClick");

    private static readonly int ThrowTrigger =
        Animator.StringToHash("isGrab");

    private static readonly int ReturnTrigger =
        Animator.StringToHash("isReturn");

    private GravityGrabbable hovered;
    private GravityGrabbable held;

    private Collider[] playerColliders;

    private Coroutine throwCoroutine;

    private bool isThrowing;
    private bool suppressFireUntilRelease;

    private int lastThrowFrame = -1;

    public bool IsHolding => held != null;
    public bool IsThrowing => isThrowing;

    // 총 발사 차단용
    public bool ConsumesFireInput =>
        IsHolding ||
        IsThrowing ||
        suppressFireUntilRelease ||
        lastThrowFrame == Time.frameCount;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerRoot == null)
            playerRoot = transform;

        playerColliders =
            playerRoot.GetComponentsInChildren<Collider>();

        if (armAnimator == null)
        {
            Debug.LogWarning(
                "[GravityGrab] Arm Animator가 비어 있습니다.",
                this);
        }
    }

    private void Update()
    {
        if (!Input.GetMouseButton(0))
            suppressFireUntilRelease = false;

        if (playerCamera == null)
            return;

        if (isThrowing)
            return;

        // 오브젝트를 잡고 있는 상태
        if (held != null)
        {
            // G키를 다시 누르면 잡기 취소
            if (Input.GetKeyDown(grabKey))
            {
                CancelGrab();
                return;
            }

            // 좌클릭하면 던지기
            if (Input.GetMouseButtonDown(0))
            {
                StartThrow();
                return;
            }

            return;
        }

        UpdateTarget();

        // G키로 잡기
        if (Input.GetKeyDown(grabKey))
        {
            if (hovered != null)
                GrabObject();
        }
    }

    private void FixedUpdate()
    {
        if (held == null || playerCamera == null)
            return;

        Rigidbody body = held.Body;

        if (body == null)
        {
            CancelGrab();
            return;
        }

        float distance = Vector3.Distance(
            body.worldCenterOfMass,
            playerCamera.transform.position);

        // 너무 멀어지면 자동 취소 + 복귀
        if (distance > breakDistance)
        {
            CancelGrab();
            return;
        }

        Vector3 target = FindSafeHoldPosition();

        Vector3 desiredVelocity =
            Vector3.ClampMagnitude(
                (target - body.worldCenterOfMass)
                * followStrength,
                maxFollowSpeed);

        Vector3 acceleration =
            Vector3.ClampMagnitude(
                (desiredVelocity - body.linearVelocity)
                * followResponse,
                maxFollowAcceleration);

        body.AddForce(
            acceleration,
            ForceMode.Acceleration);

        body.AddTorque(
            -body.angularVelocity * angularDamping,
            ForceMode.Acceleration);
    }

    // =====================================
    // 조준
    // =====================================

    private void UpdateTarget()
    {
        GravityGrabbable next = FindAimedObject();

        if (next == hovered)
            return;

        ClearHover();

        hovered = next;

        if (hovered != null)
        {
            hovered.SetOutline(
                true,
                blueOutlineMaterial);
        }
    }

    private GravityGrabbable FindAimedObject()
    {
        RaycastHit[] hits = Physics.RaycastAll(
            playerCamera.transform.position,
            playerCamera.transform.forward,
            grabRange,
            aimMask,
            QueryTriggerInteraction.Ignore);

        Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (IsPlayerCollider(hit.collider))
                continue;

            GravityGrabbable candidate =
                hit.collider.GetComponentInParent
                <GravityGrabbable>();

            if (candidate != null &&
                candidate.CanGrab(maximumGrabMass))
            {
                return candidate;
            }

            return null;
        }

        return null;
    }

    // =====================================
    // 잡기
    // =====================================

    private void GrabObject()
    {
        GravityGrabbable target = hovered;

        ClearHover();

        if (target == null)
            return;

        if (!target.BeginHold(playerColliders))
            return;

        held = target;

        // 잡기 애니메이션
        PlayGrabAnimation();
    }

    // =====================================
    // 잡기 취소
    // =====================================

    private void CancelGrab()
    {
        if (throwCoroutine != null)
        {
            StopCoroutine(throwCoroutine);
            throwCoroutine = null;
        }

        isThrowing = false;

        if (held == null)
            return;

        GravityGrabbable target = held;
        held = null;

        // 물체를 자연스럽게 떨어뜨림
        target.Drop();

        // 원래 팔 자세로 복귀
        PlayReturnAnimation();
    }

    // =====================================
    // 투척
    // =====================================

    private void StartThrow()
    {
        if (held == null || isThrowing)
            return;

        isThrowing = true;

        // 기존 총기 입력 차단
        suppressFireUntilRelease = true;
        lastThrowFrame = Time.frameCount;

        throwCoroutine =
            StartCoroutine(ThrowRoutine());
    }

    private IEnumerator ThrowRoutine()
    {
        // 던지기 모션
        PlayThrowAnimation();

        // 팔이 앞으로 뻗는 순간까지 대기
        if (throwReleaseDelay > 0f)
        {
            yield return new WaitForSeconds(
                throwReleaseDelay);
        }

        // 실제 오브젝트 발사
        ReleaseObject();

        // 던진 후 자세 유지
        if (returnStartDelay > 0f)
        {
            yield return new WaitForSeconds(
                returnStartDelay);
        }

        // 복귀 애니메이션
        PlayReturnAnimation();

        isThrowing = false;
        throwCoroutine = null;
    }

    private void ReleaseObject()
    {
        if (held == null)
            return;

        GravityGrabbable target = held;
        held = null;

        suppressFireUntilRelease = true;
        lastThrowFrame = Time.frameCount;

        target.Throw(
            playerCamera.transform.forward,
            throwSpeed);
    }

    // =====================================
    // Animator
    // =====================================
    


    private void PlayGrabAnimation()
    {
    if (armAnimator == null)
    {
        Debug.LogError("Gravity Grab: Animator 연결 안 됨!");
        return;
    }

    Debug.Log("Gravity Grab: 잡기 애니메이션 실행!");

    armAnimator.ResetTrigger(ThrowTrigger);
    armAnimator.ResetTrigger(ReturnTrigger);
    armAnimator.SetTrigger(GrabTrigger);
    }

    private void PlayThrowAnimation()
    {
        if (armAnimator == null)
            return;

        armAnimator.ResetTrigger(GrabTrigger);
        armAnimator.ResetTrigger(ReturnTrigger);

        armAnimator.SetTrigger(ThrowTrigger);
    }

    private void PlayReturnAnimation()
    {
        if (armAnimator == null)
            return;

        armAnimator.ResetTrigger(GrabTrigger);
        armAnimator.ResetTrigger(ThrowTrigger);

        armAnimator.SetTrigger(ReturnTrigger);
    }

    // =====================================
    // 벽 충돌을 고려한 고정 위치
    // =====================================

    private Vector3 FindSafeHoldPosition()
    {
        Vector3 origin =
            playerCamera.transform.position;

        Vector3 forward =
            playerCamera.transform.forward;

        float radius = held.GetApproximateRadius();
        float distance = holdDistance;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            radius,
            forward,
            holdDistance,
            obstructionMask,
            QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (IsPlayerCollider(hit.collider) ||
                held.OwnsCollider(hit.collider))
            {
                continue;
            }

            distance = Mathf.Min(
                distance,
                Mathf.Max(0.15f, hit.distance - 0.08f));
        }

        return origin + forward * distance;
    }

    private bool IsPlayerCollider(Collider target)
    {
        foreach (Collider own in playerColliders)
        {
            if (own != null && own == target)
                return true;
        }

        return false;
    }

    // =====================================
    // 외곽선 제거
    // =====================================

    private void ClearHover()
    {
        if (hovered == null)
            return;

        hovered.SetOutline(
            false,
            blueOutlineMaterial);

        hovered = null;
    }

    private void OnDisable()
    {
        ClearHover();

        if (throwCoroutine != null)
        {
            StopCoroutine(throwCoroutine);
            throwCoroutine = null;
        }

        isThrowing = false;

        if (held != null)
        {
            GravityGrabbable target = held;
            held = null;
            target.Drop();
        }
    }
}
