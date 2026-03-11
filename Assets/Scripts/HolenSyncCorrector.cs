using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Attach this component to every wager Holen prefab (alongside the existing PhotonView
/// and PhotonRigidbodyView components).
///
/// HOW IT WORKS
/// ────────────
/// 1. MASTER CLIENT periodically broadcasts its authoritative position + velocity for
///    this holen to all other clients via RPC.
///
/// 2. NON-MASTER CLIENTS compare the received position to their local one. If the gap
///    exceeds positionSnapThreshold they hard-snap immediately. If it's within the softer
///    smoothingThreshold they lerp smoothly toward the authoritative position so the
///    correction is invisible to the player.
///
/// 3. When the holen is SLEEPING (not moving), syncs are paused to save bandwidth.
///    One final "settle" sync is sent when the holen comes to rest so both clients end
///    up at exactly the same resting position.
///
/// SETUP
/// ─────
/// • Add this script to each wager Holen prefab.
/// • The existing PhotonView on the prefab is used automatically (no extra PhotonView needed).
/// • Tune the thresholds in the Inspector if needed.
/// </summary>
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody))]
public class HolenSyncCorrector : MonoBehaviourPun
{
    [Header("Sync Timing")]
    [Tooltip("How often (seconds) the Master Client broadcasts its authoritative state.")]
    public float syncInterval = 0.1f;   // 10 times per second

    [Header("Correction Thresholds")]
    [Tooltip("Distance gap that triggers an instant hard snap (large desync).")]
    public float positionSnapThreshold = 1.5f;

    [Tooltip("Distance gap that triggers a smooth lerp correction (small desync).")]
    public float positionSmoothThreshold = 0.05f;

    [Tooltip("How quickly the smooth correction lerps to the authoritative position. Higher = snappier.")]
    [Range(1f, 30f)]
    public float smoothingSpeed = 12f;

    [Tooltip("Velocity difference that triggers a velocity correction even when positions match.")]
    public float velocitySnapThreshold = 1.0f;

    // ── Private state ──────────────────────────────────────────────
    private Rigidbody rb;
    private float syncTimer;

    // Target state received from Master Client (used by non-master for smooth lerp)
    private Vector3 targetPosition;
    private Vector3 targetVelocity;
    private bool hasReceivedSync = false;

    // Track whether holen was sleeping last frame to detect wake/sleep transitions
    private bool wasSleeping = false;

    // ── Unity lifecycle ────────────────────────────────────────────
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        targetPosition = transform.position;
        targetVelocity = Vector3.zero;
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            MasterClientUpdate();
        }
        else
        {
            NonMasterClientUpdate();
        }
    }

    // ── Master Client: broadcast authoritative state ───────────────
    private void MasterClientUpdate()
    {
        bool isSleeping = rb.IsSleeping();

        // Send one final sync when the holen just came to rest
        if (!wasSleeping && isSleeping)
        {
            BroadcastState();
        }

        wasSleeping = isSleeping;

        // Skip periodic syncs while sleeping (nothing is changing)
        if (isSleeping) return;

        syncTimer -= Time.deltaTime;
        if (syncTimer <= 0f)
        {
            syncTimer = syncInterval;
            BroadcastState();
        }
    }

    private void BroadcastState()
    {
        photonView.RPC(
            nameof(RPC_ReceiveState),
            RpcTarget.Others,
            transform.position,
            rb.velocity,
            rb.angularVelocity,
            rb.IsSleeping()
        );
    }

    // ── Non-Master Client: receive and correct ─────────────────────
    private void NonMasterClientUpdate()
    {
        if (!hasReceivedSync) return;
        if (rb.IsSleeping()) return; // Already at rest; hard snap was already applied

        float positionError = Vector3.Distance(transform.position, targetPosition);

        if (positionError > positionSnapThreshold)
        {
            // Large desync — hard snap to authoritative position
            rb.MovePosition(targetPosition);
            rb.velocity = targetVelocity;
            Debug.Log($"[HolenSyncCorrector] Hard snap on '{gameObject.name}': error={positionError:F2}m");
        }
        else if (positionError > positionSmoothThreshold)
        {
            // Small desync — smooth lerp toward authoritative position
            Vector3 corrected = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothingSpeed);
            rb.MovePosition(corrected);

            // Also correct velocity if it's significantly off
            if (Vector3.Distance(rb.velocity, targetVelocity) > velocitySnapThreshold)
                rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, Time.deltaTime * smoothingSpeed);
        }
    }

    // ── RPC received by non-master clients ────────────────────────
    [PunRPC]
    private void RPC_ReceiveState(
        Vector3 authPosition,
        Vector3 authVelocity,
        Vector3 authAngularVelocity,
        bool authIsSleeping)
    {
        // Store as target so NonMasterClientUpdate can smoothly correct toward it
        targetPosition = authPosition;
        targetVelocity = authVelocity;
        hasReceivedSync = true;

        if (authIsSleeping)
        {
            // Holen has settled — hard snap to final resting position immediately
            rb.isKinematic = false;
            rb.MovePosition(authPosition);
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
            Debug.Log($"[HolenSyncCorrector] Settle snap on '{gameObject.name}' → {authPosition}");
        }
        else
        {
            // Apply angular velocity directly (not smoothed — small enough to not be jarring)
            rb.angularVelocity = authAngularVelocity;
        }
    }
}