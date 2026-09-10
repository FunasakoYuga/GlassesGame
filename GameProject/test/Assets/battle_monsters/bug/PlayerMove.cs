using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float gravity = -9.81f;

    [Header("視点操作設定")]
    [SerializeField] private Transform cameraTransform; // Main Cameraをここにアタッチ
    [SerializeField] private float lookSpeed = 120.0f;  // 右スティックの感度
    [SerializeField] private float minPitch = -90.0f;   // 見下ろす限界角度
    [SerializeField] private float maxPitch = 90.0f;    // 見上げる限界角度

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 verticalVelocity;
    private float cameraPitch = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // カメラが未割り当ての場合、子オブジェクトのMain Cameraを自動取得
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // --------------------------------------------------
        // 1. 入力取得（ゲームパッド 優先）
        // --------------------------------------------------
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            moveInput = gamepad.leftStick.ReadValue();
            lookInput = gamepad.rightStick.ReadValue();
        }
        else
        {
            // キーボード（WASD）フォールバック
            moveInput = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
                if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
                if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
                if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
            }

            // マウス視点フォールバック
            lookInput = Vector2.zero;
            if (Mouse.current != null)
            {
                lookInput = Mouse.current.delta.ReadValue() * 0.1f;
            }
        }

        // --------------------------------------------------
        // 2. 右スティックによる視点回転
        // --------------------------------------------------
        float yaw = lookInput.x * lookSpeed * Time.deltaTime;
        float pitch = lookInput.y * lookSpeed * Time.deltaTime;

        // 左右回転：プレイヤー本体をY軸で回す
        if (Mathf.Abs(yaw) > 0.001f)
        {
            transform.Rotate(Vector3.up * yaw);
        }

        // 上下回転：カメラだけをX軸で回す（角度制限付き）
        if (cameraTransform != null)
        {
            cameraPitch -= pitch;
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        // --------------------------------------------------
        // 3. 左スティックによる移動（見ている向き基準）
        // --------------------------------------------------
        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y);
        if (move.magnitude > 1.0f)
        {
            move.Normalize();
        }

        Vector3 finalVelocity = move * moveSpeed;

        // 接地判定と重力
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2.0f;
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        finalVelocity.y = verticalVelocity.y;

        // 落下バグ対策のためMoveは1フレームに1回のみ実行
        controller.Move(finalVelocity * Time.deltaTime);
    }
}