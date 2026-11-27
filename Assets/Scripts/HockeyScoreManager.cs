using Unity.Netcode;
using UnityEngine;
using TMPro;

public class HockeyScoreManager : NetworkBehaviour
{
    public static HockeyScoreManager Instance { get; private set; }

    [Header("Scoreboard UI (World Space)")]
    [SerializeField]
    private TextMeshProUGUI scoreText;

    [Header("Puck Reset Settings")]
    [SerializeField]
    private Transform puckTransform;
    [SerializeField]
    private Transform centerFaceoffPoint;
    [SerializeField]
    private float resetDelay = 1.5f;

    [Header("Team Spawn Points (Set Y >= 1.2)")]
    [SerializeField]
    private Transform[] teamASpawns;
    [SerializeField]
    private Transform[] teamBSpawns;

    // Networked scores synchronized across all players
    private NetworkVariable<int> teamAScore = new NetworkVariable<int>(0);
    private NetworkVariable<int> teamBScore = new NetworkVariable<int>(0);

    // Cached references to all spectators in the stadium
    private StadiumSpectator[] crowdSpectators;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdateScoreboard();
        CacheSpectators();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        teamAScore.OnValueChanged += OnScoreChanged;
        teamBScore.OnValueChanged += OnScoreChanged;

        UpdateScoreboard();
        CacheSpectators();
    }

    public override void OnNetworkDespawn()
    {
        teamAScore.OnValueChanged -= OnScoreChanged;
        teamBScore.OnValueChanged -= OnScoreChanged;

        base.OnNetworkDespawn();
    }

    /// Assigns Team A (even ClientId) or Team B (odd ClientId) and picks an available spawn slot.
    /// Works identically for 1v1 or 2v2 matches.
    public Transform GetSpawnPointForClient(ulong clientId)
    {
        bool isTeamA = (clientId % 2 == 0);
        Transform[] selectedTeamSpawns = isTeamA ? teamASpawns : teamBSpawns;

        if (selectedTeamSpawns == null || selectedTeamSpawns.Length == 0) return null;

        int slotIndex = (int)(clientId / 2) % selectedTeamSpawns.Length;
        return selectedTeamSpawns[slotIndex];
    }

    private void CacheSpectators()
    {
        if (crowdSpectators == null || crowdSpectators.Length == 0)
        {
            crowdSpectators = FindObjectsByType<StadiumSpectator>(FindObjectsSortMode.None);
        }
    }

    private void OnScoreChanged(int previousValue, int newValue)
    {
        UpdateScoreboard();
        CelebrateAllSpectators();
    }

    /// <summary>
    /// Called by GoalZone when the puck enters a goal.
    /// </summary>
    public void RegisterGoal(bool scoredByTeamA)
    {
        if (!IsServer) return;

        if (scoredByTeamA)
        {
            teamAScore.Value++;
        }
        else
        {
            teamBScore.Value++;
        }

        // Schedule both puck and players reset for the next faceoff
        Invoke(nameof(ResetRoundPositions), resetDelay);
    }

    private void UpdateScoreboard()
    {
        if (scoreText != null)
        {
            // Custom 7-space stadium format (Team A on the left, Team B on the right)
            scoreText.text = $"{teamAScore.Value}       {teamBScore.Value}";
        }
    }

    private void CelebrateAllSpectators()
    {
        if (crowdSpectators == null || crowdSpectators.Length == 0)
        {
            CacheSpectators();
        }

        if (crowdSpectators != null)
        {
            foreach (StadiumSpectator spectator in crowdSpectators)
            {
                if (spectator != null)
                {
                    spectator.TriggerGoalCelebration();
                }
            }
        }
    }

    private void ResetRoundPositions()
    {
        if (!IsServer) return;

        ResetPuckPosition();
        ResetAllPlayersPosition();
    }

    private void ResetAllPlayersPosition()
    {
        HockeyPlayerSpawn[] players = FindObjectsByType<HockeyPlayerSpawn>(FindObjectsSortMode.None);
        foreach (HockeyPlayerSpawn player in players)
        {
            if (player != null)
            {
                player.ResetToSpawnClientRpc();
            }
        }
    }

    private void ResetPuckPosition()
    {
        if (!IsServer || puckTransform == null || centerFaceoffPoint == null) return;

        Rigidbody puckRb = puckTransform.GetComponent<Rigidbody>();
        if (puckRb != null)
        {
            puckRb.linearVelocity = Vector3.zero;
            puckRb.angularVelocity = Vector3.zero;
        }

        // Slightly raise the spawn point  to prevent clipping into the ice collider
        Vector3 spawnPos = centerFaceoffPoint.position + Vector3.up * 0.2f;
        Quaternion spawnRot = centerFaceoffPoint.rotation;

        if (puckTransform.TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(spawnPos, spawnRot, puckTransform.localScale);
        }
        else
        {
            puckTransform.SetPositionAndRotation(spawnPos, spawnRot);
        }

        // Ensure velocity remains zero after teleporting
        if (puckRb != null)
        {
            puckRb.linearVelocity = Vector3.zero;
            puckRb.angularVelocity = Vector3.zero;
        }
    }
}