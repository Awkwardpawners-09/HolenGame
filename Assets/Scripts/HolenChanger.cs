using UnityEngine;
using UnityEngine.UI;

public class HolenChanger : MonoBehaviour
{
    public HolenData holen1Data;
    public HolenData holen2Data;
    public HolenData holen3Data;

    // UI image that shows which holen is currently selected
    public Image chosenHolenImage;

    // NOTE: chosenHolenPrefab has been removed.
    // The HolensLauncher is the single source of truth for spawning the holen in the scene.
    // Instantiating a separate preview object here was causing a second holen to appear.

    public HolensLauncher holensLauncher;

    private HolenData currentHolenData;

    public Button holen1Button;
    public Button holen2Button;
    public Button holen3Button;

    void Start()
    {
        currentHolenData = holen1Data;
        // Only update the UI on start – do NOT call ChangeBallPrefab here
        // because HolensLauncher.Start() already syncs via GetCurrentHolenData().
        UpdateHolenUI(currentHolenData);
    }

    // ─────────────────────────────────────────────
    //  BUTTON CALLBACKS
    // ─────────────────────────────────────────────
    public void ChangeHolen1()
    {
        if (!holensLauncher.GetIsBusy())
            TryChangeHolen(holen1Data);
    }

    public void ChangeHolen2()
    {
        if (!holensLauncher.GetIsBusy())
            TryChangeHolen(holen2Data);
    }

    public void ChangeHolen3()
    {
        if (!holensLauncher.GetIsBusy())
            TryChangeHolen(holen3Data);
    }

    // ─────────────────────────────────────────────
    //  INTERNAL HELPERS
    // ─────────────────────────────────────────────
    private void TryChangeHolen(HolenData holenData)
    {
        if (holenData == null) return;
        if (currentHolenData == holenData) return; // Already selected – nothing to do

        currentHolenData = holenData;
        UpdateHolenUI(holenData);

        // Tell the launcher to swap the ball.
        // ChangeBallPrefab() in the launcher destroys the current ball
        // and spawns exactly ONE new ball – no duplicates.
        if (holensLauncher != null)
            holensLauncher.ChangeBallPrefab(holenData.holenPrefab);
    }

    /// <summary>Updates only the UI image – does NOT spawn anything in the scene.</summary>
    private void UpdateHolenUI(HolenData holenData)
    {
        if (chosenHolenImage != null && holenData != null)
            chosenHolenImage.sprite = holenData.holenIcon;
    }

    // ─────────────────────────────────────────────
    //  BUTTON ENABLE / DISABLE  (called by launcher)
    // ─────────────────────────────────────────────
    public void DisableButtons()
    {
        if (holen1Button != null) holen1Button.interactable = false;
        if (holen2Button != null) holen2Button.interactable = false;
        if (holen3Button != null) holen3Button.interactable = false;
    }

    public void EnableButtons()
    {
        if (holen1Button != null) holen1Button.interactable = true;
        if (holen2Button != null) holen2Button.interactable = true;
        if (holen3Button != null) holen3Button.interactable = true;
    }

    // ─────────────────────────────────────────────
    //  GETTER
    // ─────────────────────────────────────────────
    public HolenData GetCurrentHolenData()
    {
        return currentHolenData;
    }

    // ─────────────────────────────────────────────
    //  SETTER  (called by HolensLauncherNew directly)
    // ─────────────────────────────────────────────
    /// <summary>
    /// Updates the selected holen and refreshes the UI icon.
    /// Does NOT spawn anything — the launcher handles spawning.
    /// Called by HolensLauncherNew when a holen select button is pressed.
    /// </summary>
    public void SetCurrentHolenDataDirect(HolenData holenData)
    {
        if (holenData == null) return;
        currentHolenData = holenData;
        UpdateHolenUI(holenData);
    }

}