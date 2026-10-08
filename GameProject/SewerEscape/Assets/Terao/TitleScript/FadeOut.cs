using System.Collections;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("フェード用のCanvasGroup（黒いパネル）")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("フェードにかける時間（秒）")]
    [SerializeField] private float fadeDuration = 1.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // シーン開始時に自動でフェードを開始
        if (fadeCanvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    // 黒画面から徐々にゲーム画面を明るくする処理（フェードイン）
    private IEnumerator FadeIn()
    {
        // 開始時は真っ黒にして操作を遮断
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;

        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (time / fadeDuration));
            yield return null;
        }

        // 完全に透明にしてクリック等の操作を許可
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }
}