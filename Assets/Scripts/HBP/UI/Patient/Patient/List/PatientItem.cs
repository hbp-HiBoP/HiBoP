using HBP.Data;
using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using System.Linq;
using NewTheme.Components;
using Tools.Unity;
using System.Text;

namespace HBP.UI.Anatomy
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

        [SerializeField] Text m_ImplantationText;
        [SerializeField] Tooltip m_ImplantationTooltip;

        [SerializeField] State m_ErrorState;

        public override Patient Object
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
                stringBuilder.AppendLine("Implantations :");
                string[] implantations = (from implantation in m_Object.Implantations where implantation.WasUsable select implantation.Name).ToArray();
                for (int i = 0; i < implantations.Length; i++)
                {
                    if (i < implantations.Length - 1) stringBuilder.AppendLine("  \u2022 " + implantations[i]);
                    else stringBuilder.Append("  \u2022 " + implantations[i]);
                }
                if (implantations.Length == 0)
                {
                    m_ImplantationText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None");
                }
                else
                {
                    m_ImplantationText.GetComponent<ThemeElement>().Set();
                }
                m_ImplantationTooltip.Text = stringBuilder.ToString();
                m_ImplantationText.text = implantations.Length.ToString();
            }
        }
        #endregion
    }
}