using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    public class DialogBox : MonoBehaviour
    {
        #region Properties
        [SerializeField] DialogBoxIcon[] m_Icons;
        [SerializeField] Image m_Icon;
        [SerializeField] TMPro.TMP_Text m_Message;
        [SerializeField] TMPro.TMP_Text m_Title;
        [SerializeField] Transform m_ButtonsParent;
        [SerializeField] GameObject m_ButtonPrefab;
        #endregion

        #region Public Methods
        public async UniTaskVoid Open(DialogBoxType type, string title, string message, string[] buttons)
        {
            await OpenAsync(type, title, message, buttons);
        }
        public async UniTask<int> OpenAsync(DialogBoxType type, string title, string message, string[] buttons)
        {
            m_Icon.sprite = m_Icons.FirstOrDefault(i => i.type == type).icon;
            SetRect();
            SetMessages(title, message);
            return await SetButtons(buttons);
        }
        public void Close()
        {
            Destroy(gameObject);
        }
        #endregion

        #region Private Methods
        void SetRect()
        {
            RectTransform rect = (transform as RectTransform);
            rect.offsetMin = new Vector2(0, 0);
            rect.offsetMax = new Vector2(0, 0);
        }
        void SetMessages(string title, string message)
        {
            m_Title.text = title;
            m_Message.text = message;
            LayoutElement layoutElement = m_Message.transform.parent.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = Mathf.Min(Mathf.Max(m_Title.preferredWidth, m_Message.preferredWidth), layoutElement.preferredWidth);
        }
        async UniTask<int> SetButtons(string[] buttons)
        {
            int result = -1;
            foreach (string button in buttons)
            {
                GameObject buttonObject = Instantiate(m_ButtonPrefab, m_ButtonsParent);
                buttonObject.GetComponentInChildren<Text>().text = button;
                buttonObject.GetComponent<Button>().onClick.AddListener(() =>
                {
                    result = System.Array.IndexOf(buttons, button);
                    Close();
                });
            }
            await UniTask.WaitUntil(() => result != -1);
            return result;
        }
        #endregion
    }

    [System.Serializable]
    public struct DialogBoxIcon
    {
        public DialogBoxType type;
        public Sprite icon;
    }
}

