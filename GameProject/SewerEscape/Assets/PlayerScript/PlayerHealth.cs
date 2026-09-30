using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("耐久設定")]
    [SerializeField] private int maxHits = 3;                       // 3回目でDestroy

    [Header("フェード演出設定")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;           // 画面を覆うパネルのCanvasGroup
    [Range(1.0f, 4.0f)]
    [SerializeField] private float fadeDuration = 1.5f;             // 暗くなる / 明るくなる時間（秒）
    [SerializeField] private float blackScreenWaitTime = 0.3f;      // 暗転中の停止時間（秒）

    [Header("操作・視点設定")]
    [Tooltip("移動・視点操作を行っているスクリプト（PlayerMove）をアタッチ")]
    [SerializeField] private MonoBehaviour playerMoveScript;
    [Tooltip("上下を向いているカメラ（Main Cameraなど）をアタッチ")]
    [SerializeField] private Transform playerCamera;

    private int currentHits = 0;
    private bool isDead = false;
    private bool isRespawning = false;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Quaternion spawnCameraRotation; // カメラの初期ローカル角度
    private CharacterController characterController;

    private void Awake()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        characterController = GetComponent<CharacterController>();

        // カメラの初期角度を記憶（設定されている場合）
        if (playerCamera != null)
        {
            spawnCameraRotation = playerCamera.localRotation;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckHit(other.gameObject);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        CheckHit(hit.gameObject);
    }

    private void CheckHit(GameObject target)
    {
        if (isDead || isRespawning) return;

        if (target.CompareTag("Enemy") || target.transform.root.CompareTag("Enemy"))
        {
            HandleHit();
        }
    }

    private void HandleHit()
    {
        currentHits++;
        Debug.Log($"Enemyに接触！ 被弾回数: {currentHits}/{maxHits}");

        if (currentHits >= maxHits)
        {
            Die();
        }
        else
        {
            isRespawning = true;
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        // 1. 移動・視点操作を停止
        SetPlayerInput(false);

        // 2. フェードアウト（徐々に暗転）
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        }

        // 3. 暗転中の待機
        yield return new WaitForSeconds(blackScreenWaitTime);

        // 4. 初期位置・初期向きへテレポート
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // 体の向きと位置をリセット
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        // 【視点の初期化】カメラの上下角度を初期値に戻す
        if (playerCamera != null)
        {
            playerCamera.localRotation = spawnCameraRotation;
        }

        // 【視点変数の初期化】PlayerMove側の角度累積変数をリセット（SendMessageで安全に呼び出し）
        if (playerMoveScript != null)
        {
            playerMoveScript.SendMessage("ResetLook", SendMessageOptions.DontRequireReceiver);
        }

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // 5. フェードイン（徐々に明転）
        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
            fadeCanvasGroup.blocksRaycasts = false;
        }

        // 6. 操作を再開
        SetPlayerInput(true);

        isRespawning = false;
    }

    private void SetPlayerInput(bool isEnabled)
    {
        if (playerMoveScript != null)
        {
            playerMoveScript.enabled = isEnabled;
        }
    }

    private IEnumerator Fade(float startAlpha, float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            fadeCanvasGroup.alpha = targetAlpha;
            yield break;
        }

        float timer = 0f;
        fadeCanvasGroup.alpha = startAlpha;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float smoothedT = Mathf.SmoothStep(0f, 1f, t);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothedT);

            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void Die()
    {
        isDead = true;
        SetPlayerInput(false);
        Debug.Log("3回被弾したためゲームオーバーします");
        Destroy(gameObject);
    }
}