using UnityEngine;
using Cinemachine;
using UnityEngine.UI;

public class HolensLauncher : MonoBehaviour
{

    public GameObject bottomMenu;
    
    public Transform holensPosition;
    public GameObject holensBallPrefab;

    public float rotationSpeed = 90f;
    public Animator animator;
    public CinemachineVirtualCamera cinemachineCamera;
    public Slider gaugeSlider;
    public float gaugeMin = 10f;
    public float gaugeMax = 70f;
    public float gaugeSpeed = 40f;

    private GameObject currentBall;
    private bool isBusy = false; // Track if the launcher is busy
    private bool isReady = false;
    private bool isGaugeIncreasing = true;
    private bool isGaugeActive = false;
    private float currentLaunchForce;
    private Transform defaultLookAtTarget;
    private bool hasLaunched = false;

    public HolenChanger holenChanger;

    // Flags for continuous rotation
    private bool isRotatingLeft = false;
    private bool isRotatingRight = false;

    void Start()
    {
        SpawnBall();
        if (cinemachineCamera != null)
            defaultLookAtTarget = cinemachineCamera.LookAt;

        if (gaugeSlider != null)
        {
            gaugeSlider.minValue = gaugeMin;
            gaugeSlider.maxValue = gaugeMax;
            gaugeSlider.gameObject.SetActive(false);
        }

        // Use the selected HolenData (from HolenChanger) as the starting prefab
        HolenData startingHolenData = holenChanger.GetCurrentHolenData();
        holensBallPrefab = startingHolenData.holenPrefab; // Set the initial prefab

        SpawnBall();
        PlayIdle();
    }

    void Update()
    {
        HandleRotation(); // Handle rotation based on flags
        HandleInput();

        if (isGaugeActive)
        {
            UpdateGauge();
        }
    }

    // This method will handle continuous rotation based on flags
    void HandleRotation()
    {
        if (hasLaunched) return;

        // For continuous rotation (button triggered)
        if (isRotatingLeft)
        {
            RotateLeft();
        }

        if (isRotatingRight)
        {
            RotateRight();
        }
    }

    // For button click press to trigger rotation
    public void TriggerLeftRotationStart()
    {
        isRotatingLeft = true; // Start rotating left when button is pressed
    }

    public void TriggerLeftRotationStop()
    {
        isRotatingLeft = false; // Stop rotating left when button is released
    }

    public void TriggerRightRotationStart()
    {
        isRotatingRight = true; // Start rotating right when button is pressed
    }

    public void TriggerRightRotationStop()
    {
        isRotatingRight = false; // Stop rotating right when button is released
    }

    // Rotation logic for left
    void RotateLeft()
    {
        transform.Rotate(Vector3.up * -rotationSpeed * Time.deltaTime);
    }

    // Rotation logic for right
    void RotateRight()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    void HandleInput()
    {
        if (isBusy) return;

        // The buttons will now trigger specific actions
    }

    void UpdateGauge()
    {
        float delta = gaugeSpeed * Time.deltaTime;

        if (isGaugeIncreasing)
        {
            currentLaunchForce += delta;
            if (currentLaunchForce >= gaugeMax)
            {
                currentLaunchForce = gaugeMax;
                isGaugeIncreasing = false;
            }
        }
        else
        {
            currentLaunchForce -= delta;
            if (currentLaunchForce <= gaugeMin)
            {
                currentLaunchForce = gaugeMin;
                isGaugeIncreasing = true;
            }
        }

        if (gaugeSlider != null)
            gaugeSlider.value = currentLaunchForce;
    }

    // Separate method for readying the launcher (called by Ready button)
    public void TriggerReadyAction()
    {
        if (!isReady)
        {
            StartCoroutine(PlayReadyAnimation()); // Ready the launcher
        }
    }

    // Separate method for launching the ball (called by Launch button)
    public void TriggerLaunchAction()
    {
        if (isReady)
        {
            StartCoroutine(PlayShootAnimationAndLaunch()); // Launch the ball
        }
    }

    System.Collections.IEnumerator PlayReadyAnimation()
    {
        isBusy = true;
        animator.Play("Ready");

        yield return new WaitForSeconds(0.5f);

        if (gaugeSlider != null)
        {
            gaugeSlider.gameObject.SetActive(true);
            currentLaunchForce = gaugeMin;
            isGaugeIncreasing = true;
            isGaugeActive = true;
        }

        isBusy = false;
        isReady = true; // Mark the launcher as ready
    }

    System.Collections.IEnumerator PlayShootAnimationAndLaunch()
    {
        isBusy = true;
        animator.Play("Shoot");

        yield return new WaitForSeconds(0.1f);
        LaunchBall();

        yield return new WaitForSeconds(7f);

        PlayIdle();
        yield return new WaitForSeconds(0.5f);

        isReady = false;
        SpawnBall();  // Respawn with the current selected Holen
        isBusy = false;
        hasLaunched = false;

        // Re-enable buttons after launch
        holenChanger.EnableButtons();
    }

    void LaunchBall()
    {
        if (currentBall == null) return;

        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        currentBall.transform.parent = null;
        rb.isKinematic = false;
        rb.AddForce(transform.forward * currentLaunchForce, ForceMode.Impulse);

        if (cinemachineCamera != null)
        {
            cinemachineCamera.LookAt = currentBall.transform;
            StartCoroutine(ResetCameraLookAfterSeconds(6f));
        }

        if (gaugeSlider != null)
        {
            gaugeSlider.gameObject.SetActive(false);
            isGaugeActive = false;
        }

        hasLaunched = true;
        bottomMenu.SetActive(false);
    }

    System.Collections.IEnumerator ResetCameraLookAfterSeconds(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (cinemachineCamera != null)
        {
            cinemachineCamera.LookAt = defaultLookAtTarget;
        }
    }

    void SpawnBall(GameObject ballPrefab = null)
    {
        if (ballPrefab == null)
        {
            ballPrefab = holensBallPrefab;  // Use the selected Holen ball prefab
        }

        if (currentBall != null)
            Destroy(currentBall);

        currentBall = Instantiate(ballPrefab, holensPosition.position, holensPosition.rotation);
        currentBall.transform.parent = holensPosition;

        // Set the tag of the spawned ball to "Ball"
        currentBall.tag = "Ball";

        currentBall.GetComponent<Rigidbody>().isKinematic = true;
    }

    public void ChangeBallPrefab(GameObject newPrefab)
    {
        // Destroy the current ball if it's there
        if (currentBall != null)
        {
            Destroy(currentBall);
        }

        // Spawn the new ball with the updated prefab
        SpawnBall(newPrefab);
    }

    void PlayIdle()
    {
        animator.Play("Idle");
        bottomMenu.SetActive(true);
    }

    // Create a public getter method for isBusy
    public bool GetIsBusy()
    {
        return isBusy;
    }
}
