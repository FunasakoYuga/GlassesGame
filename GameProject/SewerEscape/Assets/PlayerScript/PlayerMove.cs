using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // コルーチンを使うために必要

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float dashSpeed = 8.0f;
    [SerializeField] private float gravity = -9.81f;

    [Header("視点操作設定")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSpeed = 120.0f;
    [SerializeField] private float minPitch = -90.0f;
    [SerializeField] private float maxPitch = 90.0f;

    [Header("クイックターン設定")]
    [SerializeField] private float quickTurnSpeed = 720f; // 回転の速さ（1秒あたりの度数）

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 verticalVelocity;
    private float cameraPitch = 0f;
    private bool isDashPressed = false;
    private bool isQuickTurning = false; // 回転中フラグ

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
        // クイックターン中は通常の視点操作や移動入力を一時的に無効化したい場合はここで弾くこともできます
        // （今回は入力受付中にターンが始まると上書きされる形になります）

        // 1. 入力取得（ゲームパッド 優先）
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            moveInput = gamepad.leftStick.ReadValue();
            lookInput = gamepad.rightStick.ReadValue();

            isDashPressed = gamepad.leftShoulder.isPressed;

            // 右スティック押し込みで滑らかなクイックターン
            if (gamepad.rightStickButton.wasPressedThisFrame && !isQuickTurning)
            {
                StartCoroutine(PerformQuickTurn());
            }
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

                isDashPressed = Keyboard.current.spaceKey.isPressed;
            }

            // マウス視点フォールバック
            lookInput = Vector2.zero;
            if (Mouse.current != null)
            {
                lookInput = Mouse.current.delta.ReadValue() * 0.1f;

                // マウス右クリックで滑らかなクイックターン
                if (Mouse.current.rightButton.wasPressedThisFrame && !isQuickTurning)
                {
                    StartCoroutine(PerformQuickTurn());
                }
            }
        }

        // 2. 通常の視点回転（クイックターン中でない時だけ反映させることも可能ですが、今回はそのまま）
        float yaw = lookInput.x * lookSpeed * Time.deltaTime;
        float pitch = lookInput.y * lookSpeed * Time.deltaTime;

        if (Mathf.Abs(yaw) > 0.001f)
        {
            transform.Rotate(Vector3.up * yaw);
        }

        if (cameraTransform != null)
        {
            cameraPitch -= pitch;
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
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

    // 滑らかに180度回転させるコルーチン
    private IEnumerator PerformQuickTurn()
    {
        isQuickTurning = true;

        float targetAngle = 180f;
        float rotatedAngle = 0f;

        while (rotatedAngle < targetAngle)
        {
            float step = quickTurnSpeed * Time.deltaTime;
            if (rotatedAngle + step > targetAngle)
            {
                step = targetAngle - rotatedAngle;
            }

            transform.Rotate(0f, step, 0f);
            rotatedAngle += step;

            yield return null; // 1フレーム待つ
        }

        isQuickTurning = false;
    }
}