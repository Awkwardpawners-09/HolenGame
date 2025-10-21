using UnityEngine;

public class HolenHit : MonoBehaviour
{
    // Drag your particle prefab here in the inspector
    public GameObject particleEffectPrefab;

    // Drag your sound effect here in the inspector
    public AudioClip soundEffect;

    // The audio source to play the sound (it will be created automatically if you don't assign one)
    private AudioSource audioSource;

    // Reference to the background music AudioSource (you can drag this in the inspector)
    public AudioSource backgroundMusic;

    // Set the volume of the hit sound (make it louder than the background music)
    public float hitSoundVolume = 1.0f;

    private void Start()
    {
        // Get or create an audio source if it doesn't exist
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collided object has the "objective" tag
        if (collision.gameObject.CompareTag("Objective"))
        {
            // Spawn the particle effect at the collision point
            if (particleEffectPrefab != null)
            {
                Instantiate(particleEffectPrefab, collision.contacts[0].point, Quaternion.identity);
            }

            // Play the hit sound effect if it's assigned
            if (soundEffect != null && audioSource != null)
            {
                // Temporarily increase the volume of the hit sound effect
                audioSource.volume = hitSoundVolume;

                // Play the hit sound
                audioSource.PlayOneShot(soundEffect);
            }
        }
    }
}
