using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class OnlineRoomManager : MonoBehaviour
{
    [Header("Room Settings")]
    [SerializeField] private int maxPlayers = 8;
    [SerializeField] private string defaultRoomName = "PenguinRoom";

    [Header("UI References (Optional)")]
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text statusText;

    private ISession currentSession;
    private bool isInitialized;
    private bool isBusy;

    private void Awake()
    {
        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
        }
    }

    private async void Start()
    {
        await InitializeServicesAsync();
    }

    private async void OnDestroy()
    {
        if (createRoomButton != null)
        {
            createRoomButton.onClick.RemoveListener(OnCreateRoomClicked);
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.RemoveListener(OnJoinRoomClicked);
        }

        await LeaveCurrentSessionAsync();
    }

    /// <summary>
    /// Initializes Unity Gaming Services and signs in anonymously so the player can create or join sessions.
    /// </summary>
    public async Task InitializeServicesAsync()
    {
        if (isInitialized)
        {
            return;
        }

        try
        {
            SetStatus("Initializing services...");

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            isInitialized = true;
            SetStatus("Ready");
        }
        catch (Exception ex)
        {
            SetStatus("Auth/Services error: " + ex.Message);
            Debug.LogError($"[OnlineRoomManager] Failed to initialize services: {ex}");
        }
    }

    public async void OnCreateRoomClicked()
    {
        await CreateRoomAsync();
    }

    public async void OnJoinRoomClicked()
    {
        string code = joinCodeInputField != null ? joinCodeInputField.text.Trim() : string.Empty;
        await JoinRoomByCodeAsync(code);
    }

    /// <summary>
    /// Creates a new multiplayer session with Relay networking enabled and displays the join code.
    /// </summary>
    public async Task CreateRoomAsync()
    {
        if (isBusy)
        {
            return;
        }

        isBusy = true;
        SetInteractable(false);

        try
        {
            await InitializeServicesAsync();
            if (!isInitialized)
            {
                return;
            }

            await LeaveCurrentSessionAsync();

            SetStatus("Creating room...");

            var options = new SessionOptions
            {
                Name = defaultRoomName,
                MaxPlayers = maxPlayers
            }.WithRelayNetwork();

            currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            SubscribeToSessionEvents(currentSession);

            string joinCode = currentSession.Code;
            if (roomCodeText != null)
            {
                roomCodeText.text = $"Code: {joinCode}";
            }

            SetStatus($"Room created ({currentSession.Players.Count}/{maxPlayers})");
            Debug.Log($"[OnlineRoomManager] Session created. ID: {currentSession.Id}, Code: {joinCode}");
        }
        catch (Exception ex)
        {
            SetStatus("Create failed: " + ex.Message);
            Debug.LogError($"[OnlineRoomManager] Error creating room: {ex}");
        }
        finally
        {
            isBusy = false;
            SetInteractable(true);
        }
    }

    /// <summary>
    /// Joins an existing multiplayer session using its room/join code.
    /// </summary>
    public async Task JoinRoomByCodeAsync(string joinCode)
    {
        if (isBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus("Enter a room code first.");
            return;
        }

        isBusy = true;
        SetInteractable(false);

        try
        {
            await InitializeServicesAsync();
            if (!isInitialized)
            {
                return;
            }

            await LeaveCurrentSessionAsync();

            SetStatus("Joining room...");

            string normalizedCode = joinCode.Trim().ToUpperInvariant();
            currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(normalizedCode);
            SubscribeToSessionEvents(currentSession);

            if (roomCodeText != null)
            {
                roomCodeText.text = $"Code: {currentSession.Code}";
            }

            SetStatus($"Joined room ({currentSession.Players.Count}/{currentSession.MaxPlayers})");
            Debug.Log($"[OnlineRoomManager] Joined session. ID: {currentSession.Id}, Code: {currentSession.Code}");
        }
        catch (Exception ex)
        {
            SetStatus("Join failed: " + ex.Message);
            Debug.LogError($"[OnlineRoomManager] Error joining room '{joinCode}': {ex}");
        }
        finally
        {
            isBusy = false;
            SetInteractable(true);
        }
    }

    /// <summary>
    /// Leaves the active session cleanly if one exists.
    /// </summary>
    public async Task LeaveCurrentSessionAsync()
    {
        if (currentSession == null)
        {
            return;
        }

        ISession sessionToLeave = currentSession;
        currentSession = null;

        UnsubscribeFromSessionEvents(sessionToLeave);

        try
        {
            await sessionToLeave.LeaveAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OnlineRoomManager] Error leaving session: {ex.Message}");
        }
    }

    private void SubscribeToSessionEvents(ISession session)
    {
        if (session == null)
        {
            return;
        }

        session.PlayerJoined += HandlePlayerJoined;
        session.PlayerHasLeft += HandlePlayerLeft;
        session.RemovedFromSession += HandleRemovedFromSession;
    }

    private void UnsubscribeFromSessionEvents(ISession session)
    {
        if (session == null)
        {
            return;
        }

        session.PlayerJoined -= HandlePlayerJoined;
        session.PlayerHasLeft -= HandlePlayerLeft;
        session.RemovedFromSession -= HandleRemovedFromSession;
    }

    private void HandlePlayerJoined(string playerId)
    {
        if (currentSession != null)
        {
            SetStatus($"Player joined ({currentSession.Players.Count}/{currentSession.MaxPlayers})");
        }
    }

    private void HandlePlayerLeft(string playerId)
    {
        if (currentSession != null)
        {
            SetStatus($"Player left ({currentSession.Players.Count}/{currentSession.MaxPlayers})");
        }
    }

    private void HandleRemovedFromSession()
    {
        currentSession = null;
        SetStatus("Disconnected from room.");
    }

    private void SetInteractable(bool interactable)
    {
        if (createRoomButton != null)
        {
            createRoomButton.interactable = interactable;
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.interactable = interactable;
        }

        if (joinCodeInputField != null)
        {
            joinCodeInputField.interactable = interactable;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public async Task CreateRoomAsync(string roomName, int roomMaxPlayers, bool isPrivate)
    {
        if (isBusy)
        {
            return;
        }

        isBusy = true;
        SetInteractable(false);

        try
        {
            await InitializeServicesAsync();
            if (!isInitialized)
            {
                return;
            }

            await LeaveCurrentSessionAsync();

            SetStatus("Creating room...");

            string finalName = string.IsNullOrWhiteSpace(roomName) ? defaultRoomName : roomName.Trim();
            int finalMaxPlayers = Mathf.Clamp(roomMaxPlayers > 0 ? roomMaxPlayers : maxPlayers, 2, 8);

            var options = new SessionOptions
            {
                Name = finalName,
                MaxPlayers = finalMaxPlayers,
                IsPrivate = isPrivate
            }.WithRelayNetwork();

            currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            SubscribeToSessionEvents(currentSession);

            string joinCode = currentSession.Code;
            if (roomCodeText != null)
            {
                roomCodeText.text = $"Code: {joinCode}";
            }

            SetStatus($"Room created ({currentSession.Players.Count}/{finalMaxPlayers})");
            Debug.Log($"[OnlineRoomManager] Session '{finalName}' created (Private={isPrivate}). ID: {currentSession.Id}, Code: {joinCode}");
        }
        catch (Exception ex)
        {
            SetStatus("Create failed: " + ex.Message);
            Debug.LogError($"[OnlineRoomManager] Error creating room: {ex}");
        }
        finally
        {
            isBusy = false;
            SetInteractable(true);
        }
    }
} 