using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Helper component automatically added to each field zone GameObject at runtime.
/// Forwards OnTriggerEnter/Exit events to LevelManagerMultiple.
/// You do NOT need to add this manually.
/// </summary>
public class FieldZoneTrigger : MonoBehaviour
{
    [HideInInspector] public LevelManagerMultiple manager;

    void OnTriggerEnter(Collider other)
    {
        if (manager != null)
            manager.OnFieldZoneEnter(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (manager != null)
            manager.OnFieldZoneExit(other);
    }
}

/// <summary>
/// Drop-in replacement for LevelManager that supports multiple field zone colliders.
/// Assign any number of GameObjects (each with a Trigger Collider) to fieldZoneObjects.
/// A holen is considered "in the field" as long as it overlaps AT LEAST ONE of those zones.
/// The level is only completed once ALL holens have left every zone.
/// </summary>
public class LevelManagerMultiple : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Lives System
    // ─────────────────────────────────────────────

    [Header("Lives System")]
    [Tooltip("Maximum number of lives")]
    public int maxLives = 3;

    [Tooltip("TextMeshPro for displaying current lives")]
    public TextMeshProUGUI livesText;

    [Tooltip("Sound effect to play when a life is lost")]
    public AudioClip lifeLostSound;

    [Tooltip("AudioSource to play sounds (optional - will use/create one if null)")]
    public AudioSource audioSource;

    private int currentLives;

    // ─────────────────────────────────────────────
    //  Objective / Field Settings
    // ─────────────────────────────────────────────

    [Header("Objective Settings")]
    [Tooltip("Check objectives you want to monitor")]
    public bool checkNoHolensInField = true;

    [Tooltip("Time to wait when no holens are in ANY field zone before triggering level complete (seconds)")]
    public float waitTime = 5f;

    [Header("Play Field Zones")]
    [Tooltip("Assign one or more GameObjects that each have a Trigger Collider. " +
             "A holen counts as 'in the field' while it overlaps at least one of these. " +
             "FieldZoneTrigger will be added to each automatically at runtime.")]
    public GameObject[] fieldZoneObjects;

    // Tracks how many field zones each holen is currently inside.
    // While count > 0, the holen is still considered "in the field".
    private Dictionary<GameObject, int> holenZoneCounts = new Dictionary<GameObject, int>();

    private float noHolenTimer = 0f;
    private bool levelCompleted = false;
    private bool gameOverTriggered = false;

    // ─────────────────────────────────────────────
    //  Level Complete
    // ─────────────────────────────────────────────

    [Header("Level Complete (All Holens Cleared)")]
    [Tooltip("GameObjects to enable when all holens are cleared")]
    public GameObject[] levelCompleteObjects;

    [Tooltip("Delay before enabling level complete objects (seconds)")]
    public float levelCompleteDelay = 3f;

    // ─────────────────────────────────────────────
    //  Game Over
    // ─────────────────────────────────────────────

    [Header("Game Over (No Lives Remaining)")]
    [Tooltip("GameObjects to enable when player loses all lives")]
    public GameObject[] gameOverObjects;

    [Tooltip("Delay before enabling game over objects (seconds)")]
    public float gameOverDelay = 3f;

    // ─────────────────────────────────────────────
    //  Turn Feedback
    // ─────────────────────────────────────────────

    [Header("Turn Feedback (Launch Result)")]
    [Tooltip("Enabled briefly when the player launches but knocks out NO holens.")]
    public GameObject feedbackNoKnockout;

    [Tooltip("Enabled briefly when exactly 1 holen is knocked out of the field.")]
    public GameObject feedback1Knockout;

    [Tooltip("Enabled briefly when exactly 2 holens are knocked out of the field.")]
    public GameObject feedback2Knockout;

    [Tooltip("Enabled briefly when exactly 3 holens are knocked out of the field.")]
    public GameObject feedback3Knockout;

    [Tooltip("Enabled briefly when exactly 4 holens are knocked out of the field.")]
    public GameObject feedback4Knockout;

    [Tooltip("Enabled briefly when 5 or more holens are knocked out of the field.")]
    public GameObject feedback5Knockout;

    [Tooltip("How long (seconds) the feedback object stays visible before being disabled again.")]
    public float feedbackDisplayDuration = 4f;

    private Coroutine activeFeedbackCoroutine;
    private bool turnInProgress = false;
    private int holensKnockedOutThisTurn = 0;

    // Holens confirmed knocked out (left ALL zones) during the current turn window.
    // Stored as a set so late-arriving exit events after turnInProgress = false still count.
    private HashSet<GameObject> knockedOutThisTurn = new HashSet<GameObject>();

    // ─────────────────────────────────────────────
    //  Scene Management
    // ─────────────────────────────────────────────

    [Header("Scene Management (Optional)")]
    [Tooltip("Load a scene after level complete? Leave empty to disable")]
    public string levelCompleteSceneName = "";

    [Tooltip("Load a scene after game over? Leave empty to disable")]
    public string gameOverSceneName = "";

    [Tooltip("Enable transition effect before scene change")]
    public GameObject transitionObject;

    [Tooltip("Transition duration before loading scene (seconds)")]
    public float transitionDuration = 2f;

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    /// <summary>Number of holens that are currently overlapping at least one field zone.</summary>
    private int HolensInFieldCount
    {
        get
        {
            int count = 0;
            foreach (var kv in holenZoneCounts)
                if (kv.Value > 0) count++;
            return count;
        }
    }

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        currentLives = maxLives;
        UpdateLivesText();

        // Setup audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Register this manager on every field zone GameObject
        if (fieldZoneObjects != null)
        {
            foreach (GameObject zone in fieldZoneObjects)
            {
                if (zone == null) continue;

                Collider col = zone.GetComponent<Collider>();
                if (col == null)
                {
                    Debug.LogWarning($"[LevelManagerMultiple] '{zone.name}' has no Collider — skipping. " +
                                     "Add a Collider and enable Is Trigger.");
                    continue;
                }

                if (!col.isTrigger)
                {
                    Debug.LogWarning($"[LevelManagerMultiple] Collider on '{zone.name}' was not a trigger. " +
                                     "Setting isTrigger = true automatically.");
                    col.isTrigger = true;
                }

                FieldZoneTrigger fzt = zone.GetComponent<FieldZoneTrigger>();
                if (fzt == null)
                    fzt = zone.AddComponent<FieldZoneTrigger>();

                fzt.manager = this;
            }
        }

        // Disable completion / game-over objects
        SetObjectsActive(levelCompleteObjects, false);
        SetObjectsActive(gameOverObjects, false);

        if (transitionObject != null)
            transitionObject.SetActive(false);

        // Disable feedback objects
        DisableFeedbackObject(feedbackNoKnockout);
        DisableFeedbackObject(feedback1Knockout);
        DisableFeedbackObject(feedback2Knockout);
        DisableFeedbackObject(feedback3Knockout);
        DisableFeedbackObject(feedback4Knockout);
        DisableFeedbackObject(feedback5Knockout);
    }

    void Update()
    {
        if (gameOverTriggered || levelCompleted)
            return;

        if (currentLives <= 0)
        {
            TriggerGameOver();
            return;
        }

        if (checkNoHolensInField)
        {
            // Remove dictionary entries for any holens that were destroyed
            CleanDestroyedHolens();

            if (HolensInFieldCount == 0)
            {
                noHolenTimer += Time.deltaTime;

                if (noHolenTimer >= waitTime)
                {
                    Debug.Log("[LevelManagerMultiple] No holens in any field zone for " +
                              waitTime + " seconds. Level Complete!");
                    TriggerLevelComplete();
                }
            }
            else
            {
                noHolenTimer = 0f;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Field Zone Callbacks (called by FieldZoneTrigger)
    // ─────────────────────────────────────────────

    /// <summary>Called by FieldZoneTrigger when a collider enters one of the assigned field zones.</summary>
    public void OnFieldZoneEnter(Collider other)
    {
        if (!checkNoHolensInField) return;
        if (!other.CompareTag("Objective")) return;

        GameObject holen = other.gameObject;

        if (!holenZoneCounts.ContainsKey(holen))
            holenZoneCounts[holen] = 0;

        holenZoneCounts[holen]++;
        Debug.Log($"[LevelManagerMultiple] Holen entered a zone: {holen.name} " +
                  $"(zones occupied: {holenZoneCounts[holen]}, holens in field: {HolensInFieldCount})");
    }

    /// <summary>Called by FieldZoneTrigger when a collider exits one of the assigned field zones.</summary>
    public void OnFieldZoneExit(Collider other)
    {
        if (!checkNoHolensInField) return;
        if (!other.CompareTag("Objective")) return;

        GameObject holen = other.gameObject;

        if (!holenZoneCounts.ContainsKey(holen))
            return;

        holenZoneCounts[holen] = Mathf.Max(0, holenZoneCounts[holen] - 1);

        if (holenZoneCounts[holen] == 0)
        {
            // Holen has left every field zone — it is truly knocked out
            holenZoneCounts.Remove(holen);
            Debug.Log($"[LevelManagerMultiple] Holen fully left all field zones: {holen.name} " +
                      $"(holens in field: {HolensInFieldCount})");

            // Record the knockout regardless of whether the turn is still flagged as in-progress.
            // Physics exit events can fire a frame after turnInProgress is cleared, so we
            // collect here and evaluate the final count in OnTurnEnded.
            if (turnInProgress)
            {
                knockedOutThisTurn.Add(holen);
                Debug.Log($"[LevelManagerMultiple] Knockout recorded this turn: {holen.name} " +
                          $"(running total: {knockedOutThisTurn.Count})");
            }
        }
        else
        {
            Debug.Log($"[LevelManagerMultiple] Holen left one zone but is still in another: {holen.name} " +
                      $"(zones still occupied: {holenZoneCounts[holen]})");
        }
    }

    // ─────────────────────────────────────────────
    //  Internal Helpers
    // ─────────────────────────────────────────────

    private void CleanDestroyedHolens()
    {
        List<GameObject> dead = null;
        foreach (var kv in holenZoneCounts)
        {
            if (kv.Key == null)
            {
                if (dead == null) dead = new List<GameObject>();
                dead.Add(kv.Key);
            }
        }
        if (dead != null)
            foreach (var key in dead)
                holenZoneCounts.Remove(key);
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        foreach (GameObject obj in objects)
            if (obj != null) obj.SetActive(active);
    }

    // ─────────────────────────────────────────────
    //  Lives
    // ─────────────────────────────────────────────

    private void ReduceLife()
    {
        currentLives--;
        UpdateLivesText();
        Debug.Log("[LevelManagerMultiple] Life reduced. Current lives: " + currentLives);

        if (lifeLostSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(lifeLostSound);
            Debug.Log("[LevelManagerMultiple] Playing life lost sound effect");
        }

        if (currentLives <= 0)
            TriggerGameOver();
    }

    private void UpdateLivesText()
    {
        if (livesText != null)
            livesText.text = currentLives.ToString();
    }

    // ─────────────────────────────────────────────
    //  Level Complete / Game Over
    // ─────────────────────────────────────────────

    private void TriggerLevelComplete()
    {
        if (levelCompleted || gameOverTriggered) return;
        levelCompleted = true;
        Debug.Log("[LevelManagerMultiple] Level Complete triggered!");
        StartCoroutine(HandleLevelComplete());
    }

    private void TriggerGameOver()
    {
        if (gameOverTriggered || levelCompleted) return;
        gameOverTriggered = true;
        Debug.Log("[LevelManagerMultiple] Game Over triggered!");
        StartCoroutine(HandleGameOver());
    }

    private IEnumerator HandleLevelComplete()
    {
        yield return new WaitForSeconds(levelCompleteDelay);
        SetObjectsActive(levelCompleteObjects, true);

        if (!string.IsNullOrEmpty(levelCompleteSceneName))
        {
            if (transitionObject != null) transitionObject.SetActive(true);
            yield return new WaitForSeconds(transitionDuration);
            Debug.Log("[LevelManagerMultiple] Loading level complete scene: " + levelCompleteSceneName);
            SceneManager.LoadScene(levelCompleteSceneName);
        }
    }

    private IEnumerator HandleGameOver()
    {
        yield return new WaitForSeconds(gameOverDelay);
        SetObjectsActive(gameOverObjects, true);

        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            if (transitionObject != null) transitionObject.SetActive(true);
            yield return new WaitForSeconds(transitionDuration);
            Debug.Log("[LevelManagerMultiple] Loading game over scene: " + gameOverSceneName);
            SceneManager.LoadScene(gameOverSceneName);
        }
    }

    // ─────────────────────────────────────────────
    //  Public API  (same interface as original LevelManager)
    // ─────────────────────────────────────────────

    public void ManualLevelComplete() => TriggerLevelComplete();
    public void ManualGameOver() => TriggerGameOver();

    public void AddLife(int amount = 1)
    {
        currentLives += amount;
        UpdateLivesText();
        Debug.Log("[LevelManagerMultiple] Lives added. Current lives: " + currentLives);
    }

    public int GetCurrentLives() => currentLives;

    public void OnHolenRespawn()
    {
        if (gameOverTriggered || levelCompleted) return;
        ReduceLife();
        Debug.Log("[LevelManagerMultiple] Life deducted due to turn end");
    }

    // ─────────────────────────────────────────────
    //  Turn Feedback System
    // ─────────────────────────────────────────────

    /// <summary>Returns how many holens are currently in any field zone.</summary>
    public int GetHolensInFieldCount() => HolensInFieldCount;

    public void OnTurnStarted()
    {
        turnInProgress = true;
        holensKnockedOutThisTurn = 0;
        knockedOutThisTurn.Clear();
        Debug.Log("[LevelManagerMultiple] Turn started — tracking knockouts.");
    }

    public void OnTurnEnded()
    {
        turnInProgress = false;
        holensKnockedOutThisTurn = knockedOutThisTurn.Count;
        Debug.Log($"[LevelManagerMultiple] Turn ended — total knockouts: {holensKnockedOutThisTurn}");

        // Always show feedback here at turn end, based on the final confirmed knockout count
        ShowTurnFeedback(holensKnockedOutThisTurn, false);
    }

    public void ShowTurnFeedback(int knockedOut, bool showAfterRespawn = false)
    {
        if (gameOverTriggered || levelCompleted) return;

        GameObject target = GetFeedbackObject(knockedOut);
        if (target == null)
        {
            Debug.Log($"[LevelManagerMultiple] No feedback object assigned for {knockedOut} knockout(s).");
            return;
        }

        if (activeFeedbackCoroutine != null)
            StopCoroutine(activeFeedbackCoroutine);

        activeFeedbackCoroutine = StartCoroutine(DisplayFeedback(target));
    }

    private GameObject GetFeedbackObject(int knockedOut)
    {
        switch (knockedOut)
        {
            case 0: return feedbackNoKnockout;
            case 1: return feedback1Knockout;
            case 2: return feedback2Knockout;
            case 3: return feedback3Knockout;
            case 4: return feedback4Knockout;
            default: return feedback5Knockout; // 5+
        }
    }

    private IEnumerator DisplayFeedback(GameObject feedbackObj)
    {
        DisableFeedbackObject(feedbackNoKnockout);
        DisableFeedbackObject(feedback1Knockout);
        DisableFeedbackObject(feedback2Knockout);
        DisableFeedbackObject(feedback3Knockout);
        DisableFeedbackObject(feedback4Knockout);
        DisableFeedbackObject(feedback5Knockout);

        feedbackObj.SetActive(true);
        Debug.Log($"[LevelManagerMultiple] Feedback shown: {feedbackObj.name}");

        yield return new WaitForSeconds(feedbackDisplayDuration);

        DisableFeedbackObject(feedbackObj);
        Debug.Log($"[LevelManagerMultiple] Feedback hidden: {feedbackObj.name}");
        activeFeedbackCoroutine = null;
    }

    private void DisableFeedbackObject(GameObject obj)
    {
        if (obj != null && obj.activeSelf)
            obj.SetActive(false);
    }
}