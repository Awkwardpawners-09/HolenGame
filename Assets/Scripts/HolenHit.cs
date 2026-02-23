using UnityEngine;

public class HolenHit : MonoBehaviour
{
    // Drag your particle prefab here in the inspector
    public GameObject particleEffectPrefab;

    // Drag multiple sound effects here in the inspector - one will be chosen randomly
    public AudioClip[] soundEffects;

    // The audio source to play the sound (it will be created automatically if you don't assign one)
    private AudioSource audioSource;

    // Reference to the background music AudioSource (you can drag this in the inspector)
    public AudioSource backgroundMusic;

    // Base volume of the hit sound - actual volume will be randomized between 80-120% of this value
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

            // Play a randomly selected hit sound effect if any are assigned
            if (soundEffects != null && soundEffects.Length > 0 && audioSource != null)
            {
                // Pick a random clip from the array
                AudioClip randomClip = soundEffects[Random.Range(0, soundEffects.Length)];

                if (randomClip != null)
                {
                    // Randomize volume between 80% and 120% of the base hit sound volume
                    float randomVolume = hitSoundVolume * Random.Range(0.8f, 1.2f);
                    audioSource.volume = randomVolume;

                    // Play the selected sound
                    audioSource.PlayOneShot(randomClip, randomVolume);
                }
            }
        }
    }
}