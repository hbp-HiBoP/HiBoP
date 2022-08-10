using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Display.Informations.TrialMatrix
{
    public class Group
    {
        #region Properties
        public string Name { get; set; }
        public List<TrialMatrix> TrialMatrices { get; set; }
        #endregion

        #region Constructors
        public Group(string name, IEnumerable<TrialMatrix> trialMatrices, Vector2 limits, bool autoLimits)
        {
            Name = name;
            TrialMatrices = trialMatrices.ToList();
        }
        #endregion
    }
}