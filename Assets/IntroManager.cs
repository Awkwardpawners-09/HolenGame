using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroManager : MonoBehaviour
{
    [Header("Objects to disable after 4 seconds")]
    public List<GameObject> firstSetOfObjects;  // First set of objects to disable after 4 seconds

    [Header("Objects to disable after 5 seconds")]
    public List<GameObject> secondSetOfObjects;  // Second set of objects to disable after 9 seconds

    [Header("Play Once Settings")]
    [Tooltip("If enabled, intro will only play once per app launch")]
    public bool playOncePerSession = false;

    // Static flag that persists across scene loads but resets when app closes
    private static bool hasIntroPlayed = false;

    void Start()
    {
        // Check if the intro has already played this session
        if (hasIntroPlayed)
        {
            // Immediately disable all objects without animation
            DisableAllObjectsImmediately();
        }
        else
        {
            // Mark that intro has played and start the normal sequence
            hasIntroPlayed = true;
            StartCoroutine(DisableObjects());
        }
    }

    private void DisableAllObjectsImmediately()
    {
        // Disable the first set of objects
        foreach (GameObject obj in firstSetOfObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        // Disable the second set of objects
        foreach (GameObject obj in secondSetOfObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    private IEnumerator DisableObjects()
    {
        // Wait for 4.5 seconds
        yield return new WaitForSeconds(4.5f);

        // Disable the first set of objects
        foreach (GameObject obj in firstSetOfObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        // Wait for an additional 2 seconds (6.5 seconds in total)
        yield return new WaitForSeconds(2f);

        // Disable the second set of objects
        foreach (GameObject obj in secondSetOfObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
}