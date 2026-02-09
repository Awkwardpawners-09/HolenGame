using UnityEngine;
using Photon.Pun;

/// <summary>
/// Improved physics synchronization with automatic ownership transfer and billiards-style physics.
/// Ensures physics reactions are always simulated by the correct client.
/// </summary>
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody))]
public class HolenPhysicsSync : MonoBehaviourPun, IPunObservable
{
    [Header("Sync Settings")]
    [Tooltip("How smoothly to interpolate to network position")]
    [Range(0.05f, 0.5f)]
    public float positionLerpFactor = 0.3f;

    [Tooltip("How smoothly to interpolate rotation")]
    [Range(0.05f, 0.5f)]
    public float rotationLerpFactor = 0.3f;

    [Header("Ownership Transfer")]
    [Tooltip("Automatically transfer ownership when hit by another player's holen")]
    public bool autoTransferOwnership = true;

    [Tooltip("Minimum impact force required to transfer ownership")]
    public float ownershipTransferThreshold = 1f;

    [Header("Optimization")]
    [Tooltip("Stop syncing when velocity is below this")]
    public float velocityThreshold = 0.02f;

    [Tooltip("Distance threshold to teleport instead of lerp")]
    public float teleportThreshold = 2f;

    [Tooltip("How long to wait before allowing sleep (prevents premature stopping)")]
    public float sleepDelay = 0.5f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Components
    private Rigidbody rb;
    private PhotonView pv;

    // Network sync data
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 networkVelocity;
    private Vector3 networkAngularVelocity;

    // State tracking
    private bool isSleeping = false;
    private float lagDistance = 0f;
    private float timeSinceLastImpact = 0f;
    private bool isInitialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pv = GetComponent<PhotonView>();

        // Initialize network state to current state
        networkPosition = transform.position;
        networkRotation = transform.rotation;
        networkVelocity = Vector3.zero;
        networkAngularVelocity = Vector3.zero;
    }

    void Start()
    {
        InitializePhysics();
        isInitialized = true;

        if (showDebugInfo)
        {
            string owner = pv.Owner != null ? pv.Owner.NickName : "MasterClient";
            Debug.Log($"[HolenPhysicsSync] {gameObject.name} - Owner: {owner}, IsMine: {pv.IsMine}");
        }
    }

    /// <summary>
    /// Initialize physics properties for billiards-style gameplay
    /// </summary>
    private void InitializePhysics()
    {
        // CRITICAL: All holens must be able to simulate physics
        rb.isKinematic = false;

        // Set reasonable physics properties for better sync
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // For billiards-style gameplay: freeze Y position and X/Z rotations
        // This keeps holens sliding on a plane like real marbles
        rb.constraints = RigidbodyConstraints.FreezePositionY |
                        RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationZ;

        // Set physics material properties (you should create a PhysicMaterial for this)
        // Adjust these values for more realistic marble physics
        rb.drag = 0.5f;  // Simulates friction/air resistance
        rb.angularDrag = 0.5f;  // Simulates rotational friction
        rb.mass = 1f;

        // Start at rest
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (!isInitialized) return;

        timeSinceLastImpact += Time.fixedDeltaTime;

        if (!pv.IsMine)
        {
            // Non-owner: smoothly move to network state
            SyncToNetworkState();
        }
        else
        {
            // Owner: check if holen should sleep to save bandwidth
            CheckSleepState();
        }
    }

    /// <summary>
    /// Smoothly sync non-owned holens to the network state
    /// </summary>
    private void SyncToNetworkState()
    {
        // Calculate distance from network position
        lagDistance = Vector3.Distance(transform.position, networkPosition);

        // If too far away, teleport instead of lerp (prevents rubber-banding)
        if (lagDistance > teleportThreshold)
        {
            transform.position = networkPosition;
            transform.rotation = networkRotation;
            rb.velocity = networkVelocity;
            rb.angularVelocity = networkAngularVelocity;

            if (showDebugInfo)
                Debug.Log($"[HolenPhysicsSync] {gameObject.name} teleported (distance: {lagDistance:F2}m)");

            return;
        }

        // Smoothly interpolate position (preserve Y position)
        Vector3 targetPos = networkPosition;
        targetPos.y = transform.position.y; // Keep Y frozen for billiards gameplay

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            positionLerpFactor
        );

        // Smoothly interpolate rotation (only Y-axis rotation for billiards)
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            networkRotation,
            rotationLerpFactor
        );

        // Apply network velocity for smoother prediction
        if (networkVelocity.magnitude > velocityThreshold)
        {
            rb.velocity = Vector3.Lerp(rb.velocity, networkVelocity, positionLerpFactor);
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, networkAngularVelocity, rotationLerpFactor);
        }
        else
        {
            // Network object is at rest, slow down local object too
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, positionLerpFactor * 2f);
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, rotationLerpFactor * 2f);
        }
    }

    /// <summary>
    /// Check if the holen has stopped moving
    /// </summary>
    private void CheckSleepState()
    {
        float totalVelocity = rb.velocity.magnitude + rb.angularVelocity.magnitude;
        bool shouldSleep = totalVelocity < velocityThreshold && timeSinceLastImpact > sleepDelay;

        if (shouldSleep && !isSleeping)
        {
            // Just stopped moving
            isSleeping = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (showDebugInfo)
                Debug.Log($"[HolenPhysicsSync] {gameObject.name} stopped moving");
        }
        else if (!shouldSleep && isSleeping)
        {
            // Started moving again
            isSleeping = false;

            if (showDebugInfo)
                Debug.Log($"[HolenPhysicsSync] {gameObject.name} started moving");
        }
    }

    /// <summary>
    /// Photon serialization - called automatically to sync over network
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send our state to other players
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(rb.velocity);
            stream.SendNext(rb.angularVelocity);
            stream.SendNext(isSleeping);
        }
        else
        {
            // Receive state from the owner
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkVelocity = (Vector3)stream.ReceiveNext();
            networkAngularVelocity = (Vector3)stream.ReceiveNext();
            bool networkIsSleeping = (bool)stream.ReceiveNext();

            // If network says object is sleeping, force local to sleep too
            if (networkIsSleeping && !isSleeping)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                isSleeping = true;
            }
        }
    }

    /// <summary>
    /// CRITICAL: Handle collision-based ownership transfer
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        timeSinceLastImpact = 0f;

        // Wake up when hit
        if (isSleeping)
        {
            WakeUp();
        }

        // Check if we should transfer ownership
        if (autoTransferOwnership && collision.impulse.magnitude > ownershipTransferThreshold)
        {
            // Check if the collision was with another player's holen
            PhotonView otherPV = collision.gameObject.GetComponent<PhotonView>();

            if (otherPV != null && otherPV.IsMine && !pv.IsMine)
            {
                // The other object is owned by this client, and this object is not
                // Transfer ownership to this client for accurate physics simulation
                RequestOwnership();
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"[HolenPhysicsSync] {gameObject.name} collision with {collision.gameObject.name} (impulse: {collision.impulse.magnitude:F2})");
        }
    }

    /// <summary>
    /// Request ownership transfer from Photon
    /// </summary>
    public void RequestOwnership()
    {
        if (!pv.IsMine)
        {
            pv.RequestOwnership();

            if (showDebugInfo)
                Debug.Log($"[HolenPhysicsSync] {gameObject.name} ownership requested by {PhotonNetwork.LocalPlayer.NickName}");
        }
    }

    /// <summary>
    /// Transfer ownership immediately (use before launching)
    /// </summary>
    public void TransferOwnership(Photon.Realtime.Player newOwner)
    {
        if (pv.Owner != newOwner)
        {
            pv.TransferOwnership(newOwner);

            if (showDebugInfo)
                Debug.Log($"[HolenPhysicsSync] {gameObject.name} ownership transferred to {newOwner.NickName}");
        }
    }

    /// <summary>
    /// Call this when manually moving/launching a holen
    /// </summary>
    public void WakeUp()
    {
        isSleeping = false;
        rb.WakeUp();
        timeSinceLastImpact = 0f;

        if (showDebugInfo && pv.IsMine)
            Debug.Log($"[HolenPhysicsSync] {gameObject.name} woken up");
    }

    /// <summary>
    /// Call this BEFORE applying force to ensure proper sync and ownership
    /// </summary>
    public bool PrepareForLaunch()
    {
        // Ensure we own this object before applying force
        if (!pv.IsMine)
        {
            Debug.LogWarning($"[HolenPhysicsSync] Cannot launch {gameObject.name} - we don't own it! Requesting ownership...");
            RequestOwnership();
            return false;
        }

        WakeUp();
        return true;
    }

    /// <summary>
    /// Apply force to the holen (handles ownership and wake-up automatically)
    /// </summary>
    public void ApplyForce(Vector3 force, ForceMode forceMode = ForceMode.Impulse)
    {
        if (!PrepareForLaunch())
        {
            Debug.LogError($"[HolenPhysicsSync] Failed to launch {gameObject.name} - ownership issue");
            return;
        }

        rb.AddForce(force, forceMode);

        if (showDebugInfo)
            Debug.Log($"[HolenPhysicsSync] {gameObject.name} force applied: {force} ({forceMode})");
    }

    /// <summary>
    /// Set velocity directly (handles ownership and wake-up automatically)
    /// </summary>
    public void SetVelocity(Vector3 velocity)
    {
        if (!PrepareForLaunch())
        {
            Debug.LogError($"[HolenPhysicsSync] Failed to set velocity on {gameObject.name} - ownership issue");
            return;
        }

        rb.velocity = velocity;

        if (showDebugInfo)
            Debug.Log($"[HolenPhysicsSync] {gameObject.name} velocity set: {velocity}");
    }

    /// <summary>
    /// Check if all holens on the field have stopped moving
    /// </summary>
    public static bool AreAllHolensStopped()
    {
        HolenPhysicsSync[] allHolens = FindObjectsOfType<HolenPhysicsSync>();

        foreach (var holen in allHolens)
        {
            if (!holen.isSleeping)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Get all holens that are currently moving
    /// </summary>
    public static HolenPhysicsSync[] GetMovingHolens()
    {
        HolenPhysicsSync[] allHolens = FindObjectsOfType<HolenPhysicsSync>();
        return System.Array.FindAll(allHolens, h => !h.isSleeping);
    }

    /// <summary>
    /// Check if this specific holen is stopped
    /// </summary>
    public bool IsStopped()
    {
        return isSleeping;
    }

    /// <summary>
    /// Optional: Call after spawning to ensure initial sync
    /// </summary>
    public void ForceSync()
    {
        if (pv.IsMine)
        {
            // Forces a sync on the next frame by marking as moving
            isSleeping = false;
            timeSinceLastImpact = 0f;
        }
    }

    /// <summary>
    /// Get the current owner's name (for debugging)
    /// </summary>
    public string GetOwnerName()
    {
        return pv.Owner != null ? pv.Owner.NickName : "None";
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showDebugInfo || !Application.isPlaying) return;

        // Show ownership with color
        Gizmos.color = pv != null && pv.IsMine ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.15f);

        // Show velocity
        if (rb != null && rb.velocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + rb.velocity * 0.5f);
        }

        // Show sync status for non-owned objects
        if (pv != null && !pv.IsMine && lagDistance > 0.01f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, networkPosition);
        }
    }
#endif
}