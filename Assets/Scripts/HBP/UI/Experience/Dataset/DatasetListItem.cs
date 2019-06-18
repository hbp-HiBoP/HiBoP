using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using System.Linq;
using Tools.Unity.Lists;
using NewTheme.Components;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetListItem : ActionnableItem<d.Dataset> 
	{
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_ProtocolText;
        [SerializeField] Text m_DataInfosText;
        //[SerializeField] DatasetResumeList m_DatasetList;
        [SerializeField] State m_ErrorState;

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

                int nbData = value.Data.Count((d) => d.IsOk);
                m_DataInfosText.text = nbData.ToString();
                if (nbData == 0) m_DataInfosText.GetComponent<ThemeElement>().Set(m_ErrorState);
                else m_DataInfosText.GetComponent<ThemeElement>().Set();
                //SetData();
            }
        }
        #endregion

        #region Public Methods
        public void SetData()
        {
            //m_DatasetList.Initialize();
            //System.Collections.Generic.List<d.Dataset.Resume> resumes = new System.Collections.Generic.List<d.Dataset.Resume>();
            //string[] names = (from data in m_Object.Data select data.Name).Distinct().ToArray();
            //foreach (var name in names)
            //{
            //    d.Dataset.Resume resume = new d.Dataset.Resume();
            //    resume.Label = name;
            //    d.DataInfo[] data = m_Object.Data.Where((d) => d.Name == name).ToArray();
            //    resume.Number = data.Length;
            //    if(data.All((d) => d.isOk))
            //    {
            //        resume.State = d.Dataset.Resume.StateEnum.OK;
            //    }
            //    else
            //    {
            //        resume.State = data.Any((d) => d.isOk) ? d.Dataset.Resume.StateEnum.Warning : d.Dataset.Resume.StateEnum.Error;
            //    }
            //    resumes.Add(resume);
            //}
            //resumes.OrderBy((r) => r.Label);
            //m_DatasetList.Objects = resumes.OrderBy((r) => r.Label).ToArray();
        }
        #endregion

    }
}