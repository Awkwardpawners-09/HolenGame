using UnityEngine;

/// <summary>
/// Attach this to your Arcade Mode UI panel GameObject.
/// Saves open/closed state to PlayerPrefs whenever it changes.
/// The RESTORING of state is handled by ArcadeUIBootstrapper (on a separate always-active GameObject).
/// </summary>
public class ArcadeUI : MonoBehaviour
{
    private const string PREFS_KEY = "ArcadeUIOpen";

    // Expose the key so the bootstrapper can read it
    public static string PrefsKey => PREFS_KEY;

    private void OnEnable()
    {
        PlayerPrefs.SetInt(PREFS_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("[ArcadeUI] Panel opened — state saved.");
    }

    private void OnDisable()
    {
        if (!isQuitting)
        {
            PlayerPrefs.SetInt(PREFS_KEY, 0);
            PlayerPrefs.Save();
            Debug.Log("[ArcadeUI] Panel closed — state saved.");
        }
    }

    public void ClearSavedState()
    {
        PlayerPrefs.DeleteKey(PREFS_KEY);
        PlayerPrefs.Save();
    }

    private static bool isQuitting = false;
    private void OnApplicationQuit() => isQuitting = true;
}