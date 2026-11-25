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
    private float resetDelay = 2.0f;

    // Networked scores synchronized across all players
    private NetworkVariable<int> blueScore = new NetworkVariable<int>(0);
    private NetworkVariable<int> redScore = new NetworkVariable<int>(0);

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

        blueScore.OnValueChanged += OnScoreChanged;
        redScore.OnValueChanged += OnScoreChanged;

        UpdateScoreboard();
        CacheSpectators();
    }

    public override void OnNetworkDespawn()
    {
        blueScore.OnValueChanged -= OnScoreChanged;
        redScore.OnValueChanged -= OnScoreChanged;

        base.OnNetworkDespawn();
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
    public void RegisterGoal(bool scoredByBlue)
    {
        if (!IsServer) return;

        if (scoredByBlue)
        {
            blueScore.Value++;
        }
        else
        {
            redScore.Value++;
        }

        // Schedule puck reset to the center of the rink
        if (puckTransform != null && centerFaceoffPoint != null)
        {
            Invoke(nameof(ResetPuckPosition), resetDelay);
        }
    }

    private void UpdateScoreboard()
    {
        if (scoreText != null)
        {
            // Custom 7-space stadium format
            scoreText.text = $"{blueScore.Value}       {redScore.Value}";
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

    private void ResetPuckPosition()
    {
        if (!IsServer || puckTransform == null || centerFaceoffPoint == null) return;

        // 1. Parar en seco toda la inercia del disco
        Rigidbody puckRb = puckTransform.GetComponent<Rigidbody>();
        if (puckRb != null)
        {
            puckRb.linearVelocity = Vector3.zero;
            puckRb.angularVelocity = Vector3.zero;
        }

        // Elevamos ligeramente en Y (0.15f) para evitar que se atasque en el suelo al aparecer
        Vector3 spawnPos = centerFaceoffPoint.position + Vector3.up * 0.15f;
        Quaternion spawnRot = centerFaceoffPoint.rotation;

        // 2. Si tiene NetworkTransform, usar Teleport() para evitar la interpolación suave y sincronizar al instante
        Unity.Netcode.Components.NetworkTransform netTransform = puckTransform.GetComponent<Unity.Netcode.Components.NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(spawnPos, spawnRot, puckTransform.localScale);
        }
        else
        {
            puckTransform.SetPositionAndRotation(spawnPos, spawnRot);
        }
    }
}