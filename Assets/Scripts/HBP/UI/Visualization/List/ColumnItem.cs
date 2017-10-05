using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Visualization;
namespace HBP.UI.Visualization
{
    public class ColumnItem : Tools.Unity.Lists.Item<Column>
    {
        #region Properties
        /// <summary>
        /// The experience.
        /// </summary>
        [SerializeField] Text m_Experience;
        /// <summary>
        /// The data.
        /// </summary>
        [SerializeField] Text m_Data;
        /// <summary>
        /// The protocol.
        /// </summary>
        [SerializeField] Text m_Protocol;
        /// <summary>
        /// The bloc.
        /// </summary>
        [SerializeField] Text m_Bloc;
        public override Column Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                if (value == null || value.Dataset == null || value.Dataset.Name == null)
                {
                    m_Experience.text = "";
                }
                else
                {
                    m_Experience.text = value.Dataset.Name;
                }

                if (value == null || value.Dataset == null || value.Data == null)
                {
                    m_Data.text = "";
                }
                else
                {
                    m_Data.text = value.Data;
                }

                if (value == null || value.Dataset == null || value.Protocol.Name == null)
                {
                    m_Protocol.text = "";
                }
                else
                {
                    m_Protocol.text = value.Protocol.Name;
                }

                if (value == null || value.Dataset == null || value.Bloc.DisplayInformations.Name == null)
                {
                    m_Bloc.text = "";
                }

                else
                {
                    m_Bloc.text = value.Bloc.DisplayInformations.Name;
                }
            }
        }
        #endregion
    }
}