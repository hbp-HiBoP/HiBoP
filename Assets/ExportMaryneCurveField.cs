using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HBP.UI.Informations.Graphs;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ExportMaryneCurveField : MonoBehaviour
    {
        [SerializeField] private Text m_Title;
        [SerializeField] private InputField m_InputField;
        [SerializeField] private Text m_NumberOfChar;

        public int Index
        {
            set
            {
                m_Title.text = $"Curve {value + 1} :";
            }
        }

        private int m_BaseLength;
        public int BaseLength
        {
            get
            {
                return m_BaseLength;
            }
            set
            {
                m_BaseLength = value;
                m_NumberOfChar.text = $"{m_BaseLength + m_InputField.text.Length} chars";
            }
        }

        private Graph.Curve m_Curve;
        public Graph.Curve Curve
        {
            get
            {
                return m_Curve;
            }
            set
            {
                m_Curve = value;
                m_InputField.onValueChanged.RemoveAllListeners();
                m_InputField.onValueChanged.AddListener(OnValueChanged);
                m_InputField.text = m_Curve.ExportName;
            }
        }

        private void OnValueChanged(string value)
        {
            if (m_Curve != null)
            {
                m_Curve.ExportName = value;
            }
            m_NumberOfChar.text = $"{BaseLength + value.Length} chars";
        }
    }
}