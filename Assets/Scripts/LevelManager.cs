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
    public TextMeshProUGUI collisionText; // TextMeshProUGUI for displaying the collision count
    public int currentCollisions = 0; // To track the number of collisions (exposed to Inspector)

    // New checkbox for enabling/disabling objective mode
    public bool enableObjectiveMode = true; // Checkbox to enable/disable objective detection

    private bool canCountCollision = true; // To track if the collision count is allowed (cooldown mechanism)
    private float cooldownTime = 2f; // Cooldown duration in seconds
    private float cooldownTimer = 0f; // Timer to manage the cooldown

    private float sceneChangeDelay = 3f; // Delay time for scene change after the last collision
    private float sceneChangeTimer = 0f; // Timer to track the delay before loading the scene

    private void Update()
    {
        // If objective mode is enabled, check for objectives in trigger
        if (enableObjectiveMode)
        {
            // If there are no Objective-tagged objects in the trigger
            if (objectivesInTrigger.Count == 0)
            {
                noObjectiveTimer += Time.deltaTime;

                if (noObjectiveTimer >= waitTime && !loadingNextScene)
                {
                    loadingNextScene = true;
                    LoadNextScene();
                }
            }
            else
            {
                // Reset timer if any objective is still inside
                noObjectiveTimer = 0f;
            }
        }
        else
        {
            // Collision Mode: Handle collision counting
            if (currentCollisions >= requiredCollisions && !loadingNextScene)
            {
                sceneChangeTimer += Time.deltaTime;

                if (sceneChangeTimer >= sceneChangeDelay)
                {
                  
                    LoadNextScene();
                }
            }
        }

        // Handle cooldown timer for collisions
        if (!canCountCollision)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= cooldownTime)
            {
                canCountCollision = true; // Enable counting after cooldown
                cooldownTimer = 0f; // Reset the cooldown timer
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // If not in objective mode, count collisions
        if (!enableObjectiveMode && canCountCollision)
        {
            currentCollisions++;
            UpdateCollisionText();
           // canCountCollision = false; // Disable counting until cooldown is over

            // Reset the scene change timer after a valid collision
            sceneChangeTimer = 0f;
        }

        // If in objective mode, check for "Objective" tag in the collider
        if (enableObjectiveMode && other.CompareTag("Objective"))
        {
            objectivesInTrigger.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Remove the "Objective" tagged object from the set (only if in objective mode)
        if (enableObjectiveMode && other.CompareTag("Objective"))
        {
            objectivesInTrigger.Remove(other.gameObject);
        }
    }

    private void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex + 1);
    }

    private void UpdateCollisionText()
    {
        // Safely update the collision text if it's assigned
        if (collisionText != null)
        {
            collisionText.text = currentCollisions.ToString(); // Display only the number of collisions\\

            canCountCollision = true;
        }
    }
}
