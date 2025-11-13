using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the arcade mode gameplay flow including scene transitions and stage progression.
/// </summary>
public class ArcadeModeManager : MonoBehaviour
{
    [Header("Scene Transition")]
    [Tooltip("GameObject to enable during scene transitions (fade, loading screen, etc.)")]
    public GameObject transitionObject;

    [Tooltip("How long to show transition before loading next scene (in seconds)")]
    public float transitionDuration = 3f;

    [Header("Stage Scenes")]
    [Tooltip("Name of Stage 1 scene")]
    public string stage1SceneName = "Stage1";

    [Tooltip("Name of Stage 2 scene (optional, for future expansion)")]
    public string stage2SceneName = "Stage2";

    [Tooltip("Name of Stage 3 scene (optional, for future expansion)")]
    public string stage3SceneName = "Stage3";

    [Header("Menu Scene")]
    [Tooltip("Name of main menu scene (for returning)")]
    public string mainMenuSceneName = "MainMenu";

    public static ArcadeModeManager Instance { get; private set; }

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
    }

    /// <summary>
    /// Starts the arcade mode by loading Stage 1.
    /// Call this from a button's onClick event.
    /// </summary>
    public void StartArcadeMode()
    {
        Debug.Log("[ArcadeMode] Starting Arcade Mode - Loading Stage 1");
        StartCoroutine(LoadSceneWithTransition(stage1SceneName));
    }

    /// <summary>
    /// Loads Stage 1 with transition.
    /// </summary>
    public void LoadStage1()
    {
        Debug.Log("[ArcadeMode] Loading Stage 1");
        StartCoroutine(LoadSceneWithTransition(stage1SceneName));
    }

    /// <summary>
    /// Loads Stage 2 with transition.
    /// </summary>
    public void LoadStage2()
    {
        Debug.Log("[ArcadeMode] Loading Stage 2");
        StartCoroutine(LoadSceneWithTransition(stage2SceneName));
    }

    /// <summary>
    /// Loads Stage 3 with transition.
    /// </summary>
    public void LoadStage3()
    {
        Debug.Log("[ArcadeMode] Loading Stage 3");
        StartCoroutine(LoadSceneWithTransition(stage3SceneName));
    }

    /// <summary>
    /// Returns to main menu with transition.
    /// </summary>
    public void ReturnToMenu()
    {
        Debug.Log("[ArcadeMode] Returning to Main Menu");
        StartCoroutine(LoadSceneWithTransition(mainMenuSceneName));
    }

    /// <summary>
    /// Generic method to load any scene with transition effect.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load</param>
    private IEnumerator LoadSceneWithTransition(string sceneName)
    {
        // Enable transition object
        if (transitionObject != null)
        {
            transitionObject.SetActive(true);
            Debug.Log($"[ArcadeMode] Transition enabled for scene: {sceneName}");
        }
        else
        {
            Debug.LogWarning("[ArcadeMode] Transition object not assigned!");
        }

        // Wait for transition duration
        yield return new WaitForSeconds(transitionDuration);

        // Load the scene
        Debug.Log($"[ArcadeMode] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);

        // Note: Transition will be disabled in Awake of next scene or manually
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
        StartCoroutine(LoadSceneWithTransition(currentScene));
    }

}