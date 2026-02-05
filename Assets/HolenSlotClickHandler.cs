using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to each HolenSlotUI prefab (or have HolenSlotUI call it directly).
/// It listens for a tap on the slot and forwards the selection to
/// MultiplayerHolenController.OnHolenSelectedFromInventory().
/// 
/// Setup: Add a Button component to the slot's root (or an overlay transparent Button)
/// and assign it to the 'slotButton' field. The prefab reference that should be
/// launched is stored per-slot — assign it in HolenSlotUI.SetSlot() or here via
/// SetHolenPrefab().
/// </summary>
public class HolenSlotClickHandler : MonoBehaviour
{
    [Tooltip("The Button on this slot that the player taps")]
    public Button slotButton;

    /// <summary>
    /// The networked prefab that will replace holenBallPrefab when this slot is tapped.
    /// Must be a prefab registered in Photon's PrefabPool / Resources.
    /// Assign this from HolenSlotUI.SetSlot() after you resolve the HolenData.
    /// </summary>
    public GameObject holenPrefab;

    private MultiplayerHolenController controller;

    private void Start()
    {
        controller = FindObjectOfType<MultiplayerHolenController>();

        if (slotButton != null)
            slotButton.onClick.AddListener(OnSlotTapped);
    }

    /// <summary>
    /// Call this from HolenSlotUI.SetSlot() so each slot knows which prefab it represents.
    /// </summary>
    public void SetHolenPrefab(GameObject prefab)
    {
        holenPrefab = prefab;
    }

    private void OnSlotTapped()
    {
        if (controller == null)
        {
            Debug.LogWarning("[HolenSlotClickHandler] MultiplayerHolenController not found in scene!");
            return;
        }

        if (holenPrefab == null)
        {
            Debug.LogWarning("[HolenSlotClickHandler] No holenPrefab assigned to this slot.");
            return;
        }

        controller.OnHolenSelectedFromInventory(holenPrefab);
    }
}