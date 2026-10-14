using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    private bool isGoalReached = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        UIManager uiManager = FindAnyObjectByType<UIManager>();

        isGoalReached = true;
        uiManager.ShowResultUI();

    }
}
