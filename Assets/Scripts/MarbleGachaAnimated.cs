using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarbleGachaAnimated : MonoBehaviour
{
    public List<HolenData> marblePool;
    private PlayerData playerData => PlayerDataManager.Instance.playerData;

    [Header("Visual Effects")]
    public SunrayRevealEffect sunrayEffect;

    [Header("Inventory Reference")]
    public HolenInventoryManager inventoryManager;

    [Header("UI References")]
    public GameObject resultPanel;
    public GameObject resultBackground;
    public Image marbleImage;
    public TextMeshProUGUI marbleNameText;
    public TextMeshProUGUI rarityText;
    public CoinUIManager coinUI;
    public Button pullButton;

    [Header("Multi-Pull Settings")]
    public Button pull5Button;
    public int singlePullCost = 100;
    public int multiPullCost = 450;
    public GameObject multiResultPanel;
    public Transform[] marbleSlots;
    public Transform multiResultGrid;
    public GameObject multiResultSlotPrefab;

    [Header("Animation Settings")]
    public GameObject animationPanel;
    public Image spinningImage;
    public float spinDuration = 2f;
    public float spinSpeed = 720f;
    public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Particle Effects (Optional)")]
    public ParticleSystem pullParticles;
    public ParticleSystem revealParticles;

    [Header("Sound Effects (Optional)")]
    public AudioSource pullSound;
    public AudioSource revealSound;
    public AudioSource raritySound;

    [Header("Rarity Colors")]
    public Color commonColor = Color.gray;
    public Color uncommonColor = new Color(0.145f, 0.588f, 0.745f, 1f);
    public Color rareColor = Color.blue;
    public Color epicColor = Color.magenta;
    public Color legendaryColor = Color.yellow;
    public Color mythicColor = Color.red;

    private bool isPulling = false;

    void Start()
    {
        if (animationPanel != null)
            animationPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    public void TryBuyMarbleBag()
    {
        if (isPulling) return;

        bool success = PlayerDataManager.Instance.SpendCoins(singlePullCost);

        if (success)
            StartCoroutine(GachaPullAnimation());
        else
        {
            Debug.Log("Not enough coins!");
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
        if (awardedMarble != null)
        {
            inventoryManager.AddHolen(awardedMarble.holenID, 1);
            CheckRareHolenAchievement(awardedMarble);
            CheckMythicHolenAchievement(awardedMarble);
            CheckCollect10Achievement(1);
        }

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

        if (resultBackground != null)
            resultBackground.SetActive(true);

        resultPanel.SetActive(true);
        marbleNameText.text = awardedMarble.holenName;
        marbleImage.sprite = awardedMarble.holenIcon;
        marbleNameText.color = GetRarityColor(awardedMarble.rarity);

        if (rarityText != null)
        {
            rarityText.text = awardedMarble.rarity;
            rarityText.color = GetRarityColor(awardedMarble.rarity);
        }

        if (sunrayEffect != null)
            sunrayEffect.PlayRevealEffect(awardedMarble.rarity);

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

        yield return new WaitForSeconds(1.5f);

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

        PlayerDataManager.Instance.playerData.gacha1xQuestCompleted = true;
        PlayerDataManager.Instance.playerData.Save();
        foreach (var q in FindObjectsOfType<Gacha1xQuest>())
            q.RefreshUI();

        isPulling = false;

        Debug.Log($"🎉 Gacha awarded: {awardedMarble.holenName}");
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

        PlayerDataManager.Instance.playerData.gacha1xQuestCompleted = true;
        PlayerDataManager.Instance.playerData.Save();
        foreach (var q in FindObjectsOfType<Gacha1xQuest>())
            q.RefreshUI();

        isPulling = false;
    }

    Color GetRarityColor(string rarity)
    {
        switch (rarity.ToLower())
        {
            case "common":    return commonColor;
            case "uncommon":  return uncommonColor;
            case "rare":      return rareColor;
            case "epic":      return epicColor;
            case "legendary": return legendaryColor;
            case "mythic":  return mythicColor;
            default:          return Color.white;
        }
    }

HolenData GetRandomMarble()
{
    var validMarbles = marblePool.FindAll(m => m != null && !string.IsNullOrEmpty(m.holenID));
    if (validMarbles.Count == 0) { Debug.LogError("No valid marbles in marblePool!"); return null; }

    float roll = Random.Range(0f, 100f);

    if (roll < 16.6f)      return GetRandomOfRarity(validMarbles, "common");
    else if (roll < 33.2f) return GetRandomOfRarity(validMarbles, "uncommon");
    else if (roll < 49.8f) return GetRandomOfRarity(validMarbles, "rare");
    else if (roll < 66.4f) return GetRandomOfRarity(validMarbles, "epic");
    else if (roll < 83f)   return GetRandomOfRarity(validMarbles, "legendary");
    else                   return GetRandomOfRarity(validMarbles, "mythic");
}

    HolenData GetRandomOfRarity(List<HolenData> pool, string rarity)
    {
        var filtered = pool.FindAll(m => m.rarity.ToLower() == rarity);
        if (filtered.Count == 0) return pool[Random.Range(0, pool.Count)];
        return filtered[Random.Range(0, filtered.Count)];
    }

    private void CheckRareHolenAchievement(HolenData marble)
    {
        if (marble == null) return;
        if (PlayerDataManager.Instance.playerData.rareHolenAchievementCompleted) return;

        if (marble.rarity.ToLower() == "rare")
        {
            PlayerDataManager.Instance.playerData.rareHolenAchievementCompleted = true;
            PlayerDataManager.Instance.playerData.Save();

            foreach (var a in FindObjectsOfType<RareHolenAchievement>())
                a.RefreshUI();

            Debug.Log($"[RareHolenAchievement] Unlocked by pulling: {marble.holenName} ({marble.rarity})");
        }
    }

    private void CheckCollect10Achievement(int amountAdded)
    {
        if (PlayerDataManager.Instance.playerData.collect10HolensAchievementCompleted) return;

        PlayerDataManager.Instance.playerData.totalHolensCollected += amountAdded;
        PlayerDataManager.Instance.playerData.Save();

        if (PlayerDataManager.Instance.playerData.totalHolensCollected >= 10)
        {
            PlayerDataManager.Instance.playerData.collect10HolensAchievementCompleted = true;
            PlayerDataManager.Instance.playerData.Save();

            foreach (var a in FindObjectsOfType<Collect10HolensAchievement>())
                a.RefreshUI();

            Debug.Log("[Collect10HolensAchievement] Unlocked!");
        }
        else
        {
            foreach (var a in FindObjectsOfType<Collect10HolensAchievement>())
                a.RefreshUI();
        }
    }

    private void CheckMythicHolenAchievement(HolenData marble)
{
    if (marble == null) return;
    if (PlayerDataManager.Instance.playerData.mythicHolenAchievementCompleted) return;

    if (marble.rarity.ToLower() == "mythic")
    {
        PlayerDataManager.Instance.playerData.mythicHolenAchievementCompleted = true;
        PlayerDataManager.Instance.playerData.Save();

        foreach (var a in FindObjectsOfType<MythicHolenAchievement>())
            a.RefreshUI();

        Debug.Log($"[MythicHolenAchievement] Unlocked by pulling: {marble.holenName} ({marble.rarity})");
    }
}

    IEnumerator ShakeButton(Button button = null)
    {
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
            StartCoroutine(MultiPullAnimationAllAtOnce());
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
            if (marble == null) continue;
            awardedMarbles.Add(marble);
            inventoryManager.AddHolen(marble.holenID, 1);
            CheckRareHolenAchievement(marble);
            CheckMythicHolenAchievement(marble);
        }

        CheckCollect10Achievement(awardedMarbles.Count);

        PlayerDataManager.Instance.playerData.gachaQuestCompleted = true;
        PlayerDataManager.Instance.playerData.Save();
        foreach (var q in FindObjectsOfType<GachaQuest>())
            q.RefreshUI();

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

        if (multiResultPanel != null)
            multiResultPanel.SetActive(false);

        if (resultBackground != null)
            resultBackground.SetActive(false);

        if (pullButton != null) pullButton.interactable = true;
        if (pull5Button != null) pull5Button.interactable = true;

        isPulling = false;

        Debug.Log("🎉 Got 5 marbles!");
    }

    IEnumerator RevealMarbleInSlot(Transform slot, HolenData marble)
    {
        if (slot == null) yield break;

        Image marbleIcon = slot.Find("Marble ICON")?.GetComponent<Image>();
        TextMeshProUGUI marbleName = slot.Find("Marble NAME")?.GetComponent<TextMeshProUGUI>();
        SunrayRevealEffect sunray = slot.GetComponent<SunrayRevealEffect>();
        slot.gameObject.SetActive(true);

        if (marbleIcon != null)
            marbleIcon.sprite = marble.holenIcon;

        if (marbleName != null)
        {
            marbleName.text = marble.holenName;
            marbleName.color = GetRarityColor(marble.rarity);
        }

        if (revealSound != null)
            revealSound.Play();

        if (sunray != null)
            sunray.PlayRevealEffect(marble.rarity);

        slot.localScale = Vector3.zero;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            slot.localScale = Vector3.one * Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }

        slot.localScale = Vector3.one;

        yield return new WaitForSeconds(1f);

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
        cg.alpha = 1f;
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

    public void CloseMultiResultPanel()
    {
        CloseMultiResultPanelAnimated();
    }

    public void CloseMultiResultPanelAnimated()
    {
        StopAllCoroutines();

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
}