using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GoalZone : MonoBehaviour
{
    [Header("Goal Configuration")]
    [Tooltip("Check this if scoring in this net gives a point to the Blue team")]
    public bool awardsPointToBlue = true;
    public HockeyScoreManager scoreManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Puck") && scoreManager != null)
        {
            scoreManager.RegisterGoal(awardsPointToBlue);
        }
    }
}