using UnityEngine;
using UnityEngine.UI;

public class BreathingUI : MonoBehaviour
{
    [Tooltip("Base (resting) scale. Leave at (1,1,1) to use current local scale.")]
    public Vector3 baseScale = Vector3.one;

    [Range(0f, 0.5f), Tooltip("How much the scale changes (0.05 = ±5%).")]
    public float scaleAmplitude = 0.05f;

    [Tooltip("Breaths per minute (e.g. 6–12 feels natural).")]
    public float breathsPerMinute = 8f;

    [Tooltip("Randomize start so multiple objects don’t pulse in sync.")]
    public bool randomizeStartPhase = true;

    private Vector3 _initialScale;
    private float _phaseOffset;

    void Awake()
    {
        // If baseScale is default (1,1,1), take current local scale
        _initialScale = (baseScale == Vector3.one) ? transform.localScale : baseScale;

        _phaseOffset = randomizeStartPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    void Update()
    {
        // Breathing cycle in radians per second
        float omega = (breathsPerMinute / 60f) * Mathf.PI * 2f;
        float rad = omega * Time.time + _phaseOffset;

        // -1..+1 oscillation
        float signed = Mathf.Sin(rad);

        // Scale factor
        float scaleFactor = 1f + scaleAmplitude * signed;

        // Apply
        transform.localScale = _initialScale * scaleFactor;
    }

    void OnValidate()
    {
        breathsPerMinute = Mathf.Max(0.1f, breathsPerMinute);
        scaleAmplitude = Mathf.Clamp(scaleAmplitude, 0f, 0.5f);
    }
}
