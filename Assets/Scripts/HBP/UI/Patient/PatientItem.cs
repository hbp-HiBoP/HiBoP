using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using System.Linq;
using NewTheme.Components;
using Tools.Unity;
using System.Text;

namespace HBP.UI
{
    public class PatientItem : ActionnableItem<Data.Patient>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_PlaceText;
        [SerializeField] Text m_DateText;

        [SerializeField] Text m_MeshText;
        [SerializeField] Tooltip m_MeshTooltip;

        [SerializeField] Text m_MRIText;
        [SerializeField] Tooltip m_MRITooltip;

        [SerializeField] Text m_SitesText;
        [SerializeField] Tooltip m_SitesTooltip;

        [SerializeField] Text m_TagsText;
        [SerializeField] Tooltip m_TagsTooltip;

        [SerializeField] State m_ErrorState;

        public override Data.Patient Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
                m_PlaceText.text = value.Place;
                m_DateText.text = value.Date.ToString();

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Meshes :");
                string[] meshes = (from mesh in m_Object.Meshes where mesh.WasUsable select mesh.Name).ToArray();
                for (int i = 0; i < meshes.Length; i++)
                {
                    if (i < meshes.Length - 1) stringBuilder.AppendLine("  \u2022 " + meshes[i]);
                    else stringBuilder.Append("  \u2022 " + meshes[i]);
                }
                if (meshes.Length == 0)
                {
                    m_MeshText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None");
                }
                else
                {
                    m_MeshText.GetComponent<ThemeElement>().Set();
                }
                m_MeshTooltip.Text = stringBuilder.ToString();
                m_MeshText.text = meshes.Length.ToString();

                stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("MRIs :");
                string[] MRIs = (from MRI in m_Object.MRIs where MRI.WasUsable select MRI.Name).ToArray();
                for (int i = 0; i < MRIs.Length; i++)
                {
                    if (i < MRIs.Length - 1) stringBuilder.AppendLine("  \u2022 " + MRIs[i]);
                    else stringBuilder.Append("  \u2022 " + MRIs[i]);
                }
                if (MRIs.Length == 0)
                {
                    m_MRIText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None");
                }
                else
                {
                    m_MRIText.GetComponent<ThemeElement>().Set();
                }
                m_MRITooltip.Text = stringBuilder.ToString();
                m_MRIText.text = MRIs.Length.ToString();

                stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Sites :");
                string[] sites = m_Object.Sites.Select(s => s.Name).ToArray();
                for (int i = 0; i < sites.Length; i++)
                {
                    if (i < sites.Length - 1) stringBuilder.AppendLine("  \u2022 " + sites[i]);
                    else stringBuilder.Append("  \u2022 " + sites[i]);
                }
                if (sites.Length == 0)
                {
                    m_SitesText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None");
                }
                else
                {
                    m_SitesText.GetComponent<ThemeElement>().Set();
                }
                m_SitesTooltip.Text = stringBuilder.ToString();
                m_SitesText.text = sites.Length.ToString();

                stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Tags :");
                string[] tags = m_Object.Tags.Select(s => s.Tag.Name).ToArray();
                for (int i = 0; i < tags.Length; i++)
                {
                    if (i < tags.Length - 1) stringBuilder.AppendLine("  \u2022 " + tags[i]);
                    else stringBuilder.Append("  \u2022 " + tags[i]);
                }
                if (tags.Length == 0)
                {
                    m_TagsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None");
                }
                else
                {
                    m_TagsText.GetComponent<ThemeElement>().Set();
                }
                m_TagsTooltip.Text = stringBuilder.ToString();
                m_TagsText.text = tags.Length.ToString();
            }
        }
        #endregion
    }
}