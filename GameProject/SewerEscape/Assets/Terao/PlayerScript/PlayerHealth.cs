using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems; // コントローラーのボタン選択制御用に追加
using UnityEngine.SceneManagement; // シーン遷移用に追加

public class PlayerHealth : MonoBehaviour
{
    [Header("耐久設定")]
    [SerializeField] private int maxHits = 3;                            // 3回目で完全フェードアウト

    [Header("フェード演出設定")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;            // 画面を覆うパネル
    [SerializeField] private float lookWaitTime = 2.0f;              // 視点を向けてから暗転するまでの待機時間（秒）
    [Range(1.0f, 4.0f)]
    [SerializeField] private float fadeDuration = 1.5f;              // フェードイン / フェードアウト
    [SerializeField] private float fadeInDuration = 3.0f;            // リスポーン時フェードイン時間（秒）
    [SerializeField] private float blackScreenWaitTime = 0.3f;       // フェードの停止時間（秒）

    [Header("ゲームオーバーUI設定")]
    [SerializeField] private GameObject gameOverPanel;               // 暗転後に表示するゲームオーバーパネル
    [SerializeField] private GameObject firstGameOverButton;         // 暗転後にコントローラーで最初に選択するボタン

    [Header("リザルトUI設定")]
    [SerializeField] private GameObject resultPanel;                 // 【追加】表示するリザルトパネル
    [SerializeField] private GameObject firstResultButton;           // 【追加】表示後にコントローラーで最初に選択するボタン
    [SerializeField] private AudioSource resultAudioSource;          // 【追加】リザルト効果音を鳴らすAudioSource（未設定時はResultPanel内またはカメラ位置で再生）
    [SerializeField] private AudioClip resultClip;                   // 【追加】ResultPanel表示時に鳴らす効果音（ジングル・クリアSE）

    [Header("シーン遷移設定")]
    [SerializeField] private string titleSceneName = "TitleScene";   // タイトルシーン名

    [Header("操作・視点設定")]
    [Tooltip("移動・視点操作を行っているPlayerMoveスクリプト")]
    [SerializeField] private PlayerMove playerMove;

    [Header("アニメーション・オーディオ設定")]
    [SerializeField] private Animator playerAnimator;                // 停止させるアニメーター
    [SerializeField] private AudioSource GameOveraudioSource;                // ゲームオーバーサウンド

    [SerializeField] private AudioSource[] playerAudioSources;       // 停止させるオーディオ
    [SerializeField] private AudioClip[] dieClips;           // 死亡SE
    [SerializeField] private AudioClip gameOverClip;         // 3回死亡時（ゲームオーバー時）のSE

    [Header("ゴール時オーディオ設定")]
    [SerializeField] private AudioSource[] bugAudioSources;          // ゴール時に音量を0にするbug（敵）のAudioSource（未設定時は自動検索）

    [Header("ゴール（梯子上昇）演出設定")]
    [SerializeField] private float goalClimbDuration = 2.0f;          // 【追加】上昇演出および完全暗転までの時間（秒）
    [SerializeField] private float climbSpeed = 2.5f;                // 【追加】梯子を上る上昇速度（m/s）
    [SerializeField] private float goalFadeDuration = 1.0f;           // 【追加】暗転（フェードアウト）にかける時間（秒）
    [SerializeField] private float climbRotateSpeed = 8.0f;          // 【追加】梯子の向き（0度または180度）へ体を合わせる回転補間速度

    // 敵に当たった時にプレイヤー（カメラ）が倒れ込む演出設定
    [Header("被弾時の転倒（倒れ込み）演出設定")]
    [SerializeField] private bool enableFallDownEffect = true;       // 倒れ込むカメラ演出を行うか
    [SerializeField] private Transform cameraTransform;              // 倒す対象のカメラ（未設定時はMainCameraを自動取得）
    [SerializeField] private float fallDelay = 0.4f;                 // 敵の方を向いてから倒れ始めるまでの待ち時間（秒）
    [SerializeField] private float fallDuration = 0.6f;              // 倒れ込むのにかかる時間（秒）
    [SerializeField] private float fallDropHeight = 0.8f;            // 倒れたときに視点が下がる高さ（メートル）
    [SerializeField] private float fallRollAngle = 65f;              // 倒れたときに首（カメラ）が横に傾く角度
    [SerializeField] private string fallTriggerName = "";            // Animatorで倒れるモーションを再生する場合のTrigger名（空欄ならスキップ）

    // タイムスケール用定数（マジックナンバー回避）
    private const float NormalTimeScale = 1.0f;                      // 通常のゲーム速度
    private const float StoppedTimeScale = 0.0f;                     // 時間停止時のゲーム速度

    // オーディオ音量用定数（マジックナンバー回避）
    private const float MutedVolume = 0.0f;                          // ミュート時の音量
    private const float DefaultResultVolume = 1.0f;                  // リザルト効果音の音量

    // フェード演出用定数（マジックナンバー回避）
    private const float FadeAlphaClear = 0.0f;                       // 完全透明時のアルファ値
    private const float FadeAlphaBlack = 1.0f;                       // 完全暗転時のアルファ値

    // 梯子演出用定数（マジックナンバー回避）
    private const float HalfTurnAngle = 180.0f;                      // 梯子の反対向き（180度反転）角度

    private Vector3 defaultCamLocalPos;                              // カメラの初期ローカル座標（リスポーン復帰用）
    private Quaternion defaultCamLocalRot;                           // カメラの初期ローカル回転（リスポーン復帰用）
    private HeartbeatController heartbeatController;                 // 心臓音リセット用の参照

    private int currentHits = 0;
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
        // 前のシーンやリトライ前の停止状態（Time.timeScale = 0）を引き継がないよう、開始時に通常速度へリセット
        Time.timeScale = NormalTimeScale;

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        characterController = GetComponent<CharacterController>();

        // 心臓音スクリプトの取得
        heartbeatController = GetComponentInChildren<HeartbeatController>();

        if (playerMove == null)
        {
            playerMove = GetComponent<PlayerMove>();
            if (playerMove == null)
            {
                playerMove = GetComponentInParent<PlayerMove>();
            }
            if (playerMove == null)
            {
                playerMove = GetComponentInChildren<PlayerMove>();
            }
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

        // 開始時はリザルトパネルを非表示にしておく
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // シーン遷移やオブジェクト破棄時に時間停止が残らないよう安全に戻す
        Time.timeScale = NormalTimeScale;
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

        // 【追加】GoalItemタグを持つオブジェクトに接触した場合
        if (target.CompareTag("GoaLItem") || target.transform.root.CompareTag("GoaLItem"))
        {
            HandleGoal(target);
            return;
        }

        // Enemyタグを持つオブジェクトに接触したら被弾or死亡
        if (target.CompareTag("Enemy") || target.transform.root.CompareTag("Enemy"))
        {
            HandleHit(target);
        }
    }

    // 【追加】ゴール（GoalItem接触時）の処理
    private void HandleGoal(GameObject target)
    {
        isDead = true; // 以降の被弾やリスポーン判定を無効化

        // 梯子を上る上昇演出とフェードアウトを開始
        StartCoroutine(GoalClimbRoutine(target));
    }

    /// <summary>
    /// 【追加】梯子を上るようにプレイヤーを上昇させ、2秒後に完全フェードアウトしてResultPanelを表示するコルーチン
    /// </summary>
    private IEnumerator GoalClimbRoutine(GameObject goalTarget)
    {
        // 1. 移動・視点入力を即座に遮断
        SetPlayerInput(false);

        // 2. 物理移動・落下の干渉を防ぐためCharacterControllerを一時無効化
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // 3. 心臓音を完全に停止・リセット
        if (heartbeatController != null)
        {
            heartbeatController.ResetHeartbeat();
            heartbeatController.enabled = false;
        }

        // GoalItemのY軸回転を基準に、0度または180度のうちプレイヤーの現在向きに近い目標回転を決定
        float goalY = (goalTarget != null) ? goalTarget.transform.eulerAngles.y : transform.eulerAngles.y;
        Quaternion rot0 = Quaternion.Euler(0f, goalY, 0f);
        Quaternion rot180 = Quaternion.Euler(0f, goalY + HalfTurnAngle, 0f);
        Quaternion targetRotation = (Quaternion.Angle(transform.rotation, rot0) <= Quaternion.Angle(transform.rotation, rot180)) ? rot0 : rot180;

        // 2秒後にちょうど完全暗転するようにフェード開始タイミングを算出
        float fadeStartTime = Mathf.Max(0f, goalClimbDuration - goalFadeDuration);
        bool fadeStarted = false;

        float timer = 0f;
        while (timer < goalClimbDuration)
        {
            float dt = Time.deltaTime;
            timer += dt;

            // 梯子の向き（0度または180度）へスムーズに体を向ける
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, climbRotateSpeed * dt);

            // カメラの傾きも正面（初期ローカル回転）へ戻す
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, defaultCamLocalRot, climbRotateSpeed * dt);
            }

            // 梯子を上るように上方向へ移動
            transform.position += Vector3.up * (climbSpeed * dt);

            // フェードアウト開始タイミングになったら暗転を開始
            if (!fadeStarted && timer >= fadeStartTime)
            {
                fadeStarted = true;
                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.blocksRaycasts = true;
                    StartCoroutine(Fade(FadeAlphaClear, FadeAlphaBlack, goalFadeDuration));
                }
            }

            yield return null;
        }

        // 最終的な向きを確実に目標角度に固定
        transform.rotation = targetRotation;
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = defaultCamLocalRot;
        }

        // 確実に画面を真っ黒で固定
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = FadeAlphaBlack;
            fadeCanvasGroup.blocksRaycasts = true;
        }

        // 2秒後（上昇演出および暗転完了後）にResultPanelを表示
        ShowResultPanel();
    }

    /// <summary>
    /// 【追加】ResultPanelを表示し、音を消してゲーム内の時間を停止する処理
    /// </summary>
    public void ShowResultPanel()
    {
        // 1. 時間は止めず、プレイヤー入力・移動を完全に遮断
        SetPlayerInput(false);

        // CharacterControllerを無効化して物理移動・落下を完全停止
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // 2. プレイヤーのアニメーションを停止
        if (playerAnimator != null)
        {
            playerAnimator.speed = 0f;
        }

        // 3. PlayerのAudioの音量を0にし、AudioSourceコンポーネントをOFFにする（※ResultPanel内のオーディオは除外して保護）
        MutePlayerAudio();

        // 4. bugのAudioは停止せずそのまま鳴らしておくため呼び出しをスキップ
        // MuteBugAudio();

        // 5. 心臓音も完全に停止・リセット
        if (heartbeatController != null)
        {
            heartbeatController.ResetHeartbeat();
            heartbeatController.enabled = false;
        }

        // 6. シーン内にあるポーズ関連のスクリプトを無効化してポーズ入力を完全停止
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var script in allScripts)
        {
            if (script != null)
            {
                string scriptName = script.GetType().Name;
                if (scriptName.Contains("Pause") || scriptName.Contains("GameManager"))
                {
                    script.enabled = false;
                }
            }
        }

        // 7. ResultPanelを表示（時間は止めていないため通常通りアニメーションや演出が動きます）
        if (resultPanel != null)
        {
            EnableUnscaledTimeForUI(resultPanel);

            // コメントアウトしときます
            //    resultPanel.SetActive(true);
            // ここだけ追加させてください
            UIManager.instance.ShowResultUI();

            // コントローラー用にリザルトパネルの最初のボタンを選択状態にする
            if (firstResultButton != null && EventSystem.current != null)
            {
                StartCoroutine(SetSelectedGameOverButton(firstResultButton));
            }
        }

        // 8. 【追加】ResultPanel表示時の効果音（ジングルSE）を再生
        PlayResultSound();

        // 9. ボタン操作のためにマウスカーソルを表示・解放
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("ResultPanelを表示しました。PlayerMoveを無効化し、PlayerのAudioSourceをOFFに設定しました。");
    }

    /// <summary>
    /// 【追加】ResultPanel表示時の効果音（ジングルSE）を再生する
    /// </summary>
    private void PlayResultSound()
    {
        if (resultClip == null) return;

        // 専用のresultAudioSourceが指定されていればそれを使用
        AudioSource targetSource = resultAudioSource;

        // 未設定でResultPanel配下にAudioSourceがあればそれを優先使用
        if (targetSource == null && resultPanel != null)
        {
            targetSource = resultPanel.GetComponentInChildren<AudioSource>();
        }

        if (targetSource != null)
        {
            targetSource.volume = DefaultResultVolume;
            targetSource.PlayOneShot(resultClip, DefaultResultVolume);
        }
        else
        {
            // AudioSourceがない場合はカメラ位置で直接再生
            Vector3 playPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(resultClip, playPos, DefaultResultVolume);
        }
    }

    /// <summary>
    /// 【追加】PlayerのAudioSourceの音量を0にし、AudioSourceコンポーネントをOFFにする
    /// </summary>
    private void MutePlayerAudio()
    {
        if (playerAudioSources != null)
        {
            foreach (AudioSource audio in playerAudioSources)
            {
                if (audio != null)
                {
                    // ResultPanel配下の音源やリザルト専用AudioSourceはミュート対象から除外して保護
                    if (resultAudioSource != null && audio == resultAudioSource) continue;
                    if (resultPanel != null && audio.transform.IsChildOf(resultPanel.transform)) continue;

                    audio.volume = MutedVolume;
                    audio.Stop();
                    audio.enabled = false; // PlayerのAudioSourceをOFFにする
                }
            }
        }

        AudioSource[] selfAudios = GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audio in selfAudios)
        {
            if (audio != null)
            {
                // ResultPanel配下の音源やリザルト専用AudioSourceはミュート対象から除外して保護
                if (resultAudioSource != null && audio == resultAudioSource) continue;
                if (resultPanel != null && audio.transform.IsChildOf(resultPanel.transform)) continue;

                audio.volume = MutedVolume;
                audio.Stop();
                audio.enabled = false; // PlayerのAudioSourceをOFFにする
            }
        }
    }

    /// <summary>
    /// 【追加】bug（敵）のAudioSourceの音量を0にする
    /// </summary>
    private void MuteBugAudio()
    {
        // インスペクターで明示的に登録されている場合
        if (bugAudioSources != null && bugAudioSources.Length > 0)
        {
            foreach (AudioSource audio in bugAudioSources)
            {
                if (audio != null)
                {
                    // ResultPanel配下の音源やリザルト専用AudioSourceはミュート対象から除外して保護
                    if (resultAudioSource != null && audio == resultAudioSource) continue;
                    if (resultPanel != null && audio.transform.IsChildOf(resultPanel.transform)) continue;

                    audio.volume = MutedVolume;
                    audio.Stop();
                }
            }
            return;
        }

        // 未登録の場合はEnemyタグやオブジェクト名からbugのAudioSourceを自動検索してミュート
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
            {
                AudioSource[] enemyAudios = enemy.GetComponentsInChildren<AudioSource>();
                foreach (AudioSource audio in enemyAudios)
                {
                    if (audio != null)
                    {
                        if (resultAudioSource != null && audio == resultAudioSource) continue;
                        if (resultPanel != null && audio.transform.IsChildOf(resultPanel.transform)) continue;

                        audio.volume = MutedVolume;
                        audio.Stop();
                    }
                }
            }
        }

        // オブジェクト名に"bug"が含まれるAudioSourceも検索して音量を0にする
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource audio in allAudioSources)
        {
            if (audio != null)
            {
                // ResultPanel配下の音源やリザルト専用AudioSourceはミュート対象から除外して保護
                if (resultAudioSource != null && audio == resultAudioSource) continue;
                if (resultPanel != null && audio.transform.IsChildOf(resultPanel.transform)) continue;

                string objName = audio.gameObject.name.ToLower();
                string rootName = audio.transform.root != null ? audio.transform.root.name.ToLower() : "";

                if (objName.Contains("bug") || rootName.Contains("bug"))
                {
                    audio.volume = MutedVolume;
                    audio.Stop();
                }
            }
        }
    }

    /// <summary>
    /// 【追加】UI演出（AnimatorやParticleSystem）がTime.timeScale = 0でも停止しないようUnscaledTimeに設定する
    /// </summary>
    private void EnableUnscaledTimeForUI(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        // UIに付いているすべてのAnimatorをUnscaledTime（実時間連動）に切り替え
        Animator[] animators = targetPanel.GetComponentsInChildren<Animator>(true);
        foreach (var anim in animators)
        {
            if (anim != null)
            {
                anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }

        // UIにパーティクル等が付いている場合も実時間で再生
        ParticleSystem[] particles = targetPanel.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.useUnscaledTime = true;
            }
        }
    }

    /// <summary>
    /// 【追加】シーン内のゲームオーディオ（敵の音や環境音など）を一括停止する
    /// （ResultPanel自身に付いているクリアジングルやUI用AudioSourceは止めないように保護）
    /// </summary>
    private void StopAllGameAudio()
    {
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var audio in allAudioSources)
        {
            if (audio == null) continue;

            // ResultPanel内にあるAudioSource（リザルト画面のSEやBGM）は停止対象外にする
            if (resultPanel != null && audio.transform.IsChildOf(resultPanel.transform))
            {
                continue;
            }

            audio.Stop();
        }
    }

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

        // 倒れたカメラの位置と角度を元の高さ・向きに戻す
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
        // 1. playerMove の取得・無効化
        if (playerMove == null)
        {
            playerMove = GetComponent<PlayerMove>();
            if (playerMove == null) playerMove = GetComponentInParent<PlayerMove>();
            if (playerMove == null) playerMove = GetComponentInChildren<PlayerMove>();
        }

        if (playerMove != null)
        {
            playerMove.enabled = isEnabled;
        }

        // 2. New Input SystemのPlayerInputコンポーネントがある場合も安全に無効化
        Component playerInput = GetComponent("PlayerInput");
        if (playerInput == null) playerInput = GetComponentInParent(System.Type.GetType("UnityEngine.InputSystem.PlayerInput, Unity.InputSystem"));
        if (playerInput == null) playerInput = GetComponentInChildren(System.Type.GetType("UnityEngine.InputSystem.PlayerInput, Unity.InputSystem"));

        if (playerInput is Behaviour inputBehaviour)
        {
            inputBehaviour.enabled = isEnabled;
        }

        // 3. カメラ側に視点・回転スクリプトが付いている場合の制御
        if (cameraTransform != null && !isEnabled)
        {
            MonoBehaviour[] camScripts = cameraTransform.GetComponents<MonoBehaviour>();
            foreach (var s in camScripts)
            {
                if (s == null) continue;
                string sName = s.GetType().Name.ToLower();
                // 視点操作やマウスルック系のスクリプトを停止
                if (sName.Contains("look") || sName.Contains("camera") || sName.Contains("view"))
                {
                    s.enabled = false;
                }
            }
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
    /// 【追加】UIボタン等から呼び出すリトライ処理
    /// </summary>
    public void Retry()
    {
        // 停止した時間を通常速度に戻してからシーンを再読み込み
        Time.timeScale = NormalTimeScale;
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>
    /// 【追加】UIボタン等から呼び出すタイトル画面遷移処理
    /// </summary>
    public void BackToTitle()
    {
        // 停止した時間を通常速度に戻してからタイトルシーンを読み込み
        Time.timeScale = NormalTimeScale;
        SceneManager.LoadScene(titleSceneName);
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