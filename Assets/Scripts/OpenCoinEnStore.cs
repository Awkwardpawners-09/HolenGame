using System.Collections;
using UnityEngine;

public class OpenCoinEnStore : MonoBehaviour
{
    public GameObject panelObject;
    private CanvasGroup canvasGroup;

    public float fadeDuration = 0.3f;

    void Awake()
    {
        canvasGroup = panelObject.GetComponent<CanvasGroup>();
    }

    public void OpenPanel()
    {
        panelObject.SetActive(true);
        StartCoroutine(FadeIn());
    }

    public void ClosePanel()
    {
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeIn()
    {
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = time / fadeDuration;
            yield return null;
        }

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    IEnumerator FadeOut()
    {
        float time = 0;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = 1 - (time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0;
        panelObject.SetActive(false);
    }
}