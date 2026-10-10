using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // コントローラーのフォーカス制御のために追加

public class TitleFadeAndShine : MonoBehaviour
{
    [Header("フェードイン設定")]
    [SerializeField] private CanvasGroup titleCanvasGroup1; // タイトル1のCanvasGroup
    [SerializeField] private CanvasGroup titleCanvasGroup2; // タイトル2のCanvasGroup
    [SerializeField] private float startDelay = 0.5f;       // ゲーム開始からフェード開始までの待機秒数
    [SerializeField] private float fadeInDuration1 = 1.2f;  // タイトル1のフェードにかける秒数
    [SerializeField] private float delayBetweenTitles = 0.4f; // タイトル1表示後、タイトル2開始までの待機秒数
    [SerializeField] private float fadeInDuration2 = 1.2f;  // タイトル2のフェードにかける秒数

    [Header("光る（発光）設定")]
    [SerializeField] private Graphic titleGraphic1;          // タイトルのImageまたはText 1
    [SerializeField] private Graphic titleGraphic2;          // タイトルのImageまたはText 2
    [SerializeField] private float shineInterval = 3.0f;     // ループの待機間隔（秒）
    [SerializeField] private float flashDuration1 = 0.15f;   // タイトル1が光って元に戻るまでの秒数
    [SerializeField] private float flashDuration2 = 0.25f;   // タイトル2が光って元に戻るまでの秒数
    [SerializeField] private float delayBetweenShines = 0f;  // タイトル1が光ってからタイトル2が光るまでの時間差
    [SerializeField] private Color shineColor = Color.white; // 光った際の色（白など）

    [Header("ボタン表示・操作受付設定")]
    [SerializeField] private CanvasGroup buttonCanvasGroup;   // ボタン群（または親オブジェクト）のCanvasGroup
    [SerializeField] private float buttonFadeDuration = 0.8f; // ボタン自体のフェードイン秒数（0なら即座にパッと表示）
    [SerializeField] private GameObject firstSelectedButton;  // 操作可能になった瞬間にコントローラーで最初に選択するボタン

    private Color originalColor1;
    private Color originalColor2;

    private void Start()
    {
        // 初期状態を非表示（透明）に設定
        if (titleCanvasGroup1 != null)
        {
            titleCanvasGroup1.alpha = 0f;
        }
        if (titleCanvasGroup2 != null)
        {
            titleCanvasGroup2.alpha = 0f;
        }

        // ボタンの初期状態（非表示・操作不可）に設定
        if (buttonCanvasGroup != null)
        {
            buttonCanvasGroup.alpha = 0f;
            buttonCanvasGroup.interactable = false;   // 操作を無効化
            buttonCanvasGroup.blocksRaycasts = false; // マウスクリックの当たり判定を無効化
        }

        // 演出中の誤入力を防ぐため、コントローラーの選択状態を解除
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // 元の色を保持
        if (titleGraphic1 != null)
        {
            originalColor1 = titleGraphic1.color;
        }
        if (titleGraphic2 != null)
        {
            originalColor2 = titleGraphic2.color;
        }

        // フェード演出と発光ループを開始
        StartCoroutine(TitleSequenceRoutine());
    }

    /// <summary>
    /// タイトル1 → タイトル2の順にフェードインし、その後時間差で光らせるコルーチン
    /// </summary>
    private IEnumerator TitleSequenceRoutine()
    {
        // 開始までのディレイ
        yield return new WaitForSeconds(startDelay);

        // --- 1. タイトル1のフェードイン ---
        if (titleCanvasGroup1 != null)
        {
            yield return StartCoroutine(FadeInGroup(titleCanvasGroup1, fadeInDuration1));
        }

        // タイトル1とタイトル2の間の時間差
        yield return new WaitForSeconds(delayBetweenTitles);

        // --- 2. タイトル2のフェードイン ---
        if (titleCanvasGroup2 != null)
        {
            yield return StartCoroutine(FadeInGroup(titleCanvasGroup2, fadeInDuration2));
        }

        // --- 3. ボタンのフェードインと操作有効化 ---
        if (buttonCanvasGroup != null)
        {
            // ボタンをフェードイン（buttonFadeDurationが0より大きい場合）
            if (buttonFadeDuration > 0f)
            {
                yield return StartCoroutine(FadeInGroup(buttonCanvasGroup, buttonFadeDuration));
            }
            else
            {
                buttonCanvasGroup.alpha = 1f;
            }

            // 完全に表示されたので、操作・クリックを受け付ける
            buttonCanvasGroup.interactable = true;
            buttonCanvasGroup.blocksRaycasts = true;

            // コントローラー用に最初のボタンを選択状態にする
            if (firstSelectedButton != null && EventSystem.current != null)
            {
                yield return null; // 1フレーム待機して確実にフォーカス
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(firstSelectedButton);
            }
        }

        // --- 4. 一定時間ごとにタイトル1 → タイトル2の順に一瞬光るループ ---
        while (true)
        {
            // 次に光るまでの待機
            yield return new WaitForSeconds(shineInterval);

            // タイトル1が一瞬光る
            if (titleGraphic1 != null)
            {
                yield return StartCoroutine(FlashShineRoutine(titleGraphic1, originalColor1, flashDuration1));
            }

            // タイトル1とタイトル2が光る時間差
            yield return new WaitForSeconds(delayBetweenShines);

            // タイトル2が一瞬光る
            if (titleGraphic2 != null)
            {
                yield return StartCoroutine(FlashShineRoutine(titleGraphic2, originalColor2, flashDuration2));
            }
        }
    }

    /// <summary>
    /// 指定したCanvasGroupをフェードインさせる処理
    /// </summary>
    private IEnumerator FadeInGroup(CanvasGroup group, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        group.alpha = 1f;
    }

    /// <summary>
    /// 一瞬白く光って元の色に戻る処理
    /// </summary>
    private IEnumerator FlashShineRoutine(Graphic graphic, Color originalColor, float duration)
    {
        float halfDuration = duration / 2f;
        float elapsed = 0f;

        // 元の色 → 光の色へ
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            graphic.color = Color.Lerp(originalColor, shineColor, elapsed / halfDuration);
            yield return null;
        }

        elapsed = 0f;

        // 光の色 → 元の色へ
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            graphic.color = Color.Lerp(shineColor, originalColor, elapsed / halfDuration);
            yield return null;
        }

        graphic.color = originalColor;
    }
}