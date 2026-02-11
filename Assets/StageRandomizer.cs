using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class StageRandomizer : MonoBehaviourPunCallbacks
{
    [Header("Stage Options")]
    [Tooltip("Add all possible stage GameObjects here. One will be randomly chosen.")]
    [SerializeField] private List<GameObject> stageOptions = new List<GameObject>();

    [Header("Settings")]
    [Tooltip("If true, uses a seed for reproducible randomization")]
    [SerializeField] private bool useSeed = false;
    [SerializeField] private int seed = 0;

    private const string STAGE_INDEX_KEY = "SelectedStageIndex";
    private bool hasInitialized = false;

    private void Awake()
    {
        // Players are already in room when this scene loads
        InitializeStage();
    }

    private void InitializeStage()
    {
        if (hasInitialized)
            return;

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

        // Check if stage has already been selected
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(STAGE_INDEX_KEY))
        {
            // Stage already selected, apply it
            int selectedIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties[STAGE_INDEX_KEY];
            ApplyStageSelection(selectedIndex);
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            // Master Client selects the stage
            SelectAndSyncStage();
        }
        // Non-master clients will receive the selection via OnRoomPropertiesUpdate

        hasInitialized = true;
    }

    private void SelectAndSyncStage()
    {
        // Set seed if enabled
        if (useSeed)
        {
            Random.InitState(seed);
        }

        // Randomly select one stage
        int randomIndex = Random.Range(0, stageOptions.Count);

        Debug.Log($"StageRandomizer [Master]: Selected stage {randomIndex + 1}/{stageOptions.Count}: {stageOptions[randomIndex].name}");

        // Sync to all players via Room Custom Properties
        Hashtable properties = new Hashtable();
        properties[STAGE_INDEX_KEY] = randomIndex;
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);

        // Apply it locally immediately
        ApplyStageSelection(randomIndex);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);

        // Check if the stage selection was updated
        if (propertiesThatChanged.ContainsKey(STAGE_INDEX_KEY))
        {
            int selectedIndex = (int)propertiesThatChanged[STAGE_INDEX_KEY];

            // Don't apply if we're master client (already applied)
            if (!PhotonNetwork.IsMasterClient)
            {
                ApplyStageSelection(selectedIndex);
            }
        }
    }

    private void ApplyStageSelection(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= stageOptions.Count)
        {
            Debug.LogError($"StageRandomizer: Invalid stage index {selectedIndex}! Max index is {stageOptions.Count - 1}");
            return;
        }

        GameObject chosenStage = stageOptions[selectedIndex];
        Debug.Log($"StageRandomizer: Applying stage {selectedIndex + 1}/{stageOptions.Count}: {chosenStage.name}");

        // Process all stages
        for (int i = 0; i < stageOptions.Count; i++)
        {
            GameObject stage = stageOptions[i];

            if (i == selectedIndex)
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

    // Optional: Method to manually trigger randomization (useful for testing in editor)
    [ContextMenu("Randomize Stage Now (Master Only)")]
    public void RandomizeStageManually()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            hasInitialized = false;
            SelectAndSyncStage();
        }
        else
        {
            Debug.LogWarning("Only Master Client can manually randomize the stage!");
        }
    }
}