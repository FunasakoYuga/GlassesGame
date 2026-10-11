using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonManager : MonoBehaviour
{

    [Header("文字色設定")]
    [SerializeField] private Color normalColor = Color.white;     // 通常時の文字色
    [SerializeField] private Color highlightColor = Color.red;    // 選択中の文字色

    // 最後に選択されていたボタンを保持する変数
    private GameObject lastSelectedObject;

    // マウス移動検知用
    private Vector3 lastMousePosition;

    void Start()
    {
        
    }

    void Update()
    {
        if (EventSystem.current == null) return;

        // マウス操作時：カーソルが乗ったボタンを選択状態にする
        HandleMouseHover();

        // 現在選択中のUIがあれば記憶を更新
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            // 選択対象が切り替わった時に文字色を更新
            if (EventSystem.current.currentSelectedGameObject != lastSelectedObject)
            {
                // 直前まで選択されていたボタンの色を戻す
                if (lastSelectedObject != null)
                {
                    SetTextColor(lastSelectedObject, normalColor);
                }

                // 新しく選択されたボタンの文字を赤にする
                SetTextColor(EventSystem.current.currentSelectedGameObject, highlightColor);
            }

            lastSelectedObject = EventSystem.current.currentSelectedGameObject;
        }
        // 背景クリック等で選択が外れた場合、直前に選択していたボタンへ戻す
        else if (lastSelectedObject != null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedObject);
            SetTextColor(lastSelectedObject, highlightColor);
        }
    }

    // マウスカーソルが乗ったボタンを自動で選択状態にする処理
    private void HandleMouseHover()
    {
        // マウスが動いていない（パッド操作中など）は処理をスキップ
        if (Input.mousePosition == lastMousePosition) return;
        lastMousePosition = Input.mousePosition;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            Button btn = result.gameObject.GetComponentInParent<Button>();
            if (btn != null)
            {
                if (EventSystem.current.currentSelectedGameObject != btn.gameObject)
                {
                    EventSystem.current.SetSelectedGameObject(btn.gameObject);
                }
                break;
            }
        }
    }

    // ボタンの子オブジェクトにあるTextMeshProの色を変更する
    private void SetTextColor(GameObject obj, Color color)
    {
        if (obj == null) return;

        TextMeshProUGUI tmpText = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.color = color;
        }
    }
}