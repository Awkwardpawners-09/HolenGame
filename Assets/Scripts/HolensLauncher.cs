using UnityEngine;
using Cinemachine;
using UnityEngine.UI;

public class HolensLauncher : MonoBehaviour
{
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
    private bool isBusy = false;
    private bool isReady = false;
    private bool isGaugeIncreasing = true;
    private bool isGaugeActive = false;
    private float currentLaunchForce;
    private Transform defaultLookAtTarget;
    private bool hasLaunched = false;

    void Start()
    {
        if (cinemachineCamera != null)
            defaultLookAtTarget = cinemachineCamera.LookAt;

        if (gaugeSlider != null)
        {
            gaugeSlider.minValue = gaugeMin;
            gaugeSlider.maxValue = gaugeMax;
            gaugeSlider.gameObject.SetActive(false);
        }

        SpawnBall();
        PlayIdle();
    }

    void Update()
    {
        HandleRotation();
        HandleInput();

        if (isGaugeActive)
        {
            UpdateGauge();
        }
    }

    void HandleRotation()
    {
        if (hasLaunched) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        if (horizontal != 0)
        {
            transform.Rotate(Vector3.up * horizontal * rotationSpeed * Time.deltaTime);
        }
    }

    void HandleInput()
    {
        if (isBusy) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isReady)
            {
                isReady = true;
                StartCoroutine(PlayReadyAnimation());
            }
            else
            {
                StartCoroutine(PlayShootAnimationAndLaunch());
            }
        }
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
        SpawnBall();
        isBusy = false;
        hasLaunched = false;
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
    }

    System.Collections.IEnumerator ResetCameraLookAfterSeconds(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (cinemachineCamera != null)
        {
            cinemachineCamera.LookAt = defaultLookAtTarget;
        }
    }

    void SpawnBall()
    {
        if (currentBall != null)
            Destroy(currentBall);

        currentBall = Instantiate(holensBallPrefab, holensPosition.position, holensPosition.rotation);
        currentBall.transform.parent = holensPosition;
        currentBall.GetComponent<Rigidbody>().isKinematic = true;
    }

    void PlayIdle()
    {
        animator.Play("Idle");
    }
}
