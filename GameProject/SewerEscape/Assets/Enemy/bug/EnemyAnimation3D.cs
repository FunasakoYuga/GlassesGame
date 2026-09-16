using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimation3D : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 速度が一定以上出ていれば「移動中」とみなす
        bool isMoving = agent.velocity.magnitude > 0.1f;

        // Animatorのパラメータ「IsMoving」を切り替える
        animator.SetBool("IsMoving", isMoving);
    }
}