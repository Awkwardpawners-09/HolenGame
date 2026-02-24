using UnityEngine;

/// <summary>
/// Restores the Arcade UI panel's open/closed state when the menu scene loads.
///
/// HOW TO SET UP:
/// 1. Create a new empty GameObject in your Menu scene (e.g. name it "ArcadeUIBootstrapper").
///    Make sure it is ALWAYS ACTIVE in the Hierarchy — never disable this object.
/// 2. Add this component to it.
/// 3. Drag your Arcade UI panel GameObject into the "Arcade UI Panel" field in the Inspector.
/// 4. Done. This reads the saved state and shows/hides the panel before the first frame.
/// </summary>
public class ArcadeUIBootstrapper : MonoBehaviour
{
    [Tooltip("Drag your Arcade Mode UI panel GameObject here.")]
    public GameObject arcadeUIPanel;

    private void Awake()
    {
        if (arcadeUIPanel == null)
        {
            Debug.LogError("[ArcadeUIBootstrapper] Arcade UI Panel is not assigned! Please assign it in the Inspector.");
            return;
        }

        bool wasOpen = PlayerPrefs.GetInt(ArcadeUI.PrefsKey, 0) == 1;
        arcadeUIPanel.SetActive(wasOpen);

        Debug.Log($"[ArcadeUIBootstrapper] Restored Arcade UI state → {(wasOpen ? "OPEN" : "CLOSED")}");
    }
}