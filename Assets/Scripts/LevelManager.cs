using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    private HashSet<GameObject> objectivesInTrigger = new HashSet<GameObject>();
    private float noObjectiveTimer = 0f;
    public float waitTime = 5f;
    private bool loadingNextScene = false;

    private void Update()
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Objective"))
        {
            objectivesInTrigger.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Objective"))
        {
            objectivesInTrigger.Remove(other.gameObject);
        }
    }

    private void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex + 1);
    }
}
