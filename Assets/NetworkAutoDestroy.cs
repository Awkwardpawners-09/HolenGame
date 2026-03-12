using Photon.Pun;
using UnityEngine;

/// <summary>
/// NetworkAutoDestroy — attach to any PhotonNetwork.Instantiate'd prefab to give it
/// a self-contained network lifetime timer.
///
/// PVPSYSTEMscript adds this component at runtime after spawning Tin Can and Wall
/// prefabs so the destroy timer lives ON the object, not on an external coroutine.
/// This means the object always destroys itself after `lifetime` seconds regardless
/// of turn switches, scene state, or what PVPSYSTEMscript is doing.
///
/// HOW IT WORKS:
///   • The spawning client owns the PhotonView, so photonView.IsMine is true for them.
///   • Only the owner calls PhotonNetwork.Destroy — this is correct Photon ownership.
///   • If you have Photon's "Auto-Destroy" or "Owner Leaves" settings enabled in your
///     room options, double-check they don't conflict.
///
/// SETUP:
///   You can also pre-attach this to your prefab in the Editor and set lifetime there.
///   PVPSYSTEMscript will overwrite the lifetime value at spawn time if it adds the
///   component, or respect the pre-set value if the component already exists.
/// </summary>
public class NetworkAutoDestroy : MonoBehaviourPun
{
    [Tooltip("Seconds before this networked object destroys itself.")]
    public float lifetime = 20f;

    private void Start()
    {
        // Only the owner destroys their own PhotonNetwork objects
        if (photonView.IsMine)
            Invoke(nameof(DestroyObject), lifetime);
    }

    private void DestroyObject()
    {
        if (gameObject != null && photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
    }
}