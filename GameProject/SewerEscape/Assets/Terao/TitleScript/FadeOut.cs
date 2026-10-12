using System.Collections;
using UnityEngine;

public class FadeOut : MonoBehaviour
{
    [Header("フェード用のCanvasGroup（黒いパネル）")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("フェードにかける時間（秒）")]
    [SerializeField] private float fadeDuration = 1.0f;

    void Start()
    {
        // シーン開始時に自動でフェードを開始
        if (fadeCanvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }
    }

    // フェードイン
    private IEnumerator FadeIn()
    {
        // 開始時は真っ黒にして入力を遮断
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;

        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (time / fadeDuration));
            yield return null;
        }

        // 完全に透明にして入力を許可
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }
}