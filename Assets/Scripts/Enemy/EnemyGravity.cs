using UnityEngine;

public class EnemyGravity : MonoBehaviour
{
public float gravityStrength = 25f;

    public string groundTag = "Ground";

    public float detectRange = 1.5f;

    Rigidbody rb;

    Vector3 gravityDir = Vector3.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        DetectSurface();

        rb.AddForce(
            gravityDir * gravityStrength,
            ForceMode.Acceleration);

        AlignToSurface();
    }

    void DetectSurface()
    {
        Vector3[] dirs =
        {
            Vector3.down,
            Vector3.up,
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };

        foreach(var dir in dirs)
        {
            if (Physics.Raycast(
                transform.position,
                dir,
                out RaycastHit hit,
                detectRange))
            {
                if(hit.collider.CompareTag(groundTag))
                {
                    gravityDir = -hit.normal;
                    return;
                }
            }
        }
    }

    void AlignToSurface()
    {
        Quaternion target =
            Quaternion.FromToRotation(
                transform.up,
                -gravityDir)
            * transform.rotation;

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                target,
                Time.fixedDeltaTime * 10f);
    }

    public Vector3 GravityDir
    {
        get { return gravityDir; }
    }
}
