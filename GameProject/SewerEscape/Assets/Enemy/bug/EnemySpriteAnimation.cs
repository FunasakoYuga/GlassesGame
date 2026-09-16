using UnityEngine;
using UnityEngine.AI; // NavMeshAgentを使用するために追加

public class EnemySpriteAnimation : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private int fps = 8;

    private int oldFps;
    private int currentFrame;
    private float secondsPerFrame;
    private float timer;

    private SpriteRenderer spriteRenderer;
    private NavMeshAgent agent;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        agent = GetComponent<NavMeshAgent>(); // NavMeshAgentを取得

        ApplyFrameRate();
    }

    private void Update()
    {
        ApplyFrameRate();

        // 敵が実際に移動しているかチェック（残り距離があり、速度が出ているか）
        bool isMoving = agent.hasPath && agent.velocity.sqrMagnitude > 0.1f;

        if (isMoving)
        {
            // 移動中のみタイマーを進めてアニメーションを更新
            timer -= Time.deltaTime;
            UpdateAnimation();
        }
        else
        {
            // 停止中は1コマ目（立ちポーズ）に戻す
            ResetToIdle();
        }
    }

    private void ApplyFrameRate()
    {
        if (oldFps == fps) return;
        fps = Mathf.Max(1, fps);
        oldFps = fps;

        secondsPerFrame = 1f / fps;
        timer = secondsPerFrame;
        currentFrame = 0;
    }

    private void UpdateAnimation()
    {
        if (timer > 0) return;

        timer += secondsPerFrame;

        if (sprites != null && sprites.Length > 0)
        {
            spriteRenderer.sprite = sprites[currentFrame];
            currentFrame = (currentFrame + 1) % sprites.Length;
        }
    }

    private void ResetToIdle()
    {
        currentFrame = 0;
        timer = secondsPerFrame;
        if (sprites != null && sprites.Length > 0)
        {
            spriteRenderer.sprite = sprites[0];
        }
    }
}