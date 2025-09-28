using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Import TextMeshPro namespace

public class LevelManager : MonoBehaviour
{
    private HashSet<GameObject> objectivesInTrigger = new HashSet<GameObject>();  // Keep track of objectives in the trigger area
    private float noObjectiveTimer = 0f;  // Timer for when there are no objectives in the trigger
    public float waitTime = 5f;  // Time to wait for no objectives in trigger (5 seconds)
    private bool loadingNextScene = false;

    // Inspector variables
    public GameObject holenPlayerObject; // Set the "player" object (Holen) to detect collision
    public GameObject targetObject; // Set the target object where the collision will happen
    public int requiredCollisions = 3; // Set how many times the collision should happen
    public TextMeshProUGUI livesText; // TextMeshProUGUI for displaying the number of lives
    public int maxLives = 3; // Set the number of lives
    private int currentLives; // Tracks current lives
    private float lifeLossTimer = 0f;  // Timer to delay life reduction after collision
    private bool isLifeReductionDelayed = false;  // Flag to track if we are waiting for the delay
    public GameObject gameOverObject; // The object to be enabled when lives are less than 0

    // New checkbox for enabling/disabling objective mode
    public bool enableObjectiveMode = true; // Checkbox to enable/disable objective detection

    private bool canCountCollision = true; // To track if the collision count is allowed (cooldown mechanism)
    private float cooldownTime = 2f; // Cooldown duration in seconds
    private float cooldownTimer = 0f; // Timer to manage the cooldown

    private float sceneChangeDelay = 3f; // Delay time for scene change after the last collision
    private float sceneChangeTimer = 0f; // Timer to track the delay before loading the scene

    void Start()
    {
        currentLives = maxLives; // Initialize lives to max lives
        UpdateLivesText(); // Update the displayed lives text
        gameOverObject.SetActive(false); // Ensure the game over object is initially disabled
    }

    void Update()
    {
        // If the player has lost all lives, enable the game over object immediately
        if (currentLives <= 0)
        {
            gameOverObject.SetActive(true);  // Enable the Game Over object immediately
            return; // Exit the update method early since the game is over
        }

        // Check if no "Objective" tagged objects are in the trigger area for 5 seconds
        if (objectivesInTrigger.Count == 0)
        {
            noObjectiveTimer += Time.deltaTime;  // Increment the timer when there are no objectives in the trigger

            // If no objectives for 5 seconds, load the next scene
            if (noObjectiveTimer >= waitTime && !loadingNextScene)
            {
                loadingNextScene = true; // Flag to prevent multiple scene loads
                LoadNextScene();
            }
        }
        else
        {
            // Reset the timer if objectives are inside the trigger area
            noObjectiveTimer = 0f;
        }

        // If the life reduction is delayed, increase the timer
        if (isLifeReductionDelayed)
        {
            lifeLossTimer += Time.deltaTime;

            // If 6.5 seconds have passed, reduce life
            if (lifeLossTimer >= 6.5f)
            {
                ReduceLife(); // Reduce life once the timer reaches 6.5 seconds
                isLifeReductionDelayed = false; // Stop the timer
                lifeLossTimer = 0f; // Reset the timer
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Only handle lives reduction if the object is tagged "Ball"
        if (other.CompareTag("Ball"))
        {
            // Start the delay for life reduction
            isLifeReductionDelayed = true;

            // Optionally, you can add some feedback (visual, audio) here to show that the player lost a life
        }

        // If an "Objective" tagged object enters the trigger area, add it to the set
        if (enableObjectiveMode && other.CompareTag("Objective"))
        {
            objectivesInTrigger.Add(other.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        // If an "Objective" tagged object exits the trigger area, remove it from the set
        if (enableObjectiveMode && other.CompareTag("Objective"))
        {
            objectivesInTrigger.Remove(other.gameObject);
        }
    }

    private void ReduceLife()
    {
        // Reduce lives by 1
        currentLives--;
        UpdateLivesText(); // Update the UI text to reflect the new life count

        if (currentLives >= 0)
        {
            // Reset the timer when the player still has lives
            lifeLossTimer = 0f;
        }
    }

    private void UpdateLivesText()
    {
        // Safely update the lives text if it's assigned
        if (livesText != null)
        {
            livesText.text = currentLives.ToString(); // Display the current number of lives
        }
    }

    private void LoadNextScene()
    {
        // Load the next scene after waiting for 5 seconds with no objectives in the trigger
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;  // Get current scene index
        int nextSceneIndex = currentSceneIndex + 1; // Example: next scene is next in the build list
        SceneManager.LoadScene(nextSceneIndex);
    }
}
