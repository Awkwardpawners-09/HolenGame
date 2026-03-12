using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PVPSYSTEMscript — Interactive Defense System for PVP turns.
///
/// Works ALONGSIDE MultiplayerHolenControllerNew without modifying it.
///
/// ── BUG FIXES ────────────────────────────────────────────────────────────────
///   Bug 1 — Objects disappearing on turn switch:
///     Root cause: DestroyAfterDelay coroutine lived on this GameObject and was
///     tied to the turn flow — it could be interrupted when turn state changed.
///     Fix: Spawned objects get a NetworkAutoDestroy component added directly to
///     them. Their timer is self-contained and survives any turn switch.
///
///   Bug 2 — Standby UI not showing on first turn:
///     Root cause: lastKnownTurn initialised to false. holenController.IsTurn()
///     also returns false during GameStartSequence. So currentTurn != lastKnownTurn
///     was never true on the first real read, and OnTurnStateChanged never fired.
///     Fix: Use bool? (nullable). It starts as null, so the FIRST read — whether
///     true or false — always triggers OnTurnStateChanged and sets the UI correctly.
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PVPSYSTEMscript : MonoBehaviourPunCallbacks
{
    // ══════════════════════════════════════════════════════════════
    //  TURN DISPLAY OBJECTS
    // ══════════════════════════════════════════════════════════════
    [Header("Turn Display – Controls Visibility")]
    [Tooltip("Enabled for the player whose TURN it is (swipe/launch UI). Mutually exclusive with HolenStandbyControls.")]
    public GameObject holenControls;

    [Tooltip("Enabled for the player ON STANDBY (defense buttons). Mutually exclusive with HolenControls.")]
    public GameObject holenStandbyControls;

    // ══════════════════════════════════════════════════════════════
    //  TIN CAN SETTINGS
    // ══════════════════════════════════════════════════════════════
    [Header("Tin Can Defense")]
    [Tooltip("Prefab to spawn. Must live inside a Resources folder for PhotonNetwork.Instantiate.")]
    public GameObject tinCanPrefab;

    [Tooltip("Spawn point for Tin Can prefabs.")]
    public Transform tinCanSpawnPoint;

    [Tooltip("Seconds each Tin Can lives before auto-destroy.")]
    public float tinCanLifetime = 20f;

    [Tooltip("Seconds the Tin Can button stays on cooldown after use.")]
    public float tinCanCooldown = 40f;

    [Tooltip("Minimum random rotation per axis (degrees).")]
    public Vector3 tinCanRotationMin = new Vector3(0f, 0f, 0f);

    [Tooltip("Maximum random rotation per axis (degrees).")]
    public Vector3 tinCanRotationMax = new Vector3(0f, 360f, 0f);

    [Tooltip("TMP_Text showing the cooldown countdown. Auto-shown/hidden.")]
    public TMP_Text tinCanCooldownText;

    [Tooltip("The Tin Can Button UI element.")]
    public Button tinCanButton;

    // ══════════════════════════════════════════════════════════════
    //  WALL SETTINGS
    // ══════════════════════════════════════════════════════════════
    [Header("Wall Defense")]
    [Tooltip("Prefab to spawn. Must live inside a Resources folder for PhotonNetwork.Instantiate.")]
    public GameObject wallPrefab;

    [Tooltip("Spawn point for the Wall prefab.")]
    public Transform wallSpawnPoint;

    [Tooltip("Seconds the Wall lives before auto-destroy.")]
    public float wallLifetime = 10f;

    [Tooltip("Seconds the Wall button stays on cooldown after use.")]
    public float wallCooldown = 50f;

    [Tooltip("TMP_Text showing the cooldown countdown. Auto-shown/hidden.")]
    public TMP_Text wallCooldownText;

    [Tooltip("The Wall Button UI element.")]
    public Button wallButton;

    // ══════════════════════════════════════════════════════════════
    //  MUD SETTINGS
    // ══════════════════════════════════════════════════════════════
    [Header("Mud Defense")]
    [Tooltip("Local GameObject enabled on the ACTIVE player's screen only when Mud is triggered.")]
    public GameObject mudEffectObject;

    [Tooltip("Seconds the Mud effect stays active on the active player's screen.")]
    public float mudLifetime = 8f;

    [Tooltip("Seconds the Mud button stays on cooldown after use.")]
    public float mudCooldown = 30f;

    [Tooltip("TMP_Text showing the cooldown countdown. Auto-shown/hidden.")]
    public TMP_Text mudCooldownText;

    [Tooltip("The Mud Button UI element.")]
    public Button mudButton;

    // ══════════════════════════════════════════════════════════════
    //  PRIVATE STATE
    // ══════════════════════════════════════════════════════════════
    private MultiplayerHolenControllerNew holenController;

    // Nullable: starts as null so first read (true OR false) always triggers OnTurnStateChanged
    private bool? lastKnownTurn = null;

    private bool isTurn = false;
    private bool tinCanOnCooldown = false;
    private bool wallOnCooldown = false;
    private bool mudOnCooldown = false;

    // ══════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════
    private void Awake()
    {
        holenController = GetComponent<MultiplayerHolenControllerNew>();
        if (holenController == null)
            Debug.LogError("[PVPSYSTEMscript] MultiplayerHolenControllerNew not found on this GameObject!");
    }

    private void Start()
    {
        SetCooldownTextActive(tinCanCooldownText, false);
        SetCooldownTextActive(wallCooldownText, false);
        SetCooldownTextActive(mudCooldownText, false);

        SetDefenseButtonsInteractable(false);

        // Both panels hidden until GameStartSequence resolves who goes first
        SetActive(holenControls, false);
        SetActive(holenStandbyControls, false);
    }

    private void Update()
    {
        if (holenController == null) return;

        bool currentTurn = holenController.IsTurn();

        // Nullable check — fires on FIRST read regardless of value, fixing standby-on-turn-1 bug
        if (!lastKnownTurn.HasValue || lastKnownTurn.Value != currentTurn)
        {
            lastKnownTurn = currentTurn;
            isTurn = currentTurn;
            OnTurnStateChanged(isTurn);
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  TURN STATE CHANGE
    // ══════════════════════════════════════════════════════════════
    private void OnTurnStateChanged(bool nowMyTurn)
    {
        SetActive(holenControls, nowMyTurn);
        SetActive(holenStandbyControls, !nowMyTurn);
        SetDefenseButtonsInteractable(!nowMyTurn);
    }

    // ══════════════════════════════════════════════════════════════
    //  BUTTON CALLBACKS — assign these in Button.onClick via Inspector
    // ══════════════════════════════════════════════════════════════
    public void OnTinCanPressed()
    {
        if (isTurn || tinCanOnCooldown) return;
        SpawnTinCans();
        StartCoroutine(CooldownRoutine(tinCanCooldown, tinCanCooldownText, tinCanButton,
            b => tinCanOnCooldown = b));
    }

    public void OnWallPressed()
    {
        if (isTurn || wallOnCooldown) return;
        SpawnWall();
        StartCoroutine(CooldownRoutine(wallCooldown, wallCooldownText, wallButton,
            b => wallOnCooldown = b));
    }

    public void OnMudPressed()
    {
        if (isTurn || mudOnCooldown) return;
        // Fires to the OTHER client — the active player — to show mud on their screen only
        photonView.RPC(nameof(RPC_ActivateMud), RpcTarget.Others);
        StartCoroutine(CooldownRoutine(mudCooldown, mudCooldownText, mudButton,
            b => mudOnCooldown = b));
    }

    // ══════════════════════════════════════════════════════════════
    //  TIN CAN SPAWN
    // ══════════════════════════════════════════════════════════════
    private void SpawnTinCans()
    {
        if (tinCanPrefab == null) { Debug.LogError("[PVPSYSTEMscript] tinCanPrefab not assigned!"); return; }
        if (tinCanSpawnPoint == null) { Debug.LogError("[PVPSYSTEMscript] tinCanSpawnPoint not assigned!"); return; }

        for (int i = 0; i < 4; i++)
        {
            Quaternion rot = Quaternion.Euler(
                Random.Range(tinCanRotationMin.x, tinCanRotationMax.x),
                Random.Range(tinCanRotationMin.y, tinCanRotationMax.y),
                Random.Range(tinCanRotationMin.z, tinCanRotationMax.z)
            );

            Vector3 scatter = new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));

            GameObject spawned = PhotonNetwork.Instantiate(
                tinCanPrefab.name,
                tinCanSpawnPoint.position + scatter,
                rot
            );

            // Self-contained timer on the object itself — survives turn switches
            AttachAutoDestroy(spawned, tinCanLifetime);
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  WALL SPAWN
    // ══════════════════════════════════════════════════════════════
    private void SpawnWall()
    {
        if (wallPrefab == null) { Debug.LogError("[PVPSYSTEMscript] wallPrefab not assigned!"); return; }
        if (wallSpawnPoint == null) { Debug.LogError("[PVPSYSTEMscript] wallSpawnPoint not assigned!"); return; }

        GameObject spawned = PhotonNetwork.Instantiate(
            wallPrefab.name,
            wallSpawnPoint.position,
            wallSpawnPoint.rotation
        );

        AttachAutoDestroy(spawned, wallLifetime);
    }

    // ══════════════════════════════════════════════════════════════
    //  AUTO DESTROY HELPER
    //  Adds NetworkAutoDestroy to the spawned object so its timer
    //  lives on the object, not on this script's coroutine stack.
    // ══════════════════════════════════════════════════════════════
    private void AttachAutoDestroy(GameObject obj, float lifetime)
    {
        NetworkAutoDestroy nad = obj.GetComponent<NetworkAutoDestroy>();
        if (nad == null) nad = obj.AddComponent<NetworkAutoDestroy>();
        nad.lifetime = lifetime;
    }

    // ══════════════════════════════════════════════════════════════
    //  MUD RPC — runs on the ACTIVE player's device
    // ══════════════════════════════════════════════════════════════
    [PunRPC]
    private void RPC_ActivateMud()
    {
        if (mudEffectObject == null) { Debug.LogWarning("[PVPSYSTEMscript] mudEffectObject not assigned!"); return; }
        StartCoroutine(MudEffectRoutine());
    }

    private IEnumerator MudEffectRoutine()
    {
        mudEffectObject.SetActive(true);
        yield return new WaitForSeconds(mudLifetime);
        mudEffectObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════
    //  COOLDOWN COROUTINE
    // ══════════════════════════════════════════════════════════════
    private IEnumerator CooldownRoutine(float duration, TMP_Text countdownText, Button button,
        System.Action<bool> setOnCooldown)
    {
        setOnCooldown(true);
        if (button != null) button.interactable = false;
        SetCooldownTextActive(countdownText, true);

        float remaining = duration;
        while (remaining > 0f)
        {
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(remaining).ToString();
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        setOnCooldown(false);
        SetCooldownTextActive(countdownText, false);
        // Re-enable only if still on standby when cooldown finishes
        if (button != null && !isTurn)
            button.interactable = true;
    }

    // ══════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════
    private void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    private void SetCooldownTextActive(TMP_Text tmp, bool active)
    {
        if (tmp != null) tmp.gameObject.SetActive(active);
    }

    private void SetDefenseButtonsInteractable(bool value)
    {
        if (tinCanButton != null && !tinCanOnCooldown) tinCanButton.interactable = value;
        if (wallButton != null && !wallOnCooldown) wallButton.interactable = value;
        if (mudButton != null && !mudOnCooldown) mudButton.interactable = value;
    }
}