using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages scene transitions with delays and object activation.
/// Attach this to a persistent GameObject in your game scene.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    [SerializeField] private Button continueButton;
    [SerializeField] private string continueSceneName = "NextLevel";

    [SerializeField] private Button retryButton;
    [SerializeField] private string retrySceneName = "CurrentLevel";

    [Header("Transition Effect")]
    [SerializeField] private GameObject transitionObject;
    [SerializeField] private float delayBeforeTransition = 5f;

    private bool isTransitioning = false;

    private void Start()
    {
        // Setup button listeners
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinuePressed);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryPressed);
        }

        // Make sure transition object is disabled at start
        if (transitionObject != null)
        {
            transitionObject.SetActive(false);
        }
    }

    private void OnContinuePressed()
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene(continueSceneName));
        }
    }

    private void OnRetryPressed()
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene(retrySceneName));
        }
    }

    private IEnumerator TransitionToScene(string sceneName)
    {
        isTransitioning = true;

        // Enable the transition object
        if (transitionObject != null)
        {
            transitionObject.SetActive(true);
        }

        // Wait for the specified delay
        yield return new WaitForSeconds(delayBeforeTransition);

        // Load the scene
        SceneManager.LoadScene(sceneName);
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePressed);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryPressed);
        }
    }
}