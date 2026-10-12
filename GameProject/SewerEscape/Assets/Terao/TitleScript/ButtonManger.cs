using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement; // 現在のシーン名を取得するために追加
using TMPro;

public class ButtonManager : MonoBehaviour
{

    [Header("文字色設定")]
    [SerializeField] private Color normalColor = Color.white;     // 通常時の文字色
    [SerializeField] private Color highlightColor = Color.red;    // 選択中の文字色

    [Header("サウンド設定")]
    [SerializeField] private AudioClip clickSound;                // ボタンを押した時のSE
    [SerializeField] private bool autoAddClickSound = true;       // シーン内の全ボタンに自動でクリック音を登録するか

    // 最後に選択されていたボタンを保持する変数
    private GameObject lastSelectedObject;

    // マウス移動検知用
    private Vector3 lastMousePosition;

    void Start()
    {
        // シーン内のButtonコンポーネントすべてに自動でクリック音を付与
        if (autoAddClickSound)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            // 非アクティブ（非表示）状態のPauseパネルなどのボタンも含めてすべて取得
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Button btn in buttons)
            {
                // ManualSceneまたはAllSceneのとき、PadButtonとKeyMouButtonを除外
                if (currentSceneName == "ManualScene" || currentSceneName == "AllScene")
                {
                    string btnName = btn.gameObject.name.ToLower();
                    // pad、keymou、backが含まれるボタンはクリック音をならさない
                    if (btnName.Contains("pad") || btnName.Contains("keymou")|| btnName.Contains("back"))
                    {
                        continue;
                    }
                }
                btn.onClick.AddListener(PlayClickSound);
            }
        }
    }

    void Update()
    {
        if (EventSystem.current == null) return;

        // マウス操作時はカーソルが乗ったボタンを選択状態にする
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

    /// <summary>
    /// ボタン決定音を再生する(DontDestroy)
    /// </summary>
    public void PlayClickSound()
    {
        if (clickSound == null) return;

        // シーン遷移しても音が消えない一時オブジェクトを生成
        GameObject soundObj = new GameObject("ButtonSE_DontDestroy");
        DontDestroyOnLoad(soundObj);

        AudioSource audioSource = soundObj.AddComponent<AudioSource>();
        audioSource.clip = clickSound;
        audioSource.playOnAwake = false;
        audioSource.volume = 1.0f;
        audioSource.ignoreListenerPause = true; // ポーズ中でもSEを鳴らす設定
        audioSource.Play();

        // ポーズ中でも一時オブジェクト破棄する
        StartCoroutine(DestroyAfterPlayRealtime(soundObj, clickSound.length));
    }

    // ポーズ中でも一時オブジェクトを破棄する
    private IEnumerator DestroyAfterPlayRealtime(GameObject target, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (target != null)
        {
            Destroy(target);
        }
    }

    // マウスカーソルが乗ったボタンを自動で選択状態にする処理
    private void HandleMouseHover()
    {
        // マウスが動いていないときは処理をスキップ
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