using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace HBP.UI.Tools
{
    public class StringFormatSetter : MonoBehaviour
    {
        #region Properties
        [SerializeField] string m_Arg0;
        public string Arg0
        {
            get
            {
                return m_Arg0;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Arg0, value))
                {
                    SetResult();
                }
            }
        }

        [SerializeField] string m_Arg1;
        public string Arg1
        {
            get
            {
                return m_Arg1;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Arg1, value))
                {
                    SetResult();
                }
            }
        }

        [SerializeField] string m_Arg2;
        public string Arg2
        {
            get
            {
                return m_Arg2;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Arg2, value))
                {
                    SetResult();
                }
            }
        }

        [SerializeField] string m_Arg3;
        public string Arg3
        {
            get
            {
                return m_Arg3;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Arg3, value))
                {
                    SetResult();
                }
            }
        }

        [SerializeField] string m_Arg4;
        public string Arg4
        {
            get
            {
                return m_Arg4;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Arg4, value))
                {
                    SetResult();
                }
            }
        }


        [SerializeField] string m_Format;
        public string Format
        {
            get
            {
                return m_Format;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Format, value))
                {
                    SetResult();
                }
            }
        }

        [SerializeField, ReadOnly] string m_Result;

        [SerializeField] StringEvent m_OnChangeResult;
        public StringEvent OnChangeResult
        {
            get
            {
                return m_OnChangeResult;
            }
        }
        #endregion

        #region Private Methods
        private void OnValidate()
        {
            SetResult();
        }
        void SetResult()
        {
            try
            {
                string newResult = string.Format(m_Format, m_Arg0, m_Arg1, m_Arg2, m_Arg3, m_Arg4);
                if (newResult != m_Result)
                {
                    m_Result = newResult;
                    m_OnChangeResult.Invoke(m_Result);
                }
            }
            catch
            {
                Debug.LogWarning("Wrong format or Wrong Value");
            }
        }
        #endregion
    }
}