using UnityEngine;
using UnityEngine.UI;

public class UIFlicker : MonoBehaviour
{
    public float m_speed = 15f;

    public float m_minAlpha = 0.1f;

    public float m_maxAlpha = 1.0f;

    private Image m_targetImage;
    private float m_randomOffset;

    private void Awake()
    {
        m_targetImage = GetComponent<Image>();

        m_randomOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * m_speed + m_randomOffset, 0f);

        noise = noise * noise;

        Color currentColor = m_targetImage.color;

        currentColor.a = Mathf.Lerp(m_minAlpha, m_maxAlpha, noise);

        m_targetImage.color = currentColor;
    }
}
