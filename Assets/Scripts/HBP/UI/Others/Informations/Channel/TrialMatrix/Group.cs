using UnityEngine;

namespace HBP.UI.TrialMatrix
{
    public class Group : MonoBehaviour
    {
        #region Properties
        bool m_UsePrecalculatedLimits;
        public bool UsePrecalculatedLimits
        {
            get
            {
               return m_UsePrecalculatedLimits;
            }
            set
            {
                if(value != m_UsePrecalculatedLimits)
                {
                    m_UsePrecalculatedLimits = value;
                    foreach (var trialMatrix in TrialMatrices)
                    {
                        trialMatrix.UsePrecalculatedLimits = value;
                    }
                }
            }

        }

        Vector2 m_Limits;
        public Vector2 Limits
        {
            get
            {
                return m_Limits;
            }
            set
            {
                if (value != null && value != m_Limits)
                {
                    m_Limits = value;
                    foreach (var trialMatrix in TrialMatrices)
                    {
                        trialMatrix.Limits = m_Limits;
                    }
                }              
            }
        }
        public Display.Informations.TrialMatrix.Group Data { get; private set; }
        public TrialMatrix[] TrialMatrices { get; private set; }

        [SerializeField] GameObject m_TrialMatrixPrefab;
        #endregion

        #region Public Methods
        public void Set(Display.Informations.TrialMatrix.Group groupToDisplay)
        {
            foreach (var trialMatrix in groupToDisplay.TrialMatrices)
            {
                AddTrialMatrix(trialMatrix);
            }
        }
        #endregion

        #region Private Methods
        void AddTrialMatrix(Display.Informations.TrialMatrix.TrialMatrix data)
        {
            //TrialMatrix trialMatrix = Instantiate(m_TrialMatrixPrefab, transform).GetComponent<TrialMatrix>();
            //trialMatrix.Set(data, UsePrecalculatedLimits ? new Vector2() : Limits);

            //trialMatrix.OnChangeUsePrecalculatedLimits.AddListener((v) => UsePrecalculatedLimits = v);
            //trialMatrix.OnChangeLimits.AddListener((v) => Limits = v);
        }
        #endregion
    }
}