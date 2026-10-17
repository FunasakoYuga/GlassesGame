using System.Collections;
using System.Diagnostics.Contracts;
using System.Xml.Serialization;
using UnityEngine;

public class UIPopup : MonoBehaviour
{
    public float m_duration = 0.4f;

    public AnimationCurve m_scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 defaultScale;

    private void Awake()
    {
        // UI本来の大きさの記憶
        defaultScale = transform.localScale;
    }

    private void OnEnable()
    {
        StartCoroutine(PopupCoroutine());   
    }

    private IEnumerator PopupCoroutine()
    {
        float time = 0.0f;

        transform.localScale = Vector3.zero;

        while (time < m_duration)
        {
            time += Time.deltaTime;

            float progress = Mathf.Clamp01(time / m_duration);

            float curveValue = m_scaleCurve.Evaluate(progress);

            transform.localScale = defaultScale * curveValue;

            yield return null;
        }

        transform.localScale=defaultScale;
    }
}
