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
/// GRAPHICS QUALITY TIERS:
///
///   0  LOW       Render Scale: 0.5  | No MSAA  | Textures: Quarter | No HDR  | Shadows: 20
///   1  MEDIUM    Render Scale: 0.75 | 2x MSAA  | Textures: Half    | HDR on  | Shadows: 50
///   2  HIGH      Render Scale: 1.0  | 4x MSAA  | Textures: Full    | HDR on  | Shadows: 100  DEFAULT
///   3  VERY HIGH Render Scale: 1.0  | 4x MSAA  | Textures: Full    | HDR on  | Shadows: 150
///   4  ULTRA     Render Scale: 1.2  | 8x MSAA  | Textures: Full    | HDR on  | Shadows: 250
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class SettingsApplier : MonoBehaviour
{
    [Header("URP Post Processing")]
    [Tooltip("Assign your Global Volume here. If left empty, it will be found automatically.")]
    public Volume globalVolume;

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

    private void ApplySound(bool enabled)
    {
        AudioListener.volume = enabled ? 1f : 0f;
        Debug.Log($"[SettingsApplier] Sound -> {(enabled ? "ON" : "OFF")}");
    }

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
            case 0: // LOW
                urpAsset.renderScale = 0.5f;
                urpAsset.msaaSampleCount = 1;
                urpAsset.supportsHDR = false;
                urpAsset.shadowDistance = _shadowsEnabled ? 20f : 0f;
                QualitySettings.globalTextureMipmapLimit = 2;
                QualitySettings.lodBias = 0.3f;
                QualitySettings.maximumLODLevel = 1;
                Debug.Log("[SettingsApplier] Graphics -> LOW");
                break;

            case 1: // MEDIUM
                urpAsset.renderScale = 0.75f;
                urpAsset.msaaSampleCount = 2;
                urpAsset.supportsHDR = true;
                urpAsset.shadowDistance = _shadowsEnabled ? 50f : 0f;
                QualitySettings.globalTextureMipmapLimit = 1;
                QualitySettings.lodBias = 0.7f;
                QualitySettings.maximumLODLevel = 0;
                Debug.Log("[SettingsApplier] Graphics -> MEDIUM");
                break;

            case 2: // HIGH (default)
                urpAsset.renderScale = 1.0f;
                urpAsset.msaaSampleCount = 4;
                urpAsset.supportsHDR = true;
                urpAsset.shadowDistance = _shadowsEnabled ? 100f : 0f;
                QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.lodBias = 1.5f;
                QualitySettings.maximumLODLevel = 0;
                Debug.Log("[SettingsApplier] Graphics -> HIGH");
                break;

            case 3: // VERY HIGH
                urpAsset.renderScale = 1.0f;
                urpAsset.msaaSampleCount = 4;
                urpAsset.supportsHDR = true;
                urpAsset.shadowDistance = _shadowsEnabled ? 150f : 0f;
                QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.lodBias = 2.0f;
                QualitySettings.maximumLODLevel = 0;
                Debug.Log("[SettingsApplier] Graphics -> VERY HIGH");
                break;

            case 4: // ULTRA
                urpAsset.renderScale = 1.2f;
                urpAsset.msaaSampleCount = 8;
                urpAsset.supportsHDR = true;
                urpAsset.shadowDistance = _shadowsEnabled ? 250f : 0f;
                QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.lodBias = 3.0f;
                QualitySettings.maximumLODLevel = 0;
                Debug.Log("[SettingsApplier] Graphics -> ULTRA");
                break;
        }
    }

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
                                    : quality == 2 ? 100f
                                    : quality == 3 ? 150f
                                    : 250f;
        }
        else
        {
            urpAsset.shadowDistance = 0f;
        }

        Debug.Log($"[SettingsApplier] Shadows -> {(enabled ? $"ON (distance: {urpAsset.shadowDistance})" : "OFF")}");
    }

    private void ApplyPostProcessing(bool enabled)
    {
        if (globalVolume == null)
            globalVolume = FindObjectOfType<Volume>();

        if (globalVolume != null)
        {
            globalVolume.weight = enabled ? 1f : 0f;
            Debug.Log($"[SettingsApplier] Post Processing -> {(enabled ? "ON" : "OFF")}");
        }
        else
        {
            Debug.LogWarning("[SettingsApplier] No Global Volume found. Post Processing setting had no effect.");
        }
    }
}