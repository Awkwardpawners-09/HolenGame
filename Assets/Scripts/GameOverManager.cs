using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    // Inspector variables
    public string sceneToLoad = "Demo Menu (END)"; // The scene to load after 6 seconds, changeable in the inspector
    public float waitTime = 6f;  // Time to wait before changing the scene, adjustable in the inspector

    private bool isGameOver = false; // Flag to check if the game over screen is enabled


    private void Start()
    {
        TriggerGameOver();
    }

    void Update()
    {
        // If the game over screen is enabled, start the timer
        if (isGameOver)
        {
            waitTime -= Time.deltaTime;  // Decrease the wait time over time

            if (waitTime <= 0f)
            {
                // Change the scene once the wait time has passed
                Debug.Log("Scene Change Triggered"); // Debug log to confirm when the scene change should happen
                LoadNextScene();
            }
        }
    }

    // Call this method when you enable the Game Over object
    public void TriggerGameOver()
    {
        isGameOver = true;  // Set the game over flag to true
        gameObject.SetActive(true);  // Enable the Game Over object
        Debug.Log("Game Over Triggered"); // Debug log to confirm Game Over was triggered
    }

    // Load the next scene based on the scene name
    private void LoadNextScene()
    {
        Debug.Log("Loading Scene: " + sceneToLoad);  // Debug log to confirm scene name before loading
        SceneManager.LoadScene(sceneToLoad);  // Load the scene specified in the inspector
    }
}
