using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to the root of your "Select as Avatar" UI panel.
///
/// HOW TO SET UP:
/// 1. Assign each avatar Button from your UI into the avatarButtons array (Inspector).
/// 2. Each button's Image component should already have its avatar Sprite set as the Source Image —
///    this script reads those sprites automatically.
/// 3. Assign the same sprites to PlayerDataManager's avatarSprites[] IN THE SAME ORDER.
///    (Or use the "Auto-sync from buttons" approach — see note below.)
/// 4. Optionally assign selectedHighlight to a border/glow Image that moves to the active button.
///
/// The buttons don't need OnClick set in the Inspector — this script wires them up automatically.
/// </summary>
public class AvatarSelector : MonoBehaviour
{
    [Header("Avatar Buttons (in order, matching PlayerDataManager.avatarSprites[])")]
    [Tooltip("Drag your avatar buttons here in the same order as avatarSprites in PlayerDataManager.")]
    public Button[] avatarButtons;

    [Header("Selection Highlight (optional)")]
    [Tooltip("An Image (e.g. a glow/border) that will be repositioned to sit on the selected button.")]
    public Image selectedHighlight;

    private void Start()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[AvatarSelector] PlayerDataManager.Instance is null. Make sure it exists in the scene.");
            return;
        }

        if (avatarButtons == null || avatarButtons.Length == 0)
        {
            Debug.LogWarning("[AvatarSelector] No avatar buttons assigned.");
            return;
        }

        // Wire up each button
        for (int i = 0; i < avatarButtons.Length; i++)
        {
            int capturedIndex = i; // capture for closure
            avatarButtons[i].onClick.AddListener(() => OnAvatarButtonClicked(capturedIndex));
        }

        // Highlight the currently saved avatar
        ApplyHighlight(PlayerDataManager.Instance.playerData.selectedAvatarIndex);
    }

    private void OnAvatarButtonClicked(int index)
    {
        PlayerDataManager.Instance.SetAvatar(index);
        ApplyHighlight(index);
        Debug.Log($"[AvatarSelector] Avatar button {index} clicked.");
    }

    private void ApplyHighlight(int index)
    {
        if (selectedHighlight == null) return;
        if (avatarButtons == null || index < 0 || index >= avatarButtons.Length) return;

        // Move the highlight to sit on the selected button
        selectedHighlight.transform.SetParent(avatarButtons[index].transform, false);
        selectedHighlight.transform.SetAsFirstSibling(); // behind button content
        selectedHighlight.rectTransform.anchoredPosition = Vector2.zero;
        selectedHighlight.gameObject.SetActive(true);
    }
}