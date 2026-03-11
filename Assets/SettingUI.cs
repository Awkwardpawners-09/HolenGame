using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Settings UI controller.
/// 
/// HOW TO SET UP IN INSPECTOR:
/// ─────────────────────────────────────────────────────────────────────
/// Sound Toggle:
///   • soundButton      → Button on the "Sounds" row
///   • soundCheck       → The "Check" child GameObject of that button
///
/// Graphics Quality (mutually exclusive — only one active at a time):
///   • graphicsLowButton    → "Low"    Button (has a "Check" child)
///   • graphicsLowCheck     → "Check"  child of Low button
///   • graphicsMedButton    → "Medium" Button
///   • graphicsMedCheck     → "Check"  child of Medium button
///   • graphicsHighButton   → "High"   Button
///   • graphicsHighCheck    → "Check"  child of High button
///
/// Shadows Toggle:
///   • shadowsButton    → Button on the "Shadows" row
///   • shadowsCheck     → The "Check" child GameObject
///
/// Post Processing Toggle:
///   • postFxButton     → Button on the "Post Processing" row
///   • postFxCheck      → The "Check" child GameObject
///
/// Scene Navigation (unchanged):
///   • loadSceneButton, sceneNameToLoad, transitionObject, waitTimeBeforeLoad
///
/// Quit:
///   • quitButton
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class SettingUI : MonoBehaviour
{
    // ===================== SOUND =====================
    [Header("Sound Toggle")]
    public Button soundButton;
    public GameObject soundCheck;      // Child "Check" object — active when sound is ON

    // ===================== GRAPHICS QUALITY =====================
    [Header("Graphics Quality (Low / Medium / High)")]
    public Button graphicsLowButton;
    public GameObject graphicsLowCheck;

    public Button graphicsMedButton;
    public GameObject graphicsMedCheck;

    public Button graphicsHighButton;
    public GameObject graphicsHighCheck;

    // ===================== SHADOWS =====================
    [Header("Shadows Toggle")]
    public Button shadowsButton;
    public GameObject shadowsCheck;    // Child "Check" object — active when shadows are ON

    // ===================== POST PROCESSING =====================
    [Header("Post Processing Toggle")]
    public Button postFxButton;
    public GameObject postFxCheck;     // Child "Check" object — active when post processing is ON

    // ===================== SCENE NAVIGATION =====================
    [Header("Scene Navigation")]
    [Tooltip("Button to load a scene (e.g., Main Menu button)")]
    public Button loadSceneButton;

    [Tooltip("Name of the scene to load")]
    public string sceneNameToLoad = "MainMenu";

    [Tooltip("GameObject to enable before loading scene (e.g., loading screen)")]
    public GameObject transitionObject;

    [Tooltip("Wait time in seconds before loading scene")]
    public float waitTimeBeforeLoad = 3f;

    // ===================== QUIT =====================
    [Header("Quit Game")]
    public Button quitButton;

    private bool isLoadingScene = false;

    // ─────────────────────────────────────────────────────────────────

    void Start()
    {
        // Wire up buttons
        if (soundButton != null) soundButton.onClick.AddListener(OnSoundToggle);
        if (graphicsLowButton != null) graphicsLowButton.onClick.AddListener(() => OnSetGraphicsQuality(0));
        if (graphicsMedButton != null) graphicsMedButton.onClick.AddListener(() => OnSetGraphicsQuality(1));
        if (graphicsHighButton != null) graphicsHighButton.onClick.AddListener(() => OnSetGraphicsQuality(2));
        if (shadowsButton != null) shadowsButton.onClick.AddListener(OnShadowsToggle);
        if (postFxButton != null) postFxButton.onClick.AddListener(OnPostFxToggle);
        if (loadSceneButton != null) loadSceneButton.onClick.AddListener(OnLoadScene);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitGame);

        // Hide transition object at start
        if (transitionObject != null)
            transitionObject.SetActive(false);

        RefreshAllUI();
    }

    void OnEnable()
    {
        // Refresh whenever the settings panel is opened
        RefreshAllUI();
    }

    // ─────────────────────────────────────────────────────────────────
    // UI REFRESH
    // ─────────────────────────────────────────────────────────────────

    private void RefreshAllUI()
    {
        if (PlayerDataManager.Instance == null) return;

        SetCheckActive(soundCheck, PlayerDataManager.Instance.IsSoundEnabled());
        SetCheckActive(shadowsCheck, PlayerDataManager.Instance.IsShadowsEnabled());
        SetCheckActive(postFxCheck, PlayerDataManager.Instance.IsPostProcessingEnabled());
        RefreshGraphicsUI(PlayerDataManager.Instance.GetGraphicsQuality());
    }

    /// <summary>Updates the three graphics check marks so only the active quality shows a check.</summary>
    private void RefreshGraphicsUI(int quality)
    {
        SetCheckActive(graphicsLowCheck, quality == 0);
        SetCheckActive(graphicsMedCheck, quality == 1);
        SetCheckActive(graphicsHighCheck, quality == 2);
    }

    private static void SetCheckActive(GameObject check, bool active)
    {
        if (check != null) check.SetActive(active);
    }

    // ─────────────────────────────────────────────────────────────────
    // TOGGLE HANDLERS
    // ─────────────────────────────────────────────────────────────────

    private void OnSoundToggle()
    {
        PlayerDataManager.Instance.ToggleSound();
        SetCheckActive(soundCheck, PlayerDataManager.Instance.IsSoundEnabled());
    }

    /// <summary>Called by each graphics quality button. 0=Low, 1=Medium, 2=High.</summary>
    private void OnSetGraphicsQuality(int quality)
    {
        PlayerDataManager.Instance.SetGraphicsQuality(quality);
        RefreshGraphicsUI(quality);
    }

    private void OnShadowsToggle()
    {
        PlayerDataManager.Instance.ToggleShadows();
        SetCheckActive(shadowsCheck, PlayerDataManager.Instance.IsShadowsEnabled());
    }

    private void OnPostFxToggle()
    {
        PlayerDataManager.Instance.TogglePostProcessing();
        SetCheckActive(postFxCheck, PlayerDataManager.Instance.IsPostProcessingEnabled());
    }

    // ─────────────────────────────────────────────────────────────────
    // SCENE LOADING
    // ─────────────────────────────────────────────────────────────────

    private void OnLoadScene()
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

    private IEnumerator LoadSceneWithDelay()
    {
        isLoadingScene = true;

        if (transitionObject != null)
        {
            transitionObject.SetActive(true);
            Debug.Log($"Transition object enabled. Loading scene in {waitTimeBeforeLoad} seconds...");
        }

        yield return new WaitForSeconds(waitTimeBeforeLoad);

        Debug.Log($"Loading scene: {sceneNameToLoad}");
        SceneManager.LoadScene(sceneNameToLoad);
    }

    /// <summary>Load a specific scene immediately without delay.</summary>
    public void LoadSceneImmediately(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
    }

    /// <summary>Load a scene with a custom delay.</summary>
    public void LoadSceneWithCustomDelay(string sceneName, float delay)
    {
        StartCoroutine(LoadSceneWithCustomDelayCoroutine(sceneName, delay));
    }

    private IEnumerator LoadSceneWithCustomDelayCoroutine(string sceneName, float delay)
    {
        if (transitionObject != null)
            transitionObject.SetActive(true);

        yield return new WaitForSeconds(delay);

        SceneManager.LoadScene(sceneName);
    }

    // ─────────────────────────────────────────────────────────────────
    // QUIT GAME
    // ─────────────────────────────────────────────────────────────────

    private void OnQuitGame()
    {
        Debug.Log("Quitting application...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}