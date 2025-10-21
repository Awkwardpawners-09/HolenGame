using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using Photon.Pun;

public class PVPLauncher : MonoBehaviourPun
{
    [Header("UI References")]
    public GameObject bottomMenu;  // Reference to Bottom Menu
    public Transform holensPosition;  // Position where the HolenHandsPrefab will spawn
    public GameObject whiteOpaquePrefab;  // Reference to the "White Opaque" (HolenBallPrefab)
    public GameObject holenHandsPrefab;  // Reference to HolenHandsPrefab

    [Header("Gameplay Controls")]
    public float rotationSpeed = 60f;
    public Animator animator;
    public CinemachineVirtualCamera cinemachineCamera;
    public Slider gaugeSlider;
    public float gaugeMin = 10f;
    public float gaugeMax = 90f;
    public float gaugeSpeed = 90f;

    private GameObject currentBall;
    private bool isBusy = false;
    private bool isReady = false;
    private bool isGaugeIncreasing = true;
    private bool isGaugeActive = false;
    private float currentLaunchForce;
    private Transform defaultLookAtTarget;
    private bool hasLaunched = false;

    private bool isRotatingLeft = false;
    private bool isRotatingRight = false;

    void Start()
    {
        // Set initial UI and other setups
        SetupGaugeSlider();
        SetInitialBallPrefab();
    }

    void Update()
    {
        HandleRotation();

        if (isGaugeActive)
        {
            UpdateGauge();
        }
    }

    // Handle rotation and button presses
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

    public void TriggerLeftRotationStart()
    {
        isRotatingLeft = true;
    }

    public void TriggerLeftRotationStop()
    {
        isRotatingLeft = false;
    }

    public void TriggerRightRotationStart()
    {
        isRotatingRight = true;
    }

    public void TriggerRightRotationStop()
    {
        isRotatingRight = false;
    }

    void RotateLeft()
    {
        transform.Rotate(Vector3.up * -rotationSpeed * Time.deltaTime);
    }

    void RotateRight()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    // Update gauge values based on the gauge slider
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
        {
            gaugeSlider.value = currentLaunchForce;
        }
    }

    // Called when Ready button is pressed
    public void TriggerReadyAction()
    {
        if (!isReady)
        {
            StartCoroutine(PlayReadyAnimation());
        }
    }

    // Called when Shoot button is pressed
    public void TriggerLaunchAction()
    {
        if (isReady)
        {
            StartCoroutine(PlayShootAnimationAndLaunch());
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
        isReady = true; // Mark as ready
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
        SpawnBall();  // Respawn Holen with current selected Holen
        isBusy = false;
        hasLaunched = false;

        // Re-enable buttons after launch
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

    // Prevent multiple ball spawns
    void SpawnBall(GameObject ballPrefab = null)
    {
        if (currentBall != null) return;  // Only spawn if currentBall is null

        if (ballPrefab == null)
        {
            ballPrefab = whiteOpaquePrefab;  // Use "White Opaque" prefab
        }

        currentBall = PhotonNetwork.Instantiate("White Opaque", holensPosition.position, holensPosition.rotation);
        currentBall.transform.parent = holensPosition;

        // Set the tag of the spawned ball to "Ball"
        currentBall.tag = "Ball";

        currentBall.GetComponent<Rigidbody>().isKinematic = true;
    }

    // Change ball prefab if needed
    public void ChangeBallPrefab(GameObject newPrefab)
    {
        if (currentBall != null)
        {
            Destroy(currentBall);
        }

        // Spawn the new ball with the updated prefab
        SpawnBall(newPrefab);
    }

    // Set idle animation after launch
    void PlayIdle()
    {
        animator.Play("Idle");
        bottomMenu.SetActive(true);
    }

    // Check if the launcher is busy
    public bool GetIsBusy()
    {
        return isBusy;
    }

    // Function to instantiate the HolenHandsPrefab for both players
    void SpawnHolenHands()
    {
        if (PhotonNetwork.IsConnected)
        {
            // Instantiate the HolenHandsPrefab
            GameObject holenHands = PhotonNetwork.Instantiate("HolenHandsPrefab", holensPosition.position, Quaternion.identity);

            // After instantiation, set the camera to follow the HolenHandsPrefab
            SetCameraFollow(holenHands);
        }
    }

    // Function to set the camera to follow HolenHandsPrefab
    void SetCameraFollow(GameObject holenHands)
    {
        CinemachineFreeLook freeLookCamera = Camera.main.GetComponent<CinemachineFreeLook>();

        if (freeLookCamera != null)
        {
            // Set the Cinemachine FreeLook Camera to follow the HolenHandsPrefab
            freeLookCamera.Follow = holenHands.transform;
            freeLookCamera.LookAt = currentBall.transform;  // Camera looks at the Holen (the ball)

            Debug.Log("Cinemachine FreeLook Camera now following HolenHandsPrefab and looking at Holen.");
        }
        else
        {
            Debug.LogError("Cinemachine FreeLook Camera not found on the Main Camera.");
        }
    }

    // Setup the gauge slider and hide it initially
    void SetupGaugeSlider()
    {
        if (gaugeSlider != null)
        {
            gaugeSlider.gameObject.SetActive(false);
        }
    }

    // Set initial ball prefab to spawn
    void SetInitialBallPrefab()
    {
        if (whiteOpaquePrefab == null)
        {
            Debug.LogError("White Opaque prefab is not assigned in the Inspector!");
        }
    }

    // Disable the non-active player's HolenHandsPrefab
    public void DisableInactivePlayerHolenHands(bool isPlayer1Turn)
    {
        // Disable non-active player's HolenHandsPrefab
        if (!isPlayer1Turn)
        {
            // Disable the non-active player's HolenHandsPrefab
            GameObject nonActiveHolenHands = GameObject.Find("HolenHandsPrefab_Player2");  // Adjust this based on how your players' HolenHands are named
            if (nonActiveHolenHands != null)
            {
                nonActiveHolenHands.SetActive(false);
            }
        }
    }
}
