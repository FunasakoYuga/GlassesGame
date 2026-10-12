using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    private bool isGoalReached = false;
    [SerializeField] private UIManager uiManager;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        isGoalReached = true;
        uiManager.ShowResultUI();

    }
}
