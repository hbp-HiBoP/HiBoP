using System.IO;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Tools;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HBP.UI.Tools
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

        Sprite[] m_Sprites;

        private CancellationTokenSource m_CancellationTokenSource;

        [SerializeField] Image m_IconProgress;
        [SerializeField] Image m_FillProgress;
        [SerializeField] RectTransform m_Informations;
        [SerializeField] Text m_LoadingEffectText;
        [SerializeField] Text m_PrefixText;
        [SerializeField] Text m_InformationText;
        [SerializeField] Text m_SuffixText;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            m_Sprites = new Sprite[101];
            for (int i = 0; i < 101; ++i)
            {
                string path = Path.Combine("BrainAnim", i.ToString());
                m_Sprites[i] = Resources.Load<Sprite>(path);
            }
            Close();
        }
        public void ChangePercentage(float progress, float durationInSeconds, LoadingText message)
        {
            m_LastProgress = m_TargetProgress;
            m_TargetProgress = progress;
            DurationInSeconds = durationInSeconds;
            m_CurrentDurationInSeconds = 0;
            Text = message;
        }
        public void Open()
        {
            gameObject.SetActive(true);
        }
        public void Close()
        {
            gameObject.SetActive(false);
            Reset();
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
        private async void TextLoadingEffect(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.WaitUntil(() => gameObject.activeSelf);
                m_LoadingEffectText.text = "";
                await UniTask.WaitForSeconds(0.25f);
                m_LoadingEffectText.text = ".";
                await UniTask.WaitForSeconds(0.25f);
                m_LoadingEffectText.text = "..";
                await UniTask.WaitForSeconds(0.25f);
                m_LoadingEffectText.text = "...";
                await UniTask.WaitForSeconds(0.25f);
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_CancellationTokenSource = new();
            TextLoadingEffect(m_CancellationTokenSource.Token);
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
                m_PrefixText.text = Text.Prefix;
                m_InformationText.text = Text.Message;
                m_SuffixText.text = Text.Suffix;
                m_LastText = Text;
            }
        }
        private void Reset()
        {
            m_IconProgress.sprite = m_Sprites[0];
            m_FillProgress.fillAmount = 0;
            m_Informations.gameObject.SetActive(false);
            m_Informations.anchoredPosition = Vector2.zero;
            m_Informations.sizeDelta = Vector2.zero;
        }
        private void OnDestroy()
        {
            m_CancellationTokenSource.Cancel();
        }
        #endregion
    }
}