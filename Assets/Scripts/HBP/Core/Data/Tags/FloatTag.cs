using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Core.Data
{
    [DisplayName("Decimal")]
    public class FloatTag : BaseTag
    {
        #region Properties
        [DataMember(Name = "Clamped")] bool m_Clamped;
        public bool Clamped
        {
            get => m_Clamped;
            set
            {
                if(m_Clamped != value)
                {
                    m_Clamped = value;
                    OnNeedToRecalculateValue.Invoke();
                }
            }
        }
        [DataMember(Name = "Min")] float m_Min;
        public float Min
        {
            get => m_Min;
            set
            {
                if(m_Min != value)
                {
                    m_Min = value;
                    OnNeedToRecalculateValue.Invoke();
                }
            }

        }
        [DataMember(Name = "Max")] float m_Max;
        public float Max
        {
            get => m_Max;
            set
            {
                if(m_Max != value)
                {
                    m_Max = value;
                    OnNeedToRecalculateValue.Invoke();
                }
            }
        }

        public UnityEvent OnNeedToRecalculateValue { get; set; } = new UnityEvent();
        #endregion

        #region Constructors
        public FloatTag() : this("", false, 0, 0)
        {
        }
        public FloatTag(string name) : this(name, false, 0, 0)
        {
        }
        public FloatTag(string name, string ID) : this(name, false, 0, 0, ID)
        {
        }
        public FloatTag(string name, bool clamped, float min, float max) : base(name)
        {
            Clamped = clamped;
            Min = min;
            Max = max;
        }
        public FloatTag(string name, bool clamped, float min, float max, string ID) : base(name,ID)
        {
            Clamped = clamped;
            Min = min;
            Max = max;
        }
        #endregion

        #region Public Methods
        public float Clamp(float value)
        {
            return Clamped ? Mathf.Clamp(value, Min, Max) : value;
        }
        public float Convert(object value)
        {
            if (value != null && value is float)
            {
                return (float)value;
            }
            else
            {
                throw new Exception("Wrong value type");
            }
        }
        public override object Clone()
        {
            return new FloatTag(Name, Clamped, Min, Max, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is FloatTag floatTag)
            {
                Clamped = floatTag.Clamped;
                Min = floatTag.Min;
                Max = floatTag.Max;
            }
            if (copy is IntTag intTag)
            {
                Clamped = intTag.Clamped;
                Min = intTag.Min;
                Max = intTag.Max;
            }
        }
        #endregion
    }
}
