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
///   4. Before joining a room, set the avatar index in Custom Properties:
///        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
///        props["AvatarIndex"] = PlayerDataManager.Instance.playerData.selectedAvatarIndex;
///        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
///   5. Done — the UI auto-populates once both players are in the room.
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
        // Attempt an immediate update in case we joined a room that already has 2 players.
        RefreshUI();
    }

    // ────────────────────────────────────────────────────────────────────────
    //  PHOTON CALLBACKS  (MonoBehaviourPunCallbacks)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>Fires when a new player enters the room — refresh so slot 2 populates.</summary>
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        RefreshUI();
    }

    /// <summary>Fires when a player's Custom Properties change (e.g. avatar index updated mid-session).</summary>
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
    /// Falls back to fallbackSprite if anything is missing.
    /// </summary>
    private Sprite ResolveAvatarSprite(Photon.Realtime.Player photonPlayer)
    {
        if (PlayerDataManager.Instance == null) return fallbackSprite;

        Sprite[] sprites = PlayerDataManager.Instance.avatarSprites;
        if (sprites == null || sprites.Length == 0) return fallbackSprite;

        int index = 0;

        // For the local player, read directly from PlayerDataManager (always accurate).
        if (photonPlayer.IsLocal)
        {
            index = PlayerDataManager.Instance.playerData.selectedAvatarIndex;
        }
        else
        {
            // For remote players, read from their Photon Custom Properties.
            if (photonPlayer.CustomProperties.TryGetValue(AVATAR_INDEX_KEY, out object raw))
            {
                index = raw is int i ? i : 0;
            }
        }

        index = Mathf.Clamp(index, 0, sprites.Length - 1);
        return sprites[index] != null ? sprites[index] : fallbackSprite;
    }

    // ────────────────────────────────────────────────────────────────────────
    //  OPTIONAL: Call this anywhere to force a full refresh (e.g. after lobby).
    // ────────────────────────────────────────────────────────────────────────
    public void ForceRefresh() => RefreshUI();
}