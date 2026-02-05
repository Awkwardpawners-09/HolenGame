using UnityEngine;
using UnityEngine.UI;

public class HolenChanger : MonoBehaviour
{
    public HolenData holen1Data;
    public HolenData holen2Data;
    public HolenData holen3Data;

    public Image chosenHolenImage;
    public GameObject chosenHolenPrefab;

    public HolensLauncher holensLauncher;

    // Variable to hold the default HolenData (current selected Holen)
    private HolenData currentHolenData;

    // Store references to the buttons for enabling/disabling
    public Button holen1Button;
    public Button holen2Button;
    public Button holen3Button;

    void Start()
    {
        // Set the first HolenData as the default on start (this will be used as the initial prefab)
        currentHolenData = holen1Data;

        // Set the initial Holen as selected
        UpdateHolen(currentHolenData);
    }

    // This method will be exposed in the Unity Inspector for button clicks
    public void ChangeHolen1()
    {
        if (!holensLauncher.GetIsBusy()) // Check if not busy (in launching state)
        {
            UpdateHolen(holen1Data);
        }
    }

    public void ChangeHolen2()
    {
        if (!holensLauncher.GetIsBusy()) // Check if not busy (in launching state)
        {
            UpdateHolen(holen2Data);
        }
    }

    public void ChangeHolen3()
    {
        if (!holensLauncher.GetIsBusy()) // Check if not busy (in launching state)
        {
            UpdateHolen(holen3Data);
        }
    }

    // Function to change the image and prefab based on selected HolenData
    void UpdateHolen(HolenData holenData)
    {
        // Only update if the HolenData is different from the current
        if (currentHolenData != holenData)
        {
            // Set the new HolenData as the current selected Holen
            currentHolenData = holenData;

            // Change the image of the "ChosenHolen" based on the selected HolenData
            if (chosenHolenImage != null)
            {
                chosenHolenImage.sprite = holenData.holenIcon;
            }

            // Update the HolensLauncher with the new prefab immediately
            if (holensLauncher != null)
            {
                holensLauncher.ChangeBallPrefab(holenData.holenPrefab);
            }

            // Optionally, instantiate the chosen prefab in a specific location
            if (chosenHolenPrefab != null)
            {
                Destroy(chosenHolenPrefab);
                chosenHolenPrefab = Instantiate(holenData.holenPrefab, transform.position, Quaternion.identity);
            }
        }
    }

    // Disable buttons when launching is in progress
    public void DisableButtons()
    {
        holen1Button.interactable = false;
        holen2Button.interactable = false;
        holen3Button.interactable = false;
    }

    // Re-enable buttons after launch
    public void EnableButtons()
    {
        holen1Button.interactable = true;
        holen2Button.interactable = true;
        holen3Button.interactable = true;
    }

    // Method to get the current selected Holen (HolenData)
    public HolenData GetCurrentHolenData()
    {
        return currentHolenData;
    }
}