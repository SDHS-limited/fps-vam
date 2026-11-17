using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Put on the root of each grabbable prop, alongside a dynamic Rigidbody.</summary>
[RequireComponent(typeof(Rigidbody))]
public class GravityGrabbable : MonoBehaviour
{
    [Header("Impact")]
    [Min(0f)] public float hitDamage = 35f;
    [Min(0f)] public float knockbackImpulse = 22f;
    [Min(0f)] public float minimumHitSpeed = 6f;
    [Min(0.1f)] public float activeThrowSeconds = 4f;
    [Tooltip("Allow the prop to hit multiple distinct enemies during the same throw.")]
    public bool penetrateEnemies = true;
    [Range(0.1f, 1f)] public float retainedSpeedAfterEnemy = 0.9f;
    [Min(0)] public int maximumEnemyHits = 0; // 0 = unlimited

    private Rigidbody body;
    private Collider[] ownColliders;
    private Collider[] ownerColliders;
    private bool previousGravity;
    private CollisionDetectionMode previousCollisionMode;
    private RigidbodyInterpolation previousInterpolation;
    private bool hasTemporaryThrowCollisionMode;
    private readonly List<MeshRenderer> outlineRenderers = new List<MeshRenderer>();
    private readonly HashSet<GravityGrabEnemyReceiver> enemiesHit = new HashSet<GravityGrabEnemyReceiver>();
    private readonly List<(Collider, Collider)> ignoredEnemyPairs = new List<(Collider, Collider)>();
    private Coroutine throwTimer;
    private Coroutine restoreOwnerTimer;
    private Vector3 lastFlightVelocity;

    public Rigidbody Body => body;
    public bool IsHeld { get; private set; }
    public bool IsThrown { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        ownColliders = GetComponentsInChildren<Collider>();
    }

    public bool CanGrab(float maxMass)
    {
        return isActiveAndEnabled && body != null && !body.isKinematic &&
               !IsHeld && body.mass <= maxMass && ownColliders.Length > 0;
    }

    public bool OwnsCollider(Collider c)
    {
        foreach (Collider own in ownColliders)
            if (own != null && own == c) return true;
        return false;
    }

    public float GetApproximateRadius()
    {
        bool found = false;
        Bounds b = default;
        foreach (Collider c in ownColliders)
        {
            if (c == null || !c.enabled || c.isTrigger) continue;
            if (!found) { b = c.bounds; found = true; }
            else b.Encapsulate(c.bounds);
        }
        return found ? Mathf.Clamp(b.extents.magnitude, 0.12f, 1.5f) : 0.5f;
    }

    public bool BeginHold(Collider[] playerColliders)
    {
        if (!CanGrab(float.MaxValue)) return false;
        if (throwTimer != null) { StopCoroutine(throwTimer); throwTimer = null; }
        if (restoreOwnerTimer != null) { StopCoroutine(restoreOwnerTimer); restoreOwnerTimer = null; }
        RestoreIgnoredEnemyCollisions();
        enemiesHit.Clear();
        if (hasTemporaryThrowCollisionMode)
        {
            body.collisionDetectionMode = previousCollisionMode;
            hasTemporaryThrowCollisionMode = false;
        }
        IsThrown = false;
        IsHeld = true;
        ownerColliders = playerColliders;

        previousGravity = body.useGravity;
        previousCollisionMode = body.collisionDetectionMode;
        previousInterpolation = body.interpolation;
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        IgnoreOwnerCollisions(true);
        return true;
    }

    public void Throw(Vector3 forward, float speed)
    {
        if (!IsHeld) return;
        EndHold(true);
        IsThrown = true;
        enemiesHit.Clear();
        body.linearVelocity = forward.normalized * speed;
        lastFlightVelocity = body.linearVelocity;
        StartRestoreOwnerTimer();
        throwTimer = StartCoroutine(EndThrowTimer());
    }

    public void Drop()
    {
        if (!IsHeld) return;
        EndHold(false);
        IsThrown = false;
        StartRestoreOwnerTimer();
    }

    private void EndHold(bool keepContinuousCollision)
    {
        IsHeld = false;
        body.useGravity = previousGravity;
        body.interpolation = previousInterpolation;
        body.collisionDetectionMode = keepContinuousCollision
            ? CollisionDetectionMode.ContinuousDynamic : previousCollisionMode;
        hasTemporaryThrowCollisionMode = keepContinuousCollision;
    }

    private void FixedUpdate()
    {
        if (IsThrown)
            lastFlightVelocity = body.linearVelocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsThrown || collision.contactCount == 0) return;

        GravityGrabEnemyReceiver enemy =
            collision.collider.GetComponentInParent<GravityGrabEnemyReceiver>();
        if (enemy == null)
        {
            // A wall or other prop ends the damaging flight.
            IsThrown = false;
            return;
        }

        if (enemiesHit.Contains(enemy) || collision.relativeVelocity.magnitude < minimumHitSpeed)
            return;

        enemiesHit.Add(enemy);
        Vector3 direction = lastFlightVelocity.sqrMagnitude > 0.01f
            ? lastFlightVelocity.normalized : transform.forward;
        Vector3 point = collision.GetContact(0).point;
        enemy.ReceiveGravityImpact(hitDamage, knockbackImpulse, direction, point);

        if (penetrateEnemies)
        {
            IgnoreEnemyCollisions(enemy);
            float retained = Mathf.Max(body.linearVelocity.magnitude,
                lastFlightVelocity.magnitude * retainedSpeedAfterEnemy);
            body.linearVelocity = direction * retained;
        }

        if (maximumEnemyHits > 0 && enemiesHit.Count >= maximumEnemyHits)
            IsThrown = false;
    }

    private void IgnoreEnemyCollisions(GravityGrabEnemyReceiver enemy)
    {
        foreach (Collider ours in ownColliders)
        {
            if (ours == null) continue;
            foreach (Collider theirs in enemy.GetComponentsInChildren<Collider>())
            {
                if (theirs == null || !theirs.enabled) continue;
                Physics.IgnoreCollision(ours, theirs, true);
                ignoredEnemyPairs.Add((ours, theirs));
            }
        }
    }

    private void RestoreIgnoredEnemyCollisions()
    {
        foreach (var pair in ignoredEnemyPairs)
            if (pair.Item1 != null && pair.Item2 != null)
                Physics.IgnoreCollision(pair.Item1, pair.Item2, false);
        ignoredEnemyPairs.Clear();
    }

    private void IgnoreOwnerCollisions(bool ignore)
    {
        if (ownerColliders == null) return;
        foreach (Collider ours in ownColliders)
        {
            if (ours == null) continue;
            foreach (Collider player in ownerColliders)
            {
                if (player != null && player != ours)
                    Physics.IgnoreCollision(ours, player, ignore);
            }
        }
    }

    private void StartRestoreOwnerTimer()
    {
        if (restoreOwnerTimer != null) StopCoroutine(restoreOwnerTimer);
        restoreOwnerTimer = StartCoroutine(RestoreOwnerAfterDelay());
    }

    private IEnumerator RestoreOwnerAfterDelay()
    {
        yield return new WaitForSeconds(0.35f);
        if (!IsHeld) IgnoreOwnerCollisions(false);
        restoreOwnerTimer = null;
    }

    private IEnumerator EndThrowTimer()
    {
        yield return new WaitForSeconds(activeThrowSeconds);
        IsThrown = false;
        if (!IsHeld && hasTemporaryThrowCollisionMode)
        {
            body.collisionDetectionMode = previousCollisionMode;
            hasTemporaryThrowCollisionMode = false;
        }
        RestoreIgnoredEnemyCollisions();
        throwTimer = null;
    }

    public void SetOutline(bool visible, Material outlineMaterial)
    {
        if (visible && outlineRenderers.Count == 0 && outlineMaterial != null)
            BuildOutline(outlineMaterial);
        foreach (MeshRenderer r in outlineRenderers)
            if (r != null) r.enabled = visible;
    }

    private void BuildOutline(Material material)
    {
        MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mesh in meshes)
        {
            MeshRenderer original = mesh.GetComponent<MeshRenderer>();
            if (original == null || mesh.sharedMesh == null) continue;

            GameObject shell = new GameObject("GravityGrab_BlueOutline");
            shell.transform.SetParent(mesh.transform, false);
            shell.layer = mesh.gameObject.layer;
            MeshFilter shellFilter = shell.AddComponent<MeshFilter>();
            shellFilter.sharedMesh = mesh.sharedMesh;
            MeshRenderer shellRenderer = shell.AddComponent<MeshRenderer>();
            int subMeshCount = Mathf.Max(1, mesh.sharedMesh.subMeshCount);
            Material[] materials = new Material[subMeshCount];
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            shellRenderer.sharedMaterials = materials;
            shellRenderer.shadowCastingMode = ShadowCastingMode.Off;
            shellRenderer.receiveShadows = false;
            shellRenderer.enabled = false;
            outlineRenderers.Add(shellRenderer);
        }
    }

    private void OnDisable()
    {
        if (hasTemporaryThrowCollisionMode && body != null)
        {
            body.collisionDetectionMode = previousCollisionMode;
            hasTemporaryThrowCollisionMode = false;
        }
        IsThrown = false;
        if (IsHeld)
        {
            EndHold(false);
            IgnoreOwnerCollisions(false);
        }
        RestoreIgnoredEnemyCollisions();
        IgnoreOwnerCollisions(false);
    }
}
