using System.Collections;
using System.ComponentModel.Design.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LiquidTransition : MonoBehaviour
{
    [SerializeField] private RawImage m_transitionImage;
    [SerializeField] private Image m_escapedPanel;
    [SerializeField] private float m_transitionTime = 1.5f;


    private void Awake()
    {
        if(m_transitionImage != null) m_transitionImage.gameObject.SetActive(false);
        m_escapedPanel.material.SetFloat("_Cutoff", 0.0f);
    }

    public void StartLiquidTransition()
    {
        // 遷移開始時にRawImageをアクティブに
        m_transitionImage.gameObject.SetActive(true);

        // 色を黒に指定(R,G,B,Alpha値)
        Color liquidColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        m_transitionImage.material.SetColor("_LiquidColor", liquidColor);

        StartCoroutine(TransitionCoroutine());
    }

    private IEnumerator TransitionCoroutine()
    {
        float time = 0;

        float maxCutoff = 1.2f;

        while (time < m_transitionTime)
        {
            time += Time.deltaTime;
            float progress = Mathf.Lerp(0f, maxCutoff, time / m_transitionTime);
            m_transitionImage.material.SetFloat("_Cutoff", progress);
            m_escapedPanel.material.SetFloat("_Cutoff", progress);

            yield return null;
        }
    }

}
