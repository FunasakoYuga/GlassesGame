using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleUIManager : MonoBehaviour
{
    [Header("フェード用のCanvasGroup（黒いパネル）")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("フェードにかける時間（秒）")]
    [SerializeField] private float fadeDuration = 1.0f;

    // 連打防止用フラグ
    private bool isTransitioning = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 開始時はフェード画面を透明にしてクリックを通すように設定
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    public void OnStartButtonClicked()
    {
        // 既に遷移中の場合は何もしない
        if (isTransitioning) return;

        StartCoroutine(FadeOutAndLoadScene());
    }

    public void OnExitButtonClicked()
    {
        // 既に遷移中の場合は何もしない
        if (isTransitioning) return;

        StartCoroutine(FadeOutAndQuit());
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        // フェードアウト完了まで待機
        yield return StartCoroutine(FadeOut());

        SceneManager.LoadScene("AllScene");
    }

    private IEnumerator FadeOutAndQuit()
    {
        // フェードアウト完了まで待機
        yield return StartCoroutine(FadeOut());

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeOut()
    {
        isTransitioning = true;

        if (fadeCanvasGroup != null)
        {
            // フェード中は他のボタンを押せないように操作を遮断
            fadeCanvasGroup.blocksRaycasts = true;

            float time = 0f;
            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(time / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }
    }
}