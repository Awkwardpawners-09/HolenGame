using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroManager : MonoBehaviour
{
    [Header("Objects to disable after 4 seconds")]
    public List<GameObject> firstSetOfObjects;  // First set of objects to disable after 4 seconds

    [Header("Objects to disable after 5 seconds")]
    public List<GameObject> secondSetOfObjects;  // Second set of objects to disable after 9 seconds

    void Start()
    {
        // Start the process of disabling objects after the specified times
        StartCoroutine(DisableObjects());
    }

    private IEnumerator DisableObjects()
    {
        // Wait for 4 seconds
        yield return new WaitForSeconds(4.5f);

        // Disable the first set of objects
        foreach (GameObject obj in firstSetOfObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        // Wait for an additional 5 seconds (9 seconds in total)
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
