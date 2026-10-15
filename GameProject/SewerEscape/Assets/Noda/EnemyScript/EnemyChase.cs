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
    [SerializeField] private float wanderRadius = 8f;          // 1回移動で探すランダム位置の半径
    [SerializeField] private float minWaitTime = 1f;           // 到着後の最小立ち止まり時間（秒）
    [SerializeField] private float maxWaitTime = 4f;           // 到着後の最大立ち止まり時間（秒）

    [Header("索敵設定")]
    [SerializeField] private float closeDetectionRange = 3f; // 至近距離（背後でも無条件で気づく距離）
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

    [Header("サウンド設定")]
    [SerializeField] private AudioSource audioSource;       // 音を鳴らすAudioSource
    [SerializeField] private AudioClip noticeSound;         // 敵に見つかった時のSE
    [Range(0f, 1f)][SerializeField] private float noticeSoundVolume = 1.0f; // SEの音量（0.0 〜 1.0）
    [SerializeField] private float noticeSoundCooldown = 3.0f;               // 発見SEの連続再生を防ぐクールダウン時間（秒）
    private float lastNoticeSoundTime = -999f;                               // 最後に発見SEを鳴らした時刻

    [Header("エコー設定（コード制御）")]
    [SerializeField] private bool useEcho = true;                           // エコーを有効にするか
    [Range(10f, 5000f)][SerializeField] private float echoDelay = 500f;    // エコーの遅延時間（ミリ秒: 10 〜 5000）
    [Range(0f, 1f)][SerializeField] private float echoDecayRatio = 0.5f;   // エコーの減衰率（0.0 〜 1.0）
    [Range(0f, 1f)][SerializeField] private float echoDryMix = 1f;         // 元の音の大きさ（0.0 〜 1.0）
    [Range(0f, 1f)][SerializeField] private float echoWetMix = 0.5f;       // エコー音の大きさ（0.0 〜 1.0）
    private AudioEchoFilter echoFilter;                                     // エコーフィルターの参照用

    [Header("出現条件設定（追加の敵用）")]
    [SerializeField] private bool useSpawnCondition = false;      // 条件付きで出現させる場合はチェックを入れる
    [SerializeField] private int requiredCollectedCount = 50;     // 出現に必要なアイテム取得数
    private bool isSpawned = false;                               // すでに出現したか
    private KeyFgmManager keyManager;
    private Renderer[] renderers;
    private Collider enemyCollider;

    [Header("被弾時（ロビー送還時）のテレポート設定")]
    [SerializeField] private bool teleportOnHit = true;             // プレイヤーにヒットした時にテレポートするか
    [SerializeField] private float teleportDelay = 2.5f;            // 被弾からテレポートするまでの遅延時間（秒）
    [SerializeField] private bool useStartPosAsLobby = true;      // チェックを入れるとStart時の位置に戻る
    [SerializeField] private Vector3 customLobbyTeleportPos;      // 上のチェックを外した場合のテレポート先座標
    private Vector3 startPosition;                                // 初期位置の保持用

    private Vector3 lastKnownPosition; // 最後にプレイヤーを見た位置
    private float currentWaitDuration = 2f; // 今回立ち止まる目標時間
    private float waitTimer = 0f;
    private bool isAttacking = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        keyManager = FindAnyObjectByType<KeyFgmManager>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // ▼ コードからAudioEchoFilterを制御・設定する処理
        SetupEchoFilter();

        startPosition = transform.position;

        if (useSpawnCondition)
        {
            SetSpawnState(false);
            isSpawned = false;
        }
        else
        {
            isSpawned = true;
            if (currentState == EnemyState.Searching)
            {
                SetRandomDestination();
            }
        }
    }

    // エコーフィルターをコードでセットアップするメソッド
    private void SetupEchoFilter()
    {
        if (useEcho)
        {
            // 既にアタッチされていなければ追加する
            echoFilter = GetComponent<AudioEchoFilter>();
            if (echoFilter == null)
            {
                echoFilter = gameObject.AddComponent<AudioEchoFilter>();
            }

            // インスペクターで設定した数値をコードからフィルターに反映
            echoFilter.delay = echoDelay;
            echoFilter.decayRatio = echoDecayRatio;
            echoFilter.dryMix = echoDryMix;
            echoFilter.wetMix = echoWetMix;
            echoFilter.enabled = true;
        }
        else
        {
            // エコーが無効に設定されている場合はコンポーネントがあれば無効化する
            echoFilter = GetComponent<AudioEchoFilter>();
            if (echoFilter != null)
            {
                echoFilter.enabled = false;
            }
        }
    }

    void Update()
    {
        if (useSpawnCondition && !isSpawned)
        {
            if (keyManager != null)
            {
                int collectedCount = keyManager.GetTotalKeyFgmNum() - keyManager.GetKeyFgmNum();
                if (collectedCount >= requiredCollectedCount)
                {
                    SetSpawnState(true);
                    isSpawned = true;
                    if (currentState == EnemyState.Searching)
                    {
                        SetRandomDestination();
                    }
                    Debug.Log($"アイテムが {collectedCount} 個取得されたため、追加の敵が出現しました！");
                }
            }
            return;
        }

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

        if (!isAttacking)
        {
            bool isMoving = (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) && agent.velocity.magnitude > 0.1f;
            animator.SetBool("IsMoving", isMoving);
        }
    }

    private void SetSpawnState(bool isActive)
    {
        if (enemyCollider != null) enemyCollider.enabled = isActive;
        if (agent != null) agent.enabled = isActive;
        foreach (var r in renderers)
        {
            if (r != null) r.enabled = isActive;
        }
    }

    private void UpdateSearchingState(bool canSeePlayer, float distance)
    {
        if (canSeePlayer && distance <= detectionRange)
        {
            SetState(EnemyState.Chasing);
            return;
        }

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= currentWaitDuration)
                {
                    SetRandomDestination();
                    waitTimer = 0f;
                }
            }
        }
    }

    private void UpdateChasingState(bool canSeePlayer, float distance)
    {
        if (playerTransform == null) return;

        lastKnownPosition = playerTransform.position;

        if (!isAttacking && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(lastKnownPosition);
        }

        if (!canSeePlayer || distance > loseTargetRange)
        {
            SetState(EnemyState.Investigating);
        }
    }

    private void UpdateInvestigatingState(bool canSeePlayer, float distance)
    {
        if (canSeePlayer && distance <= detectionRange)
        {
            SetState(EnemyState.Chasing);
            return;
        }

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                waitTimer += Time.deltaTime;

                if (waitTimer >= searchWaitTime)
                {
                    SetState(EnemyState.Searching);
                }
            }
        }
    }

    private void SetRandomDestination()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
            currentWaitDuration = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    private void SetState(EnemyState newState)
    {
        if (newState == EnemyState.Chasing && currentState != EnemyState.Chasing)
        {
            // クールダウン時間が経過している場合のみSEを鳴らす
            if (audioSource != null && noticeSound != null)
            {
                if (Time.time >= lastNoticeSoundTime + noticeSoundCooldown)
                {
                    audioSource.PlayOneShot(noticeSound, noticeSoundVolume);
                    lastNoticeSoundTime = Time.time;
                }
            }
        }

        currentState = newState;
        waitTimer = 0f;

        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

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

    private bool HasLineOfSight(float distance)
    {
        if (playerTransform == null) return false;

        if (distance <= closeDetectionRange)
        {
            return true;
        }

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

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        animator.SetBool("IsMoving", false);
        animator.SetTrigger(attackTriggerName);

        if (playerTransform != null)
        {
            Vector3 direction = (playerTransform.position - transform.position);
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        if (teleportOnHit)
        {
            Invoke(nameof(DelayedTeleport), teleportDelay);
        }

        Invoke(nameof(ResetAttack), attackCooldown);
    }

    private void DelayedTeleport()
    {
        TeleportToLobbyPosition();
    }

    private void TeleportToLobbyPosition()
    {
        Vector3 targetPos = useStartPosAsLobby ? startPosition : customLobbyTeleportPos;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(targetPos);
        }
        else
        {
            transform.position = targetPos;
        }

        SetState(EnemyState.Searching);
    }

    private void ResetAttack()
    {
        isAttacking = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, closeDetectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);

        if (currentState == EnemyState.Searching)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, wanderRadius);
        }
    }
}