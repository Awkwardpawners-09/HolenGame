using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviour
{
    private MultiplayerHolenController gameController;
    private PhotonView photonView;

    private bool isRotatingLeft = false;
    private bool isRotatingRight = false;

    // This method will initialize the PlayerControl with a reference to MultiplayerHolenController and PhotonView
    public void InitializeControls(MultiplayerHolenController controller, PhotonView pv)
    {
        gameController = controller;
        photonView = pv;
    }

    // This method will be called from MultiplayerHolenController to handle rotation and movement
    public void HandleControls(float horizontalInput, float rotationSpeed)
    {
        if (gameController != null && gameController.IsTurn())
        {
            if (horizontalInput < 0)  // Joystick moved left
            {
                RotateHands(-rotationSpeed * Time.deltaTime);
            }
            else if (horizontalInput > 0)  // Joystick moved right
            {
                RotateHands(rotationSpeed * Time.deltaTime);
            }
        }
    }

    // Synchronized rotation method
    private void RotateHands(float rotationAmount)
    {
        // Rotate locally
        transform.Rotate(Vector3.up, rotationAmount);

        // Sync rotation to other players via RPC
        if (photonView != null)
        {
            photonView.RPC("RPC_SyncRotation", RpcTarget.Others, transform.rotation);
        }
    }

    [PunRPC]
    private void RPC_SyncRotation(Quaternion newRotation)
    {
        // Apply the rotation from the active player to all other clients
        transform.rotation = newRotation;
    }

    // Methods to simulate UI button presses for rotation
    public void StartRotatingLeft() { isRotatingLeft = true; }
    public void StopRotatingLeft() { isRotatingLeft = false; }
    public void StartRotatingRight() { isRotatingRight = true; }
    public void StopRotatingRight() { isRotatingRight = false; }
}