using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("耐久設定")]
    [SerializeField] private int maxHits = 3;                       // 3回目で完全フェードアウト

    [Header("フェード演出設定")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;           // 画面を覆うパネル
    [SerializeField] private float lookWaitTime = 2.0f;             // 視点を向けてから暗転するまでの待機時間（秒）
    [Range(1.0f, 4.0f)]
    [SerializeField] private float fadeDuration = 1.5f;             // フェードイン / フェードアウト
    [SerializeField] private float fadeInDuration = 3.0f;           // リスポーン時のゆっくりフェードインする時間（秒）
    [SerializeField] private float blackScreenWaitTime = 0.3f;      // フェードの停止時間（秒）

    [Header("ゲームオーバーUI設定")]
    [SerializeField] private GameObject gameOverPanel;              // 暗転後に表示するゲームオーバーパネル

    [Header("操作・視点設定")]
    [Tooltip("移動・視点操作を行っているPlayerMoveスクリプト")]
    [SerializeField] private PlayerMove playerMove;

    [Header("アニメーション・オーディオ設定")]
    [SerializeField] private Animator playerAnimator;               // 停止させるアニメーター（未設定なら自動取得）
    [SerializeField] private AudioSource[] playerAudioSources;      // 停止させるオーディオ（未設定なら自動取得）

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

        // Animatorが未設定なら自身または子オブジェクトから取得
        if (playerAnimator == null)
        {
            playerAnimator = GetComponentInChildren<Animator>();
        }

        // AudioSourceが未設定なら自身または子オブジェクトから全て取得
        if (playerAudioSources == null || playerAudioSources.Length == 0)
        {
            playerAudioSources = GetComponentsInChildren<AudioSource>();
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        // 開始時はゲームオーバーパネルを非表示にしておく
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
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

        // 敵に当たった瞬間にアニメーションとAudioを停止
        StopAudioAndAnimation();

        // 敵と接触したら敵のほうにカメラを向ける
        if (playerMove != null)
        {
            // 敵の胴体あたりを見るように少し高さを増加
            Vector3 enemyCenter = target.transform.position + Vector3.up * 1.0f;
            playerMove.RotateToTarget(enemyCenter, 0.4f);
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

        // 視点を敵に向けてから2秒待機
        yield return new WaitForSeconds(lookWaitTime);

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
            yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));
            fadeCanvasGroup.blocksRaycasts = false;
        }

        // 6. 操作を再開
        SetPlayerInput(true);

        // リスポーン完了後にアニメーションを再開
        ResumeAnimation();

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

        // 視点を敵に向けてからフェードアウト待機
        yield return new WaitForSeconds(lookWaitTime);

        // フェードアウト
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

            // 確実に真っ黒（Alpha = 1）で固定(リザルト)
            fadeCanvasGroup.alpha = 1f;
        }

        // フェードアウト後にマウスの操作を解放してカーソルを表示する
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // プレイヤーの移動判定と当たり判定のロック
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

    /// <summary>
    /// Audioとアニメーションを停止する
    /// </summary>
    private void StopAudioAndAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.speed = 0f; // アニメーションをその場で一時停止
        }

        if (playerAudioSources != null)
        {
            foreach (AudioSource audio in playerAudioSources)
            {
                if (audio != null)
                {
                    audio.Stop(); // 足音や効果音を停止
                }
            }
        }
    }

    /// <summary>
    /// アニメーションの再生速度を通常に戻す
    /// </summary>
    private void ResumeAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.speed = 1f;
        }
    }
}