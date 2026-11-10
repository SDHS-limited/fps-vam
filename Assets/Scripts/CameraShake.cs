using UnityEngine;

// A short, repeatable camera punch. Shots can overlap without competing coroutines.
public class CameraShake : MonoBehaviour
{
    [Min(0f)] public float maximumOffset = 0.065f;
    [Range(0f, 1f)] public float verticalRatio = 0.2f;

    private Vector3 restingPosition;
    private float offset;
    private float recoveryRate;

    void OnEnable()
    {
        restingPosition = transform.localPosition;
        offset = 0f;
    }

    public void Shake(float duration, float magnitude)
    {
        if (Time.timeScale == 0f) return;
        offset = Mathf.Min(maximumOffset, offset + Mathf.Max(0f, magnitude) * 0.28f);
        recoveryRate = maximumOffset / Mathf.Max(0.06f, duration);
    }

    void LateUpdate()
    {
        if (Time.timeScale == 0f) return;
        transform.localPosition = restingPosition + new Vector3(0f, offset * verticalRatio, -offset);
        offset = Mathf.MoveTowards(offset, 0f, recoveryRate * Time.deltaTime);
    }

    void OnDisable()
    {
        transform.localPosition = restingPosition;
    }
}
