using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Visualization;

namespace HBP.UI.Visualization
{
    public class ColumnItem : Tools.Unity.Lists.Item<Column>
    {
        #region Attributs
        #region UI Elements
        /// <summary>
        /// The experience.
        /// </summary>
        [SerializeField]
        Text m_experience;

        /// <summary>
        /// The data.
        /// </summary>
        [SerializeField]
        Text m_data;

        /// <summary>
        /// The protocol.
        /// </summary>
        [SerializeField]
        Text m_protocol;

        /// <summary>
        /// The bloc.
        /// </summary>
        [SerializeField]
        Text m_bloc;
        #endregion
        #endregion

        #region Private Methods
        protected override void SetObject(Column column)
        {
            if (column == null || column.Dataset == null || column.Dataset.Name == null)
            {
                m_experience.text = "";
            }
            else
            {
                m_experience.text = column.Dataset.Name;
            }

            if (column == null || column.Dataset == null || column.DataLabel == null)
            {
                m_data.text = "";
            }
            else
            {
                m_data.text = column.DataLabel;
            }

            if (column == null || column.Dataset == null || column.Protocol.Name == null)
            {
                m_protocol.text = "";
            }
            else
            {
                m_protocol.text = column.Protocol.Name;
            }

            if (column == null || column.Dataset == null || column.Bloc.DisplayInformations.Name == null)
            {
                m_bloc.text = "";
            }

            else
            {
                m_bloc.text = column.Bloc.DisplayInformations.Name;
            }
        }
        #endregion
    }
}