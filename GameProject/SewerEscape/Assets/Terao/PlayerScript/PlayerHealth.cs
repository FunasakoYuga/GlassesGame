using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("耐久設定")]
    [SerializeField] private int maxHits = 3;                       // 3回目で完全フェードアウト

    [Header("フェード演出設定")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;           // 画面を覆うパネル
    [Range(1.0f, 4.0f)]
    [SerializeField] private float fadeDuration = 1.5f;             // フェードイン / フェードアウト
    [SerializeField] private float blackScreenWaitTime = 0.3f;      // フェードの停止時間（秒）

    [Header("操作・視点設定")]
    [Tooltip("移動・視点操作を行っているPlayerMoveスクリプト")]
    [SerializeField] private PlayerMove playerMove;

    private int currentHits = 0;
    private bool isDead = false;
    private bool isRespawning = false;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private CharacterController characterController;

    private void Awake()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        characterController = GetComponent<CharacterController>();

        if (playerMove == null)
        {
            playerMove = GetComponent<PlayerMove>();
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
            HandleHit(target);
        }
    }

    // 被弾処理（当たった相手のオブジェクトを受け取る）
    private void HandleHit(GameObject target)
    {
        currentHits++;
        Debug.Log($"Enemyに接触！ 被弾回数: {currentHits}/{maxHits}");

        // 敵と接触したら敵のほうにカメラを向ける
        if (playerMove != null)
        {
            // 敵の胴体あたりを見るように少し高さを加算（+1.0m）
            Vector3 enemyCenter = target.transform.position + Vector3.up * 1.0f;
            playerMove.LookAtPosition(enemyCenter);
        }

        if (currentHits >= maxHits)
        {
            StartCoroutine(Die());
        }
        else
        {
            isRespawning = true;
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        // 操作と視点ロック
        SetPlayerInput(false);

        // 2. 徐々に暗転
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        }

        // フェード待機
        yield return new WaitForSeconds(blackScreenWaitTime);

        // リスポーン地点に復活
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        // 視点と操作の初期化
        if (playerMove != null)
        {
            playerMove.ResetLook();
        }

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // 5. 徐々にフェードアウト
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
        if (playerMove != null)
        {
            playerMove.enabled = isEnabled;
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

    private IEnumerator Die()
    {
        isDead = true;
        SetPlayerInput(false);
        Debug.Log("3回被弾したためゲームオーバー");
        // 1. フェードアウト
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

            // 確実に真っ黒（Alpha = 1）で固定(リザルト)
            fadeCanvasGroup.alpha = 1f;
        }

        // 2. プレイヤーの移動判定・当たり判定を停止
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 3. プレイヤーの見た目（3Dモデル）のみを非表示にする
        // （カメラやCanvasを消さないよう、RendererのみをOFFにする）
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        Debug.Log("ゲームオーバー：画面を暗転させたまま停止しました");
    }
}