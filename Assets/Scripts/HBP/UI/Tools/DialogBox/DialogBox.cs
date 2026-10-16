using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using System.Linq;
using System.Threading;
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
        private UniTaskCompletionSource<int> completion;

        #endregion

        #region Public Methods

        public async UniTaskVoid Open(DialogBoxType type, string title, string message, string[] buttons)
        {
            await OpenAsync(type, title, message, buttons);
        }

        public async UniTask<int> OpenAsync(DialogBoxType type, string title, string message, string[] buttons, CancellationToken token = default)
        {
            completion = new UniTaskCompletionSource<int>();
            try
            {
                token.ThrowIfCancellationRequested();
                SetIcon(type);
                SetRect();
                SetMessages(title, message);
                SetButtons(buttons.Length == 0 ? new[] { "OK" } : buttons);
                ButtonsCreated();
                return await completion.Task.AttachExternalCancellation(token);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                if (this) Close();
            }
        }

        public void Close()
        {
            completion?.TrySetResult(-1);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        protected virtual void OnDisable() => completion?.TrySetResult(-1);

        protected virtual void OnDestroy() => completion?.TrySetResult(-1);

        #endregion

        #region Private Methods

        protected virtual void SetIcon(DialogBoxType type)
        {
            if (m_Icon != null) m_Icon.sprite = m_Icons.FirstOrDefault(i => i.type == type).icon;
        }

        protected virtual void SetRect()
        {
            RectTransform rect = (transform as RectTransform);
            rect.offsetMin = new Vector2(0, 0);
            rect.offsetMax = new Vector2(0, 0);
        }

        protected virtual void SetMessages(string title, string message)
        {
            m_Title.text = title;
            m_Message.text = message;
            LayoutElement layoutElement = m_Message.transform.parent.GetComponent<LayoutElement>();
            if (layoutElement != null)
                layoutElement.preferredWidth = Mathf.Min(Mathf.Max(m_Title.preferredWidth, m_Message.preferredWidth), layoutElement.preferredWidth);
        }

        protected virtual void ButtonsCreated()
        {
        }

        void SetButtons(string[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                GameObject buttonObject = Instantiate(m_ButtonPrefab, m_ButtonsParent);
                buttonObject.GetComponentInChildren<Text>().text = buttons[i];
                buttonObject.GetComponent<Button>().onClick.AddListener(() => completion.TrySetResult(index));
            }
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
