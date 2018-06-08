using CielaSpike;
using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class AdvancedSiteConditions : BaseSiteConditions
    {
        #region Properties
        [SerializeField] InputField m_InputField;
        #endregion

        #region Public Methods
        protected override bool CheckConditions(Site site) // TODO
        {
            return false;
        }
        #endregion
    }
}