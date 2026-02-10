using UnityEngine;
using Photon.Pun;

/// <summary>
/// Add this component to your Holen prefabs to ensure proper sync from Master Client.
/// This handles position, rotation, and velocity syncing when Master Client has physics authority.
/// 
/// IMPORTANT: This component is ONLY active in multiplayer mode.
/// In single-player mode (when not connected to Photon), this component does nothing
/// and will not interfere with your single-player physics.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class HolenSyncComponent : MonoBehaviourPun, IPunObservable
{
    [Header("Sync Settings")]
    [Tooltip("How smoothly to interpolate position updates")]
    public float positionLerpSpeed = 10f;

    [Tooltip("How smoothly to interpolate rotation updates")]
    public float rotationLerpSpeed = 10f;

    [Tooltip("If true, also sync velocity from Master Client")]
    public bool syncVelocity = true;

    [Header("Mode Detection")]
    [Tooltip("If true, shows debug info about whether component is active")]
    public bool showModeDebug = false;

    private Rigidbody rb;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 networkVelocity;
    private Vector3 networkAngularVelocity;

    private bool isInitialized = false;
    private bool isMultiplayerMode = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Initialize network values to current values
        networkPosition = transform.position;
        networkRotation = transform.rotation;

        if (rb != null)
        {
            networkVelocity = rb.velocity;
            networkAngularVelocity = rb.angularVelocity;
        }

        isInitialized = true;
    }

    void Start()
    {
        // Check if we're in multiplayer mode
        isMultiplayerMode = PhotonNetwork.IsConnected;

        if (showModeDebug)
        {
            if (isMultiplayerMode)
            {
                Debug.Log($"[HolenSyncComponent] {gameObject.name} - MULTIPLAYER MODE - Sync enabled");
            }
            else
            {
                Debug.Log($"[HolenSyncComponent] {gameObject.name} - SINGLE-PLAYER MODE - Component inactive, won't affect physics");
            }
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        // CRITICAL: Only run sync logic if we're in multiplayer mode
        if (!isMultiplayerMode) return;

        // CRITICAL FIX: Only non-Master-Client players interpolate
        // Master Client simulates physics, others just display the results
        if (!PhotonNetwork.IsMasterClient)
        {
            // Smoothly interpolate to network position/rotation
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * rotationLerpSpeed);

            // If we have a rigidbody and it's kinematic, we can also set velocity for visual effects
            if (syncVelocity && rb != null && rb.isKinematic)
            {
                // Store velocity for potential use (e.g., trail effects)
                // Don't actually apply force since physics is disabled on non-Master clients
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // CRITICAL: Only sync data if we're in multiplayer mode
        if (!isMultiplayerMode) return;

        if (stream.IsWriting)
        {
            // CRITICAL FIX: Only Master Client sends data
            // This ensures only the authoritative client broadcasts physics state
            if (PhotonNetwork.IsMasterClient)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);

                if (syncVelocity && rb != null)
                {
                    stream.SendNext(rb.velocity);
                    stream.SendNext(rb.angularVelocity);
                }
                else
                {
                    stream.SendNext(Vector3.zero);
                    stream.SendNext(Vector3.zero);
                }
            }
        }
        else
        {
            // Other clients receive and store network state
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkVelocity = (Vector3)stream.ReceiveNext();
            networkAngularVelocity = (Vector3)stream.ReceiveNext();

            // For very fast-moving objects, you might want to extrapolate
            // based on velocity and the time since the last update
            if (syncVelocity)
            {
                float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                networkPosition += networkVelocity * lag;
            }
        }
    }

    /// <summary>
    /// Call this to forcefully sync position immediately (useful after teleporting)
    /// </summary>
    public void ForceSync()
    {
        networkPosition = transform.position;
        networkRotation = transform.rotation;

        if (rb != null)
        {
            networkVelocity = rb.velocity;
            networkAngularVelocity = rb.angularVelocity;
        }
    }

    public Vector3 GetNetworkVelocity()
    {
        return networkVelocity;
    }

    public Vector3 GetNetworkAngularVelocity()
    {
        return networkAngularVelocity;
    }
}