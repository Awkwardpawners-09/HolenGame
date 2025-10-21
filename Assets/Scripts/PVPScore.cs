using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class PVPScore : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_Text localPlayerScoreText;   // Shows "You: X"
    public TMP_Text opponentScoreText;      // Shows "Opponent: X"
    public GameObject winnerUI;             // UI to show when local player wins
    public GameObject loserUI;              // UI to show when local player loses

    [Header("Settings")]
    public float noHolensWaitTime = 5f;     // Time to wait if no holens remain before game over
    public int pointsPerKnockout = 1;       // Points per holen knocked out

    private int player1Score = 0;
    private int player2Score = 0;

    private float noHolensTimer = 0f;
    private bool gameOverTriggered = false;

    private MultiplayerHolenController holenController;
    private List<GameObject> holensToDestroy = new List<GameObject>();

    void Start()
    {
        holenController = FindObjectOfType<MultiplayerHolenController>();

        UpdateScoreUI();

        if (winnerUI != null)
            winnerUI.SetActive(false);
        if (loserUI != null)
            loserUI.SetActive(false);
    }

    void Update()
    {
        GameObject[] allHolens = GameObject.FindGameObjectsWithTag("Objective");
        int holensInside = 0;

        foreach (GameObject holen in allHolens)
        {
            Collider holenCollider = holen.GetComponent<Collider>();
            if (holenCollider != null && IsInsideTrigger(holenCollider))
            {
                holensInside++;
            }
        }

        if (holensInside == 0)
        {
            noHolensTimer += Time.deltaTime;

            if (noHolensTimer >= noHolensWaitTime && !gameOverTriggered)
            {
                TriggerGameOver();
            }
        }
        else
        {
            noHolensTimer = 0f;
        }
    }

    private bool IsInsideTrigger(Collider otherCollider)
    {
        Collider thisTrigger = GetComponent<Collider>();
        if (thisTrigger != null && thisTrigger.isTrigger)
        {
            return thisTrigger.bounds.Intersects(otherCollider.bounds);
        }
        return false;
    }

    private void TriggerGameOver()
    {
        gameOverTriggered = true;

        bool localPlayerWon = false;

        if (holenController != null)
        {
            if (holenController.isPlayer1)
            {
                localPlayerWon = player1Score > player2Score;
            }
            else
            {
                localPlayerWon = player2Score > player1Score;
            }
        }

        if (localPlayerWon)
        {
            if (winnerUI != null)
                winnerUI.SetActive(true);
            Debug.Log($"You Win! Final Scores - You: {(holenController.isPlayer1 ? player1Score : player2Score)}, Opponent: {(holenController.isPlayer1 ? player2Score : player1Score)}");
        }
        else
        {
            if (loserUI != null)
                loserUI.SetActive(true);
            Debug.Log($"You Lose! Final Scores - You: {(holenController.isPlayer1 ? player1Score : player2Score)}, Opponent: {(holenController.isPlayer1 ? player2Score : player1Score)}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Objective"))
        {
            if (holenController != null && holenController.currentHolenBall != null)
            {
                if (other.gameObject == holenController.currentHolenBall)
                {
                    return;
                }
            }

            AwardPointsToCurrentPlayer();

            if (!holensToDestroy.Contains(other.gameObject))
            {
                holensToDestroy.Add(other.gameObject);
            }
        }
    }

    private void AwardPointsToCurrentPlayer()
    {
        if (holenController == null) return;

        if (holenController.isPlayer1 && holenController.IsTurn())
        {
            player1Score += pointsPerKnockout;
            Debug.Log($"Player 1 knocked out a holen! Score: {player1Score}");

            photonView.RPC("RPC_UpdateScore", RpcTarget.Others, 1, player1Score);
        }
        else if (!holenController.isPlayer1 && holenController.IsTurn())
        {
            player2Score += pointsPerKnockout;
            Debug.Log($"Player 2 knocked out a holen! Score: {player2Score}");

            photonView.RPC("RPC_UpdateScore", RpcTarget.Others, 2, player2Score);
        }

        UpdateScoreUI();
    }

    [PunRPC]
    private void RPC_UpdateScore(int playerNumber, int newScore)
    {
        if (playerNumber == 1)
        {
            player1Score = newScore;
        }
        else if (playerNumber == 2)
        {
            player2Score = newScore;
        }

        UpdateScoreUI();
    }

    public void OnTurnEnd()
    {
        StartCoroutine(DestroyQueuedHolens());
    }

    private IEnumerator DestroyQueuedHolens()
    {
        yield return new WaitForSeconds(0.5f);

        foreach (GameObject holen in holensToDestroy)
        {
            if (holen != null && PhotonNetwork.IsMasterClient)
            {
                PhotonView pv = holen.GetComponent<PhotonView>();
                if (pv != null)
                {
                    PhotonNetwork.Destroy(holen);
                }
                else
                {
                    Destroy(holen);
                }
            }
        }

        holensToDestroy.Clear();
    }

    private void UpdateScoreUI()
    {
        if (holenController == null) return;

        int localScore = holenController.isPlayer1 ? player1Score : player2Score;
        int opponentScore = holenController.isPlayer1 ? player2Score : player1Score;

        if (localPlayerScoreText != null)
            localPlayerScoreText.text = $"You: {localScore}";

        if (opponentScoreText != null)
            opponentScoreText.text = $"Opponent: {opponentScore}";
    }
}