using System.Collections;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;

    [Header("Damage Settings")]
    [SerializeField] private string damageTag = "Enemy";
    [SerializeField] private bool destroyWeaponOnHit = false;

    [Header("On Death")]
    [SerializeField] private GameObject deathPrefab;
    [SerializeField] private Rigidbody rigidbodyToEnableOnDeath;
    [SerializeField] private Rigidbody2D rigidbody2DToEnableOnDeath;

    [Header("Hit Feedback")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private float vibrationDuration = 1f;
    [SerializeField] private float vibrationStrength = 0.1f;
    [SerializeField] private float vibrationSpeed = 40f;

    private int currentHealth;
    private AudioSource audioSource;
    private Vector3 originalPosition;
    private bool isVibrating = false;

    private void Start()
    {
        currentHealth = maxHealth;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(damageTag))
        {
            if (destroyWeaponOnHit)
                Destroy(collision.gameObject);

            TakeDamage(1);
        }
    }

    // 2D variant � delete whichever you don't need
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(damageTag))
        {
            if (destroyWeaponOnHit)
                Destroy(collision.gameObject);

            TakeDamage(1);
        }
    }

    private void TakeDamage(int amount)
    {
        currentHealth -= amount;
        PlayHitFeedback();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void PlayHitFeedback()
    {
        // Play sound
        if (hitSound != null)
            audioSource.PlayOneShot(hitSound);

        // Start vibration only if not already vibrating
        if (!isVibrating)
            StartCoroutine(Vibrate());
    }

    private IEnumerator Vibrate()
    {
        isVibrating = true;
        originalPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < vibrationDuration)
        {
            float offsetX = Mathf.Sin(elapsed * vibrationSpeed) * vibrationStrength;
            float offsetY = Mathf.Sin(elapsed * vibrationSpeed * 1.3f) * vibrationStrength;
            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        isVibrating = false;
    }

    private void Die()
    {
        StopAllCoroutines();
        transform.localPosition = originalPosition;

        // Toggle isKinematic on the assigned Rigidbody
        if (rigidbodyToEnableOnDeath != null)
            rigidbodyToEnableOnDeath.isKinematic = !rigidbodyToEnableOnDeath.isKinematic;

        if (rigidbody2DToEnableOnDeath != null)
            rigidbody2DToEnableOnDeath.isKinematic = !rigidbody2DToEnableOnDeath.isKinematic;

        if (deathPrefab != null)
            Instantiate(deathPrefab, transform.position, transform.rotation);

        gameObject.SetActive(false);
    }
}