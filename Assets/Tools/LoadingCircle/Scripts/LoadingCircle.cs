using System.Collections;
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

    string m_LastText;
    string m_Text;
    public string Text
    {
        get
        {
            return m_Text;
        }
        set
        {
            m_Text = value;
        }
    }
    bool loading;

    [SerializeField]
    Text m_LoadingEffectText;
    [SerializeField]
    Text m_InformationText;
    #endregion

    #region Public Methods
    public void Set(float progress,string text)
    {
        Progress = progress;
        Text = text;
    }
    public void ChangePercentage(float progress, float seconds, string message)
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
    void LateUpdate()
    {
        if (!loading && m_LoadingEffectText.gameObject.activeSelf) m_TextCoroutine = StartCoroutine(c_Load());
    }

    private void Update()
    {
        if(m_Progress != m_LastProgress)
        {
            int percentage = Mathf.FloorToInt(m_Progress * 100.0f);
            transform.GetChild(1).GetComponent<Image>().fillAmount = m_Progress;
            string path = "BrainAnim" + System.IO.Path.DirectorySeparatorChar + percentage;
            Sprite sprite = Resources.Load<Sprite>(path) as Sprite;
            transform.GetChild(2).GetComponent<Image>().sprite = sprite;
            m_LastProgress = m_Progress;
        }
        if (m_Text != m_LastText)
        {
            StopCoroutine(m_TextCoroutine);
            loading = false;
            m_InformationText.text = m_Text;
            m_LoadingEffectText.text = "";
            m_LastText = m_Text;
        }
    }
    #endregion
}
