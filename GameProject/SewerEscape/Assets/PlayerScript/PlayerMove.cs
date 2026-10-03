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
    [SerializeField] private float minPitch = -90.0f;
    [SerializeField] private float maxPitch = 90.0f;

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private float cameraPitch = 0f;

    // アニメーション制御用
    private Animator animator;
    private string currentAnimationState = "PlayerAnimator";

    private const string STATE_IDLE = "Idle";
    private const string STATE_WALK = "Walk";
    private const string STATE_RUN = "Run";
    private const string STATE_JUMP = "Jump";

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

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
        bool isDashPressed = false;
        bool isLookingBack = false;
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
            if (Keyboard.current.spaceKey.wasPressedThisFrame) isJump = true;
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
            if (Mouse.current.rightButton.isPressed) isLookingBack = true;
        }

        // 視点操作
        if (Mathf.Abs(yaw) > 0.0001f)
        {
            transform.Rotate(Vector3.up * yaw);
        }

        if (cameraTransform != null)
        {
            cameraPitch -= pitch;
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
            float yawOffset = isLookingBack ? 180f : 0f;
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, yawOffset, 0f);
        }

        // 移動処理
        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y);
        if (move.magnitude > 1.0f)
        {
            move.Normalize();
        }

        float currentSpeed = isDashPressed ? dashSpeed : moveSpeed;
        Vector3 finalVelocity = move * currentSpeed;

        if (controller.isGrounded)
        {
            if (verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2.0f;
            }
            if (isJump)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            }
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        finalVelocity.y = verticalVelocity.y;
        controller.Move(finalVelocity * Time.deltaTime);

        // アニメーションをスクリプトから制御
        UpdateAnimation(moveInput, isDashPressed);
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
                targetState = isDash ? STATE_RUN : STATE_WALK;
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
            cameraPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }
}