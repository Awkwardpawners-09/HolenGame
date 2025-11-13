using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Objective Detection")]
    [Tooltip("Enable objective mode to detect holens in play field")]
    public bool enableObjectiveMode = true;

    [Tooltip("Time to wait when no objectives in trigger before game over (seconds)")]
    public float waitTime = 5f;

    private HashSet<GameObject> objectivesInTrigger = new HashSet<GameObject>();
    private float noObjectiveTimer = 0f;
    private bool gameOverTriggered = false;

    [Header("Lives System")]
    [Tooltip("Maximum number of lives")]
    public int maxLives = 3;

    [Tooltip("TextMeshPro for displaying current lives")]
    public TextMeshProUGUI livesText;

    private int currentLives;
    private float lifeLossTimer = 0f;
    private bool isLifeReductionDelayed = false;

    [Header("Game Over")]
    [Tooltip("GameObject to enable when game over (no lives or no holens)")]
    public GameObject gameOverObject;

    [Tooltip("Time to wait before loading game over scene (seconds)")]
    public float gameOverDelay = 3f;

    [Header("Scene Management")]
    [Tooltip("Scene to load after game over or when lives reach 0")]
    public string gameOverSceneName = "GameOver";

    [Tooltip("Enable transition effect before scene change")]
    public GameObject transitionObject;

    [Tooltip("Transition duration before loading scene (seconds)")]
    public float transitionDuration = 2f;

    void Start()
    {
        currentLives = maxLives;
        UpdateLivesText();

        if (gameOverObject != null)
            gameOverObject.SetActive(false);

        if (transitionObject != null)
            transitionObject.SetActive(false);
    }

    void Update()
    {
        // If game over already triggered, don't check anything
        if (gameOverTriggered)
            return;

        // Check if player has lost all lives
        if (currentLives <= 0)
        {
            TriggerGameOver();
            return;
        }

        // Check if no "Objective" tagged objects are in the trigger area
        if (enableObjectiveMode && objectivesInTrigger.Count == 0)
        {
            noObjectiveTimer += Time.deltaTime;

            // If no objectives for specified wait time, trigger game over
            if (noObjectiveTimer >= waitTime)
            {
                Debug.Log("[LevelManager] No holens in play field for " + waitTime + " seconds. Game Over!");
                TriggerGameOver();
            }
        }
        else
        {
            // Reset the timer if objectives are inside the trigger area
            noObjectiveTimer = 0f;
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

        // Track objectives in trigger area
        if (enableObjectiveMode && other.CompareTag("Objective"))
        {
            objectivesInTrigger.Add(other.gameObject);
            Debug.Log("[LevelManager] Objective entered: " + other.name + ". Total in area: " + objectivesInTrigger.Count);
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Remove objectives that exit the trigger area
        if (enableObjectiveMode && other.CompareTag("Objective"))
        {
            objectivesInTrigger.Remove(other.gameObject);
            Debug.Log("[LevelManager] Objective exited: " + other.name + ". Total in area: " + objectivesInTrigger.Count);
        }
    }

    /// <summary>
    /// Reduces player lives by 1
    /// </summary>
    private void ReduceLife()
    {
        currentLives--;
        UpdateLivesText();
        Debug.Log("[LevelManager] Life reduced. Current lives: " + currentLives);

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
    /// Triggers the game over sequence
    /// </summary>
    private void TriggerGameOver()
    {
        if (gameOverTriggered)
            return;

        gameOverTriggered = true;
        Debug.Log("[LevelManager] Game Over triggered!");

        // Enable game over object
        if (gameOverObject != null)
        {
            gameOverObject.SetActive(true);
        }

        // Start coroutine to load game over scene
        StartCoroutine(LoadGameOverScene());
    }

    /// <summary>
    /// Loads the game over scene with transition
    /// </summary>
    private IEnumerator LoadGameOverScene()
    {
        // Wait for game over delay
        yield return new WaitForSeconds(gameOverDelay);

        // Enable transition if assigned
        if (transitionObject != null)
        {
            transitionObject.SetActive(true);
            Debug.Log("[LevelManager] Transition enabled");
        }

        // Wait for transition duration
        yield return new WaitForSeconds(transitionDuration);

        // Load game over scene
        Debug.Log("[LevelManager] Loading scene: " + gameOverSceneName);
        SceneManager.LoadScene(gameOverSceneName);
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