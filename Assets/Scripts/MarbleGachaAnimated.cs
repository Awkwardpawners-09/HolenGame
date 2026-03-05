using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarbleGachaAnimated : MonoBehaviour
{
    public List<HolenData> marblePool; // ScriptableObjects
    private PlayerData playerData => PlayerDataManager.Instance.playerData;

    [Header("Visual Effects")]
    public SunrayRevealEffect sunrayEffect;

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

    [Header("Multi-Pull Settings")]
    public Button pull5Button; // x5 pull button
    public int singlePullCost = 100;
    public int multiPullCost = 450; // Discounted! (normally 500)
    public GameObject multiResultPanel; // Special panel for showing 5 results
    public Transform[] marbleSlots;
    public Transform multiResultGrid; // Grid to hold the 5 marbles
    public GameObject multiResultSlotPrefab; // Prefab for each marble slot

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
        bool success = PlayerDataManager.Instance.SpendCoins(singlePullCost);

        if (success)
        {
            StartCoroutine(GachaPullAnimation());
        }
        else
        {
            Debug.Log("Not enough coins!");
            // Optional: Shake the button or show error message
            StartCoroutine(ShakeButton(pullButton));
        }
    }

    IEnumerator GachaPullAnimation()
{
    isPulling = true;

    if (resultBackground != null)
        resultBackground.SetActive(true);

    if (pullButton != null)
        pullButton.interactable = false;

    if (animationPanel != null)
        animationPanel.SetActive(true);

    if (pullSound != null)
        pullSound.Play();

    if (pullParticles != null)
        pullParticles.Play();

    float elapsed = 0f;
    Vector3 originalPos = spinningImage != null ? spinningImage.transform.localPosition : Vector3.zero;

    while (elapsed < spinDuration)
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / spinDuration;

        if (spinningImage != null)
        {
            float intensity = Mathf.Sin(progress * Mathf.PI) * 20f;
            float shakeSpeed = 30f;

            float x = Mathf.Sin(elapsed * shakeSpeed) * intensity;
            float y = Mathf.Cos(elapsed * shakeSpeed * 1.3f) * intensity;
            float rotation = Mathf.Sin(elapsed * shakeSpeed * 0.7f) * intensity * 0.5f;

            spinningImage.transform.localPosition = originalPos + new Vector3(x, y, 0);
            spinningImage.transform.rotation = Quaternion.Euler(0, 0, rotation);

            float scale = 1f + Mathf.Sin(elapsed * 15f) * 0.05f;
            spinningImage.transform.localScale = Vector3.one * scale;
        }

        yield return null;
    }

    if (spinningImage != null)
        spinningImage.transform.localPosition = originalPos;

    HolenData awardedMarble = GetRandomMarble();
    inventoryManager.AddHolen(awardedMarble.holenID, 1);

    if (animationPanel != null)
        animationPanel.SetActive(false);

    yield return new WaitForSeconds(0.3f);

    if (revealSound != null)
        revealSound.Play();

    if (revealParticles != null)
    {
        var main = revealParticles.main;
        main.startColor = GetRarityColor(awardedMarble.rarity);
        revealParticles.Play();
    }

    // Show result panel
    if (resultBackground != null)
        resultBackground.SetActive(true);

    resultPanel.SetActive(true);
    marbleNameText.text = awardedMarble.holenName;
    marbleImage.sprite = awardedMarble.holenIcon;

    if (rarityText != null)
    {
        rarityText.text = awardedMarble.rarity;
        rarityText.color = GetRarityColor(awardedMarble.rarity);
    }

    if (sunrayEffect != null)
        sunrayEffect.PlayRevealEffect(awardedMarble.rarity);

    // Scale up animation
    resultPanel.transform.localScale = Vector3.zero;
    float scaleElapsed = 0f;
    float scaleDuration = 0.5f;

    while (scaleElapsed < scaleDuration)
    {
        scaleElapsed += Time.deltaTime;
        resultPanel.transform.localScale = Vector3.one * Mathf.Lerp(0, 1, scaleElapsed / scaleDuration);
        yield return null;
    }

    resultPanel.transform.localScale = Vector3.one;

    // Wait before fading
    yield return new WaitForSeconds(1.5f);

    // Fade out result panel
    CanvasGroup cg = resultPanel.GetComponent<CanvasGroup>();
    if (cg == null) cg = resultPanel.AddComponent<CanvasGroup>();

    float fadeElapsed = 0f;
    float fadeDuration = 0.3f;

    while (fadeElapsed < fadeDuration)
    {
        fadeElapsed += Time.deltaTime;
        cg.alpha = Mathf.Lerp(1, 0, fadeElapsed / fadeDuration);
        yield return null;
    }

    resultPanel.SetActive(false);
    cg.alpha = 1f;

    if (resultBackground != null)
        resultBackground.SetActive(false);

    if (spinningImage != null)
    {
        spinningImage.transform.rotation = Quaternion.identity;
        spinningImage.transform.localScale = Vector3.one;
    }

    if (pullButton != null)
        pullButton.interactable = true;

    // ✅ Mark quest complete after marble is awarded
    PlayerDataManager.Instance.playerData.gacha1xQuestCompleted = true;
    PlayerDataManager.Instance.playerData.Save();
    foreach (var q in FindObjectsOfType<Gacha1xQuest>())
        q.RefreshUI();

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
    
    if (rarityText != null)
    {
        rarityText.text = marble.rarity;
        rarityText.color = GetRarityColor(marble.rarity);
    }

    // ✨ PLAY SUNRAY EFFECT
    if (sunrayEffect != null)
    {
        sunrayEffect.PlayRevealEffect(marble.rarity);
    }

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
    StopAllCoroutines();

    resultPanel.SetActive(false);
    CanvasGroup cg = resultPanel.GetComponent<CanvasGroup>();
    if (cg != null) cg.alpha = 1f;
    resultPanel.transform.localScale = Vector3.one;

    if (resultBackground != null)
        resultBackground.SetActive(false);

    if (animationPanel != null)
        animationPanel.SetActive(false);

    if (spinningImage != null)
    {
        spinningImage.transform.localPosition = Vector3.zero;
        spinningImage.transform.rotation = Quaternion.identity;
        spinningImage.transform.localScale = Vector3.one;
    }

    if (pullButton != null) pullButton.interactable = true;

    // ✅ Mark quest complete even if skipped
    PlayerDataManager.Instance.playerData.gacha1xQuestCompleted = true;
    PlayerDataManager.Instance.playerData.Save();
    foreach (var q in FindObjectsOfType<Gacha1xQuest>())
        q.RefreshUI();

    isPulling = false;
}

IEnumerator ShakeButton(Button button = null)
{
    // If no button specified, use the default pullButton
    if (button == null)
        button = pullButton;
    
    if (button == null) yield break;

    Vector3 originalPos = button.transform.localPosition;
    float elapsed = 0f;
    float duration = 0.5f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float x = Random.Range(-10f, 10f);
        float y = Random.Range(-10f, 10f);
        button.transform.localPosition = originalPos + new Vector3(x, y, 0);
        yield return null;
    }

    button.transform.localPosition = originalPos;
}


 public void TryBuyMultiPull()
    {
        if (isPulling) return;

        bool success = PlayerDataManager.Instance.SpendCoins(multiPullCost);

        if (success)
        {
            StartCoroutine(MultiPullAnimationAllAtOnce());
        }
        else
        {
            Debug.Log("Not enough coins for x5 pull!");
            StartCoroutine(ShakeButton(pull5Button));
        }
    }

IEnumerator MultiPullAnimationAllAtOnce()
{
    isPulling = true;

    if (pullButton != null) pullButton.interactable = false;
    if (pull5Button != null) pull5Button.interactable = false;

    if (animationPanel != null)
        animationPanel.SetActive(true);

    if (pullSound != null)
        pullSound.Play();

    if (pullParticles != null)
        pullParticles.Play();

    for (int i = 0; i < 5; i++)
    {
        yield return StartCoroutine(ShakeAnimation(0.4f));
        yield return new WaitForSeconds(0.1f);
    }

    if (pullParticles != null)
        pullParticles.Stop();

    List<HolenData> awardedMarbles = new List<HolenData>();
    for (int i = 0; i < 5; i++)
    {
        HolenData marble = GetRandomMarble();
        awardedMarbles.Add(marble);
        inventoryManager.AddHolen(marble.holenID, 1);
    }

            // ✅ Always mark quest complete, even if GachaQuest UI isn't visible
        PlayerDataManager.Instance.playerData.gachaQuestCompleted = true;
        PlayerDataManager.Instance.playerData.Save();
        Debug.Log($"[GachaQuest] Saved to PlayerPrefs: {PlayerPrefs.GetInt("GachaQuestCompleted", 0)}");
        Debug.Log($"[GachaQuest] Quest marked complete: {PlayerDataManager.Instance.playerData.gachaQuestCompleted}");

        foreach (var q in FindObjectsOfType<GachaQuest>())
        {
            Debug.Log($"[GachaQuest] Found and refreshing: {q.gameObject.name}");
            q.RefreshUI();
        }

    if (animationPanel != null)
        animationPanel.SetActive(false);

    yield return new WaitForSeconds(0.1f);

    if (resultBackground != null)
        resultBackground.SetActive(true);

    if (multiResultPanel != null)
        multiResultPanel.SetActive(true);

    for (int i = 0; i < awardedMarbles.Count && i < marbleSlots.Length; i++)
    {
        yield return StartCoroutine(RevealMarbleInSlot(marbleSlots[i], awardedMarbles[i]));
        yield return new WaitForSeconds(1f);
    }

    // ✅ FIX BUG 1: Auto-close after all marbles revealed
    if (multiResultPanel != null)
        multiResultPanel.SetActive(false);

    if (resultBackground != null)
        resultBackground.SetActive(false);

    // ✅ FIX BUG 2: Always re-enable buttons and reset isPulling here
    if (pullButton != null) pullButton.interactable = true;
    if (pull5Button != null) pull5Button.interactable = true;

    isPulling = false;

        Debug.Log("🎉 Got 5 marbles!");

}

IEnumerator RevealMarbleInSlot(Transform slot, HolenData marble)
{
    if (slot == null) yield break;

    // Find components in the slot
    Image marbleIcon = slot.Find("Marble ICON")?.GetComponent<Image>();
    TextMeshProUGUI marbleName = slot.Find("Marble NAME")?.GetComponent<TextMeshProUGUI>();
    SunrayRevealEffect sunray = slot.GetComponent<SunrayRevealEffect>();
    slot.gameObject.SetActive(true);

    // Set the marble data
    if (marbleIcon != null)
        marbleIcon.sprite = marble.holenIcon;

    if (marbleName != null)
    {
        marbleName.text = marble.holenName;
        marbleName.color = GetRarityColor(marble.rarity);
    }

    // Play reveal sound
    if (revealSound != null)
        revealSound.Play();

    // Start sunray effect
    if (sunray != null)
        sunray.PlayRevealEffect(marble.rarity);

    // Scale up animation for this slot
    slot.localScale = Vector3.zero;
    float elapsed = 0f;
    float duration = 0.4f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / duration;
        float scale = Mathf.Lerp(0, 1, progress);
        slot.localScale = Vector3.one * scale;
        yield return null;
    }
    
    slot.localScale = Vector3.one;

    yield return new WaitForSeconds(1f);

// Fade out over 0.3 seconds
float fadeElapsed = 0f;
float fadeDuration = 0.3f;
CanvasGroup cg = slot.GetComponent<CanvasGroup>();
if (cg == null) cg = slot.gameObject.AddComponent<CanvasGroup>();

while (fadeElapsed < fadeDuration)
{
    fadeElapsed += Time.deltaTime;
    cg.alpha = Mathf.Lerp(1, 0, fadeElapsed / fadeDuration);
    yield return null;
}

slot.gameObject.SetActive(false);
cg.alpha = 1f; // Reset for next time
}

    IEnumerator ShakeAnimation(float duration)
    {
        float elapsed = 0f;
        Vector3 originalPos = spinningImage != null ? spinningImage.transform.localPosition : Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            if (spinningImage != null)
            {
                float intensity = Mathf.Sin(progress * Mathf.PI) * 25f;
                float shakeSpeed = 40f;

                float x = Mathf.Sin(elapsed * shakeSpeed) * intensity;
                float y = Mathf.Cos(elapsed * shakeSpeed * 1.3f) * intensity;
                float rotation = Mathf.Sin(elapsed * shakeSpeed * 0.7f) * intensity * 0.5f;

                spinningImage.transform.localPosition = originalPos + new Vector3(x, y, 0);
                spinningImage.transform.rotation = Quaternion.Euler(0, 0, rotation);

                float scale = 1f + Mathf.Sin(elapsed * 15f) * 0.05f;
                spinningImage.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        if (spinningImage != null)
        {
            spinningImage.transform.localPosition = originalPos;
            spinningImage.transform.rotation = Quaternion.identity;
            spinningImage.transform.localScale = Vector3.one;
        }
    }



public void CloseMultiResultPanelAnimated()
{
    StopAllCoroutines();

    // Reset all marble slots
    foreach (Transform slot in marbleSlots)
    {
        if (slot != null)
        {
            slot.gameObject.SetActive(false);
            slot.localScale = Vector3.one;

            CanvasGroup cg = slot.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }
    }

    // Reset spinning image
    if (spinningImage != null)
    {
        spinningImage.transform.localPosition = Vector3.zero;
        spinningImage.transform.rotation = Quaternion.identity;
        spinningImage.transform.localScale = Vector3.one;
    }

    if (multiResultPanel != null)
        multiResultPanel.SetActive(false);

    if (resultBackground != null)
        resultBackground.SetActive(false);

    if (animationPanel != null)
        animationPanel.SetActive(false);

    if (pullButton != null) pullButton.interactable = true;
    if (pull5Button != null) pull5Button.interactable = true;

    isPulling = false;
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
}