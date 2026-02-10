using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Manages the arcade mode gameplay flow including scene transitions and stage progression.
/// </summary>
public class ArcadeModeManager : MonoBehaviourPunCallbacks
{
    [Header("Scene Transition")]
    [Tooltip("GameObject to enable during scene transitions (fade, loading screen, etc.)")]
    public GameObject transitionObject;

    [Tooltip("How long to show transition before loading next scene (in seconds)")]
    public float transitionDuration = 3f;

    [Header("Stage Scenes")]
    [Tooltip("List of stage scenes in order")]
    public List<string> stageScenes = new List<string> { "Stage1", "Stage2", "Stage3" };

    [Header("Menu Scene")]
    [Tooltip("Name of main menu scene (for returning)")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Network Settings")]
    [Tooltip("Maximum time to wait for Photon disconnect before forcing scene load (in seconds)")]
    public float disconnectTimeout = 5f;

    public static ArcadeModeManager Instance { get; private set; }

    private int currentStageIndex = 0;
    private bool isDisconnecting = false;
    private string pendingSceneName = "";

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure transition is disabled at start
        if (transitionObject != null)
        {
            transitionObject.SetActive(false);
        }
        else
        {
            Debug.LogError("[ArcadeMode] TRANSITION OBJECT NOT ASSIGNED! Please assign it in the Inspector.");
        }
    }

    private void Start()
    {
        // Additional check to help debug
        if (transitionObject == null)
        {
            Debug.LogError("[ArcadeMode] Transition object is NULL! Assign it in the Inspector on: " + gameObject.name);
        }
        else
        {
            Debug.Log("[ArcadeMode] Transition object assigned: " + transitionObject.name);
        }
    }

    /// <summary>
    /// Starts the arcade mode by loading the first stage.
    /// Call this from a button's onClick event.
    /// </summary>
    public void StartArcadeMode()
    {
        Debug.Log("[ArcadeMode] Starting Arcade Mode - Loading First Stage");
        currentStageIndex = 0;
        LoadStageByIndex(0);
    }

    /// <summary>
    /// Loads a stage by its index in the stageScenes list.
    /// </summary>
    /// <param name="index">Index of the stage (0-based)</param>
    public void LoadStageByIndex(int index)
    {
        if (index >= 0 && index < stageScenes.Count)
        {
            currentStageIndex = index;
            Debug.Log($"[ArcadeMode] Loading Stage {index + 1}: {stageScenes[index]}");
            StartCoroutine(DisconnectAndLoadScene(stageScenes[index]));
        }
        else
        {
            Debug.LogError($"[ArcadeMode] Stage index {index} is out of range! Total stages: {stageScenes.Count}");
        }
    }

    /// <summary>
    /// Loads the next stage in sequence.
    /// </summary>
    public void LoadNextStage()
    {
        int nextIndex = currentStageIndex + 1;
        if (nextIndex < stageScenes.Count)
        {
            LoadStageByIndex(nextIndex);
        }
        else
        {
            Debug.Log("[ArcadeMode] No more stages! Arcade mode complete.");
            // Optionally return to menu or show completion screen
        }
    }

    /// <summary>
    /// Loads the previous stage in sequence.
    /// </summary>
    public void LoadPreviousStage()
    {
        int prevIndex = currentStageIndex - 1;
        if (prevIndex >= 0)
        {
            LoadStageByIndex(prevIndex);
        }
        else
        {
            Debug.LogWarning("[ArcadeMode] Already at the first stage!");
        }
    }

    // ========== BUTTON-FRIENDLY STAGE LOADING METHODS ==========
    // These methods have no parameters so they work with Unity's onClick events
    // Add or remove methods as needed for your stages

    public void LoadElement0() => LoadStageByIndex(0);
    public void LoadElement1() => LoadStageByIndex(1);
    public void LoadElement2() => LoadStageByIndex(2);
    public void LoadElement3() => LoadStageByIndex(3);
    public void LoadElement4() => LoadStageByIndex(4);
    public void LoadElement5() => LoadStageByIndex(5);
    public void LoadElement6() => LoadStageByIndex(6);
    public void LoadElement7() => LoadStageByIndex(7);
    public void LoadElement8() => LoadStageByIndex(8);
    public void LoadElement9() => LoadStageByIndex(9);

    // Add more stage methods here if you have more than 10 stages:
    // public void LoadStage11() => LoadStageByIndex(10);
    // public void LoadStage12() => LoadStageByIndex(11);
    // etc.

    /// <summary>
    /// Returns to main menu with transition (does not disconnect from Photon).
    /// </summary>
    public void ReturnToMenu()
    {
        Debug.Log("[ArcadeMode] Returning to Main Menu");
        StartCoroutine(LoadSceneWithTransition(mainMenuSceneName));
    }

    /// <summary>
    /// Disconnects from Photon network before loading a scene.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load after disconnect</param>
    private IEnumerator DisconnectAndLoadScene(string sceneName)
    {
        pendingSceneName = sceneName;

        // Check if connected to Photon
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[ArcadeMode] Disconnecting from Photon network...");
            isDisconnecting = true;

            // Leave room if in one
            if (PhotonNetwork.InRoom)
            {
                Debug.Log("[ArcadeMode] Leaving Photon room...");
                PhotonNetwork.LeaveRoom();

                // Wait for room leave
                float timer = 0f;
                while (PhotonNetwork.InRoom && timer < disconnectTimeout)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }
            }

            // Disconnect from Photon
            PhotonNetwork.Disconnect();

            // Wait for disconnect with timeout
            float disconnectTimer = 0f;
            while (PhotonNetwork.IsConnected && disconnectTimer < disconnectTimeout)
            {
                disconnectTimer += Time.deltaTime;
                yield return null;
            }

            if (PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("[ArcadeMode] Photon disconnect timed out! Force loading scene...");
            }
            else
            {
                Debug.Log("[ArcadeMode] Successfully disconnected from Photon");
            }

            isDisconnecting = false;
        }
        else
        {
            Debug.Log("[ArcadeMode] Not connected to Photon, proceeding to load scene");
        }

        // Now load the scene with transition
        yield return StartCoroutine(LoadSceneWithTransition(sceneName));
    }

    /// <summary>
    /// Generic method to load any scene with transition effect.
    /// Uses async loading to prevent immediate scene destruction.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load</param>
    private IEnumerator LoadSceneWithTransition(string sceneName)
    {
        Debug.Log($"[ArcadeMode] === STARTING TRANSITION FOR: {sceneName} ===");

        // Enable transition object
        if (transitionObject != null)
        {
            Debug.Log($"[ArcadeMode] Activating transition object: {transitionObject.name}");
            transitionObject.SetActive(true);

            // Force canvas update if it's a UI element
            Canvas canvas = transitionObject.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
                canvas.enabled = true;
            }

            Debug.Log($"[ArcadeMode] Transition object active state: {transitionObject.activeSelf}");
        }
        else
        {
            Debug.LogError("[ArcadeMode] CRITICAL: Transition object is NULL! Cannot show transition.");
        }

        // Wait for transition duration
        Debug.Log($"[ArcadeMode] Waiting {transitionDuration} seconds before loading...");
        yield return new WaitForSeconds(transitionDuration);

        // Load the scene asynchronously
        Debug.Log($"[ArcadeMode] Now loading scene: {sceneName}");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log($"[ArcadeMode] Scene {sceneName} loaded successfully");
    }

    /// <summary>
    /// Photon callback when disconnected from server.
    /// </summary>
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[ArcadeMode] Photon disconnected: {cause}");

        if (isDisconnecting && !string.IsNullOrEmpty(pendingSceneName))
        {
            Debug.Log($"[ArcadeMode] Disconnect complete, ready to load: {pendingSceneName}");
        }
    }

    /// <summary>
    /// Photon callback when left a room.
    /// </summary>
    public override void OnLeftRoom()
    {
        Debug.Log("[ArcadeMode] Left Photon room");
    }

    /// <summary>
    /// Manually disable the transition (call this after scene loads if needed)
    /// </summary>
    public void DisableTransition()
    {
        if (transitionObject != null)
        {
            transitionObject.SetActive(false);
            Debug.Log("[ArcadeMode] Transition disabled");
        }
    }

    /// <summary>
    /// Reloads the current scene with transition.
    /// Useful for retry functionality.
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[ArcadeMode] Reloading scene: {currentScene}");
        StartCoroutine(DisconnectAndLoadScene(currentScene));
    }

    /// <summary>
    /// Gets the total number of stages.
    /// </summary>
    public int GetTotalStages()
    {
        return stageScenes.Count;
    }

    /// <summary>
    /// Gets the current stage index (0-based).
    /// </summary>
    public int GetCurrentStageIndex()
    {
        return currentStageIndex;
    }
}