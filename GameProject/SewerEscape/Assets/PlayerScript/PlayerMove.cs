using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float dashSpeed = 8.0f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -7f;

    [Header("視点操作設定")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float GamePadlookSpeed = 150.0f; // ゲームパッド感度
    [SerializeField] private float MouselookSpeed = 1.0f;     // マウス感度
    [SerializeField] private float minPitch = -90.0f;
    [SerializeField] private float maxPitch = 90.0f;

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private float cameraPitch = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

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

        // 1. 移動入力の取得

        // ゲームパッド
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stickMove = gamepad.leftStick.ReadValue();
            if (stickMove.sqrMagnitude > 0.04f) // デッドゾーン設定
            {
                moveInput = stickMove;
            }
            if (gamepad.leftShoulder.isPressed) isDashPressed = true;
            if (gamepad.rightStickButton.isPressed) isLookingBack = true;
            if (gamepad.buttonSouth.wasPressedThisFrame) isJump = true;
        }

        // キーボード（キーが押されていたら上書き）
        if (Keyboard.current != null)
        {
            Vector2 keyMove = Vector2.zero;
            // キーボード操作(WASD)
            if (Keyboard.current.wKey.isPressed) keyMove.y += 1f;
            if (Keyboard.current.sKey.isPressed) keyMove.y -= 1f;
            if (Keyboard.current.aKey.isPressed) keyMove.x -= 1f;
            if (Keyboard.current.dKey.isPressed) keyMove.x += 1f;

            if (keyMove.sqrMagnitude > 0.01f)
            {
                moveInput = keyMove;
            }
            if (Keyboard.current.shiftKey.isPressed) isDashPressed = true; // Shiftキーでダッシュ
            if (Keyboard.current.spaceKey.isPressed) isJump = true;        // SpaceKeyでダッシュ
        }

        // 2. 視点入力の取得（デバイスごとの競合を防止）
        bool hasGamepadLook = false;

        // ゲームパッドの右スティック
        if (gamepad != null)
        {
            Vector2 stickLook = gamepad.rightStick.ReadValue();
            // スティックが少しでも倒されている場合のみ計算（デッドゾーン 0.04f）
            if (stickLook.sqrMagnitude > 0.04f)
            {
                yaw = stickLook.x * GamePadlookSpeed * Time.deltaTime;
                pitch = stickLook.y * GamePadlookSpeed * Time.deltaTime;
                hasGamepadLook = true;
            }
        }

        // マウス（ゲームパッドを触っていない、かつマウスが動いた時だけ処理）
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

        // 3. 視点回転の適用
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

        // 4. 移動処理
        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y);
        if (move.magnitude > 1.0f)
        {
            move.Normalize();
        }

        float currentSpeed = isDashPressed ? dashSpeed : moveSpeed;
        Vector3 finalVelocity = move * currentSpeed;

        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2.0f;
        }
        if (controller.isGrounded)
        {
            // 地面に張り付かせるための下向き微小速度
            if (verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2.0f;
            }

            // 接地時のみジャンプ初速を計算
            if (isJump)
            {
                // 目標の高さ(jumpHeight)に届く初速度: v = sqrt(2 * g * h)
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            }
        }
        else
        {
            // 空中にいる間は重力を加算
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        finalVelocity.y = verticalVelocity.y;

        controller.Move(finalVelocity * Time.deltaTime);
    }
    public void ResetLook()
    {
        // リスポーン時視点リセット
        cameraPitch = 0f;
        verticalVelocity = Vector3.zero;

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.identity;
        }
    }

    // 敵のいる座標へ瞬時に視点を向ける
    public void LookAtPosition(Vector3 targetPosition)
    {
        // 1. 水平方向（プレイヤー本体の向き）を敵に向ける
        Vector3 lookDirection = targetPosition - transform.position;
        lookDirection.y = 0f; // 水平方向のみ計算

        // 完全に重なっていない場合のみ回転（ゼロベクトル警告を防止）
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        // 2. 垂直方向（カメラの上下角度）を敵の高さに向ける
        if (cameraTransform != null)
        {
            Vector3 camToTarget = targetPosition - cameraTransform.position;
            float flatDistance = new Vector2(camToTarget.x, camToTarget.z).magnitude;

            // 水平からの仰角・俯角を計算（Unityのピッチ符号に合わせて反転）
            float targetPitch = -Mathf.Atan2(camToTarget.y, flatDistance) * Mathf.Rad2Deg;
            cameraPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

            // カメラの角度を反映
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }
}