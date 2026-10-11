using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("ポーズ画面のUIパネル")]
    [SerializeField] private GameObject pausePanel;

    [Header("ポーズを開いたときに最初に選択するボタン")]
    [SerializeField] private GameObject firstSelectedButton;

    [Header("視点操作スクリプト（ポーズ中に自動で一時無効化）")]
    [SerializeField] private MonoBehaviour cameraController;

    [Header("フェード用のCanvasGroup（黒いパネル）")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("フェードにかける時間（秒）")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("ポーズメニューの各ボタン")]
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject manualButton;
    [SerializeField] private GameObject titleButton;

    [Header("操作説明用のCanvas")]
    [SerializeField] private GameObject manualCanvas;

    [Header("操作説明のUIオブジェクト")]
    [SerializeField] private GameObject keyMouManual;
    [SerializeField] private GameObject padManual;

    [Header("切り替え用ボタンのオブジェクト")]
    [SerializeField] private GameObject padButton;      // キーボード表示中に押す「PAD」ボタン
    [SerializeField] private GameObject keyMouButton;   // パッド表示中に押す「KEY/MOUSE」ボタン

    [Header("操作説明内の戻るボタン（あれば設定）")]
    [SerializeField] private GameObject manualBackButton;

    [Header("ボタン押下演出の待機時間（秒）")]
    [SerializeField] private float pressDelay = 0.1f;   // パッドで押した色（灰色）が見えるようにする待機時間

    // 現在ポーズ中かどうかのフラグ（外部スクリプトから PauseManager.IsPaused で参照可能）
    public static bool IsPaused { get; private set; } = false;

    // シーン遷移中の連打・操作防止フラグ
    private bool isTransitioning = false;

    // 操作説明切り替え中の連打防止フラグ
    private bool isSwitching = false;

    void Start()
    {
        // 開始時はポーズフラグをリセットし、ポーズ画面を非表示にしておく
        IsPaused = false;
        isTransitioning = false;
        isSwitching = false;

        // 音の停止状態もリセット
        AudioListener.pause = false;

        // 開始時はフェード画面を透明にしてクリックを通すように設定
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // 操作説明の表示状態を初期化
        ResetManualUI();

        // 通常プレイ時のカーソル状態（非表示・中央固定）
        LockCursor();
    }

    void Update()
    {
        // 遷移中はポーズ切り替え入力を無効化
        if (isTransitioning) return;

        // キーボードのEscキー、またはゲームパッドのメニュー（Start/Options）ボタンが押されたとき
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            // 操作説明Canvasが表示されている場合は、ポーズ解除ではなく操作説明を閉じる
            if (manualCanvas != null && manualCanvas.activeSelf)
            {
                CloseManual();
                return;
            }

            TogglePause();
        }
    }

    /// <summary>
    /// ポーズ状態の切り替え
    /// </summary>
    public void TogglePause()
    {
        if (IsPaused)
        {
            ContinueGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// ゲームを一時停止する
    /// </summary>
    public void PauseGame()
    {
        IsPaused = true;

        // ゲーム内時間を停止
        Time.timeScale = 0f;

        // ゲーム内の音を一時停止
        AudioListener.pause = true;

        // ポーズ画面を表示
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        // 操作説明を閉じた初期状態（3つのボタン表示）に戻す
        ResetManualUI();

        // 視点操作スクリプトを停止
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        // マウスカーソルを表示して自由に動かせるようにする
        UnlockCursor();

        // ゲームパッド操作用に最初のボタンを選択状態にする
        if (firstSelectedButton != null && EventSystem.current != null)
        {
            StartCoroutine(SetSelectedButtonCoroutine(firstSelectedButton));
        }
    }

    /// <summary>
    /// ゲームを再開する（ContinueボタンのOnClickからも呼び出し可能）
    /// </summary>
    public void ContinueGame()
    {
        IsPaused = false;

        // ゲーム内時間を通常に戻す
        Time.timeScale = 1f;

        // ゲーム内の音を再開
        AudioListener.pause = false;

        // ポーズ画面を非表示
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // 操作説明UIも非表示に戻しておく
        ResetManualUI();

        // 視点操作スクリプトを再開
        if (cameraController != null)
        {
            cameraController.enabled = true;
        }

        // マウスカーソルを再度非表示・中央固定
        LockCursor();

        // 選択状態を解除
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// 操作説明ボタン（ManualButton）のOnClickに設定する
    /// </summary>
    public void OnManualClicked()
    {
        if (isTransitioning) return;
        OpenManual();
    }

    /// <summary>
    /// 操作説明を開き、ボタン群を非表示にする
    /// </summary>
    public void OpenManual()
    {
        // 3つのボタンを非表示
        if (continueButton != null) continueButton.SetActive(false);
        if (manualButton != null) manualButton.SetActive(false);
        if (titleButton != null) titleButton.SetActive(false);

        // 操作説明Canvasを表示
        if (manualCanvas != null)
        {
            manualCanvas.SetActive(true);
        }

        // 操作説明内の戻るボタンがあれば表示
        if (manualBackButton != null) manualBackButton.SetActive(true);

        // 開始時の初期状態を設定（キーボード・マウスを表示、パッドを非表示）
        ShowKeyMouManualImmediate();
    }

    /// <summary>
    /// ボタンを押すたびに交互に表示を切り替える
    /// </summary>
    public void ToggleManual()
    {
        if (keyMouManual == null || padManual == null || isSwitching) return;
        StartCoroutine(ToggleManualCoroutine());
    }

    private IEnumerator ToggleManualCoroutine()
    {
        isSwitching = true;
        yield return new WaitForSecondsRealtime(pressDelay); // 押下色（灰色）を見せるため少し待機

        // 現在のキーボード表示状態を反転させて切り替え
        bool isKeyMouActive = keyMouManual.activeSelf;
        keyMouManual.SetActive(!isKeyMouActive);
        padManual.SetActive(isKeyMouActive);

        // ボタンの表示状態も連動して切り替え
        if (padButton != null) padButton.SetActive(!isKeyMouActive);
        if (keyMouButton != null) keyMouButton.SetActive(isKeyMouActive);

        // コントローラー用に選択フォーカスを切り替えたボタンへ移動
        SetSelected(!isKeyMouActive ? padButton : keyMouButton);

        isSwitching = false;
    }

    /// <summary>
    /// キーボード・マウス操作説明を表示
    /// </summary>
    public void ShowKeyMouManual()
    {
        if (isSwitching) return;
        StartCoroutine(ShowKeyMouManualCoroutine());
    }

    private IEnumerator ShowKeyMouManualCoroutine()
    {
        isSwitching = true;
        yield return new WaitForSecondsRealtime(pressDelay);
        ShowKeyMouManualImmediate();
        isSwitching = false;
    }

    private void ShowKeyMouManualImmediate()
    {
        if (keyMouManual != null) keyMouManual.SetActive(true);
        if (padManual != null) padManual.SetActive(false);

        // PADボタンを表示し、KeyMouseボタンを非表示
        if (padButton != null) padButton.SetActive(true);
        if (keyMouButton != null) keyMouButton.SetActive(false);

        // コントローラー用にPADボタンを選択
        SetSelected(padButton);
    }

    /// <summary>
    /// パッド操作説明を表示
    /// </summary>
    public void ShowPadManual()
    {
        if (isSwitching) return;
        StartCoroutine(ShowPadManualCoroutine());
    }

    private IEnumerator ShowPadManualCoroutine()
    {
        isSwitching = true;
        yield return new WaitForSecondsRealtime(pressDelay);

        if (keyMouManual != null) keyMouManual.SetActive(false);
        if (padManual != null) padManual.SetActive(true);

        // PADボタンを非表示にし、KeyMouseボタンを表示
        if (padButton != null) padButton.SetActive(false);
        if (keyMouButton != null) keyMouButton.SetActive(true);

        // コントローラー用にKeyMouseボタンを選択
        SetSelected(keyMouButton);

        isSwitching = false;
    }

    /// <summary>
    /// 指定したボタンをコントローラーの選択状態にする
    /// </summary>
    private void SetSelected(GameObject target)
    {
        if (target != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(SetSelectedButtonCoroutine(target));
        }
    }

    /// <summary>
    /// 操作説明を閉じ、ボタン群を再表示する（戻るボタンのOnClick等からも呼び出し可能）
    /// </summary>
    public void CloseManual()
    {
        ResetManualUI();

        // ボタン再表示後、ManualButtonを再選択
        if (manualButton != null && EventSystem.current != null)
        {
            StartCoroutine(SetSelectedButtonCoroutine(manualButton));
        }
    }

    // 操作説明を閉じて3つのボタンを表示する初期状態へ戻す処理
    private void ResetManualUI()
    {
        // 操作説明Canvasを非表示にする
        if (manualCanvas != null)
        {
            manualCanvas.SetActive(false);
        }

        if (keyMouManual != null) keyMouManual.SetActive(false);
        if (padManual != null) padManual.SetActive(false);
        if (padButton != null) padButton.SetActive(false);
        if (keyMouButton != null) keyMouButton.SetActive(false);
        if (manualBackButton != null) manualBackButton.SetActive(false);

        if (continueButton != null) continueButton.SetActive(true);
        if (manualButton != null) manualButton.SetActive(true);
        if (titleButton != null) titleButton.SetActive(true);
    }

    /// <summary>
    /// ゲームに戻るボタン（ContinueButton）のOnClickに設定する
    /// </summary>
    public void OnContinueClicked()
    {
        if (isTransitioning) return;
        ContinueGame();
    }

    /// <summary>
    /// タイトルに戻るボタン（TitleButton）のOnClickに設定する
    /// </summary>
    public void OnTitleClicked()
    {
        if (isTransitioning) return;
        GoToTitle("TitleScene");
    }

    /// <summary>
    /// タイトル画面へ戻る（TitleボタンのOnClickから呼び出し用）
    /// </summary>
    public void GoToTitle(string titleSceneName = "TitleScene")
    {
        if (isTransitioning) return;
        StartCoroutine(FadeOutAndLoadTitle(titleSceneName));
    }

    // フェードアウトしてからタイトルシーンを読み込むコルーチン
    private IEnumerator FadeOutAndLoadTitle(string titleSceneName)
    {
        isTransitioning = true;

        if (fadeCanvasGroup != null)
        {
            // フェードパネルが非アクティブならアクティブ化
            fadeCanvasGroup.gameObject.SetActive(true);

            // ポーズパネル等の手前に確実に描画されるよう最前面へ移動
            fadeCanvasGroup.transform.SetAsLastSibling();

            // フェード中は他のボタンを押せないように操作を遮断
            fadeCanvasGroup.blocksRaycasts = true;

            float time = 0f;
            while (time < fadeDuration)
            {
                // ポーズ中（Time.timeScale = 0）でも動くように unscaledDeltaTime を使用
                time += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(time / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }
        else
        {
            Debug.LogWarning("[PauseManager] FadeCanvasGroup が設定されていません！フェードなしで遷移します。");
        }

        // シーン遷移前に必ず時間とカーソルを戻しておく
        Time.timeScale = 1f;
        AudioListener.pause = false; // 音の停止も解除
        UnlockCursor();

        SceneManager.LoadScene(titleSceneName);
    }

    // カーソルを画面内に閉じ込めて非表示にする
    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // カーソルの固定を解除して表示する
    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // パネル表示直後に確実にボタンを選択状態にするための待機コルーチン
    private IEnumerator SetSelectedButtonCoroutine(GameObject target)
    {
        yield return null; // 1フレーム待機
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
        }
    }

    void OnDestroy()
    {
        // シーン破棄時に確実に時間を戻しておく
        Time.timeScale = 1f;
        AudioListener.pause = false; // 音の停止も解除
    }
}