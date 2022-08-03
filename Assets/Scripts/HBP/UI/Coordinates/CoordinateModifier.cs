using UnityEngine;
using UnityEngine.UI;
using Tools.CSharp;
using System.Globalization;
using HBP.Core.Data;

namespace HBP.UI
{
    /// <summary>
    /// Window to modify a coordinate.
    /// </summary>
    public class CoordinateModifier : ObjectModifier<Core.Data.Coordinate>
    {
        #region Properties
        [SerializeField] InputField m_ReferenceSystemInputField;
        [SerializeField] InputField m_XInputField;
        [SerializeField] InputField m_YInputField;
        [SerializeField] InputField m_ZInputField;

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_ReferenceSystemInputField.interactable = value;
                m_XInputField.interactable = value;
                m_YInputField.interactable = value;
                m_ZInputField.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            m_ReferenceSystemInputField.onEndEdit.AddListener(ChangeReferenceSystem);
            m_XInputField.onValueChanged.AddListener(ChangeX);
            m_YInputField.onValueChanged.AddListener(ChangeY);
            m_ZInputField.onValueChanged.AddListener(ChangeZ);
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToModify">Coordinate to modify</param>
        protected override void SetFields(Coordinate objectToModify)
        {
            m_ReferenceSystemInputField.text = objectToModify.ReferenceSystem;
            m_XInputField.text = objectToModify.Position.x.ToString(CultureInfo.InvariantCulture);
            m_YInputField.text = objectToModify.Position.y.ToString(CultureInfo.InvariantCulture);
            m_ZInputField.text = objectToModify.Position.z.ToString(CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Change the reference system.
        /// </summary>
        /// <param name="referenceSystem">Name of the reference system</param>
        void ChangeReferenceSystem(string referenceSystem)
        {
            if(referenceSystem != "")
            {
                ObjectTemp.ReferenceSystem = referenceSystem;
            }
            else
            {
                m_ReferenceSystemInputField.text = ObjectTemp.ReferenceSystem;
            }
        }
        /// <summary>
        /// Change the x.
        /// </summary>
        /// <param name="value">Value</param>
        void ChangeX(string value)
        {
            if(NumberExtension.TryParseFloat(value, out float x))
            {
                ObjectTemp.Position = new SerializableVector3(x, ObjectTemp.Position.y, ObjectTemp.Position.z);
            }
        }
        /// <summary>
        /// Change the y.
        /// </summary>
        /// <param name="value">value</param>
        void ChangeY(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float y))
            {
                ObjectTemp.Position = new SerializableVector3(ObjectTemp.Position.x, y, ObjectTemp.Position.z);
            }
        }
        /// <summary>
        /// Change the z.
        /// </summary>
        /// <param name="value">value</param>
        void ChangeZ(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float z))
            {
                ObjectTemp.Position = new SerializableVector3(ObjectTemp.Position.x, ObjectTemp.Position.y,z);
            }
        }
        #endregion
    }
}