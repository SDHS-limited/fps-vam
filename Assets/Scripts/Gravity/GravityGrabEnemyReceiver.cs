using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Add to each enemy root. Bind On Damage (float) to your existing HP script's
/// TakeDamage(float) using the Inspector's dynamic float UnityEvent.
/// For physical knockback and wall-crush, the enemy needs a non-kinematic Rigidbody.
/// </summary>
[System.Serializable]
public class GravityGrabFloatEvent : UnityEvent<float> { }

public class GravityGrabEnemyReceiver : MonoBehaviour
{
    [Header("Existing HP system bridge")]
    public GravityGrabFloatEvent onDamage = new GravityGrabFloatEvent();
    public UnityEvent onImpact = new UnityEvent();

    [Header("Wall crush")]
    [SerializeField] private bool enableWallCrush = true;
    [SerializeField] private LayerMask wallLayers = ~0;
    [SerializeField, Min(0f)] private float wallCrushDamage = 20f;
    [SerializeField, Min(0f)] private float minimumWallSpeed = 4f;
    [SerializeField, Min(0f)] private float crushWindow = 0.8f;

    private Rigidbody body;
    private bool awaitingWallCollision;
    private float crushExpiresAt;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    public void ReceiveGravityImpact(float damage, float impulse, Vector3 direction, Vector3 point)
    {
        onDamage.Invoke(damage);
        onImpact.Invoke();

        if (body != null && !body.isKinematic)
            body.AddForceAtPosition(direction.normalized * impulse, point, ForceMode.Impulse);

        awaitingWallCollision = enableWallCrush;
        crushExpiresAt = Time.time + crushWindow;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!awaitingWallCollision || Time.time > crushExpiresAt)
            return;
        if (((1 << collision.gameObject.layer) & wallLayers.value) == 0)
            return;
        if (collision.collider.attachedRigidbody != null)
            return; // Only static level scenery counts as a wall.
        if (collision.relativeVelocity.magnitude < minimumWallSpeed)
            return;

        awaitingWallCollision = false;
        onDamage.Invoke(wallCrushDamage);
    }
}
