using UnityEngine;

public class ButtonBlink : MonoBehaviour
{
    public float m_speed = 10.0f;
    public float minAlpha = 0.0f;
    public float maxAlpha = 1.0f;
    public CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        float wave = Mathf.Sin(Time.time * m_speed);
        float normalizedWave = (wave + 1.0f) / 2.0f;

        canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, normalizedWave);
    }
}
