using HBP.Data.Anatomy;
using System.Globalization;
using Tools.Unity.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Anatomy
{
    public class CoordinateItem : ActionnableItem<Coordinate>
    {
        #region Properties
        [SerializeField] Text m_ReferenceSystemText;
        [SerializeField] Text m_XText;
        [SerializeField] Text m_YText;
        [SerializeField] Text m_ZText;

        public override Coordinate Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;

                m_ReferenceSystemText.text = value.ReferenceSystem;
                m_XText.text = value.Value.x.ToString("0.##", CultureInfo.InvariantCulture);
                m_YText.text = value.Value.y.ToString("0.##", CultureInfo.InvariantCulture);
                m_ZText.text = value.Value.z.ToString("0.##", CultureInfo.InvariantCulture);
            }
        }
        #endregion
    }
}