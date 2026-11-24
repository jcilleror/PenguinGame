using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages navigation between lobby UI panels (Authentication, Lobby List, Create Lobby,
/// and Join Lobby) and forwards user actions to <see cref="OnlineRoomManager"/>.
/// </summary>
public class LobbyUIFlowController : MonoBehaviour
{
    // UI panel containers toggled during the lobby flow.
    [Header("Panels")]
    [SerializeField] private GameObject authPanel;
    [SerializeField] private GameObject lobbyListPanel;
    [SerializeField] private GameObject createLobbyPanel;
    [SerializeField] private GameObject joinLobbyPanel;

    // Controls inside the authentication panel.
    [Header("Auth Panel UI")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button authenticateButton;

    // Navigation buttons inside the lobby list panel.
    [Header("Lobby List Panel UI")]
    [SerializeField] private Button openCreateLobbyButton;
    [SerializeField] private Button openJoinLobbyButton;

    // Inputs and action button inside the create lobby panel.
    [Header("Create Lobby Panel UI")]
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private TMP_Dropdown privacyDropdown;
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;
    [SerializeField] private Button createRoomButton;

    // Input and action button inside the join lobby panel.
    [Header("Join Lobby Panel UI")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinRoomButton;

    // Reference to the multiplayer session manager handling Unity Multiplayer Services.
    [Header("Multiplayer")]
    [SerializeField] private OnlineRoomManager onlineRoomManager;

    /// <summary>
    /// Stores the player display name entered during authentication so other scripts can read it.
    /// </summary>
    public static string SavedPlayerName { get; private set; } = "PenguinPlayer";

    /// <summary>
    /// Registers button click listeners and sets the initial active panel when the object awakens.
    /// </summary>
    private void Awake()
    {
        if (authenticateButton != null)
        {
            authenticateButton.onClick.AddListener(OnAuthenticateClicked);
        }

        // Switch directly to the create or join panels when requested from the lobby list.
        if (openCreateLobbyButton != null)
        {
            openCreateLobbyButton.onClick.AddListener(() => ShowPanel(createLobbyPanel));
        }

        if (openJoinLobbyButton != null)
        {
            openJoinLobbyButton.onClick.AddListener(() => ShowPanel(joinLobbyPanel));
        }

        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
        }

        // Start the flow on the authentication screen.
        ShowPanel(authPanel);
    }

    /// <summary>
    /// Activates <paramref name="targetPanel"/> and deactivates all other registered lobby panels
    /// so overlapping menus do not block input.
    /// </summary>
    public void ShowPanel(GameObject targetPanel)
    {
        if (authPanel != null) authPanel.SetActive(targetPanel == authPanel);
        if (lobbyListPanel != null) lobbyListPanel.SetActive(targetPanel == lobbyListPanel);
        if (createLobbyPanel != null) createLobbyPanel.SetActive(targetPanel == createLobbyPanel);
        if (joinLobbyPanel != null) joinLobbyPanel.SetActive(targetPanel == joinLobbyPanel);
    }

    /// <summary>
    /// Deactivates every lobby panel so the gameplay view is unobstructed once a session starts.
    /// </summary>
    public void HideAllPanels()
    {
        if (authPanel != null) authPanel.SetActive(false);
        if (lobbyListPanel != null) lobbyListPanel.SetActive(false);
        if (createLobbyPanel != null) createLobbyPanel.SetActive(false);
        if (joinLobbyPanel != null) joinLobbyPanel.SetActive(false);
    }

    /// <summary>
    /// Saves the entered player name, initializes online services, and advances to the lobby list panel.
    /// </summary>
    private async void OnAuthenticateClicked()
    {
        // Keep the default name if the player leaves the input blank.
        if (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            SavedPlayerName = playerNameInput.text.Trim();
        }

        if (onlineRoomManager != null)
        {
            await onlineRoomManager.InitializeServicesAsync();
        }

        ShowPanel(lobbyListPanel);
    }

    /// <summary>
    /// Reads room configuration from the Create Lobby panel, creates the session via
    /// <see cref="OnlineRoomManager"/>, and hides the panels once creation completes.
    /// </summary>
    private async void OnCreateRoomClicked()
    {
        if (onlineRoomManager == null)
        {
            return;
        }

        // Fallback to a default room name when the input field is empty.
        string roomName = (lobbyNameInput != null && !string.IsNullOrWhiteSpace(lobbyNameInput.text))
            ? lobbyNameInput.text.Trim()
            : "PenguinRoom";

        // Inspect the selected dropdown label to determine whether the session should be private.
        bool isPrivate = false;
        if (privacyDropdown != null && privacyDropdown.options.Count > privacyDropdown.value)
        {
            string selectedPrivacy = privacyDropdown.options[privacyDropdown.value].text.Trim().ToLowerInvariant();
            isPrivate = selectedPrivacy.Contains("priv");
        }

        // Parse and clamp the selected player limit to the supported room size range (2 to 8).
        int playersLimit = 8;
        if (maxPlayersDropdown != null && maxPlayersDropdown.options.Count > maxPlayersDropdown.value)
        {
            string selectedMax = maxPlayersDropdown.options[maxPlayersDropdown.value].text.Trim();
            if (int.TryParse(selectedMax, out int parsedMax) && parsedMax > 0)
            {
                playersLimit = Mathf.Clamp(parsedMax, 2, 8);
            }
        }

        await onlineRoomManager.CreateRoomAsync(roomName, playersLimit, isPrivate);

        // Hide the UI panels after the host creates the session so the player sees the game world.
        HideAllPanels();
    }

    /// <summary>
    /// Reads the join code from the Join Lobby panel, joins the matching session via
    /// <see cref="OnlineRoomManager"/>, and hides the panels once joined.
    /// </summary>
    private async void OnJoinRoomClicked()
    {
        if (onlineRoomManager == null)
        {
            return;
        }

        string code = joinCodeInput != null ? joinCodeInput.text.Trim() : string.Empty;

        // Avoid sending an empty join request if no code was typed.
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        await onlineRoomManager.JoinRoomByCodeAsync(code);

        // Hide the UI panels after joining so the client enters the active room view cleanly.
        HideAllPanels();
    }
}