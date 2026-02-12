using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SettingUI : MonoBehaviour
{
    [Header("Sound Toggle")]
    public Button soundToggleButton;
    public GameObject soundOnIcon;  // Shows when sound is ON
    public GameObject soundOffIcon; // Shows when sound is OFF

    [Header("Scene Navigation")]
    [Tooltip("Button to load a scene (e.g., Main Menu button)")]
    public Button loadSceneButton;

    [Tooltip("Name of the scene to load")]
    public string sceneNameToLoad = "MainMenu";

    [Tooltip("GameObject to enable before loading scene (e.g., loading screen)")]
    public GameObject transitionObject;

    [Tooltip("Wait time in seconds before loading scene")]
    public float waitTimeBeforeLoad = 3f;

    [Header("Quit Game")]
    [Tooltip("Button to close the application")]
    public Button quitButton;

    private bool isLoadingScene = false;

    void Start()
    {
        // Setup button listeners
        if (soundToggleButton != null)
        {
            soundToggleButton.onClick.AddListener(OnSoundToggle);
        }

        if (loadSceneButton != null)
        {
            loadSceneButton.onClick.AddListener(OnLoadScene);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitGame);
        }

        // Initialize UI state
        UpdateSoundUI();

        // Make sure transition object is hidden at start
        if (transitionObject != null)
        {
            transitionObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        // Refresh UI when settings panel opens
        UpdateSoundUI();
    }

    void OnSoundToggle()
    {
        // Toggle sound using the HolenInventoryManager
        HolenInventoryManager.Instance.ToggleSound();

        // Update UI
        UpdateSoundUI();
    }

    void UpdateSoundUI()
    {
        if (HolenInventoryManager.Instance == null)
            return;

        bool soundEnabled = HolenInventoryManager.Instance.IsSoundEnabled();

        // Update icon visuals
        if (soundOnIcon != null)
            soundOnIcon.SetActive(soundEnabled);

        if (soundOffIcon != null)
            soundOffIcon.SetActive(!soundEnabled);
    }

    // ===================== SCENE LOADING =====================

    void OnLoadScene()
    {
        if (isLoadingScene)
        {
            Debug.LogWarning("Scene is already loading...");
            return;
        }

        if (string.IsNullOrEmpty(sceneNameToLoad))
        {
            Debug.LogError("Scene name is not set in SettingsUI!");
            return;
        }

        StartCoroutine(LoadSceneWithDelay());
    }

    IEnumerator LoadSceneWithDelay()
    {
        isLoadingScene = true;

        // Enable transition object (e.g., loading screen, fade panel)
        if (transitionObject != null)
        {
            transitionObject.SetActive(true);
            Debug.Log($"Transition object enabled. Loading scene in {waitTimeBeforeLoad} seconds...");
        }

        // Wait for specified time
        yield return new WaitForSeconds(waitTimeBeforeLoad);

        // Load the scene
        Debug.Log($"Loading scene: {sceneNameToLoad}");
        SceneManager.LoadScene(sceneNameToLoad);
    }

    // ===================== QUIT GAME =====================

    void OnQuitGame()
    {
        Debug.Log("Quitting application...");

#if UNITY_EDITOR
        // Stop playing in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quit the application in build
        Application.Quit();
#endif
    }

    // ===================== PUBLIC HELPER METHODS =====================

    /// <summary>
    /// Load a specific scene immediately without delay (useful for other scripts)
    /// </summary>
    public void LoadSceneImmediately(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Load a scene with custom delay
    /// </summary>
    public void LoadSceneWithCustomDelay(string sceneName, float delay)
    {
        StartCoroutine(LoadSceneWithCustomDelayCoroutine(sceneName, delay));
    }

    IEnumerator LoadSceneWithCustomDelayCoroutine(string sceneName, float delay)
    {
        if (transitionObject != null)
        {
            transitionObject.SetActive(true);
        }

        yield return new WaitForSeconds(delay);

        SceneManager.LoadScene(sceneName);
    }
}