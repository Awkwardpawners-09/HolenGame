using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

/// <summary>
/// Multiplayer Player UI — displays Player 1 and Player 2 info (name + avatar).
///
/// HOW IT WORKS:
///   - The LOCAL player is ALWAYS shown as "Player 1" in the UI (left/top slot).
///   - The REMOTE opponent is always shown as "Player 2" (right/bottom slot).
///   - Player names are pulled from Photon nicknames (set these before joining a room).
///   - Avatars are pulled from PlayerDataManager.avatarSprites using the avatar index
///     stored in each player's Photon Custom Properties under the key "AvatarIndex".
///
/// SETUP:
///   1. Add this component to any GameObject in your multiplayer scene.
///   2. Assign the Inspector fields below (TMP texts + Image slots).
///   3. Before joining a room, set the local player's Photon nickname:
///        PhotonNetwork.LocalPlayer.NickName = PlayerDataManager.Instance.GetPlayerName();
///   4. That's it — this script now handles pushing the AvatarIndex to Photon automatically
///      via OnJoinedRoom. You no longer need to set Custom Properties manually before joining.
///
/// NOTE: Do NOT modify MultiplayerHolenControllerNew.cs — this script is fully standalone.
/// </summary>
public class MultiplayerPlayerUI : MonoBehaviourPunCallbacks
{
    private const string AVATAR_INDEX_KEY = "AvatarIndex";

    // ── Inspector: Player 1 Slot (always LOCAL player) ──────────────────────
    [Header("Player 1 Slot  (always the LOCAL player)")]
    [Tooltip("TMP text that shows the local player's name.")]
    public TMP_Text player1NameText;

    [Tooltip("Image that shows the local player's avatar sprite.")]
    public Image player1AvatarImage;

    // ── Inspector: Player 2 Slot (always REMOTE opponent) ───────────────────
    [Header("Player 2 Slot  (always the REMOTE opponent)")]
    [Tooltip("TMP text that shows the opponent's name.")]
    public TMP_Text player2NameText;

    [Tooltip("Image that shows the opponent's avatar sprite.")]
    public Image player2AvatarImage;

    // ── Inspector: Fallbacks ─────────────────────────────────────────────────
    [Header("Fallbacks")]
    [Tooltip("Shown when a player has no name set.")]
    public string fallbackLocalName = "You";
    [Tooltip("Shown when the opponent has no name set.")]
    public string fallbackRemoteName = "Opponent";
    [Tooltip("Shown when a sprite cannot be resolved.")]
    public Sprite fallbackSprite;

    // ────────────────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // If we're already in a room when this script starts (e.g. scene was loaded
        // after joining), push our properties immediately and refresh.
        if (PhotonNetwork.InRoom)
        {
            PushLocalPlayerPropertiesToPhoton();
        }

        RefreshUI();
    }

    // ────────────────────────────────────────────────────────────────────────
    //  PHOTON CALLBACKS  (MonoBehaviourPunCallbacks)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fires the moment we successfully join a room.
    /// This is the most reliable place to push our AvatarIndex to Photon
    /// so the opponent always receives the correct value.
    /// </summary>
    public override void OnJoinedRoom()
    {
        PushLocalPlayerPropertiesToPhoton();
        RefreshUI();
    }

    /// <summary>Fires when a new player enters the room — refresh so slot 2 populates.</summary>
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        RefreshUI();
    }

    /// <summary>
    /// Fires when any player's Custom Properties change.
    /// Covers both AvatarIndex and NickName updates that may arrive mid-session.
    /// </summary>
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer,
                                                  ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey(AVATAR_INDEX_KEY))
            RefreshUI();
    }

    // ────────────────────────────────────────────────────────────────────────
    //  CORE UPDATE
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Populates both UI slots.
    /// Slot 1 = local player, Slot 2 = remote opponent (regardless of ActorNumber).
    /// </summary>
    public void RefreshUI()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.CurrentRoom == null)
        {
            // Not in a room yet — show placeholders.
            SetSlot1("Waiting...", null);
            SetSlot2("Waiting...", null);
            return;
        }

        Photon.Realtime.Player local = PhotonNetwork.LocalPlayer;
        Photon.Realtime.Player remote = GetRemotePlayer();

        // ── Slot 1: local player ─────────────────────────────────────────
        string localName = string.IsNullOrWhiteSpace(local.NickName) ? fallbackLocalName : local.NickName;
        Sprite localSprite = ResolveAvatarSprite(local);
        SetSlot1(localName, localSprite);

        // ── Slot 2: opponent ─────────────────────────────────────────────
        if (remote != null)
        {
            string remoteName = string.IsNullOrWhiteSpace(remote.NickName) ? fallbackRemoteName : remote.NickName;
            Sprite remoteSprite = ResolveAvatarSprite(remote);
            SetSlot2(remoteName, remoteSprite);
        }
        else
        {
            SetSlot2("Waiting for opponent...", null);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    //  PHOTON PROPERTY SYNC
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pushes the local player's current AvatarIndex (and NickName as a safety net)
    /// into Photon Custom Properties so every other client can read them.
    ///
    /// Called automatically in OnJoinedRoom and on Start (if already in a room).
    /// You can also call this manually after the player changes their avatar mid-session.
    /// </summary>
    private void PushLocalPlayerPropertiesToPhoton()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("[MultiplayerPlayerUI] PlayerDataManager.Instance is null — cannot push avatar index.");
            return;
        }

        int avatarIndex = PlayerDataManager.Instance.playerData.selectedAvatarIndex;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { AVATAR_INDEX_KEY, avatarIndex }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // Also guarantee the NickName is set from PlayerDataManager in case
        // the caller forgot to set it before joining.
        if (string.IsNullOrWhiteSpace(PhotonNetwork.LocalPlayer.NickName))
        {
            string savedName = PlayerDataManager.Instance.GetPlayerName();
            if (!string.IsNullOrWhiteSpace(savedName))
                PhotonNetwork.LocalPlayer.NickName = savedName;
        }

        Debug.Log($"[MultiplayerPlayerUI] Pushed to Photon → AvatarIndex: {avatarIndex}, NickName: {PhotonNetwork.LocalPlayer.NickName}");
    }

    // ────────────────────────────────────────────────────────────────────────
    //  HELPERS
    // ────────────────────────────────────────────────────────────────────────

    private void SetSlot1(string playerName, Sprite sprite)
    {
        if (player1NameText != null) player1NameText.text = playerName;
        if (player1AvatarImage != null) player1AvatarImage.sprite = sprite != null ? sprite : fallbackSprite;
    }

    private void SetSlot2(string playerName, Sprite sprite)
    {
        if (player2NameText != null) player2NameText.text = playerName;
        if (player2AvatarImage != null) player2AvatarImage.sprite = sprite != null ? sprite : fallbackSprite;
    }

    /// <summary>Returns the first player in the room who is NOT the local player.</summary>
    private Photon.Realtime.Player GetRemotePlayer()
    {
        foreach (var kvp in PhotonNetwork.CurrentRoom.Players)
        {
            if (!kvp.Value.IsLocal)
                return kvp.Value;
        }
        return null;
    }

    /// <summary>
    /// Reads the "AvatarIndex" Custom Property from the given Photon player and
    /// looks up the corresponding sprite from PlayerDataManager.avatarSprites.
    ///
    /// For the LOCAL player  → always reads from PlayerDataManager directly (ground truth).
    /// For the REMOTE player → reads from their Photon Custom Properties.
    ///
    /// Falls back to fallbackSprite if anything is missing.
    /// </summary>
    private Sprite ResolveAvatarSprite(Photon.Realtime.Player photonPlayer)
    {
        if (PlayerDataManager.Instance == null) return fallbackSprite;

        Sprite[] sprites = PlayerDataManager.Instance.avatarSprites;
        if (sprites == null || sprites.Length == 0) return fallbackSprite;

        int index = 0;

        if (photonPlayer.IsLocal)
        {
            // Local player — read directly from PlayerDataManager (always accurate).
            index = PlayerDataManager.Instance.playerData.selectedAvatarIndex;
        }
        else
        {
            // Remote player — read from their Photon Custom Properties.
            if (photonPlayer.CustomProperties.TryGetValue(AVATAR_INDEX_KEY, out object raw))
            {
                // Photon sometimes deserialises integers as int or as byte — handle both.
                if (raw is int i) index = i;
                else if (raw is byte b) index = b;
                else
                {
                    // Last-resort: try a string parse (edge case with some Photon versions).
                    int.TryParse(raw.ToString(), out index);
                }
            }
            else
            {
                // Key not yet received — this can happen if the opponent joined before
                // their properties propagated. Log a warning so it's easy to spot.
                Debug.LogWarning($"[MultiplayerPlayerUI] '{AVATAR_INDEX_KEY}' not found in Custom Properties for player '{photonPlayer.NickName}'. Showing fallback.");
            }
        }

        index = Mathf.Clamp(index, 0, sprites.Length - 1);
        return sprites[index] != null ? sprites[index] : fallbackSprite;
    }

    // ────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this anywhere to force a full refresh (e.g. after returning from a lobby).
    /// </summary>
    public void ForceRefresh() => RefreshUI();

    /// <summary>
    /// Call this if the local player changes their avatar WHILE already in a room
    /// (e.g. in a pre-game lobby scene that uses Photon). Pushes the new index to
    /// all other clients and refreshes the local UI.
    /// </summary>
    public void OnLocalAvatarChanged()
    {
        PushLocalPlayerPropertiesToPhoton();
        RefreshUI();
    }
}