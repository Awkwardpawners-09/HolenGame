using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to any Image GameObject (e.g. the big profile picture in your HUD or Profile screen)
/// and it will always show the player's currently selected avatar — including across scene loads.
///
/// HOW TO USE:
/// - Attach to an Image component on any GameObject in any scene.
/// - No extra setup needed; it auto-registers with PlayerDataManager.
///
/// To get the current avatar sprite from another script:
///     Sprite avatar = PlayerDataManager.Instance.GetCurrentAvatarSprite();
/// </summary>
[RequireComponent(typeof(Image))]
public class PlayerAvatarDisplay : MonoBehaviour
{
    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        // Subscribe to future avatar changes
        PlayerDataManager.OnAvatarChanged += OnAvatarChanged;

        // Apply immediately if manager is already alive
        if (PlayerDataManager.Instance != null)
        {
            Sprite current = PlayerDataManager.Instance.GetCurrentAvatarSprite();
            if (current != null) _image.sprite = current;

            PlayerDataManager.Instance.RegisterAvatarDisplay(_image);
        }
    }

    private void OnDisable()
    {
        PlayerDataManager.OnAvatarChanged -= OnAvatarChanged;

        if (PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.UnregisterAvatarDisplay(_image);
    }

    private void OnAvatarChanged(Sprite newSprite)
    {
        if (newSprite != null) _image.sprite = newSprite;
    }
}