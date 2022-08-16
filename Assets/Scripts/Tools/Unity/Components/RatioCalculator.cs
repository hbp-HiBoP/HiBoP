using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace HBP.UI.Components
{
    public class RatioCalculator : MonoBehaviour
    {
        #region Properties
        [SerializeField] float m_Value;
        public float Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_Value, value))
                {
                    SetValue();
                }
            }
        }

        [SerializeField] float m_Min;
        public float Min
        {
            get
            {
                return m_Min;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Min, value))
                {
                    SetMin();
                }
            }
        }

        [SerializeField] float m_Max;
        public float Max
        {
            get
            {
                return m_Max;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Max, value))
                {
                    SetMax();
                }
            }
        }

        [ReadOnly, SerializeField] float m_Result;
        float Result
        {
            get
            {
                return m_Result;
            }
        }

        public Vector2 Range
        {
            get
            {
                return new Vector2(m_Min, m_Max);
            }
            set
            {
                Min = value.x;
                Max = value.y;
            }
        }

        [SerializeField] FloatEvent m_OnChangeResult;
        public FloatEvent OnChangeResult
        {
            get
            {
                return m_OnChangeResult;
            }
        }
        #endregion

        #region Setters
        private void OnValidate()
        {
            SetValue();
            SetMin();
            SetMax();
        }
        void SetValue()
        {
            SetResult();
        }
        void SetMin()
        {
            m_Max = Mathf.Max(m_Min, m_Max);
            SetResult();
        }
        void SetMax()
        {
            m_Min = Mathf.Min(m_Min, m_Max);
            SetResult();
        }
        void SetResult()
        {
            float result = (m_Value - m_Min) / (m_Max - m_Min);
            if(result != m_Result)
            {
                m_Result = result;
                OnChangeResult.Invoke(m_Result);
            }
        }
        #endregion
    }
}