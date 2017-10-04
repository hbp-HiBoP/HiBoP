using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using System.Linq;
using System.Collections.Generic;
using Tools.Unity.Lists;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetListItem : ActionnableItem<d.Dataset> 
	{
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_ProtocolText;
        [SerializeField] Text m_DataInfosText;
        [SerializeField] Button m_DataInfosButton;
        [SerializeField] DatasetResumeList m_DatasetResumeList;

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
                m_ProtocolText.text = value.Protocol.Name;

                int nbData = value.Data.Length;
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
                SetResumes();
            }
        }
        #endregion

        #region Public Methods
        public void SetResumes()
        {
            System.Collections.Generic.List<d.Dataset.Resume> resumes = new System.Collections.Generic.List<d.Dataset.Resume>();
            string[] names = (from data in m_Object.Data select data.Name).Distinct().ToArray();
            foreach (var name in names)
            {
                d.Dataset.Resume resume = new d.Dataset.Resume();
                resume.Label = name;
                d.DataInfo[] data = m_Object.Data.Where((d) => d.Name == name).ToArray();
                resume.Number = data.Length;
                if(data.All((d) => d.isOk))
                {
                    resume.State = d.Dataset.Resume.StateEnum.OK;
                }
                else
                {
                    resume.State = data.Any((d) => d.isOk) ? d.Dataset.Resume.StateEnum.Warning : d.Dataset.Resume.StateEnum.Error;
                }
                resumes.Add(resume);
            }
            resumes.OrderBy((r) => r.Label);
            m_DatasetResumeList.Objects = resumes.OrderBy((r) => r.Label).ToArray();
        }
        #endregion

    }
}