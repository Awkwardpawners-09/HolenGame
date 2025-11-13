using System.Collections;
using UnityEngine;
using TMPro; // Import TextMeshPro namespace

public class GameStartManager : MonoBehaviour
{
    // Inspector variables
    public TextMeshProUGUI countdownText; // The TextMeshProUGUI component for displaying the countdown text
    public float countdownDuration = 5f; // Total duration for the countdown (5 seconds)

    private void Start()
    {
        StartCoroutine(GameStartSequence());
    }

    private IEnumerator GameStartSequence()
    {
        // Initial setup: Display the "3" after 2 seconds
        yield return new WaitForSeconds(2f);
        countdownText.text = "Ready?"; // Change text to "3"

        // After 2.5 seconds, change text to "2"
        yield return new WaitForSeconds(0.5f);
        countdownText.text = "Ready?";

        // After 3 seconds, change text to "1"
        yield return new WaitForSeconds(0.5f);
        countdownText.text = "Ready?";

        // After 3.5 seconds, change text to "HOLENS!"
        yield return new WaitForSeconds(0.5f);
        countdownText.text = "HOLENS!";

        // Wait for the remaining time (1.5 seconds) to complete the 5-second duration
        yield return new WaitForSeconds(1.5f);

        // Disable the GameStart object after 5 seconds
        gameObject.SetActive(false);
    }
}
