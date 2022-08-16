using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Tools;

namespace HBP.UI
{
    [ExecuteInEditMode]
    public class LoadingCircle : MonoBehaviour
    {
        #region Properties
        float m_TargetProgress;
        float m_LastProgress;
        public float Progress { get; set; }

        LoadingText m_LastText;
        public LoadingText Text { get; set; }

        float m_CurrentDurationInSeconds;
        public float DurationInSeconds { get; set; }

        Coroutine m_TextCoroutine;


        bool loading;
        Sprite[] m_Sprites;

        [SerializeField] Image m_IconProgress;
        [SerializeField] Image m_FillProgress;
        [SerializeField] Text m_LoadingEffectText;
        [SerializeField] Text m_PrefixText;
        [SerializeField] Text m_InformationText;
        [SerializeField] Text m_SuffixText;
        #endregion

        #region Public Methods
        public void ChangePercentage(float progress, float durationInSeconds, LoadingText message)
        {
            m_LastProgress = m_TargetProgress;
            m_TargetProgress = progress;
            DurationInSeconds = durationInSeconds;
            m_CurrentDurationInSeconds = 0;
            Text = message;
        }
        public void Close()
        {
            Destroy(gameObject);
        }
        public void ShowInformations()
        {
            Animator animator = transform.GetComponent<Animator>();
            animator.Play("ShowInformations");
        }
        public void HideInformations()
        {
            Animator animator = transform.GetComponent<Animator>();
            animator.Play("HideInformations");
        }
        #endregion

        #region Coroutines
        IEnumerator c_TextLoadingEffect()
        {
            loading = true;
            m_LoadingEffectText.text = "";
            yield return new WaitForSeconds(0.25f);
            m_LoadingEffectText.text = ".";
            yield return new WaitForSeconds(0.25f);
            m_LoadingEffectText.text = "..";
            yield return new WaitForSeconds(0.25f);
            m_LoadingEffectText.text = "...";
            yield return new WaitForSeconds(0.25f);
            loading = false;
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Sprites = new Sprite[101];
            for (int i = 0; i < 101; ++i)
            {
                string path = Path.Combine("BrainAnim", i.ToString());
                m_Sprites[i] = Resources.Load<Sprite>(path) as Sprite;
            }
        }
        void LateUpdate()
        {
            if (!loading && m_LoadingEffectText.gameObject.activeSelf) m_TextCoroutine = StartCoroutine(c_TextLoadingEffect());
        }

        private void Update()
        {
            if (!Mathf.Approximately(Progress, m_TargetProgress))
            {
                float t = Mathf.Approximately(DurationInSeconds, 0) ? 1 : m_CurrentDurationInSeconds / DurationInSeconds;
                Progress = Mathf.Lerp(m_LastProgress, m_TargetProgress, t);
                int percentage = Mathf.Min(Mathf.FloorToInt(Progress * 100.0f), 100);
                m_FillProgress.fillAmount = Progress;
                m_IconProgress.sprite = m_Sprites[percentage];
                m_CurrentDurationInSeconds += Time.deltaTime;
            }
            if (Text != m_LastText)
            {
                StopCoroutine(m_TextCoroutine);
                loading = false;
                m_PrefixText.text = Text.Prefix;
                m_PrefixText.SetLayoutElementMinimumWidthToContainWholeText();
                m_InformationText.text = Text.Message;
                m_SuffixText.text = Text.Suffix;
                m_SuffixText.SetLayoutElementMinimumWidthToContainWholeText();
                m_LastText = Text;
            }
        }
        #endregion
    }
}