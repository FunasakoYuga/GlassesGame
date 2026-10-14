using UnityEngine;

// 危険度の段階（3段階＋安全）
public enum DangerLevel
{
    Safe = 0,   // 範囲外
    Stage1 = 1, // 第1段階：遠距離（気配を感じる）
    Stage2 = 2, // 第2段階：中距離（警戒圏内）
    Stage3 = 3  // 第3段階：近距離（襲撃圏内）
}

public class HeartbeatController : MonoBehaviour
{
    [Header("オーディオ設定")]
    [SerializeField] private AudioSource heartbeatAudio;

    [Header("各段階の再生速度（Pitch）")]
    [SerializeField] private float NearPitch = 1.6f;   // 第3段階（最速）
    [SerializeField] private float MiddlePitch = 1.2f; // 第2段階（中速）
    [SerializeField] private float FarPitch = 0.9f;    // 第1段階（低速）

    // このフレームで敵から通知された「最大の危険度」を記録する変数
    private DangerLevel maxDangerThisFrame = DangerLevel.Safe;

    // 敵側から毎フレーム呼び出してもらう通知用メソッド
    public void NotifyDanger(DangerLevel level)
    {
        // 複数の敵から通知が来ても、一番危険度が高いものを優先して残す
        if (level > maxDangerThisFrame)
        {
            maxDangerThisFrame = level;
        }
    }

    // ★追加：リスポーン時などに心臓音を通常状態（安全状態）へ即座にリセットするメソッド
    public void ResetHeartbeat()
    {
        maxDangerThisFrame = DangerLevel.Safe;

        if (heartbeatAudio != null)
        {
            if (heartbeatAudio.isPlaying)
            {
                heartbeatAudio.Stop();
            }
            heartbeatAudio.pitch = 1.0f; // ピッチを標準（1.0）に戻す
        }
    }

    // ★追加：プレイヤーが非アクティブ化→再アクティブ化でリスポーンする場合にも自動でリセットする
    void OnDisable()
    {
        ResetHeartbeat();
    }

    // すべての敵のUpdate（通知）が終わった後に実行される
    void LateUpdate()
    {
        UpdateHeartbeatSound(maxDangerThisFrame);

        // 次のフレームのために危険度をリセット
        // （これにより、敵が離れたり消滅したりした瞬間に自動でSafeへ戻る）
        maxDangerThisFrame = DangerLevel.Safe;
    }

    private void UpdateHeartbeatSound(DangerLevel currentLevel)
    {
        // 範囲外なら音を止める
        if (currentLevel == DangerLevel.Safe)
        {
            if (heartbeatAudio.isPlaying)
            {
                heartbeatAudio.Stop();
            }
            return;
        }

        // 範囲内に入ったら再生開始
        if (!heartbeatAudio.isPlaying)
        {
            heartbeatAudio.Play();
        }

        // 通知された段階に応じて速度（Pitch）を変更
        switch (currentLevel)
        {
            case DangerLevel.Stage3:
                heartbeatAudio.pitch = NearPitch;
                break;
            case DangerLevel.Stage2:
                heartbeatAudio.pitch = MiddlePitch;
                break;
            case DangerLevel.Stage1:
                heartbeatAudio.pitch = FarPitch;
                break;
        }
    }
}