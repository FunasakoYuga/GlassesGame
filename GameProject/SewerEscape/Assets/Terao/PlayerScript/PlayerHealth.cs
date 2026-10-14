using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems; // コントローラーのボタン選択制御用に追加

public class PlayerHealth : MonoBehaviour
{
    [Header("耐久設定")]
    [SerializeField] private int maxHits = 3;                       // 3回目で完全フェードアウト

    [Header("フェード演出設定")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;           // 画面を覆うパネル
    [SerializeField] private float lookWaitTime = 2.0f;             // 視点を向けてから暗転するまでの待機時間（秒）
    [Range(1.0f, 4.0f)]
    [SerializeField] private float fadeDuration = 1.5f;             // フェードイン / フェードアウト
    [SerializeField] private float fadeInDuration = 3.0f;           // リスポーン時フェードイン時間（秒）
    [SerializeField] private float blackScreenWaitTime = 0.3f;      // フェードの停止時間（秒）

    [Header("ゲームオーバーUI設定")]
    [SerializeField] private GameObject gameOverPanel;              // 暗転後に表示するゲームオーバーパネル
    [SerializeField] private GameObject firstGameOverButton;        // 暗転後にコントローラーで最初に選択するボタン

    [Header("操作・視点設定")]
    [Tooltip("移動・視点操作を行っているPlayerMoveスクリプト")]
    [SerializeField] private PlayerMove playerMove;

    [Header("アニメーション・オーディオ設定")]
    [SerializeField] private Animator playerAnimator;               // 停止させるアニメーター
    [SerializeField] private AudioSource GameOveraudioSource;               // ゲームオーバーサウンド

    [SerializeField] private AudioSource[] playerAudioSources;      // 停止させるオーディオ
    [SerializeField] private AudioClip[] dieClips;          // 死亡SE
    [SerializeField] private AudioClip gameOverClip;        // 3回死亡時（ゲームオーバー時）のSE

    // ★追加：敵に当たった時にプレイヤー（カメラ）が倒れ込む演出設定
    [Header("被弾時の転倒（倒れ込み）演出設定")]
    [SerializeField] private bool enableFallDownEffect = true;      // 倒れ込むカメラ演出を行うか
    [SerializeField] private Transform cameraTransform;             // 倒す対象のカメラ（未設定時はMainCameraを自動取得）
    [SerializeField] private float fallDelay = 0.4f;                // 敵の方を向いてから倒れ始めるまでの待ち時間（秒）
    [SerializeField] private float fallDuration = 0.6f;             // 倒れ込むのにかかる時間（秒）
    [SerializeField] private float fallDropHeight = 0.8f;           // 倒れたときに視点が下がる高さ（メートル）
    [SerializeField] private float fallRollAngle = 65f;             // 倒れたときに首（カメラ）が横に傾く角度
    [SerializeField] private string fallTriggerName = "";           // Animatorで倒れるモーションを再生する場合のTrigger名（空欄ならスキップ）

    private Vector3 defaultCamLocalPos;                             // ★追加：カメラの初期ローカル座標（リスポーン復帰用）
    private Quaternion defaultCamLocalRot;                          // ★追加：カメラの初期ローカル回転（リスポーン復帰用）
    private HeartbeatController heartbeatController;                // ★追加：心臓音リセット用の参照

    public int currentHits = 0;
    private bool isDead = false;
    private bool isRespawning = false;

    // 外部から演出中・死亡中かを確認するためのプロパティ（Pause制御用に追加）
    public bool IsDead => isDead;
    public bool IsRespawning => isRespawning;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private CharacterController characterController;

    private void Awake()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        characterController = GetComponent<CharacterController>();

        // 心臓音スクリプトの取得
        heartbeatController = GetComponentInChildren<HeartbeatController>();

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

        // カメラが未設定なら子オブジェクトまたはMainCameraを取得し、初期位置を記録
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }
        if (cameraTransform != null)
        {
            defaultCamLocalPos = cameraTransform.localPosition;
            defaultCamLocalRot = cameraTransform.localRotation;
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
        // リスポーンか死亡か
        if (isDead || isRespawning) return;

        // Enemyタグを持つオブジェクトに接触したら被弾or死亡
        if (target.CompareTag("Enemy") || target.transform.root.CompareTag("Enemy"))
        {
            HandleHit(target);
        }
    }

    // 被弾処理
    // 被弾処理
    private void HandleHit(GameObject target)
    {
        currentHits++;
        Debug.Log($"Enemyに接触！ 被弾回数: {currentHits}/{maxHits}");

        // 敵に当たった瞬間にアニメーションとAudioを停止
        StopAudioAndAnimation();

        // 死亡SEを再生
        PlayDieSound();

        // 敵と接触したら敵のほうにカメラを向ける
        if (playerMove != null)
        {
            // 敵の胴体あたりを見るように少し高さを増加
            Vector3 enemyCenter = target.transform.position + Vector3.up * 1.0f;
            playerMove.RotateToTarget(enemyCenter, 0.4f);
        }

        // 3回目（ゲームオーバー）の死亡かどうかで心臓音の扱いを分岐
        if (currentHits >= maxHits)
        {
            // 3回目の死亡（ゲームオーバー）のときは、心臓音を完全に停止・リセットして二度と鳴らないようにする
            if (heartbeatController != null)
            {
                heartbeatController.ResetHeartbeat();
                // 心臓音のコンポーネント自体を無効化しておくと完全に安心です
                heartbeatController.enabled = false;
            }

            StartCoroutine(Die());
        }
        else
        {
            // ２回被弾時は心臓音を一度リセットする
            if (heartbeatController != null)
            {
                heartbeatController.ResetHeartbeat();
            }

            isRespawning = true;
            StartCoroutine(RespawnRoutine());
        }

        // プレイヤーが倒れる演出
        if (enableFallDownEffect)
        {
            StartCoroutine(FallDownRoutine());
        }
    }

    // 敵の方を向いたあと、地面にバタッと倒れ込む演出
    private IEnumerator FallDownRoutine()
    {
        // 敵の方を完全に向き終わるまで待つ
        if (playerMove != null)
        {
            while (playerMove.IsRotatingToTarget)
            {
                yield return null;
            }
        }

        // 敵の方へ振り向く演出が終わるまで少し待つ
        if (fallDelay > 0f)
        {
            yield return new WaitForSeconds(fallDelay);
        }

        if (cameraTransform == null) yield break;

        Vector3 startPos = cameraTransform.localPosition;
        Quaternion startRot = cameraTransform.localRotation;

        // 地面に向かって視点を下げ、首を横に傾ける目標値を作成
        Vector3 targetPos = new Vector3(startPos.x, Mathf.Max(0.15f, startPos.y - fallDropHeight), startPos.z);
        Quaternion targetRot = startRot * Quaternion.Euler(15f, 0f, fallRollAngle);

        float timer = 0f;
        while (timer < fallDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fallDuration);
            // 最初はゆっくり、地面にぶつかる直前に加速して倒れるイージング（t * t）
            float easeIn = t * t;

            cameraTransform.localPosition = Vector3.Lerp(startPos, targetPos, easeIn);
            cameraTransform.localRotation = Quaternion.Slerp(startRot, targetRot, easeIn);

            yield return null;
        }

        cameraTransform.localPosition = targetPos;
        cameraTransform.localRotation = targetRot;
    }

    private IEnumerator RespawnRoutine()
    {
        // 操作と視点ロック
        SetPlayerInput(false);

        // 視点を敵に向けてから2秒待機
        yield return new WaitForSeconds(lookWaitTime);

        // 徐々に暗転
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

        // ★追加：倒れたカメラの位置と角度を元の高さ・向きに戻す
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = defaultCamLocalPos;
            cameraTransform.localRotation = defaultCamLocalRot;
        }

        // 視点と操作の初期化
        if (playerMove != null)
        {
            playerMove.ResetLook();
        }

        // リスポーン地点に戻った際にも心臓音をリセット
        if (heartbeatController != null)
        {
            heartbeatController.ResetHeartbeat();
        }

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // 徐々にフェードアウト
        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));
            fadeCanvasGroup.blocksRaycasts = false;
        }

        // 入力再開
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

        // フェードアウト後にマウスの入力を解放してカーソルを表示する
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // ゲームオーバー時SEを再生
            PlayGameOverSound();

            // コントローラー用にゲームオーバーボタンを選択
            if (firstGameOverButton != null && EventSystem.current != null)
            {
                StartCoroutine(SetSelectedGameOverButton(firstGameOverButton));
            }
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

        // プレイヤーの見た目のみを非表示にする
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        Debug.Log("ゲームオーバー：画面を暗転させたまま停止しました");
    }

    // パネル表示後に確実にボタンを選択状態にする
    private IEnumerator SetSelectedGameOverButton(GameObject target)
    {
        yield return null;
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
        }
    }

    /// <summary>
    /// アニメーションとサウンドを停止する
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
    /// 死亡SE再生
    /// </summary>
    private void PlayDieSound()
    {
        if (dieClips == null || dieClips.Length == 0) return;

        // 登録されているクリップの中からランダムに1つ選択
        AudioClip clip = dieClips[Random.Range(0, dieClips.Length)];
        if (clip == null) return;

        // プレイヤーのAudioSourceを使って再生
        if (playerAudioSources != null && playerAudioSources.Length > 0 && playerAudioSources[0] != null)
        {
            playerAudioSources[0].volume = 1.0f;
            playerAudioSources[0].PlayOneShot(clip, 1.0f);
        }
        else
        {
            // AudioSourceが取得できていない場合の処理
            Vector3 playPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(clip, playPos, 1.0f);
        }
    }

    /// <summary>
    /// ゲームオーバーBGM再生
    /// </summary>
    private void PlayGameOverSound()
    {
        // 専用のGameOverClipがあればそれを再生、未設定ならdieClipsから再生
        AudioClip clipToPlay = gameOverClip;
        if (clipToPlay == null && dieClips != null && dieClips.Length > 0)
        {
            clipToPlay = dieClips[Random.Range(0, dieClips.Length)];
        }

        if (clipToPlay == null) return;

        // GameOveraudioSourceがインスペクターで設定されていればそれを優先、未設定ならplayerAudioSourcesを使用
        AudioSource targetSource = GameOveraudioSource;
        if (targetSource == null && playerAudioSources != null && playerAudioSources.Length > 0)
        {
            targetSource = playerAudioSources[0];
        }

        if (targetSource != null)
        {
            targetSource.volume = 1.0f;
            targetSource.PlayOneShot(clipToPlay, 1.0f);
        }
        else
        {
            Vector3 playPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(clipToPlay, playPos, 0.8f);
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