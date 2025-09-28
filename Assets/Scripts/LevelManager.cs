using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Import TextMeshPro namespace

public class LevelManager : MonoBehaviour
{
    private HashSet<GameObject> objectivesInTrigger = new HashSet<GameObject>();
    private float noObjectiveTimer = 0f;
    public float waitTime = 5f;
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
        // Check if lives are 0 and enable the game over object immediately
        if (currentLives <= 0)
        {
            gameOverObject.SetActive(true);  // Enable the Game Over object immediately
            return; // Exit the update method early since the game is over
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
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene("Demo Menu");
    }

    // Other gameplay-related methods, such as handling objectives, scene transitions, etc.
}
