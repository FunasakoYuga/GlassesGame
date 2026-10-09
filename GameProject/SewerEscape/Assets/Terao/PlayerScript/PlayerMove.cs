using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5.0f; // 歩行速度
    [SerializeField] private float dashSpeed = 8.0f; // ダッシュ速度
    [SerializeField] private float jumpHeight = 1.5f; // ジャンプの高さ
    [SerializeField] private float gravity = -7f; // ジャンプ時の重力

    [Header("視点操作設定")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float GamePadlookSpeed = 150.0f; // パッドでの視点感度
    [SerializeField] private float MouselookSpeed = 1.0f; // マウスでの視点感度
    [SerializeField] private float UpMaxPitch = -90.0f;
    [SerializeField] private float DownMaxPitch = 90.0f;

    [Header("サウンド設定")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] walkClips;         // Walk（歩行）用足音SE（複数登録でランダム再生）
    [SerializeField] private AudioClip[] runClips;          // Run（ダッシュ）用足音SE（複数登録でランダム再生）
    [SerializeField] private AudioClip[] jumpClips;         // Jump（ジャンプ）SE（複数登録でランダム再生）
    [SerializeField] private float walkStepInterval = 0.5f; // 歩行時の足音間隔（秒）
    [SerializeField] private float dashStepInterval = 0.3f; // ダッシュ時の足音間隔（秒）
    [SerializeField] private float footstepVolume = 0.8f;   // 音量

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private float cameraPitch = 0f;

    // 足音タイマーおよび接地判定追跡用
    private float stepTimer = 0f;
    private bool wasGrounded = true;

    // 被弾時等の強制旋回制御用
    private Coroutine lookCoroutine;
    private bool isRotatingToTarget = false;

    // アニメーション制御用
    private Animator animator;
    private string currentAnimationState = "PlayerAnimator";

    private const string STATE_IDLE = "Idle";
    private const string STATE_WALK = "Walk";
    private const string STATE_RUN = "Run";
    private const string STATE_WALKBACK = "WalkBack";
    private const string STATE_RUNBACK = "RunBack";
    private const string STATE_JUMP = "Jump";

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector2 moveInput = Vector2.zero;
        float yaw = 0f;
        float pitch = 0f;
        bool isDashPressed = false; // ダッシュスピード
        bool isLookingBack = false; // 視点反転フラグ
        bool isJump = false;

        // 移動入力
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stickMove = gamepad.leftStick.ReadValue();
            if (stickMove.sqrMagnitude > 0.04f)
            {
                moveInput = stickMove;
            }
            if (gamepad.leftShoulder.isPressed) isDashPressed = true;
            if (gamepad.rightStickButton.isPressed) isLookingBack = true;
            if (gamepad.buttonSouth.wasPressedThisFrame) isJump = true;
        }

        if (Keyboard.current != null)
        {
            Vector2 keyMove = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) keyMove.y += 1f;
            if (Keyboard.current.sKey.isPressed) keyMove.y -= 1f;
            if (Keyboard.current.aKey.isPressed) keyMove.x -= 1f;
            if (Keyboard.current.dKey.isPressed) keyMove.x += 1f;

            if (keyMove.sqrMagnitude > 0.01f)
            {
                moveInput = keyMove;
            }
            if (Keyboard.current.shiftKey.isPressed) isDashPressed = true;
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                isJump = true;
            }
        }

        // 視点入力
        bool hasGamepadLook = false;
        if (gamepad != null)
        {
            Vector2 stickLook = gamepad.rightStick.ReadValue();
            if (stickLook.sqrMagnitude > 0.04f)
            {
                yaw = stickLook.x * GamePadlookSpeed * Time.deltaTime;
                pitch = stickLook.y * GamePadlookSpeed * Time.deltaTime;
                hasGamepadLook = true;
            }
        }

        if (!hasGamepadLook && Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            if (mouseDelta.sqrMagnitude > 0.001f)
            {
                yaw = mouseDelta.x * MouselookSpeed * 0.1f;
                pitch = mouseDelta.y * MouselookSpeed * 0.1f;
            }
            if (Mouse.current.leftButton.isPressed) isLookingBack = true;
        }

        // 視点操作
        if (!isRotatingToTarget)
        {
            if (Mathf.Abs(yaw) > 0.0001f)
            {
                transform.Rotate(Vector3.up * yaw);
            }

            if (cameraTransform != null)
            {
                cameraPitch -= pitch;
                cameraPitch = Mathf.Clamp(cameraPitch, UpMaxPitch, DownMaxPitch);
                float yawOffset = isLookingBack ? 180f : 0f;
                cameraTransform.localRotation = Quaternion.Euler(cameraPitch, yawOffset, 0f);
            }
        }

        // 移動処理
        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y);
        if (move.magnitude > 1.0f)
        {
            move.Normalize();
        }

        float currentSpeed = isDashPressed ? dashSpeed : moveSpeed;

        Vector3 finalVelocity = move * currentSpeed;

        // 接地しているときは即座にジャンプ可能
        if (controller.isGrounded)
        {
            // 空中から着地した瞬間に再生
            if (!wasGrounded)
            {
                PlayJumpSound();
            }

            if (verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2.0f;
            }
            if (isJump)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * 2.0f * Mathf.Abs(gravity));
            }
        }
        else
        {
            verticalVelocity.y -= Mathf.Abs(gravity) * Time.deltaTime;
        }

        wasGrounded = controller.isGrounded;

        finalVelocity.y = verticalVelocity.y;
        controller.Move(finalVelocity * Time.deltaTime);

        // アニメーションをスクリプトから制御
        UpdateAnimation(moveInput, isDashPressed);

        // 再生中アニメーション状態に応じた足音制御
        HandleFootstepSound(moveInput);
    }

    /// <summary>
    /// 現在のアニメーション状態に応じて足音SEと再生間隔を切り替える
    /// </summary>
    private void HandleFootstepSound(Vector2 moveInput)
    {
        // 空中にいる、または移動入力がない・アイドル時はタイマーリセット
        if (!controller.isGrounded || moveInput.sqrMagnitude <= 0.01f || currentAnimationState == STATE_IDLE || currentAnimationState == STATE_JUMP)
        {
            stepTimer = 0f;
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            return;
        }

        // 後退系・前進系それぞれのアニメーションに応じた間隔の設定
        bool isRunningState = (currentAnimationState == STATE_RUN || currentAnimationState == STATE_RUNBACK);
        float currentInterval = isRunningState ? dashStepInterval : walkStepInterval;

        stepTimer += Time.deltaTime;
        if (stepTimer >= currentInterval)
        {
            PlayFootstepByState(currentAnimationState);
            stepTimer = 0f;
        }
    }

    /// <summary>
    /// 各アニメーションステート専用のサウンド素材を選択して再生する
    /// </summary>
    /// <param name="stateName">アニメーションステート名（省略時は現在のステート）</param>
    public void PlayFootstepByState(string stateName = "")
    {
        if (audioSource == null) return;

        string state = string.IsNullOrEmpty(stateName) ? currentAnimationState : stateName;
        AudioClip[] targetClips = null;

        // ステートごとに個別のクリップ配列を参照
        switch (state)
        {
            case STATE_WALK:
            case STATE_WALKBACK:
                targetClips = walkClips;
                break;
            case STATE_RUN:
            case STATE_RUNBACK:
                targetClips = runClips;
                break;
        }

        if (targetClips != null && targetClips.Length > 0)
        {
            AudioClip clip = targetClips[Random.Range(0, targetClips.Length)];
            if (clip != null)
            {
                audioSource.pitch = Random.Range(0.93f, 1.07f);
                audioSource.clip = clip;
                audioSource.volume = footstepVolume;
                audioSource.Play();
            }
        }
    }

    /// <summary>
    /// ジャンプ着地時のサウンド再生
    /// </summary>
    private void PlayJumpSound()
    {
        if (jumpClips == null || jumpClips.Length == 0) return;

        AudioClip clip = jumpClips[Random.Range(0, jumpClips.Length)];
        if (clip != null)
        {
            // 足音の停止処理に消されないよう、独立してSEを再生
            AudioSource.PlayClipAtPoint(clip, transform.position, footstepVolume);
        }
    }

    /// <summary>
    /// 入力状態に応じてアニメーションを直接切り替える
    /// </summary>
    private void UpdateAnimation(Vector2 moveInput, bool isDash)
    {
        if (animator == null) return;

        string targetState;

        // 地面についていない（空中・ジャンプ中）場合
        if (!controller.isGrounded)
        {
            targetState = STATE_JUMP;
        }
        else
        {
            // 入力状態に応じてアニメーションの変化
            if (moveInput.sqrMagnitude > 0.01f)
            {
                // 後ろ入力（Sキーまたはスティック手前倒し）の判定
                if (moveInput.y < -0.1f)
                {
                    targetState = isDash ? STATE_RUNBACK : STATE_WALKBACK;
                }
                else
                {
                    targetState = isDash ? STATE_RUN : STATE_WALK;
                }
            }
            else
            {
                targetState = STATE_IDLE;
            }
        }

        // すでに再生中のアニメーションだったら何も行わない
        if (currentAnimationState == targetState) return;

        animator.CrossFade(targetState, 0.1f);
        currentAnimationState = targetState;
    }

    public void ResetLook()
    {
        cameraPitch = 0f;
        verticalVelocity = Vector3.zero;
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.identity;
        }
    }

    public void LookAtPosition(Vector3 targetPosition)
    {
        Vector3 lookDirection = targetPosition - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        if (cameraTransform != null)
        {
            Vector3 camToTarget = targetPosition - cameraTransform.position;
            float flatDistance = new Vector2(camToTarget.x, camToTarget.z).magnitude;
            float targetPitch = -Mathf.Atan2(camToTarget.y, flatDistance) * Mathf.Rad2Deg;
            cameraPitch = Mathf.Clamp(targetPitch, UpMaxPitch, DownMaxPitch);
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    /// <summary>
    /// 右回り（時計回り）に旋回しながら対象へ視点を合わせる
    /// </summary>
    /// <param name="targetPosition">向きたい目標位置（敵の位置）</param>
    /// <param name="duration">旋回にかける時間（秒）</param>
    public void RotateToTarget(Vector3 targetPosition, float duration = 0.4f)
    {
        if (lookCoroutine != null)
        {
            StopCoroutine(lookCoroutine);
        }
        lookCoroutine = StartCoroutine(RotateRightCoroutine(targetPosition, duration));
    }

    private IEnumerator RotateRightCoroutine(Vector3 targetPosition, float duration)
    {
        isRotatingToTarget = true;

        Vector3 lookDirection = targetPosition - transform.position;
        lookDirection.y = 0f;

        float startYaw = transform.eulerAngles.y;
        float targetYaw = startYaw;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            targetYaw = Quaternion.LookRotation(lookDirection).eulerAngles.y;
        }

        // 最短回転となる角度差を計算（-180〜180度：正なら右回り、負なら左回り）
        float deltaYaw = Mathf.DeltaAngle(startYaw, targetYaw);

        // 目標の上下ピッチ角を算出
        float startPitch = cameraPitch;
        float targetPitch = startPitch;
        if (cameraTransform != null)
        {
            Vector3 camToTarget = targetPosition - cameraTransform.position;
            float flatDistance = new Vector2(camToTarget.x, camToTarget.z).magnitude;
            targetPitch = -Mathf.Atan2(camToTarget.y, flatDistance) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, UpMaxPitch, DownMaxPitch);
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // プレイヤー本体の水平回転（右回り）
            float currentYaw = startYaw + deltaYaw * smoothT;
            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

            // カメラの上下ピッチ回転
            cameraPitch = Mathf.Lerp(startPitch, targetPitch, smoothT);
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }

            yield return null;
        }

        // 完了時に目標の向きへ確定
        transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        cameraPitch = targetPitch;
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        isRotatingToTarget = false;
        lookCoroutine = null;
    }

    /// <summary>
    /// アニメーションを一時停止する（被弾時・死亡時）
    /// </summary>
    public void PauseAnimation()
    {
        if (animator != null)
        {
            animator.speed = 0f;
        }
    }

    /// <summary>
    /// アニメーションを再開する（リスポーン完了時）
    /// </summary>
    public void ResumeAnimation()
    {
        if (animator != null)
        {
            animator.speed = 1f;
        }
    }
}