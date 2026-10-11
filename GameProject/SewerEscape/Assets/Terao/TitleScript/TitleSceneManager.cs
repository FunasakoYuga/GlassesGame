using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneManager : MonoBehaviour
{
    [Header("フェード用のCanvasGroup（黒いパネル）")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("フェードにかける時間（秒）")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("BGM設定（ManualScene遷移時も残すAudioSource）")]
    [SerializeField] private AudioSource bgmAudioSource;

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

        // ManualSceneから戻ってきた際に、BGMが2重に鳴るのを防ぎつつ参照を維持
        if (bgmAudioSource != null)
        {
            AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (var src in sources)
            {
                if (src != bgmAudioSource && src.clip == bgmAudioSource.clip)
                {
                    Destroy(bgmAudioSource.gameObject);
                    bgmAudioSource = src;
                    break;
                }
            }
        }
    }

    public void OnStartButtonClicked()
    {
        // 既に遷移中の場合は何もしない
        if (isTransitioning) return;

        StartCoroutine(FadeOutAndLoadScene("AllScene"));
    }

    /// <summary>
    /// 操作方法画面へ移動するメソッド（UIボタンのOnClickやイベントから呼び出し）
    /// </summary>
    public void GoToManual()
    {
        // 既に遷移中の場合は何もしない（連打防止）
        if (isTransitioning) return;

        // ManualSceneに行くときはBGMを破棄させずに残す
        if (bgmAudioSource != null)
        {
            DontDestroyOnLoad(bgmAudioSource.gameObject);
        }

        StartCoroutine(FadeOutAndLoadScene("ManualScene"));
    }

    public void OnExitButtonClicked()
    {
        // 既に遷移中の場合は何もしない
        if (isTransitioning) return;

        StartCoroutine(FadeOutAndQuit());
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        // ManualScene以外の場合はBGMも徐々にフェードアウトさせる
        bool shouldFadeBgm = (sceneName != "ManualScene");

        // フェードアウト完了まで待機
        yield return StartCoroutine(FadeOut(shouldFadeBgm));

        // ManualScene以外（AllSceneなど）へ行くときはBGMを完全に停止・破棄する
        if (shouldFadeBgm && bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
            Destroy(bgmAudioSource.gameObject);
        }

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeOutAndQuit()
    {
        // フェードアウト完了まで待機
        yield return StartCoroutine(FadeOut(fadeBgm: true));

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeOut(bool fadeBgm = true)
    {
        isTransitioning = true;

        // フェード開始時の元の音量を記録
        float startVolume = bgmAudioSource != null ? bgmAudioSource.volume : 1f;

        if (fadeCanvasGroup != null)
        {
            // フェード中は他のボタンを押せないように操作を遮断
            fadeCanvasGroup.blocksRaycasts = true;

            float time = 0f;
            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                float progress = Mathf.Clamp01(time / fadeDuration);
                fadeCanvasGroup.alpha = progress;

                // ManualScene以外へ行くときは画面暗転に合わせて音量を徐々に下げる
                if (fadeBgm && bgmAudioSource != null)
                {
                    bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, progress);
                }

                yield return null; // 1フレーム待機
            }

            fadeCanvasGroup.alpha = 1f;
        }

        // フェードアウト完了時に確実に音量を0にしておく
        if (fadeBgm && bgmAudioSource != null)
        {
            bgmAudioSource.volume = 0f;
        }
    }
}