using Cinemachine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerHolenController : MonoBehaviourPunCallbacks
{
    [Header("References")]
    public GameObject holenHandsPrefab;
    public GameObject holenBallPrefab;
    public Transform spawnPoint;
    public Transform ballSpawnPoint;
    public Slider powerGauge;
    public Camera mainCamera;
    public CinemachineVirtualCamera activePlayerCamera;
    public CinemachineVirtualCamera birdsEyeCamera;
    public TMP_Text playerLabelText;
    public TMP_Text turnDisplayText;
    public Transform cameraSpawnPoint;
    public FixedJoystick joystick;

    [Header("UI Button Parent")]
    public GameObject uiButtonsParent;
    public GameObject loadingUI;

    [Header("Player Info")]
    public bool isPlayer1;

    [Header("Power Gauge Settings")]
    public float gaugeMin = 10f;
    public float gaugeMax = 70f;
    public float gaugeSpeed = 40f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 50f;

    [Header("Camera Settings")]
    public Vector3 cameraFollowOffset = new Vector3(0f, 8f, -6f);
    public float cameraAimScreenY = 0.80f;

    private GameObject currentHolenHands;
    public GameObject currentHolenBall { get; private set; }
    private PlayerController playerControlScript;
    private bool isReady = false;
    private bool isTurn = false;
    private string playerRole = "";
    private bool isButtonPressed = false;

    private bool isGaugeIncreasing = true;
    private bool isGaugeActive = false;
    private float currentLaunchForce;
    private Transform defaultLookAtTarget;



    void Start()
    {
        DisableControls();
        SetInitialCameraPosition();

        if (activePlayerCamera != null)
            defaultLookAtTarget = activePlayerCamera.LookAt;

        if (powerGauge != null)
        {
            powerGauge.minValue = gaugeMin;
            powerGauge.maxValue = gaugeMax;
            powerGauge.gameObject.SetActive(false);
        }

        SetCameraView(false);
        StartCoroutine(GameStartSequence());
    }

    private IEnumerator GameStartSequence()
    {
        loadingUI.SetActive(true);
        Debug.Log("Waiting for both players to connect...");

        while (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            yield return null;
        }

        Debug.Log("Both players are connected.");
        yield return new WaitForSeconds(3f);

        isPlayer1 = PhotonNetwork.LocalPlayer.ActorNumber == 1;
        playerRole = isPlayer1 ? "Player 1" : "Player 2";

        Debug.Log($"Local player assigned as {playerRole}");

        loadingUI.SetActive(false);
        UpdateTurnDisplayText();

        if (isPlayer1)
        {
            isTurn = true;
            EnableControls();
            SpawnHolenHands();
            UpdateTurnDisplayText();
            Debug.Log("Player 1's turn has started.");
        }
    }

    void Update()
    {
        if (isTurn && currentHolenHands != null)
        {
            float horizontalInput = joystick.Horizontal;
            playerControlScript.HandleControls(horizontalInput, rotationSpeed);

            if (isGaugeActive)
            {
                UpdateGauge();
            }

            if (isButtonPressed && !isReady)
            {
                ReadyHolen();
            }
            else if (isButtonPressed && isReady)
            {
                ShootHolen();
                isButtonPressed = false;
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

        if (powerGauge != null)
            powerGauge.value = currentLaunchForce;
    }

    private void SetInitialCameraPosition()
    {
        if (cameraSpawnPoint != null)
        {
            mainCamera.transform.position = cameraSpawnPoint.position;
            mainCamera.transform.rotation = cameraSpawnPoint.rotation;
        }
    }

    public void SpawnHolenHands()
    {
        currentHolenHands = PhotonNetwork.Instantiate(holenHandsPrefab.name, spawnPoint.position, Quaternion.identity);

        PhotonView handsPhotonView = currentHolenHands.GetComponent<PhotonView>();

        photonView.RPC("RPC_PlayAnimation", RpcTarget.All, "Idle");

        if (isTurn && activePlayerCamera != null)
        {
            activePlayerCamera.Follow = currentHolenHands.transform;
            activePlayerCamera.LookAt = currentHolenBall?.transform;
            AdjustCameraPosition();
        }

        playerControlScript = currentHolenHands.AddComponent<PlayerController>();
        playerControlScript.InitializeControls(this, handsPhotonView);

        SpawnHolenBall();

        Debug.Log($"{playerRole} spawned Holen Hands and Ball");
    }

    public bool IsTurn()
    {
        return isTurn;
    }

    private void AdjustCameraPosition()
    {
        if (activePlayerCamera != null)
        {
            CinemachineTransposer transposer = activePlayerCamera.GetCinemachineComponent<CinemachineTransposer>();
            CinemachineComposer composer = activePlayerCamera.GetCinemachineComponent<CinemachineComposer>();
            if (transposer != null)
            {
                transposer.m_FollowOffset = cameraFollowOffset;
                composer.m_ScreenY = cameraAimScreenY;
            }
        }
    }

    private void SpawnHolenBall()
    {
        currentHolenBall = PhotonNetwork.Instantiate(holenBallPrefab.name, ballSpawnPoint.position, Quaternion.identity);
        Rigidbody rb = currentHolenBall.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        if (activePlayerCamera != null && currentHolenBall != null && isTurn)
        {
            activePlayerCamera.LookAt = currentHolenBall.transform;
        }
    }

    public void ReadyHolen()
    {
        if (isTurn && !isReady && currentHolenHands != null)
        {
            isReady = true;

            photonView.RPC("RPC_PlayAnimation", RpcTarget.All, "Ready");

            if (powerGauge != null)
            {
                powerGauge.gameObject.SetActive(true);
                powerGauge.interactable = true;
                currentLaunchForce = gaugeMin;
                isGaugeIncreasing = true;
                isGaugeActive = true;
            }

            Debug.Log($"{playerRole} is now ready.");
        }
    }

    public void SetButtonPressed(bool pressed)
    {
        isButtonPressed = pressed;
    }

    public void ShootHolen()
    {
        if (isTurn && isReady && currentHolenHands != null && currentHolenBall != null)
        {
            isReady = false;

            float power = currentLaunchForce;

            isGaugeActive = false;

            photonView.RPC("RPC_ShootHolen", RpcTarget.All, power);

            if (powerGauge != null)
            {
                powerGauge.gameObject.SetActive(false);
                powerGauge.interactable = false;
            }

            StartCoroutine(CompleteTurn());
            Debug.Log($"{playerRole} launched Holen Ball with power: {power}");
        }
    }

    [PunRPC]
    private void RPC_PlayAnimation(string triggerName)
    {
        if (currentHolenHands != null)
        {
            Animator animator = currentHolenHands.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(triggerName);
            }
        }
    }

    [PunRPC]
    private void RPC_ShootHolen(float power)
    {
        if (currentHolenHands != null)
        {
            Animator animator = currentHolenHands.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Shoot");
            }
        }

        if (currentHolenBall != null)
        {
            Rigidbody rb = currentHolenBall.GetComponent<Rigidbody>();
            rb.isKinematic = false;

            rb.AddForce(currentHolenHands.transform.forward * power, ForceMode.Impulse);

            if (activePlayerCamera != null && isTurn)
            {
                activePlayerCamera.LookAt = currentHolenBall.transform;
            }
        }
    }

    private IEnumerator CompleteTurn()
    {
        yield return new WaitForSeconds(7f);

        if (isTurn && activePlayerCamera != null)
        {
            activePlayerCamera.Follow = null;

            if (defaultLookAtTarget != null)
            {
                activePlayerCamera.LookAt = defaultLookAtTarget;
            }
            else
            {
                activePlayerCamera.LookAt = null;
            }

            if (cameraSpawnPoint != null)
            {
                mainCamera.transform.position = cameraSpawnPoint.position;
                mainCamera.transform.rotation = cameraSpawnPoint.rotation;
            }
        }

        if (currentHolenHands != null)
            PhotonNetwork.Destroy(currentHolenHands);
        if (currentHolenBall != null)
            PhotonNetwork.Destroy(currentHolenBall);

        currentHolenHands = null;
        currentHolenBall = null;
        playerControlScript = null;
        isGaugeActive = false;

        EndTurn();
    }

    private void DisableControls()
    {
        if (powerGauge != null)
        {
            powerGauge.gameObject.SetActive(false);
            powerGauge.interactable = false;
        }
        isGaugeActive = false;
        uiButtonsParent.SetActive(false);

        SetCameraView(false);
    }

    private void EnableControls()
    {
        if (powerGauge != null)
        {
            powerGauge.gameObject.SetActive(false);
            powerGauge.interactable = false;
        }
        isGaugeActive = false;

        uiButtonsParent.SetActive(true);

        SetCameraView(true);
    }

    private void UpdateTurnDisplayText()
    {
        if (turnDisplayText != null)
        {
            turnDisplayText.text = isTurn ? "Your Turn" : "Opponent's Turn";
        }

        if (playerLabelText != null)
        {
            playerLabelText.text = isPlayer1 ? "Player 1" : "Player 2";
        }
    }

    private void EndTurn()
    {
        isTurn = false;
        isReady = false;
        isButtonPressed = false;
        isGaugeActive = false;
        DisableControls();
        UpdateTurnDisplayText();

        Debug.Log($"{playerRole} ended their turn. Switching to other player.");

        PVPScore scoreManager = FindObjectOfType<PVPScore>();
        if (scoreManager != null)
        {
            scoreManager.OnTurnEnd();
        }

        photonView.RPC("SwitchTurn", RpcTarget.Others);
    }

    private void SetCameraView(bool isActiveTurn)
    {
        if (activePlayerCamera != null && birdsEyeCamera != null)
        {
            if (isActiveTurn)
            {
                activePlayerCamera.Priority = 20;
                birdsEyeCamera.Priority = 10;
            }
            else
            {
                activePlayerCamera.Priority = 10;
                birdsEyeCamera.Priority = 20;
            }
        }
    }

    [PunRPC]
    private void SwitchTurn()
    {
        isTurn = true;
        isReady = false;
        isButtonPressed = false;
        isGaugeActive = false;

        EnableControls();
        UpdateTurnDisplayText();
        SpawnHolenHands();

        Debug.Log($"{playerRole}'s turn started");
    }
}