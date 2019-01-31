using System.Collections;
using System.IO;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class LoadingCircle : MonoBehaviour
{
    #region Properties
    [SerializeField, HideInInspector]
    float m_Progress;
    public float Progress
    {
        get
        {
            return m_Progress;
        }
        set
        {
            m_Progress = value;
        }
    }

    float m_LastProgress;
    Coroutine m_TextCoroutine;
    Coroutine m_ChangePercentageCoroutine;
    bool m_ChangePercentageCoroutineIsRunning;
    float m_EndPercentageCurrentCoroutine;

    LoadingText m_LastText;
    public LoadingText Text { get; set; }
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
    public void ChangePercentage(float progress, float seconds, LoadingText message)
    {
        if (m_ChangePercentageCoroutineIsRunning)
        {
            StopCoroutine(m_ChangePercentageCoroutine);
            Progress = m_EndPercentageCurrentCoroutine;
        }
        ChangePercentage(m_Progress, progress, seconds);
        Text = message;
    }
    public void ChangePercentage(float start, float end, float seconds)
    {
        if(!Mathf.Approximately(start,end))
        {
            m_EndPercentageCurrentCoroutine = end;
            if (seconds > 0)
            {
                m_ChangePercentageCoroutine = StartCoroutine(c_ChangePercentage(start, end, seconds));
            }
            else
            {
                Progress = end;
            }
        }

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
    IEnumerator c_ChangePercentage(float start, float end, float seconds)
    {
        m_ChangePercentageCoroutineIsRunning = true;
        float actualTime = 0;
        while(actualTime < seconds)
        {
            actualTime += Time.deltaTime;
            Progress = Mathf.Lerp(start, end, actualTime / seconds);
            yield return new WaitForEndOfFrame();
        }
        m_ChangePercentageCoroutineIsRunning = false;
    }
    IEnumerator c_Load()
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
        if (!loading && m_LoadingEffectText.gameObject.activeSelf) m_TextCoroutine = StartCoroutine(c_Load());
    }

    private void Update()
    {
        if(m_Progress != m_LastProgress)
        {
            int percentage = Mathf.Min(Mathf.FloorToInt(m_Progress * 100.0f),100);
            m_FillProgress.fillAmount = m_Progress;
            m_IconProgress.sprite = m_Sprites[percentage];
            m_LastProgress = m_Progress;
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
