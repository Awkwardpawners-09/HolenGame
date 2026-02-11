using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
public class DraggableCircle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    [Tooltip("How smoothly the object follows the cursor/finger")]
    public float dragSmoothness = 15f;

    [Tooltip("Maximum velocity when released")]
    public float maxReleaseVelocity = 20f;

    [Header("Collision Sound Settings")]
    [Tooltip("Audio clips to play on collision (can add multiple for variety)")]
    public AudioClip[] collisionSounds;

    [Tooltip("Minimum impact velocity to trigger sound")]
    public float minimumVelocity = 0.5f;

    [Tooltip("Maximum volume (0-1)")]
    [Range(0f, 1f)]
    public float maxVolume = 1f;

    [Tooltip("Minimum volume (0-1)")]
    [Range(0f, 1f)]
    public float minVolume = 0.3f;

    [Header("Advanced Sound Settings")]
    [Tooltip("Scale volume based on collision force")]
    public bool velocityBasedVolume = true;

    [Tooltip("Velocity that produces maximum volume")]
    public float maxVelocityForVolume = 10f;

    [Tooltip("Minimum time between sounds (prevents spam)")]
    public float soundCooldown = 0.1f;

    [Tooltip("Random pitch variation for more natural sound")]
    [Range(0f, 0.5f)]
    public float pitchVariation = 0.1f;

    // Private variables
    private RectTransform rectTransform;
    private Rigidbody2D rb;
    private Canvas canvas;
    private Camera uiCamera;
    private AudioSource audioSource;
    private bool isDragging = false;
    private Vector2 lastPosition;
    private Vector2 dragVelocity;
    private float lastSoundTime = 0f;

    void Start()
    {
        // Get components
        rectTransform = GetComponent<RectTransform>();
        rb = GetComponent<Rigidbody2D>();
        canvas = GetComponentInParent<Canvas>();

        // Get the camera that renders this canvas
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = null;
        }
        else
        {
            uiCamera = canvas.worldCamera;
            if (uiCamera == null)
            {
                uiCamera = Camera.main;
            }
        }

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Verify rigidbody settings
        if (rb != null)
        {
            rb.gravityScale = 1f; // Will be controlled by GyroGravityManager
            rb.isKinematic = false;
        }

        // Validate sounds
        if (collisionSounds == null || collisionSounds.Length == 0)
        {
            Debug.LogWarning("No collision sounds assigned to " + gameObject.name);
        }

        lastPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // Calculate drag velocity
        if (isDragging)
        {
            Vector2 currentPosition = rectTransform.anchoredPosition;
            dragVelocity = (currentPosition - lastPosition) / Time.deltaTime;
            lastPosition = currentPosition;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;

        // Make kinematic while dragging
        rb.isKinematic = true;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        lastPosition = rectTransform.anchoredPosition;
        dragVelocity = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Convert screen position to canvas local position
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            uiCamera,
            out localPoint
        );

        // Smoothly move to target position
        Vector2 targetPosition = localPoint;
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            dragSmoothness * Time.deltaTime
        );
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        // Re-enable physics
        rb.isKinematic = false;

        // Apply the drag velocity as momentum
        Vector2 releaseVelocity = dragVelocity;

        // Clamp velocity
        if (releaseVelocity.magnitude > maxReleaseVelocity)
        {
            releaseVelocity = releaseVelocity.normalized * maxReleaseVelocity;
        }

        // Apply velocity to rigidbody
        rb.velocity = releaseVelocity;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check cooldown
        if (Time.time - lastSoundTime < soundCooldown) return;

        // Check if we have sounds
        if (collisionSounds == null || collisionSounds.Length == 0) return;

        // Calculate impact velocity
        float impactVelocity = collision.relativeVelocity.magnitude;

        // Play sound if impact is strong enough
        if (impactVelocity >= minimumVelocity)
        {
            PlayCollisionSound(impactVelocity);
            lastSoundTime = Time.time;
        }
    }

    void PlayCollisionSound(float impactVelocity)
    {
        // Select random sound
        AudioClip soundToPlay = collisionSounds[Random.Range(0, collisionSounds.Length)];

        // Calculate volume
        float volume = maxVolume;
        if (velocityBasedVolume)
        {
            float normalizedVelocity = Mathf.Clamp01(impactVelocity / maxVelocityForVolume);
            volume = Mathf.Lerp(minVolume, maxVolume, normalizedVelocity);
        }

        // Add pitch variation
        float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.pitch = pitch;

        // Play sound
        audioSource.PlayOneShot(soundToPlay, volume);
    }

    // Public method to manually trigger sound
    public void PlaySound()
    {
        if (collisionSounds != null && collisionSounds.Length > 0)
        {
            PlayCollisionSound(5f);
        }
    }
}