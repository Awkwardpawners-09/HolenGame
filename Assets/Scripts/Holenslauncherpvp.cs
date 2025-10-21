using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class Holenslauncherpvp : MonoBehaviourPun
{
    public GameObject holenHandPrefab;
    public Transform holenPosition;
    public Slider gaugeSlider; // Reference to the Power Gauge slider
    public GameObject currentHand;

    private bool isReady = false;
    private float currentLaunchForce = 0f;
    private bool isRotatingLeft = false;
    private bool isRotatingRight = false;

    void Start()
    {
        SpawnHolenHand();
        if (gaugeSlider != null)
        {
            gaugeSlider.minValue = 10f;
            gaugeSlider.maxValue = 70f;
            gaugeSlider.onValueChanged.AddListener(OnGaugeChanged); // Listen to slider value changes
        }
    }

    // Spawns the Holenhand at the correct position
    private void SpawnHolenHand()
    {
        if (currentHand != null)
        {
            Destroy(currentHand);
        }
        currentHand = Instantiate(holenHandPrefab, holenPosition.position, holenPosition.rotation);
        currentHand.SetActive(true);
    }

    // Called when Power Gauge value is changed
    private void OnGaugeChanged(float value)
    {
        currentLaunchForce = value;
    }

    // Trigger rotation to the left
    public void TriggerLeftRotationStart()
    {
        isRotatingLeft = true;
    }

    // Stop rotating to the left
    public void TriggerLeftRotationStop()
    {
        isRotatingLeft = false;
    }

    // Trigger rotation to the right
    public void TriggerRightRotationStart()
    {
        isRotatingRight = true;
    }

    // Stop rotating to the right
    public void TriggerRightRotationStop()
    {
        isRotatingRight = false;
    }

    // Trigger ready action for the Holen (starts the power gauge)
    public void TriggerReadyAction()
    {
        if (!isReady)
        {
            // Start ready animation (if applicable)
            isReady = true;
            // Enable the Power Gauge
            if (gaugeSlider != null)
            {
                gaugeSlider.gameObject.SetActive(true);
            }
        }
    }

    // Trigger the launch action
    public void TriggerLaunchAction()
    {
        if (isReady)
        {
            LaunchBall();
            isReady = false;
        }
    }

    // Launch the Holenball with the strength of the power gauge
    private void LaunchBall()
    {
        Rigidbody rb = currentHand.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.AddForce(transform.forward * currentLaunchForce, ForceMode.Impulse);
    }
}