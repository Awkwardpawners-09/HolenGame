using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Lives System")]
    [Tooltip("Maximum number of lives")]
    public int maxLives = 3;

    [Tooltip("TextMeshPro for displaying current lives")]
    public TextMeshProUGUI livesText;

    [Tooltip("Sound effect to play when a life is lost")]
    public AudioClip lifeLostSound;

    [Tooltip("AudioSource to play sounds (optional - will use/create one if null)")]
    public AudioSource audioSource;

    private int currentLives;
    private float lifeLossTimer = 0f;
    private bool isLifeReductionDelayed = false;

    [Header("Objective Settings")]
    [Tooltip("Check objectives you want to monitor")]
    public bool checkNoHolensInField = true;

    [Tooltip("Time to wait when no holens in play field before triggering (seconds)")]
    public float waitTime = 5f;

    private HashSet<GameObject> holensInTrigger = new HashSet<GameObject>();
    private float noHolenTimer = 0f;
    private bool levelCompleted = false;
    private bool gameOverTriggered = false;

    [Header("Level Complete (All Holens Cleared)")]
    [Tooltip("GameObjects to enable when all holens are cleared")]
    public GameObject[] levelCompleteObjects;

    [Tooltip("Delay before enabling level complete objects (seconds)")]
    public float levelCompleteDelay = 3f;

    [Header("Game Over (No Lives Remaining)")]
    [Tooltip("GameObjects to enable when player loses all lives")]
    public GameObject[] gameOverObjects;

    [Tooltip("Delay before enabling game over objects (seconds)")]
    public float gameOverDelay = 3f;

    [Header("Scene Management (Optional)")]
    [Tooltip("Load a scene after level complete? Leave empty to disable")]
    public string levelCompleteSceneName = "";

    [Tooltip("Load a scene after game over? Leave empty to disable")]
    public string gameOverSceneName = "";

    [Tooltip("Enable transition effect before scene change")]
    public GameObject transitionObject;

    [Tooltip("Transition duration before loading scene (seconds)")]
    public float transitionDuration = 2f;

    void Start()
    {
        currentLives = maxLives;
        UpdateLivesText();

        // Setup audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Disable all completion/game over objects at start
        if (levelCompleteObjects != null)
        {
            foreach (GameObject obj in levelCompleteObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        if (gameOverObjects != null)
        {
            foreach (GameObject obj in gameOverObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        if (transitionObject != null)
            transitionObject.SetActive(false);
    }

    void Update()
    {
        // If game over or level complete already triggered, don't check anything
        if (gameOverTriggered || levelCompleted)
            return;

        // Check if player has lost all lives
        if (currentLives <= 0)
        {
            TriggerGameOver();
            return;
        }

        // Check "No holens in field" objective
        if (checkNoHolensInField && holensInTrigger.Count == 0)
        {
            noHolenTimer += Time.deltaTime;

            // If no holens for specified wait time, level complete!
            if (noHolenTimer >= waitTime)
            {
                Debug.Log("[LevelManager] No holens in play field for " + waitTime + " seconds. Level Complete!");
                TriggerLevelComplete();
            }
        }
        else
        {
            // Reset the timer if holens are inside the trigger area
            noHolenTimer = 0f;
        }

        // Handle delayed life reduction
        if (isLifeReductionDelayed)
        {
            lifeLossTimer += Time.deltaTime;

            if (lifeLossTimer >= 6.5f)
            {
                ReduceLife();
                isLifeReductionDelayed = false;
                lifeLossTimer = 0f;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Handle ball collision for lives reduction
        if (other.CompareTag("Ball"))
        {
            isLifeReductionDelayed = true;
            Debug.Log("[LevelManager] Ball entered trigger. Life will be reduced in 6.5 seconds.");
        }

        // Track holens (objectives) in trigger area
        if (checkNoHolensInField && other.CompareTag("Objective"))
        {
            holensInTrigger.Add(other.gameObject);
            Debug.Log("[LevelManager] Holen entered: " + other.name + ". Total in area: " + holensInTrigger.Count);
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Remove holens that exit the trigger area
        if (checkNoHolensInField && other.CompareTag("Objective"))
        {
            holensInTrigger.Remove(other.gameObject);
            Debug.Log("[LevelManager] Holen exited: " + other.name + ". Total in area: " + holensInTrigger.Count);
        }
    }

    /// <summary>
    /// Reduces player lives by 1 and plays sound effect
    /// </summary>
    private void ReduceLife()
    {
        currentLives--;
        UpdateLivesText();
        Debug.Log("[LevelManager] Life reduced. Current lives: " + currentLives);

        // Play life lost sound effect
        if (lifeLostSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(lifeLostSound);
            Debug.Log("[LevelManager] Playing life lost sound effect");
        }

        if (currentLives <= 0)
        {
            TriggerGameOver();
        }
    }

    /// <summary>
    /// Updates the lives display text
    /// </summary>
    private void UpdateLivesText()
    {
        if (livesText != null)
        {
            livesText.text = currentLives.ToString();
        }
    }

    /// <summary>
    /// Triggers the level complete sequence (all holens cleared)
    /// </summary>
    private void TriggerLevelComplete()
    {
        if (levelCompleted || gameOverTriggered)
            return;

        levelCompleted = true;
        Debug.Log("[LevelManager] Level Complete triggered!");

        // Start coroutine to handle level complete
        StartCoroutine(HandleLevelComplete());
    }

    /// <summary>
    /// Triggers the game over sequence (no lives remaining)
    /// </summary>
    private void TriggerGameOver()
    {
        if (gameOverTriggered || levelCompleted)
            return;

        gameOverTriggered = true;
        Debug.Log("[LevelManager] Game Over triggered!");

        // Start coroutine to handle game over
        StartCoroutine(HandleGameOver());
    }

    /// <summary>
    /// Handles level complete sequence with delay
    /// </summary>
    private IEnumerator HandleLevelComplete()
    {
        // Wait for level complete delay
        yield return new WaitForSeconds(levelCompleteDelay);

        // Enable all level complete objects
        if (levelCompleteObjects != null)
        {
            foreach (GameObject obj in levelCompleteObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log("[LevelManager] Enabled level complete object: " + obj.name);
                }
            }
        }

        // Load scene if specified
        if (!string.IsNullOrEmpty(levelCompleteSceneName))
        {
            // Enable transition if assigned
            if (transitionObject != null)
            {
                transitionObject.SetActive(true);
                Debug.Log("[LevelManager] Transition enabled");
            }

            // Wait for transition duration
            yield return new WaitForSeconds(transitionDuration);

            // Load scene
            Debug.Log("[LevelManager] Loading level complete scene: " + levelCompleteSceneName);
            SceneManager.LoadScene(levelCompleteSceneName);
        }
    }

    /// <summary>
    /// Handles game over sequence with delay
    /// </summary>
    private IEnumerator HandleGameOver()
    {
        // Wait for game over delay
        yield return new WaitForSeconds(gameOverDelay);

        // Enable all game over objects
        if (gameOverObjects != null)
        {
            foreach (GameObject obj in gameOverObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log("[LevelManager] Enabled game over object: " + obj.name);
                }
            }
        }

        // Load scene if specified
        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            // Enable transition if assigned
            if (transitionObject != null)
            {
                transitionObject.SetActive(true);
                Debug.Log("[LevelManager] Transition enabled");
            }

            // Wait for transition duration
            yield return new WaitForSeconds(transitionDuration);

            // Load scene
            Debug.Log("[LevelManager] Loading game over scene: " + gameOverSceneName);
            SceneManager.LoadScene(gameOverSceneName);
        }
    }

    /// <summary>
    /// Public method to manually trigger level complete (for external calls)
    /// </summary>
    public void ManualLevelComplete()
    {
        TriggerLevelComplete();
    }

    /// <summary>
    /// Public method to manually trigger game over (for external calls)
    /// </summary>
    public void ManualGameOver()
    {
        TriggerGameOver();
    }

    /// <summary>
    /// Public method to add lives (for power-ups, etc.)
    /// </summary>
    public void AddLife(int amount = 1)
    {
        currentLives += amount;
        UpdateLivesText();
        Debug.Log("[LevelManager] Lives added. Current lives: " + currentLives);
    }

    /// <summary>
    /// Public method to get current lives
    /// </summary>
    public int GetCurrentLives()
    {
        return currentLives;
    }
}