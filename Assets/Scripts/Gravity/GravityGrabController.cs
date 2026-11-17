using UnityEngine;

/// <summary>
/// REVFIELD / Gravity Grab - attach to the player or a player-owned component.
/// Unity 6, physics-based hold. G = grab/drop, left click = throw.
/// </summary>
public class GravityGrabController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Material blueOutlineMaterial;

    [Header("Input / Targeting")]
    [SerializeField] private KeyCode grabKey = KeyCode.G;
    [SerializeField] private LayerMask aimMask = ~0;
    [SerializeField, Min(0.5f)] private float grabRange = 8f;
    [SerializeField, Min(0.1f)] private float maximumGrabMass = 100f;

    [Header("Holding")]
    [SerializeField, Min(0.5f)] private float holdDistance = 2.8f;
    [SerializeField, Min(1f)] private float followStrength = 12f;
    [SerializeField, Min(1f)] private float followResponse = 20f;
    [SerializeField, Min(1f)] private float maxFollowSpeed = 24f;
    [SerializeField, Min(1f)] private float maxFollowAcceleration = 150f;
    [SerializeField, Min(0f)] private float angularDamping = 12f;
    [SerializeField, Min(1f)] private float breakDistance = 10f;
    [SerializeField] private LayerMask obstructionMask = ~0;

    [Header("Throwing")]
    [SerializeField, Min(1f)] private float throwSpeed = 32f;

    private GravityGrabbable hovered;
    private GravityGrabbable held;
    private Collider[] playerColliders;
    private int lastThrowFrame = -1;
    private bool suppressFireUntilRelease;

    public bool IsHolding => held != null;
    // Check this from existing gun scripts to prevent firing on the throw frame.
    public bool ConsumesFireInput => IsHolding || suppressFireUntilRelease || lastThrowFrame == Time.frameCount;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        if (playerRoot == null)
            playerRoot = transform;
        playerColliders = playerRoot.GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        if (!Input.GetMouseButton(0)) suppressFireUntilRelease = false;
        if (playerCamera == null) return;

        if (held != null)
        {
            if (Input.GetMouseButtonDown(0))
                ThrowHeld();
            else if (Input.GetKeyDown(grabKey))
                DropHeld();
            return;
        }

        GravityGrabbable next = FindAimedObject();
        if (next != hovered)
        {
            ClearHover();
            hovered = next;
            if (hovered != null)
                hovered.SetOutline(true, blueOutlineMaterial);
        }

        if (Input.GetKeyDown(grabKey) && hovered != null)
        {
            GravityGrabbable target = hovered;
            ClearHover();
            if (target.BeginHold(playerColliders))
                held = target;
        }
    }

    private void FixedUpdate()
    {
        if (held == null || playerCamera == null) return;
        Rigidbody body = held.Body;

        if (Vector3.Distance(body.worldCenterOfMass, playerCamera.transform.position) > breakDistance)
        {
            DropHeld();
            return;
        }

        Vector3 target = FindSafeHoldPosition();
        Vector3 desiredVelocity = Vector3.ClampMagnitude(
            (target - body.worldCenterOfMass) * followStrength, maxFollowSpeed);
        Vector3 acceleration = Vector3.ClampMagnitude(
            (desiredVelocity - body.linearVelocity) * followResponse, maxFollowAcceleration);

        body.AddForce(acceleration, ForceMode.Acceleration);
        body.AddTorque(-body.angularVelocity * angularDamping, ForceMode.Acceleration);
    }

    private GravityGrabbable FindAimedObject()
    {
        // RaycastAll lets us ignore the player's own colliders without shooting through walls.
        RaycastHit[] hits = Physics.RaycastAll(
            playerCamera.transform.position,
            playerCamera.transform.forward,
            grabRange, aimMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (IsPlayerCollider(hit.collider)) continue;

            GravityGrabbable candidate = hit.collider.GetComponentInParent<GravityGrabbable>();
            if (candidate != null && candidate.CanGrab(maximumGrabMass))
                return candidate;
            return null; // Ordinary scenery blocks selection.
        }
        return null;
    }

    private Vector3 FindSafeHoldPosition()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 forward = playerCamera.transform.forward;
        float radius = held.GetApproximateRadius();
        float distance = holdDistance;

        // Avoid selecting a hold point on the far side of a wall.
        RaycastHit[] hits = Physics.SphereCastAll(
            origin, radius, forward, holdDistance,
            obstructionMask, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (IsPlayerCollider(hit.collider) || held.OwnsCollider(hit.collider))
                continue;
            distance = Mathf.Min(distance, Mathf.Max(0.15f, hit.distance - 0.08f));
        }
        return origin + forward * distance;
    }

    private bool IsPlayerCollider(Collider c)
    {
        foreach (Collider own in playerColliders)
            if (own != null && own == c) return true;
        return false;
    }

    private void ThrowHeld()
    {
        GravityGrabbable target = held;
        held = null;
        lastThrowFrame = Time.frameCount;
        suppressFireUntilRelease = true;
        target.Throw(playerCamera.transform.forward, throwSpeed);
    }

    private void DropHeld()
    {
        if (held == null) return;
        GravityGrabbable target = held;
        held = null;
        target.Drop();
    }

    private void ClearHover()
    {
        if (hovered != null)
            hovered.SetOutline(false, blueOutlineMaterial);
        hovered = null;
    }

    private void OnDisable()
    {
        ClearHover();
        DropHeld();
    }
}
