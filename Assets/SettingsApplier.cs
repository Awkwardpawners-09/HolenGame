using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Applies saved settings to Unity's actual systems (URP).
///
/// HOW TO SET UP:
/// ─────────────────────────────────────────────────────────────────────
/// 1. Add this script to the same GameObject as PlayerDataManager
///    (so it persists across scenes with DontDestroyOnLoad).
///
/// 2. Assign the URP Global Volume in the Inspector:
///    • globalVolume → your scene's Global Volume GameObject
///      If left empty, it will be found automatically.
///
/// GRAPHICS QUALITY TIERS (applied directly — no Quality Settings setup needed):
///
///   LOW    Render Scale: 0.5 | No MSAA | Textures: Quarter res
///          No HDR | No soft particles | Shadow distance: 20 | LOD bias: 0.3
///
///   MEDIUM Render Scale: 0.75 | 2x MSAA | Textures: Half res
///          HDR on | Soft particles on | Shadow distance: 50 | LOD bias: 0.7
///
///   HIGH   Render Scale: 1.0 | 4x MSAA | Textures: Full res
///          HDR on | Soft particles on | Shadow distance: 100 | LOD bias: 1.5
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class SettingsApplier : MonoBehaviour
{
    [Header("URP Post Processing")]
    [Tooltip("Assign your Global Volume here. If left empty, it will be found automatically.")]
    public Volume globalVolume;

    // Tracks the current shadow toggle state so graphics quality changes
    // can correctly restore/skip shadow distance.
    private bool _shadowsEnabled = true;

    private void Start()
    {
        if (globalVolume == null)
            globalVolume = FindObjectOfType<Volume>();

        ApplyAllSettings();
    }

    private void OnEnable()
    {
        PlayerDataManager.OnSoundSettingChanged += ApplySound;
        PlayerDataManager.OnGraphicsQualityChanged += ApplyGraphicsQuality;
        PlayerDataManager.OnShadowsSettingChanged += ApplyShadows;
        PlayerDataManager.OnPostProcessingSettingChanged += ApplyPostProcessing;
    }

    private void OnDisable()
    {
        PlayerDataManager.OnSoundSettingChanged -= ApplySound;
        PlayerDataManager.OnGraphicsQualityChanged -= ApplyGraphicsQuality;
        PlayerDataManager.OnShadowsSettingChanged -= ApplyShadows;
        PlayerDataManager.OnPostProcessingSettingChanged -= ApplyPostProcessing;
    }

    // ─────────────────────────────────────────────────────────────────
    // APPLY ALL
    // ─────────────────────────────────────────────────────────────────

    private void ApplyAllSettings()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("[SettingsApplier] PlayerDataManager not ready yet.");
            return;
        }

        _shadowsEnabled = PlayerDataManager.Instance.IsShadowsEnabled();

        ApplySound(PlayerDataManager.Instance.IsSoundEnabled());
        ApplyGraphicsQuality(PlayerDataManager.Instance.GetGraphicsQuality());
        ApplyShadows(_shadowsEnabled);
        ApplyPostProcessing(PlayerDataManager.Instance.IsPostProcessingEnabled());
    }

    // ─────────────────────────────────────────────────────────────────
    // SOUND — mutes/unmutes all audio globally
    // ─────────────────────────────────────────────────────────────────

    private void ApplySound(bool enabled)
    {
        AudioListener.volume = enabled ? 1f : 0f;
        Debug.Log($"[SettingsApplier] Sound → {(enabled ? "ON" : "OFF")}");
    }

    // ─────────────────────────────────────────────────────────────────
    // GRAPHICS QUALITY
    // Directly sets URP + Unity render settings per tier.
    // 0 = Low | 1 = Medium | 2 = High
    // ─────────────────────────────────────────────────────────────────

    private void ApplyGraphicsQuality(int quality)
    {
        UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        if (urpAsset == null)
        {
            Debug.LogWarning("[SettingsApplier] Could not find URP Asset.");
            return;
        }

        switch (quality)
        {
            case 0: // ── LOW ──────────────────────────────────────────
                urpAsset.renderScale = 0.5f;   // Half screen resolution
                urpAsset.msaaSampleCount = 1;      // No anti-aliasing
                urpAsset.supportsHDR = false;  // No HDR lighting
                urpAsset.shadowDistance = _shadowsEnabled ? 20f : 0f;
                QualitySettings.globalTextureMipmapLimit = 2;   // Quarter texture resolution
                QualitySettings.lodBias = 0.3f;  // Aggressively pop in lower LODs
                QualitySettings.maximumLODLevel = 1;     // Skip highest detail LOD
                Debug.Log("[SettingsApplier] Graphics → LOW");
                break;

            case 1: // ── MEDIUM ───────────────────────────────────────
                urpAsset.renderScale = 0.75f;
                urpAsset.msaaSampleCount = 2;      // 2x MSAA
                urpAsset.supportsHDR = true;
                urpAsset.shadowDistance = _shadowsEnabled ? 50f : 0f;
                QualitySettings.globalTextureMipmapLimit = 1;   // Half texture resolution
                QualitySettings.lodBias = 0.7f;
                QualitySettings.maximumLODLevel = 0;
                Debug.Log("[SettingsApplier] Graphics → MEDIUM");
                break;

            case 2: // ── HIGH ─────────────────────────────────────────
                urpAsset.renderScale = 1.0f;  // Full native resolution
                urpAsset.msaaSampleCount = 4;     // 4x MSAA
                urpAsset.supportsHDR = true;
                urpAsset.shadowDistance = _shadowsEnabled ? 100f : 0f;
                QualitySettings.globalTextureMipmapLimit = 0;  // Full texture resolution
                QualitySettings.lodBias = 1.5f; // Always use highest detail LOD
                QualitySettings.maximumLODLevel = 0;
                Debug.Log("[SettingsApplier] Graphics → HIGH");
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // SHADOWS — toggles via URP shadow distance
    // Respects the current graphics quality tier for the distance value.
    // ─────────────────────────────────────────────────────────────────

    private void ApplyShadows(bool enabled)
    {
        _shadowsEnabled = enabled;

        UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null)
        {
            Debug.LogWarning("[SettingsApplier] Could not find URP Asset.");
            return;
        }

        if (enabled)
        {
            int quality = PlayerDataManager.Instance != null
                ? PlayerDataManager.Instance.GetGraphicsQuality()
                : 2;

            urpAsset.shadowDistance = quality == 0 ? 20f
                                    : quality == 1 ? 50f
                                    : 100f;
        }
        else
        {
            urpAsset.shadowDistance = 0f;
        }

        Debug.Log($"[SettingsApplier] Shadows → {(enabled ? $"ON (distance: {urpAsset.shadowDistance})" : "OFF")}");
    }

    // ─────────────────────────────────────────────────────────────────
    // POST PROCESSING (URP) — toggles Global Volume weight
    // ─────────────────────────────────────────────────────────────────

    private void ApplyPostProcessing(bool enabled)
    {
        if (globalVolume == null)
            globalVolume = FindObjectOfType<Volume>();

        if (globalVolume != null)
        {
            globalVolume.weight = enabled ? 1f : 0f;
            Debug.Log($"[SettingsApplier] Post Processing → {(enabled ? "ON" : "OFF")}");
        }
        else
        {
            Debug.LogWarning("[SettingsApplier] No Global Volume found. Post Processing setting had no effect.");
        }
    }
}