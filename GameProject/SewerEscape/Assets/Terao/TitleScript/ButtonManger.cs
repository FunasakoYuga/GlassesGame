using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonManager : MonoBehaviour
{
    [Header("最初に選択状態にするボタン")]
    [SerializeField] private GameObject firstSelectedButton;

    // 直前に選択していたUI要素を記憶する変数
    private GameObject lastSelectedObject;

    void Start()
    {
        // ゲームパッド操作用に、指定したボタンを初期選択状態にする
        if (firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
            lastSelectedObject = firstSelectedButton;
        }
    }

    void Update()
    {
        if (EventSystem.current == null) return;

        // 現在選択中のUIがあれば記憶を更新
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            lastSelectedObject = EventSystem.current.currentSelectedGameObject;
        }
        // 背景クリック等で選択が外れた場合、直前に選択していたボタンへ戻す
        else if (lastSelectedObject != null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedObject);
        }
    }
}