using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarbleGachaAnimated : MonoBehaviour
{
    public List<HolenData> marblePool; // ScriptableObjects
    private PlayerData playerData => PlayerDataManager.Instance.playerData;

    [Header("Inventory Reference")]
    public HolenInventoryManager inventoryManager;

    [Header("UI References")]
    public GameObject resultPanel;
    public GameObject resultBackground; // ADD THIS
    public Image marbleImage;
    public TextMeshProUGUI marbleNameText;
    public TextMeshProUGUI rarityText; // Optional
    public CoinUIManager coinUI;
    public Button pullButton; // The gacha pull button

    [Header("Animation Settings")]
    public GameObject animationPanel; // Panel that shows during animation
    public Image spinningImage; // Image that spins during pull
    public float spinDuration = 2f; // How long the spin lasts
    public float spinSpeed = 720f; // Degrees per second
    public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Particle Effects (Optional)")]
    public ParticleSystem pullParticles; // Plays during spin
    public ParticleSystem revealParticles; // Plays on reveal
    
    [Header("Sound Effects (Optional)")]
    public AudioSource pullSound;
    public AudioSource revealSound;
    public AudioSource raritySound; // Different sound per rarity

    [Header("Rarity Colors")]
    public Color commonColor = Color.gray;
    public Color rareColor = Color.blue;
    public Color epicColor = Color.magenta;
    public Color legendaryColor = Color.yellow;

    private bool isPulling = false;

    void Start()
    {
        // Hide animation panel at start
        if (animationPanel != null)
            animationPanel.SetActive(false);
        
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    public void TryBuyMarbleBag()
    {
        if (isPulling) return; // Prevent double-clicks

        // Use coinUI if available so UI refreshes instantly
        bool success = (coinUI != null) ? coinUI.SpendCoins(100) : playerData.SpendCoins(100);

        if (success)
        {
            StartCoroutine(GachaPullAnimation());
        }
        else
        {
            Debug.Log("Not enough coins!");
            // Optional: Shake the button or show error message
            StartCoroutine(ShakeButton());
        }
    }

    IEnumerator GachaPullAnimation()
    {
        isPulling = true;

        if (resultBackground != null)
            resultBackground.SetActive(true);
        
        // Disable pull button during animation
        if (pullButton != null)
            pullButton.interactable = false;

        // Show animation panel
        if (animationPanel != null)
            animationPanel.SetActive(true);

        // Play pull sound
        if (pullSound != null)
            pullSound.Play();

        // Start particle effect
        if (pullParticles != null)
            pullParticles.Play();

        // Spinning animation
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / spinDuration;
            float curveValue = spinCurve.Evaluate(progress);
            
            if (spinningImage != null)
            {
                float angle = curveValue * spinSpeed * elapsed;
                spinningImage.transform.rotation = Quaternion.Euler(0, 0, -angle);
                
                // Optional: Pulse scale
                float scale = 1f + Mathf.Sin(elapsed * 10f) * 0.1f;
                spinningImage.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        // Stop pull particles
        if (pullParticles != null)
            pullParticles.Stop();

        // Get the awarded marble
        HolenData awardedMarble = GetRandomMarble();

        // Add to inventory
        inventoryManager.AddHolen(awardedMarble.holenID, 1);

        // Hide animation panel
        if (animationPanel != null)
            animationPanel.SetActive(false);

        // Small delay for dramatic effect
        yield return new WaitForSeconds(0.3f);

        // Play reveal sound
        if (revealSound != null)
            revealSound.Play();

        // Play reveal particles with rarity color
        if (revealParticles != null)
        {
            var main = revealParticles.main;
            main.startColor = GetRarityColor(awardedMarble.rarity);
            revealParticles.Play();
        }

        // Show result
        ShowMarbleResult(awardedMarble);

        // Reset spinning image
        if (spinningImage != null)
        {
            spinningImage.transform.rotation = Quaternion.identity;
            spinningImage.transform.localScale = Vector3.one;
        }

        // Re-enable pull button
        if (pullButton != null)
            pullButton.interactable = true;

        isPulling = false;

        Debug.Log($"🎉 Gacha awarded: {awardedMarble.holenName}");
    }

    void ShowMarbleResult(HolenData marble)
    {
        if (resultBackground != null)
            resultBackground.SetActive(true);
        
        resultPanel.SetActive(true);
        
        marbleNameText.text = marble.holenName;
        marbleImage.sprite = marble.holenIcon;
        
        // Optional: Show rarity
        if (rarityText != null)
        {
            rarityText.text = marble.rarity;
            rarityText.color = GetRarityColor(marble.rarity);
        }

        // Optional: Color the marble image border by rarity
        //marbleImage.color = GetRarityColor(marble.rarity);

        // Animate result panel entrance
        StartCoroutine(AnimateResultPanel());
    }

    IEnumerator AnimateResultPanel()
    {
        // Scale up animation
        resultPanel.transform.localScale = Vector3.zero;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float scale = Mathf.Lerp(0, 1, progress);
            resultPanel.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        resultPanel.transform.localScale = Vector3.one;
    }

    Color GetRarityColor(string rarity)
    {
        switch (rarity.ToLower())
        {
            case "common": return commonColor;
            case "rare": return rareColor;
            case "epic": return epicColor;
            case "legendary": return legendaryColor;
            default: return Color.white;
        }
    }

    HolenData GetRandomMarble()
    {
        // You can add weighted randomness here for rarities
        int randomIndex = Random.Range(0, marblePool.Count);
        return marblePool[randomIndex];
    }

    public void CloseResultPanel()
    {
        StartCoroutine(CloseResultPanelAnimated());
    }

IEnumerator CloseResultPanelAnimated()
{
    // Scale down animation
    float elapsed = 0f;
    float duration = 0.3f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / duration;
        float scale = Mathf.Lerp(1, 0, progress);
        resultPanel.transform.localScale = Vector3.one * scale;
        yield return null;
    }

    resultPanel.SetActive(false);
    
    // Hide background too (ADD THIS)
    if (resultBackground != null)
        resultBackground.SetActive(false);
    
    resultPanel.transform.localScale = Vector3.one;
}

    IEnumerator ShakeButton()
    {
        if (pullButton == null) yield break;

        Vector3 originalPos = pullButton.transform.localPosition;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-10f, 10f);
            float y = Random.Range(-10f, 10f);
            pullButton.transform.localPosition = originalPos + new Vector3(x, y, 0);
            yield return null;
        }

        pullButton.transform.localPosition = originalPos;
    }
}

// =============================================================================
// Optional: Weighted Gacha System for Rarity
// =============================================================================
public class WeightedGachaSystem : MonoBehaviour
{
    [System.Serializable]
    public class RarityWeight
    {
        public string rarity;
        public float weight; // Higher = more common
    }

    public List<RarityWeight> rarityWeights = new List<RarityWeight>()
    {
        new RarityWeight { rarity = "Common", weight = 70f },
        new RarityWeight { rarity = "Uncommon", weight = 50f },
        new RarityWeight { rarity = "Rare", weight = 20f },
        new RarityWeight { rarity = "Epic", weight = 8f },
        new RarityWeight { rarity = "Legendary", weight = 2f }
    };

    public HolenData GetWeightedRandomMarble(List<HolenData> marblePool)
    {
        // Pick a rarity based on weights
        string chosenRarity = GetRandomRarity();

        // Filter marbles by that rarity
        var filtered = marblePool.FindAll(m => m.rarity == chosenRarity);

        // If no marbles of that rarity, pick any random
        if (filtered.Count == 0)
        {
            return marblePool[Random.Range(0, marblePool.Count)];
        }

        return filtered[Random.Range(0, filtered.Count)];
    }

    string GetRandomRarity()
    {
        float totalWeight = 0f;
        foreach (var r in rarityWeights)
            totalWeight += r.weight;

        float randomValue = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var r in rarityWeights)
        {
            cumulative += r.weight;
            if (randomValue <= cumulative)
                return r.rarity;
        }

        return "Common"; // fallback
    }
}