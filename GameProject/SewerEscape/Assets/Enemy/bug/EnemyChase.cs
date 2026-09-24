using UnityEngine;
using UnityEngine.AI;

public class EnemyChase : MonoBehaviour
{
    public enum EnemyState
    {
        Searching,     // ランダムパトロール・うろつき中
        Chasing,       // 追跡中（視認中）
        Investigating  // 最後の目撃地点へ移動中
    }

    [Header("現在の状態")]
    [SerializeField] private EnemyState currentState = EnemyState.Searching;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform playerTransform;

    [Header("ターゲット設定")]
    [SerializeField] private string playerTag = "Player";

    [Header("ランダムパトロール（Wander）設定")]
    [SerializeField] private float wanderRadius = 8f;        // 1回移動で探すランダム位置の半径
    [SerializeField] private float minWaitTime = 1f;          // 到着後の最小立ち止まり時間（秒）
    [SerializeField] private float maxWaitTime = 4f;          // 到着後の最大立ち止まり時間（秒）

    [Header("索敵設定")]
    [SerializeField] private float detectionRange = 10f; // 発見距離
    [SerializeField] private float loseTargetRange = 15f; // 見失う距離
    [SerializeField] private float eyeHeight = 1.5f; // 目の高さ
    [SerializeField] private LayerMask obstacleLayer; // 障害物レイヤー
    [SerializeField] private float fieldOfViewAngle = 120f; // 視野角

    [Header("見失った後の挙動設定")]
    [SerializeField] private float searchWaitTime = 3f; // 最後の目撃地点に着いてから見渡す時間

    [Header("攻撃設定")]
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private float attackCooldown = 1.5f;

    private Vector3 lastKnownPosition; // 最後にプレイヤーを見た位置
    private float currentWaitDuration = 2f; // 今回立ち止まる目標時間
    private float waitTimer = 0f;
    private bool isAttacking = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 初回のランダム目的地を設定
        if (currentState == EnemyState.Searching)
        {
            SetRandomDestination();
        }
    }

    void Update()
    {
        // スポーン後などにプレイヤーが新しく読み込まれた場合も考慮して自動再検索
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        float distanceToPlayer = (playerTransform != null) ? Vector3.Distance(transform.position, playerTransform.position) : float.MaxValue;
        bool canSeePlayer = (playerTransform != null) && HasLineOfSight(distanceToPlayer);

        switch (currentState)
        {
            case EnemyState.Searching:
                UpdateSearchingState(canSeePlayer, distanceToPlayer);
                break;

            case EnemyState.Chasing:
                UpdateChasingState(canSeePlayer, distanceToPlayer);
                break;

            case EnemyState.Investigating:
                UpdateInvestigatingState(canSeePlayer, distanceToPlayer);
                break;
        }

        // アニメーション制御
        if (!isAttacking)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f;
            animator.SetBool("IsMoving", isMoving);
        }
    }

    // --- 各ステートの処理 ---

    // 1. 完全ランダムパトロール状態の処理
    private void UpdateSearchingState(bool canSeePlayer, float distance)
    {
        // プレイヤーを発見したら即座に追跡へ
        if (canSeePlayer && distance <= detectionRange)
        {
            SetState(EnemyState.Chasing);
            return;
        }

        // 目的地のランダム地点に到達したか判定
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;

            // ランダムに設定された「立ち止まり時間」を経過したら、次の新しいランダム目的地を設定
            if (waitTimer >= currentWaitDuration)
            {
                SetRandomDestination();
                waitTimer = 0f;
            }
        }
    }

    // 2. 追跡状態の処理
    private void UpdateChasingState(bool canSeePlayer, float distance)
    {
        if (playerTransform == null) return;

        lastKnownPosition = playerTransform.position;

        if (!isAttacking)
        {
            agent.SetDestination(lastKnownPosition);
        }

        if (!canSeePlayer || distance > loseTargetRange)
        {
            Debug.Log("視認不可！最後の目撃位置に向かいます。");
            SetState(EnemyState.Investigating);
        }
    }

    // 3. 調査状態（最後の目撃位置へ移動）の処理
    private void UpdateInvestigatingState(bool canSeePlayer, float distance)
    {
        if (canSeePlayer && distance <= detectionRange)
        {
            Debug.Log("プレイヤーを再発見！");
            SetState(EnemyState.Chasing);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= searchWaitTime)
            {
                Debug.Log("見失いました。ランダムパトロールへ切り替えます。");
                SetState(EnemyState.Searching);
            }
        }
    }

    // --- ランダム目的地設定ロジック ---
    private void SetRandomDestination()
    {
        // 敵を中心とした「 wanderRadius 」範囲内のランダムな球体上の点を取得
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        // 指定したランダム位置から一番近い「NavMesh上の床」の位置を取得
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);

            // 到着後に立ち止まる時間もランダム（例: 1秒〜4秒の間で毎回変化）
            currentWaitDuration = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    // 状態を切り替えるメソッド
    private void SetState(EnemyState newState)
    {
        currentState = newState;
        waitTimer = 0f;

        switch (newState)
        {
            case EnemyState.Chasing:
                agent.isStopped = false;
                break;

            case EnemyState.Investigating:
                agent.isStopped = false;
                agent.SetDestination(lastKnownPosition);
                break;

            case EnemyState.Searching:
                SetRandomDestination();
                break;
        }
    }

    // --- 視線判定処理 ---
    private bool HasLineOfSight(float distance)
    {
        if (playerTransform == null) return false;

        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 targetEyePosition = playerTransform.position + Vector3.up * eyeHeight;
        Vector3 directionToPlayer = (targetEyePosition - eyePosition).normalized;

        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > fieldOfViewAngle * 0.5f) return false;

        if (Physics.Raycast(eyePosition, directionToPlayer, distance, obstacleLayer))
        {
            return false;
        }

        return true;
    }

    // --- 攻撃処理 ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !isAttacking)
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        animator.SetBool("IsMoving", false);
        animator.SetTrigger(attackTriggerName);

        Invoke(nameof(ResetAttack), attackCooldown);
    }

    private void ResetAttack()
    {
        isAttacking = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    // --- Gizmos描画 ---
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);

        // ランダム目標を探す可視化用の円（緑色）
        if (currentState == EnemyState.Searching)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, wanderRadius);
        }
    }
}