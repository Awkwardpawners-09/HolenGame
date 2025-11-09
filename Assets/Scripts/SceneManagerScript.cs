using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerScript : MonoBehaviour
{
    public GameObject findingOpponentPanel; // Reference to the "Finding Opponent" panel

    // Method to transition to the TestGame scene (or any other scene)
    public void GoToTestGameScene()
    {
        // Optionally, we can ensure the inventory data persists by keeping the HolenInventoryManager across scenes
        if (FindObjectOfType<HolenInventoryManager>() != null)
        {
            DontDestroyOnLoad(FindObjectOfType<HolenInventoryManager>().gameObject);
        }

        // Load the TestGame scene
        SceneManager.LoadScene("Demo Game 1");
    }

    public void GoToMenu()
    {
        // Optionally, we can ensure the inventory data persists by keeping the HolenInventoryManager across scenes
        if (FindObjectOfType<HolenInventoryManager>() != null)
        {
            DontDestroyOnLoad(FindObjectOfType<HolenInventoryManager>().gameObject);
        }

        // Load the TestGame scene
        SceneManager.LoadScene("Demo Menu");
    }

    public void GoToLevel1()
    {
        // Optionally, we can ensure the inventory data persists by keeping the HolenInventoryManager across scenes
        if (FindObjectOfType<HolenInventoryManager>() != null)
        {
            DontDestroyOnLoad(FindObjectOfType<HolenInventoryManager>().gameObject);
        }

        // Load the TestGame scene
        SceneManager.LoadScene("Demo Game 1");
    }
    public void GoToLevel2()
    {
        // Optionally, we can ensure the inventory data persists by keeping the HolenInventoryManager across scenes
        if (FindObjectOfType<HolenInventoryManager>() != null)
        {
            DontDestroyOnLoad(FindObjectOfType<HolenInventoryManager>().gameObject);
        }

        // Load the TestGame scene
        SceneManager.LoadScene("Demo Game 2");
    }
    public void GoToLevel3()
    {
        // Optionally, we can ensure the inventory data persists by keeping the HolenInventoryManager across scenes
        if (FindObjectOfType<HolenInventoryManager>() != null)
        {
            DontDestroyOnLoad(FindObjectOfType<HolenInventoryManager>().gameObject);
        }

        // Load the TestGame scene
        SceneManager.LoadScene("Demo Game 3");
    }
    public void GoToLevel4()
    {
        // Optionally, we can ensure the inventory data persists by keeping the HolenInventoryManager across scenes
        if (FindObjectOfType<HolenInventoryManager>() != null)
        {
            DontDestroyOnLoad(FindObjectOfType<HolenInventoryManager>().gameObject);
        }

        // Load the TestGame scene
        SceneManager.LoadScene("Demo Game 4");
    }
    public void GoToLevel5()
    {
        // Optionally, we can ensure the inventory data persists by keeping the HolenInventoryManager across scenes
        if (FindObjectOfType<HolenInventoryManager>() != null)
        {
            DontDestroyOnLoad(FindObjectOfType<HolenInventoryManager>().gameObject);
        }

        // Load the TestGame scene
        SceneManager.LoadScene("Demo Game 5");
    }

    // You can call this to go back to the Main Menu or load any other scene
    public void GoToMainMenuScene()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // Optionally, use this method to trigger the "finding opponent" process
    public void StartFindingOpponentProcess()
    {
        findingOpponentPanel.SetActive(true); // Show the "Finding Opponent" panel
        // Add logic for matchmaking here
    }

    public void GoToTestLobby()
    {
        SceneManager.LoadScene("TestGame");
    }
}
