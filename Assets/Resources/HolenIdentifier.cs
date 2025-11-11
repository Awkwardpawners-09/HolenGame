using UnityEngine;

/// <summary>
/// Attach this component to holen prefabs to identify which HolenData they represent.
/// This allows the game to track which specific holens are knocked out.
/// </summary>
public class HolenIdentifier : MonoBehaviour
{
    [Header("Holen Data Reference")]
    public HolenData holenData;

    /// <summary>
    /// Optional: Set the HolenData when spawning dynamically.
    /// </summary>
    public void SetHolenData(HolenData data)
    {
        holenData = data;
    }

    /// <summary>
    /// Get the HolenData associated with this holen.
    /// </summary>
    public HolenData GetHolenData()
    {
        return holenData;
    }
}