using System.Collections;
using Tools.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Selector : MonoBehaviour
{
    #region Properties
    [SerializeField, Candlelight.PropertyBackingField]
    private bool m_Selected;
    public bool Selected
    {
        get { return m_Selected; }
        set
        {
            m_Selected = value;
            m_TargetGraphic.gameObject.SetActive(value);
            OnChangeValue.Invoke(value);
            if(value) transform.SetAsLastSibling();
        }
    }
    public GenericEvent<bool> OnChangeValue = new GenericEvent<bool>();

    [SerializeField, Candlelight.PropertyBackingField]
    private Graphic m_TargetGraphic;
    public Graphic TargetGraphic
    {
        get { return m_TargetGraphic; }
        set { m_TargetGraphic = value; }
    }
    #endregion

    #region Private Methods
    void Start()
    {
        SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager) selectionManager.Add(this);
    }
    private void Update()
    {
#if UNITY_EDITOR
        //if (Selected && Input.GetKeyDown(KeyCode.A))
        //{
        //    StartCoroutine(c_SaveWindowScreenshot());
        //}
#endif
    }
    private void OnDestroy()
    {
        SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager) selectionManager.Remove(this);
    }
    #endregion

    #region Coroutines
    private IEnumerator c_SaveWindowScreenshot()
    {
        yield return new WaitForEndOfFrame();
        Rect rect = GetComponent<RectTransform>().ToScreenSpace();
        Texture2D sceneTexture = Texture2DExtension.ScreenRectToTexture(rect);
        string screenshotPath = @"D:/HBP/HiBoP/Docs/LaTeX/Window.png";
        ClassLoaderSaver.GenerateUniqueSavePath(ref screenshotPath);
        sceneTexture.SaveToPNG(screenshotPath);
    }
    #endregion
}
