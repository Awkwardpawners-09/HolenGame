using UnityEngine;
using System.Collections.Generic;

public class StageRandomizer : MonoBehaviour
{
    [Header("Stage Options")]
    [Tooltip("Add all possible stage GameObjects here. One will be randomly chosen.")]
    [SerializeField] private List<GameObject> stageOptions = new List<GameObject>();

    [Header("Settings")]
    [Tooltip("If true, uses a seed for reproducible randomization")]
    [SerializeField] private bool useSeed = false;
    [SerializeField] private int seed = 0;

    private void Awake()
    {
        RandomizeStage();
    }

    private void RandomizeStage()
    {
        // Validate that we have stage options
        if (stageOptions == null || stageOptions.Count == 0)
        {
            Debug.LogWarning("StageRandomizer: No stage options assigned! Please add GameObjects to the stageOptions list.");
            return;
        }

        // Remove any null entries from the list
        stageOptions.RemoveAll(item => item == null);

        if (stageOptions.Count == 0)
        {
            Debug.LogWarning("StageRandomizer: All stage options are null!");
            return;
        }

        // Set seed if enabled
        if (useSeed)
        {
            Random.InitState(seed);
        }

        // Randomly select one stage
        int randomIndex = Random.Range(0, stageOptions.Count);
        GameObject chosenStage = stageOptions[randomIndex];

        Debug.Log($"StageRandomizer: Selected stage {randomIndex + 1}/{stageOptions.Count}: {chosenStage.name}");

        // Process all stages
        for (int i = 0; i < stageOptions.Count; i++)
        {
            GameObject stage = stageOptions[i];

            if (i == randomIndex)
            {
                // This is the chosen stage - make sure it's enabled
                stage.SetActive(true);
            }
            else
            {
                // Not the chosen stage - disable and destroy
                stage.SetActive(false);
                Destroy(stage);
            }
        }
    }

    // Optional: Method to manually trigger randomization (useful for testing)
    [ContextMenu("Randomize Stage Now")]
    public void RandomizeStageManually()
    {
        RandomizeStage();
    }
}