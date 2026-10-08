using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneManager : MonoBehaviour
{
    [Header("フェード用のCanvasGroup（黒いパネル）")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("フェードにかける時間（秒）")]
    [SerializeField] private float fadeDuration = 1.0f;

    // 連打防止用フラグ
    private bool isTransitioning = false;

    void Start()
    {
        // 開始時はフェード画面を透明にしてクリックを通すように設定
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    void Update()
    {

    }

    /// <summary>
    /// 操作方法画面へ移動するメソッド（UIボタンのOnClickやイベントから呼び出し）
    /// </summary>
    public void GoToManual()
    {
        // 既に遷移中の場合は何もしない（連打防止）
        if (isTransitioning) return;

        StartCoroutine(FadeOutAndLoadScene("ManualScene"));
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        isTransitioning = true;

        if (fadeCanvasGroup != null)
        {
            // フェード中は他のボタンを押せないようにクリックを遮断
            fadeCanvasGroup.blocksRaycasts = true;

            float time = 0f;
            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(time / fadeDuration);
                yield return null; // 1フレーム待機
            }

            fadeCanvasGroup.alpha = 1f;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}