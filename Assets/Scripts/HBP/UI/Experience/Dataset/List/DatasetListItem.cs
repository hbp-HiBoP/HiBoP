using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using System.Linq;
using Tools.Unity.Lists;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetListItem : ActionnableItem<d.Dataset> 
	{
        #region Properties
        [SerializeField] Text m_NameText;

        [SerializeField] Text m_DataInfosText;
        [SerializeField] Button m_DataInfosButton;
        [SerializeField] LabelList m_DataInfosList;

        public override d.Dataset Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;

                m_NameText.text = value.Name;

                int nbData = value.Data.Count;
                m_DataInfosText.text = nbData.ToString();
                if(nbData == 0)
                {
                    m_DataInfosText.color = ApplicationState.Theme.General.Error;
                    m_DataInfosButton.interactable = false;
                }
                else
                {
                    m_DataInfosText.color = ApplicationState.Theme.Window.Content.Text.Color;
                    m_DataInfosButton.interactable = true;
                }
            }
        }
        #endregion

        #region Public Methods
        public void SetDataInfos()
        {
            m_DataInfosList.Objects = (from data in m_Object.Data select data.Name).ToArray();
        }
        #endregion

    }
}