using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting;

public class UIManager : MonoBehaviour
{
    public static UIManager instance { get; private set; }

    [SerializeField] private string titileSceneName = "TitleScene";

    // HUD
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private TextMeshProUGUI ItemText;
    [SerializeField] private TextMeshProUGUI lifeText;
    [SerializeField] private TextMeshProUGUI objectiveText;

    // リザルト
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI timeScoreText;
    [SerializeField] private TextMeshProUGUI itemScoreText;
    [SerializeField] private TextMeshProUGUI lostLifeText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshProUGUI finalRankText;

    // ゲームオーバー
    [SerializeField] private GameObject gameOverPanel;


    private int maxItem = 10;
    private int currentItem = 0;
    private int maxLives = 3;
    private int currentLives = 3;
    private float passedTime = 0.0f;
    private bool isGameActive = false;

    void Start()
    {
        hudPanel.SetActive(true);
        resultPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        if (objectiveText != null) objectiveText.text = "COLLECT ALL ITEMS";
    }

    void Update()
    {
        if (isGameActive) passedTime += Time.deltaTime;

        // デバッグ用
        //if (Keyboard.current.digit1Key.wasPressedThisFrame) OnItemCollected();
        //if (Keyboard.current.digit2Key.wasPressedThisFrame) OnPlayerDamaged();
        //if (Keyboard.current.digit3Key.wasPressedThisFrame) ShowResultUI();
    }

    // ゲーム開始時のUI初期化
    public void SetUpGameUI(int totalItem, int startLives)
    {
        maxItem = totalItem;
        currentItem = 0;
        maxLives = startLives;
        currentLives = startLives;
        passedTime = 0;
        isGameActive = true;

        hudPanel.SetActive(true);
        resultPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        UpdateHUD();
        if (objectiveText != null) objectiveText.text = "COLLECT ALL ITEM";
    }

    // HUD更新
    private void UpdateHUD()
    {
        int remaining = maxItem - currentItem;
        if (ItemText != null) ItemText.text = remaining.ToString();
        if (lifeText != null) lifeText.text = "LIVES:" + currentLives.ToString();
    }

    public void OnItemCollected()
    {
        if (!isGameActive) return;

        if (currentItem != maxItem)
        {
            currentItem++;

        }
        if (currentItem >= maxItem)
        {
            currentItem = maxItem;
            if (objectiveText != null) objectiveText.text = "GET BACK TO THE PORTAL";
        }
        UpdateHUD();
    }

    public void OnPlayerDamaged()
    {
        Debug.Log("call");

        if (!isGameActive) return;

        currentLives--;
        UpdateHUD();
        Debug.Log("damage");

        if (currentLives <= 0)
        {
            ShowGameOverUI();
        }
    }

    public void ShowResultUI()
    {
        isGameActive = false;
        hudPanel.SetActive(false);
        resultPanel.SetActive(true);

        int minites = Mathf.FloorToInt(passedTime / 60F);
        int seconds = Mathf.FloorToInt(passedTime % 60F);
        if (timeScoreText != null)
        {
            timeScoreText.text = string.Format("TIME: { 0:0 } { 1:00 }", minites, seconds);
        }

        int livesLost = maxLives - currentLives;
        if (itemScoreText != null) itemScoreText.text = "SOUL SHARDS: " + currentItem;
        if (lostLifeText != null) lostLifeText.text = "LIVES LOST: " + livesLost + " (残り残機: " + currentLives + ")";

        int totalScore = (currentItem * 10) + (currentLives * 50);
        if (totalScoreText != null) totalScoreText.text = "TOTAL SCORE: " + totalScore;

        string rank = "C";
        if (livesLost == 0 && currentItem >= maxItem) rank = "S";
        else if (livesLost == 1) rank = "A";
        else if (livesLost == 2) rank = "B";

        if (finalRankText != null) finalRankText.text = "FINAL RANK: " + rank;

        UnlockCursur();
    }

    public void ShowGameOverUI()
    {
        isGameActive = false;
        hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        UnlockCursur();
    }

    public void OnReturnToTitle()
    {
        SceneManager.LoadScene(titileSceneName);
    }
    public void OnRetry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // デバッグ用
    private void UnlockCursur()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}