using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static class to hold PVP match results between scene transitions.
/// This persists even when PVPScore singleton is destroyed.
/// </summary>
public static class PVPDataHolder
{
    [System.Serializable]
    public class KnockedOutHolen
    {
        public string holenID;
        public string holenName;
        public int playerNumber; // 1 or 2

        public KnockedOutHolen(string id, string name, int player)
        {
            holenID = id;
            holenName = name;
            playerNumber = player;
        }
    }

    private static List<KnockedOutHolen> player1KnockedOut = new List<KnockedOutHolen>();
    private static List<KnockedOutHolen> player2KnockedOut = new List<KnockedOutHolen>();
    private static int localPlayerNumber = 0;

    /// <summary>
    /// Store the match results before transitioning to result scene.
    /// Accepts raw data to avoid type dependency on PVPScore.
    /// </summary>
    public static void StoreMatchResults(
        List<(string holenID, string holenName, int playerNumber)> p1Holens,
        List<(string holenID, string holenName, int playerNumber)> p2Holens,
        int localPlayer)
    {
        player1KnockedOut.Clear();
        player2KnockedOut.Clear();

        // Convert tuples to our static version
        foreach (var holen in p1Holens)
        {
            player1KnockedOut.Add(new KnockedOutHolen(holen.holenID, holen.holenName, holen.playerNumber));
        }

        foreach (var holen in p2Holens)
        {
            player2KnockedOut.Add(new KnockedOutHolen(holen.holenID, holen.holenName, holen.playerNumber));
        }

        localPlayerNumber = localPlayer;

        Debug.Log($"[PVPDataHolder] Stored results: P1={player1KnockedOut.Count}, P2={player2KnockedOut.Count}, LocalPlayer={localPlayerNumber}");
    }

    /// <summary>
    /// Get holens knocked out by a specific player.
    /// </summary>
    public static List<KnockedOutHolen> GetPlayerKnockedOutHolens(int playerNumber)
    {
        if (playerNumber == 1)
            return new List<KnockedOutHolen>(player1KnockedOut);
        else if (playerNumber == 2)
            return new List<KnockedOutHolen>(player2KnockedOut);

        return new List<KnockedOutHolen>();
    }

    /// <summary>
    /// Get the local player's number.
    /// </summary>
    public static int GetLocalPlayerNumber()
    {
        return localPlayerNumber;
    }

    /// <summary>
    /// Check if we have valid match data stored.
    /// </summary>
    public static bool HasMatchData()
    {
        return localPlayerNumber > 0;
    }

    /// <summary>
    /// Clear all stored data after results have been processed.
    /// </summary>
    public static void ClearData()
    {
        player1KnockedOut.Clear();
        player2KnockedOut.Clear();
        localPlayerNumber = 0;
        Debug.Log("[PVPDataHolder] Data cleared");
    }
}