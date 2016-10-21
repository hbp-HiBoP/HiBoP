using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Tools.Unity
{
    public class ProgressWindow : MonoBehaviour
    {
        public ProgressBar m_progressBar;
        public Text m_informations;
        public Text m_loadingEffect;
        bool m_loading;

        public void Set(float percentage, string message)
        {
            Set(percentage);
            Set(message);
        }

        public void Set(float percentage)
        {
            m_progressBar.Percentage = percentage;
        }

        public void ChangePercentageInSeconds(float start, float end, float seconds)
        {
            StartCoroutine(c_ChangePercentageInSeconds(start, end, seconds));
        }

        public float GetPercentage()
        {
            return m_progressBar.Percentage;
        }

        public void Set(string message)
        {
            m_informations.text = message;
            ClearLoadingEffect();
        }

        public void Close()
        {
            GameObject.Destroy(this.gameObject);
        }

        void Update()
        {
            if(!m_loading)
            {
                StartCoroutine(ShowLoading());
            }
        }

        IEnumerator c_ChangePercentageInSeconds(float start, float end, float seconds)
        {
            float l_delay = 0.05f;
            int max = Mathf.CeilToInt(seconds / l_delay);
            float l_ratio = (end - start) / (max-1);
            for (int i = 0; i < max; i++)
            {
                if(i != (max-1))
                {
                    Set(start + i * l_ratio);
                }
                else
                {
                    Set(end);
                }
                yield return new WaitForSeconds(0.05f);
            }
        }

        IEnumerator ShowLoading()
        {
            m_loadingEffect.rectTransform.localPosition = new Vector2(m_informations.rectTransform.rect.width / 2.0f +2, 0);
            m_loading = true;
            m_loadingEffect.text = "";
            yield return new WaitForSeconds(0.25f);
            m_loadingEffect.text = ".";
            yield return new WaitForSeconds(0.25f);
            m_loadingEffect.text = ". .";
            yield return new WaitForSeconds(0.25f);
            m_loadingEffect.text = ". . .";
            yield return new WaitForSeconds(0.25f);
            m_loading = false;
            yield return null;
        }

        void ClearLoadingEffect()
        {
            StopAllCoroutines();
            m_loading = false;
            m_loadingEffect.text = "";
        }
    }
}

