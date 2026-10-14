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
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.3f; // 半透明時の最小アルファ値
    [SerializeField] private float blinkSpeed = 2.0f;             // 半透明を繰り返す速さ

    [Header("サウンド設定")]
    [SerializeField] private AudioClip clickSound;                // ボタンを押した時のSE
    [SerializeField] private bool autoAddClickSound = true;       // シーン内の全ボタンに自動でクリック音を登録するか

    // 最後に選択されていたボタンを保持する変数
    private GameObject lastSelectedObject;

    // マウス移動検知用
    private Vector3 lastMousePosition;

    // 点滅アニメーション用のタイマー
    private float blinkTimer = 0f;

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
                    if (btnName.Contains("pad") || btnName.Contains("keymou") || btnName.Contains("back"))
                    {
                        continue;
                    }

                    // AllSceneのとき、continue、manual、titleが含まれるボタンもクリック音を鳴らさない
                    if (currentSceneName == "AllScene" && (btnName.Contains("continue") || btnName.Contains("manual") || btnName.Contains("title")))
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
                blinkTimer = 0f; // 選択が切り替わったら不透明の状態からスタートさせる
            }

            lastSelectedObject = EventSystem.current.currentSelectedGameObject;
        }
        // 背景クリック等で選択が外れた場合、直前に選択していたボタンへ戻す
        else if (lastSelectedObject != null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedObject);
            SetTextColor(lastSelectedObject, highlightColor);
        }

        // 選択中のボタンがある間、文字の透明度（アルファ値）を周期的に変化させる
        if (lastSelectedObject != null)
        {
            // ポーズ中（Time.timeScale == 0）でも点滅するように unscaledDeltaTime を使用
            blinkTimer += Time.unscaledDeltaTime * blinkSpeed;

            Color blinkingColor = highlightColor;
            blinkingColor.a = Mathf.Lerp(highlightColor.a, minAlpha, Mathf.PingPong(blinkTimer, 1.0f));
            SetTextColor(lastSelectedObject, blinkingColor);
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