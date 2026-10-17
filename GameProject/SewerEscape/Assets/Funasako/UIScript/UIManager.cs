using System.Collections; // コルーチン用に追加
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.UIElements;
// ビルドしたときバグ発見
// using UnityEngine.WSA;

public class UIManager : MonoBehaviour
{
    public static UIManager instance { get; private set; }

    [SerializeField] private string titileSceneName = "TitleScene";

    // フェード設定
    [Header("フェード設定")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;   // フェード用の黒いパネル
    [SerializeField] private float fadeDuration = 1.0f;     // フェードにかける秒数

    // HUD
    [SerializeField] private GameObject m_hudPanel;
    [SerializeField] private TextMeshProUGUI m_ItemText;
    [SerializeField] private TextMeshProUGUI m_lifeText;
    [SerializeField] private TextMeshProUGUI m_objectiveText;

    // リザルト
    [SerializeField] private GameObject m_resultPanel;
    [SerializeField] private TextMeshProUGUI m_timeText;
    [SerializeField] private TextMeshProUGUI m_timeScoreText;
    [SerializeField] private TextMeshProUGUI m_resultItemText;
    [SerializeField] private TextMeshProUGUI m_itemScoreText;
    [SerializeField] private TextMeshProUGUI m_resultLifeText;
    [SerializeField] private TextMeshProUGUI m_lostLifeText;
    [SerializeField] private TextMeshProUGUI m_ScoreText;
    [SerializeField] private TextMeshProUGUI m_totalScoreText;
    [SerializeField] private TextMeshProUGUI m_RankText;
    [SerializeField] private TextMeshProUGUI m_finalRankText;
    [SerializeField] private GameObject m_button;

    // ゲームオーバー
    [SerializeField] private GameObject m_gameOverPanel;

    private int m_maxItem = 0;
    private int m_currentItem = 0;
    private int m_maxLivesMemo = 3;
    private int m_currentLives = 3;
    private int m_currentHits = 0;
    private float m_passedTime = 0.0f;
    private float m_transitionDelay = 1.0f;
    private float m_scoreDelayTime = 0.5f;
    private float m_rankDelayTime = 1.0f;
    private bool m_isGameActive = false;
    private bool m_checkMaxItem = false;

    [SerializeField] private KeyFgmManager m_keymanager;
    //    [SerializeField] private PlayerHealth m_playerHealth;
    PlayerHealth m_playerHealth;
    LiquidTransition m_transition;


    public AudioSource m_audioSource;
    public AudioClip m_scoreSe;
    public AudioClip m_rankSe;
    public AudioClip m_panelSe;
    public AudioClip m_tlansSe;
    public AudioClip m_bgm;

    // リフレクション
    private FieldInfo m_hitsField;
    private FieldInfo m_maxHitsField;

    // 遷移中の連打防止フラグ
    private bool isTransitioning = false;

    void Start()
    {
        isTransitioning = false;

        m_playerHealth = FindAnyObjectByType<PlayerHealth>();
        m_transition=GetComponent<LiquidTransition>();

        // フェードパネルの初期化（透明・クリック透過）
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
        m_isGameActive = true;
        m_hudPanel.SetActive(true);
        m_resultPanel.SetActive(false);
        m_objectiveText.text = "COLLECT ALL ITEMS";



        // ↓重い処理らしいので普通にGet関数とか取得できるようにしてもらう

        m_hitsField = typeof(PlayerHealth).GetField("currentHits", BindingFlags.NonPublic | BindingFlags.Instance);
        m_maxHitsField = typeof(PlayerHealth).GetField("maxHits", BindingFlags.NonPublic | BindingFlags.Instance);

        if (m_maxHitsField != null)
        {
            m_maxLivesMemo = (int)m_maxHitsField.GetValue(m_playerHealth);
        }
    }

    void Update()
    {
        // ▼ 追加：パッドのAボタン（buttonSouth）でボタンを押す処理
        if (!isTransitioning && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            // 現在選択中のボタンがあればそれを押す
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null)
            {
                UnityEngine.UI.Button selectedBtn = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.Button>();
                if (selectedBtn != null && selectedBtn.interactable)
                {
                    selectedBtn.onClick.Invoke();
                }
            }
            // 選択中のものがなければ、表示されている m_button を押す
            else if (m_button != null && m_button.activeInHierarchy)
            {
                UnityEngine.UI.Button btn = m_button.GetComponentInChildren<UnityEngine.UI.Button>();
                if (btn != null && btn.interactable)
                {
                    btn.onClick.Invoke();
                }
            }
        }

        if (!m_isGameActive) return;

        if (m_passedTime > 0 && !m_checkMaxItem)
        {
            m_checkMaxItem = true;
            // アイテム数初期値を記録
            m_maxItem = m_keymanager.GetKeyFgmNum();
        }

        m_passedTime += Time.deltaTime;

        m_currentItem = m_keymanager.GetKeyFgmNum();
        m_ItemText.text = m_currentItem.ToString();

        if (m_keymanager.IsClearReach())
        {
            m_objectiveText.text = "BACK TO THE PORTAL";
        }

        m_currentHits = (int)m_hitsField.GetValue(m_playerHealth);
        m_currentLives = m_maxLivesMemo - m_currentHits;
        m_lifeText.text = m_currentLives.ToString();

        // デバッグ用
        //if (Keyboard.current.digit1Key.wasPressedThisFrame) OnItemCollected();
        //if (Keyboard.current.digit2Key.wasPressedThisFrame) OnPlayerDamaged();
        if (Keyboard.current.digit3Key.wasPressedThisFrame) ShowResultUI();
    }


    public void ShowResultUI()
    {

        m_timeText.gameObject.SetActive(false);
        m_timeScoreText.gameObject.SetActive(false);
        m_resultItemText.gameObject.SetActive(false);
        m_itemScoreText.gameObject.SetActive(false);
        m_resultLifeText.gameObject.SetActive(false);
        m_lostLifeText.gameObject.SetActive(false);
        m_ScoreText.gameObject.SetActive(false);
        m_totalScoreText.gameObject.SetActive(false);
        m_RankText.gameObject.SetActive(false);
        m_finalRankText.gameObject.SetActive(false);
        m_button.SetActive(false);
        m_isGameActive = false;
        m_hudPanel.SetActive(false);
        m_resultPanel.SetActive(true);
        StartCoroutine(ShowEscapePanel());

    }

    private IEnumerator ShowEscapePanel()
    {
        PlaySound(m_panelSe);
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(ShowScore());
    }
    private IEnumerator ShowScore()
    {
        m_transition.StartLiquidTransition();
        PlaySound(m_tlansSe);

        yield return new WaitForSeconds(m_transitionDelay);

        StartCoroutine(ShowResultSequence());

        UnlockCursur();
        yield return null;
    }

    private IEnumerator ShowResultSequence()
    {
        yield return new WaitForSeconds(m_scoreDelayTime);

        int minites = Mathf.FloorToInt(m_passedTime / 60F);
        int seconds = Mathf.FloorToInt(m_passedTime % 60F);
        m_timeScoreText.text = $"{minites:00}:{seconds:00}";
        m_timeText.gameObject.SetActive (true);
        m_timeScoreText.gameObject.SetActive(true);
        m_audioSource.Stop();
        PlaySound(m_scoreSe);


        yield return new WaitForSeconds(m_scoreDelayTime);

        m_itemScoreText.text = (m_maxItem - m_currentItem).ToString();
        m_resultItemText.gameObject.SetActive (true);
        m_itemScoreText.gameObject.SetActive(true);
        PlaySound(m_scoreSe);

        yield return new WaitForSeconds(m_scoreDelayTime);

        int lostLife = m_maxLivesMemo - m_currentHits;
        m_lostLifeText.text = lostLife.ToString();
        m_resultLifeText.gameObject.SetActive (true);
        m_lostLifeText.gameObject.SetActive(true);
        PlaySound(m_scoreSe);

        yield return new WaitForSeconds(m_scoreDelayTime);

        int totalScore = (m_currentItem * 10) + (m_currentHits * 50);
        m_totalScoreText.text = totalScore.ToString();
        m_ScoreText.gameObject.SetActive (true);
        m_totalScoreText.gameObject.SetActive(true);
        PlaySound(m_scoreSe);

        yield return new WaitForSeconds (m_scoreDelayTime);

        m_RankText.gameObject.SetActive (true);
        PlaySound (m_scoreSe);

        yield return new WaitForSeconds(m_rankDelayTime);

        string rank = "C";
        if (lostLife == 0 && m_currentItem >= m_maxItem) rank = "S";
        else if (lostLife == 1) rank = "A";
        else if (lostLife == 2) rank = "B";
        m_finalRankText.text = rank.ToString();
        m_finalRankText.gameObject.SetActive(true);
        PlaySound(m_rankSe);

        yield return new WaitForSeconds(m_rankDelayTime);

        m_button.gameObject.SetActive(true);

        // ▼ 追加：ボタン表示時に自動で選択状態にする
        UnityEngine.UI.Button resultBtn = m_button.GetComponentInChildren<UnityEngine.UI.Button>();
        if (resultBtn != null && UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(resultBtn.gameObject);
        }

        yield return new WaitForSeconds (m_scoreDelayTime);
        m_audioSource.Stop();
        PlaySound(m_bgm);

    }

    private void PlaySound(AudioClip se)
    {
        m_audioSource.PlayOneShot(se);
    }

   

    //public void ShowGameOverUI()
    //{
    //    m_isGameActive = false;
    //    m_hudPanel.SetActive(false);
    //    if (m_gameOverPanel != null) m_gameOverPanel.SetActive(true);

    //    UnlockCursur();
    //}

    public void OnReturnToTitle()
    {
        if (isTransitioning) return;
        StartCoroutine(FadeOutAndLoadScene(titileSceneName));
    }

    public void OnRetry()
    {
        if (isTransitioning) return;
        StartCoroutine(FadeOutAndLoadScene(SceneManager.GetActiveScene().name));
    }

    // フェードアウトしてからシーンを読み込む
    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        isTransitioning = true;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.transform.SetAsLastSibling(); // 最前面に描画
            fadeCanvasGroup.blocksRaycasts = true;         // 入力禁止

            float time = 0f;
            while (time < fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(time / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }

        // シーン遷移前に時間と音をもとに戻す
        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene(sceneName);
    }

    // デバッグ用
    private void UnlockCursur()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }
}