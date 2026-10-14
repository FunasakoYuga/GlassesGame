using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimation3D : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    // プレイヤーのTag名を指定（デフォルトは "Player"）
    [SerializeField] private string playerTag = "Player";
    // AnimatorのTriggerパラメータ名
    [SerializeField] private string attackTriggerName = "Attack";

    // 攻撃中のフラグと冷却時間
    private bool isAttacking = false;
    [SerializeField] private float attackCooldown = 1.5f; // 攻撃モーションの長さ＋余韻の時間（秒）

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 攻撃中ではない時のみ「移動アニメーション」のパラメータを更新
        if (!isAttacking)
        {
            // 速度が一定以上出ていれば「移動中」とみなす
            bool isMoving = agent.velocity.magnitude > 0.1f;
            animator.SetBool("IsMoving", isMoving);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーに接触し、かつ現在攻撃中でない場合
        if (other.CompareTag(playerTag) && !isAttacking)
        {
            StartAttack();
        }
        if (other.CompareTag(playerTag))
        {
            Debug.Log("プレイヤーに当たりました！攻撃します。"); // ←これを追加
            animator.SetTrigger(attackTriggerName);
        }
    }

    private void StartAttack()
    {
        isAttacking = true;

        // 移動を停止する
        agent.isStopped = true;
        agent.velocity = Vector3.zero; // 慣性での滑りを防ぐために速度をリセット

        // 攻撃アニメーションを再生
        animator.SetBool("IsMoving", false);
        animator.SetTrigger(attackTriggerName);

        // 指定した時間（攻撃完了）のあとに移動を再開するコルーチンを開始
        Invoke(nameof(ResetAttack), attackCooldown);
    }

    private void ResetAttack()
    {
        isAttacking = false;

        // 移動を再開する
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }
}

