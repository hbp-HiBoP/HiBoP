using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class LoadingCircle : MonoBehaviour
{
    #region Properties
    [SerializeField]
    float progress;
    public float Progress
    {
        get
        {
            return progress;
        }
        set
        {
            progress = value;
            int percentage = Mathf.FloorToInt(progress * 100.0f);
            int circles = percentage / 5;
            transform.GetChild(1).GetComponent<Image>().fillAmount = circles * 0.05f;
            string path = "BrainAnim" + System.IO.Path.DirectorySeparatorChar + percentage;
            Sprite sprite = Resources.Load<Sprite>(path) as Sprite;
            transform.GetChild(2).GetComponent<Image>().sprite = sprite;
        }
    }

    string text;
    public string Text
    {
        get
        {
            return text;
        }
        set
        {
            StopAllCoroutines();
            loading = false;
            transform.GetChild(0).GetChild(0).GetComponent<Text>().text = value;
            transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = "";
            text = value;
        }
    }
    bool loading;
    #endregion

    #region Public Methods
    public void Set(float progress,string text)
    {
        Progress = progress;
        Text = text;
    }
    public void ChangePercentage(float start, float end, float seconds)
    {
        StartCoroutine(c_ChangePercentage(start, end, seconds));
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
        float delay = 0.05f;
        int max = Mathf.CeilToInt(seconds / delay);
        float ratio = (end - start) / (max - 1);
        for (int i = 0; i < max; i++)
        {
            if (i != (max - 1))
            {
                Progress = start + i * ratio;
            }
            else
            {
                Progress = end;
            }
            yield return new WaitForSeconds(0.05f);
        }
    }
    IEnumerator c_Load()
    {
        loading = true;
        Text loadingEffect = transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
        Text information = transform.GetChild(0).GetChild(0).GetComponent<Text>();

        loadingEffect.rectTransform.localPosition = new Vector2(information.rectTransform.rect.width / 2.0f + 1, 0);
        loadingEffect.text = "";
        yield return new WaitForSeconds(0.25f);
        loadingEffect.text = ".";
        yield return new WaitForSeconds(0.25f);
        loadingEffect.text = "..";
        yield return new WaitForSeconds(0.25f);
        loadingEffect.text = "...";
        yield return new WaitForSeconds(0.25f);
        loading = false;
    }
    #endregion

    #region Private Methods
    void LateUpdate()
    {
        if (!loading && transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf) StartCoroutine(c_Load());
    }
    #endregion
}
