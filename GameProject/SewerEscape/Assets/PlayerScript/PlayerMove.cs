using UnityEngine;
using UnityEngine.InputSystem;



[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5.0f;    // 移動速度
    [SerializeField] private float dashSpeed = 8.0f;    // ダッシュ速度
    [SerializeField] private float gravity = -9.81f;

    [Header("視点操作設定")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float GamePadlookSpeed = 120.0f;
    [SerializeField] private float MouselookSpeed = 150.0f;
    [SerializeField] private float minPitch = -90.0f;
    [SerializeField] private float maxPitch = 90.0f;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 verticalVelocity;
    private float cameraPitch = 0f;
    private bool isDashPressed = false; // ダッシュ中かどうか
    private bool isLookingBack = false; // 後ろを向いているか
    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // 1. 入力取得（ゲームパッド 優先）
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            moveInput = gamepad.leftStick.ReadValue();
            lookInput = gamepad.rightStick.ReadValue();

            isDashPressed = gamepad.leftShoulder.isPressed; // LBボタンでダッシュ

            isLookingBack = gamepad.rightStickButton.isPressed; // 右スティックを押している間後ろを見る
        }
        else
        {
            // キーボード操作
            moveInput = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
                if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
                if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
                if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;

                isDashPressed = Keyboard.current.spaceKey.isPressed;
            }

            // マウス視点操作
            lookInput = Vector2.zero;
            if (Mouse.current != null)
            {
                lookInput = Mouse.current.delta.ReadValue() * 0.1f;

                isLookingBack = Mouse.current.rightButton.isPressed;    // 右クリックで後ろを見る
            }
        }

        // 2. 通常の視点回転（クイックターン中でない時だけ反映させることも可能ですが、今回はそのまま）
        float yaw = lookInput.x * GamePadlookSpeed * Time.deltaTime;
        float yaws = lookInput.x * MouselookSpeed;
        float pitch = lookInput.y * GamePadlookSpeed * Time.deltaTime;
        float pitchs = lookInput.y * MouselookSpeed;

        if (Mathf.Abs(yaw) > 0.001f)
        {
            transform.Rotate(Vector3.up * yaw);
            transform.Rotate(Vector3.up * yaws);
        }

        if (cameraTransform != null)
        {
            cameraPitch -= pitch;
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

           // 後ろを見るときは視点を180度後ろに動かす
            float yawOffset = isLookingBack ? 180f : 0f;
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, yawOffset, 0f);
        }

        // 3. 移動処理
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
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        finalVelocity.y = verticalVelocity.y;

        controller.Move(finalVelocity * Time.deltaTime);
    }
}