using UnityEngine;

/// <summary>
/// Controls an object's vulnerability state, cycling between Invulnerable and Vulnerable.
/// Invulnerable: high mass & drag. Vulnerable: low mass & drag.
/// If hit during Vulnerable window, permanently stays in the vulnerable physics state.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MechanicFrozen : MonoBehaviour
{
    public enum VulnerabilityState { Invulnerable, Vulnerable }

    [Header("State Timings")]
    [Tooltip("How long (seconds) the object stays Invulnerable before switching.")]
    public float invulnerableDuration = 3f;

    [Tooltip("How long (seconds) the Vulnerable window lasts.")]
    public float vulnerableDuration = 2f;

    [Tooltip("How long (seconds) to wait after Vulnerable before looping back to Invulnerable.")]
    public float postVulnerableDelay = 2f;

    [Header("Invulnerable Physics")]
    [Tooltip("Rigidbody mass while Invulnerable.")]
    public float invulnerableMass = 99f;

    [Tooltip("Rigidbody drag while Invulnerable.")]
    public float invulnerableDrag = 99f;

    [Header("Vulnerable Physics")]
    [Tooltip("Rigidbody mass while Vulnerable (and permanently after being hit).")]
    public float vulnerableMass = 0.1f;

    [Tooltip("Rigidbody drag while Vulnerable (and permanently after being hit).")]
    public float vulnerableDrag = 0f;

    [Header("Optional: Invulnerable Indicator")]
    [Tooltip("(Optional) GameObject to enable while Invulnerable, disabled otherwise.")]
    public GameObject invulnerableIndicator;

    // ── Runtime read-only info (visible in Inspector) ──────────────────────
    [Header("Runtime Info (Read-Only)")]
    [SerializeField, ReadOnlyField] private VulnerabilityState currentState = VulnerabilityState.Invulnerable;
    [SerializeField, ReadOnlyField] private bool isPermanentlyVulnerable = false;
    [SerializeField, ReadOnlyField] private float stateTimer = 0f;

    private Rigidbody rb;

    // ───────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        SetInvulnerable();
        stateTimer = invulnerableDuration;
    }

    private void Update()
    {
        // Once permanently vulnerable, nothing left to cycle.
        if (isPermanentlyVulnerable) return;

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            AdvanceState();
        }
    }

    // ── State Machine ───────────────────────────────────────────────────────

    private void AdvanceState()
    {
        switch (currentState)
        {
            case VulnerabilityState.Invulnerable:
                SetVulnerable();
                stateTimer = vulnerableDuration;
                break;

            case VulnerabilityState.Vulnerable:
                // After the vulnerable window closes without a hit, wait then go invulnerable.
                SetInvulnerable();
                stateTimer = postVulnerableDelay + invulnerableDuration;
                break;
        }
    }

    private void SetInvulnerable()
    {
        currentState = VulnerabilityState.Invulnerable;
        rb.mass = invulnerableMass;
        rb.drag = invulnerableDrag;

        if (invulnerableIndicator != null)
            invulnerableIndicator.SetActive(true);
    }

    private void SetVulnerable()
    {
        currentState = VulnerabilityState.Vulnerable;
        rb.mass = vulnerableMass;
        rb.drag = vulnerableDrag;

        if (invulnerableIndicator != null)
            invulnerableIndicator.SetActive(false);
    }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Call this when the object is hit. Only has an effect during the Vulnerable window.
    /// </summary>
    public void OnHit()
    {
        if (isPermanentlyVulnerable) return;
        if (currentState != VulnerabilityState.Vulnerable) return;

        isPermanentlyVulnerable = true;

        // Lock physics to vulnerable values forever.
        rb.mass = vulnerableMass;
        rb.drag = vulnerableDrag;

        if (invulnerableIndicator != null)
            invulnerableIndicator.SetActive(false);

        Debug.Log($"[VulnerabilityController] {gameObject.name} permanently set to Vulnerable state after being hit!");
    }

    /// <summary>
    /// Returns true only if the object can currently receive a permanent-vulnerability hit.
    /// </summary>
    public bool IsVulnerableNow => currentState == VulnerabilityState.Vulnerable && !isPermanentlyVulnerable;
}

// ── ReadOnlyField attribute (runtime side — always compiled) ───────────────
[System.AttributeUsage(System.AttributeTargets.Field)]
public class ReadOnlyFieldAttribute : PropertyAttribute { }

// ── ReadOnlyField drawer (editor side only) ────────────────────────────────
#if UNITY_EDITOR
namespace UnityEditor
{
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyFieldAttribute))]
    public class ReadOnlyFieldDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
#endif