using UnityEngine;

public class GoalZone : MonoBehaviour
{
    [Header("Goal Configuration")]
    public bool awardsPointToBlue = true;
    public HockeyScoreManager scoreManager;

    private bool goalCooldown = false;

    private void OnTriggerEnter(Collider other)
    {
        if (goalCooldown) return;

        GameObject rootObj = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

        if (rootObj.CompareTag("Puck") || rootObj.name.ToLower().Contains("punk") || rootObj.name.ToLower().Contains("puck"))
        {
            HockeyScoreManager manager = scoreManager != null ? scoreManager : HockeyScoreManager.Instance;
            if (manager != null)
            {
                goalCooldown = true;
                manager.RegisterGoal(awardsPointToBlue);
                Invoke(nameof(ResetCooldown), 2.5f);
            }
        }
    }

    private void ResetCooldown()
    {
        goalCooldown = false;
    }
}