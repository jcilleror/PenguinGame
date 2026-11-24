using System.Collections;
using TMPro;
using UnityEngine;

public class HockeyScoreManager : MonoBehaviour
{
    [Header("Stadium Scoreboard (World Space UI)")]
    public TMP_Text scoreText;

    [Header("Puck Reset Settings")]
    public Rigidbody puckRigidbody;
    public Transform centerSpawnPoint;
    public float resetDelay = 1.5f;

    private int blueScore = 0;
    private int redScore = 0;
    private bool isResetting = false;

    private void Start()
    {
        UpdateScoreboard();
    }

    public void RegisterGoal(bool scoredByBlue)
    {
        if (isResetting) return;

        if (scoredByBlue)
            blueScore++;
        else
            redScore++;

        UpdateScoreboard();
        StartCoroutine(ResetPuckRoutine());
    }

    private void UpdateScoreboard()
    {
        if (scoreText != null)
        {
            // Keep 7 spaces between scores to match the stadium scoreboard layout
            scoreText.text = $"{blueScore}       {redScore}";
        }
    }

    private IEnumerator ResetPuckRoutine()
    {
        isResetting = true;

        // Brief pause so players can see the puck enter the goal before resetting
        yield return new WaitForSeconds(resetDelay);

        if (puckRigidbody != null && centerSpawnPoint != null)
        {
            puckRigidbody.linearVelocity = Vector3.zero;
            puckRigidbody.angularVelocity = Vector3.zero;
            puckRigidbody.transform.position = centerSpawnPoint.position;
            puckRigidbody.transform.rotation = Quaternion.identity;
        }

        isResetting = false;
    }
}