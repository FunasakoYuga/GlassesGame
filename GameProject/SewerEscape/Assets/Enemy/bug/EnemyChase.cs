using UnityEngine;
using UnityEngine.AI; // NavMeshを使うために必要

public class EnemyChase : MonoBehaviour
{
    [SerializeField] private Transform target; // 追跡対象（プレイヤー）
    private NavMeshAgent agent;

    void Start()
    {
        // 敵に付いている NavMeshAgent を取得
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (target != null)
        {
            // プレイヤーの位置を目的地に設定
            agent.SetDestination(target.position);
        }
    }
}