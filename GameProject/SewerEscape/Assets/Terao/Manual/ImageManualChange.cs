using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems; // UIの選択状態を制御するために追加

public class ImageManualChange : MonoBehaviour
{
    [Header("操作説明のUIオブジェクト")]
    [SerializeField] private GameObject keyMouManual;
    [SerializeField] private GameObject padManual;

    [Header("切り替え用ボタンのオブジェクト")]
    [SerializeField] private GameObject padButton;      // キーボード表示中に押す「PAD」ボタン
    [SerializeField] private GameObject keyMouButton;   // パッド表示中に押す「KEY/MOUSE」ボタン

    private void Start()
    {
        // 開始時の初期状態を設定（キーボード・マウスを表示、パッドを非表示）
        ShowKeyMouManual();
    }

    /// <summary>
    /// ボタンを押すたびに交互に表示を切り替える
    /// </summary>
    public void ToggleManual()
    {
        if (keyMouManual == null || padManual == null) return;

        // 現在のキーボード表示状態を反転させて切り替え
        bool isKeyMouActive = keyMouManual.activeSelf;
        keyMouManual.SetActive(!isKeyMouActive);
        padManual.SetActive(isKeyMouActive);

        // ボタンの表示状態も連動して切り替え
        if (padButton != null) padButton.SetActive(!isKeyMouActive);
        if (keyMouButton != null) keyMouButton.SetActive(isKeyMouActive);

        // コントローラー用に選択フォーカスを切り替えたボタンへ移動
        SetSelected(!isKeyMouActive ? padButton : keyMouButton);
    }

    /// <summary>
    /// キーボード・マウス操作説明を表示
    /// </summary>
    public void ShowKeyMouManual()
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
        if (keyMouManual != null) keyMouManual.SetActive(false);
        if (padManual != null) padManual.SetActive(true);

        // PADボタンを非表示にし、KeyMouseボタンを表示
        if (padButton != null) padButton.SetActive(false);
        if (keyMouButton != null) keyMouButton.SetActive(true);

        // コントローラー用にKeyMouseボタンを選択
        SetSelected(keyMouButton);
    }

    /// <summary>
    /// 指定したボタンをコントローラーの選択状態にする
    /// </summary>
    private void SetSelected(GameObject target)
    {
        if (target != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(SetSelectedCoroutine(target));
        }
    }

    private IEnumerator SetSelectedCoroutine(GameObject target)
    {
        yield return null; // 1フレーム待機
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}