using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Add this to your MarbleGachaAnimated class or create a separate script

public class SunrayRevealEffect : MonoBehaviour
{
    [Header("Sunray Settings")]
    public Image sunrayImage; // Your sunray/burst image
    public float rotationSpeed = 50f; // Degrees per second
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    public float displayDuration = 2f; // How long it stays visible

    [Header("Rarity Colors")]
    public Color commonColor = new Color(0.7f, 0.7f, 0.7f); // Gray
    public Color uncommonColor = new Color(0.145f, 0.588f, 0.745f, 1f); // Green
    public Color rareColor = new Color(0.3f, 0.5f, 1f); // Blue
    public Color epicColor = new Color(0.8f, 0.2f, 1f); // Purple
    public Color legendaryColor = new Color(1f, 0.8f, 0f); // Gold

    private Coroutine currentEffect;

    public void PlayRevealEffect(string rarity)
    {
        // Stop any existing effect
        if (currentEffect != null)
            StopCoroutine(currentEffect);

        // Set color based on rarity
        Color targetColor = GetRarityColor(rarity);
        
        // Start the effect
        currentEffect = StartCoroutine(SunrayEffectCoroutine(targetColor));
    }

    IEnumerator SunrayEffectCoroutine(Color color)
    {
        if (sunrayImage == null) yield break;

        // Reset rotation and alpha
        sunrayImage.transform.rotation = Quaternion.identity;
        sunrayImage.color = new Color(color.r, color.g, color.b, 0);
        sunrayImage.gameObject.SetActive(true);

        // Fade in while rotating
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, elapsed / fadeInDuration);
            sunrayImage.color = new Color(color.r, color.g, color.b, alpha);
            
            // Rotate
            sunrayImage.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            
            yield return null;
        }

        // Full brightness and continue rotating
        sunrayImage.color = color;
        elapsed = 0f;
        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            sunrayImage.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        // Fade out while still rotating
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, elapsed / fadeOutDuration);
            sunrayImage.color = new Color(color.r, color.g, color.b, alpha);
            
            // Rotate
            sunrayImage.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            
            yield return null;
        }

        // Hide
        sunrayImage.gameObject.SetActive(false);
    }

    Color GetRarityColor(string rarity)
    {
        switch (rarity.ToLower())
        {
            case "common": return commonColor;
            case "uncommon": return uncommonColor;
            case "rare": return rareColor;
            case "epic": return epicColor;
            case "legendary": return legendaryColor;
            default: return Color.white;
        }
    }
}

// =====================================================================
// UPDATE YOUR ShowMarbleResult() method to trigger this effect
// =====================================================================
/*

Add this field to your MarbleGachaAnimated class:

    [Header("Visual Effects")]
    public SunrayRevealEffect sunrayEffect; // Drag component here

Then update ShowMarbleResult():

void ShowMarbleResult(HolenData marble)
{
    if (resultBackground != null)
        resultBackground.SetActive(true);
    
    resultPanel.SetActive(true);
    
    marbleNameText.text = marble.holenName;
    marbleImage.sprite = marble.holenIcon;
    
    // Optional: Show rarity text
    if (rarityText != null)
    {
        rarityText.text = marble.rarity;
        rarityText.color = GetRarityColor(marble.rarity);
    }

    // ✨ PLAY SUNRAY EFFECT WITH RARITY COLOR
    if (sunrayEffect != null)
    {
        sunrayEffect.PlayRevealEffect(marble.rarity);
    }

    // Animate result panel entrance
    StartCoroutine(AnimateResultPanel());
}

*/