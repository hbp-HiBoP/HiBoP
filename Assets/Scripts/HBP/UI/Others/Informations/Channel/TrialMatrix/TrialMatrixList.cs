using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;
using System;
using data = HBP.Display.Informations;

namespace HBP.UI.Module3D.Informations.TrialMatrix
{
    public class TrialMatrixList : MonoBehaviour
    {
        #region Properties
        public data.TrialMatrix.Group[] TrialMatrixGroups { get; private set; }
        public GenericEvent<Vector2, data.Data> OnLimitsChanged { get; set; }
        public GenericEvent<bool, data.Data> OnAutoLimitsChanged { get; set; }


        #endregion

        #region Public Methods
        public void Set(IEnumerable<data.TrialMatrix.Group> trialMatrixGroups)
        {
            Clear();
            TrialMatrixGroups = trialMatrixGroups.ToArray();
            foreach (var group in TrialMatrixGroups) AddGroup(group);
        }
        #endregion

        #region Private Methods
        void AddGroup(data.TrialMatrix.Group group)
        {
            GameObject gameObject = new GameObject(group.Name, new Type[] { typeof(RectTransform), typeof(HorizontalLayoutGroup) });
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(transform);
        }
        void Clear()
        {
            int numberOfChildren = transform.childCount;
            for (int c = 0; c < numberOfChildren; c++)
            {
                Destroy(transform.GetChild(c).gameObject);
            }
            TrialMatrixGroups = new data.TrialMatrix.Group[0];
        }
        #endregion
    }
}