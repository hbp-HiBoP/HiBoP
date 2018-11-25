using HBP.Data.TrialMatrix.Grid;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.TrialMatrix.Grid
{
    public class Bloc : MonoBehaviour
    {
        #region Properties
        string m_title;
        public string Title
        {
            get { return m_title; }
            set
            {
                m_title = value;
                OnChangeTitle.Invoke(value);
            }
        }
        public StringEvent OnChangeTitle;

        bool m_UsePrecalculatedLimits;
        public bool UsePrecalculatedLimits
        {
            get
            {
                return m_UsePrecalculatedLimits;
            }
            set
            {
                if (value != m_UsePrecalculatedLimits)
                {
                    m_UsePrecalculatedLimits = value;
                    //foreach (var trialMatrix in TrialMatrixByChannel.Values)
                    //{
                    //    trialMatrix.UsePrecalculatedLimits = value;
                    //}
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
                    //foreach (var trialMatrix in TrialMatrixByChannel.Values)
                    //{
                    //    trialMatrix.Limits = m_Limits;
                    //}
                }
            }
        }

        Dictionary<ChannelStruct, TrialMatrix> TrialMatrixByChannel { get; set; }
        //[SerializeField] GameObject m_TrialMatrixPrefab;
        //[SerializeField] GameObject m_FillerPrefab;
        #endregion

        #region Public Methods
        public void Set(HBP.Data.Experience.Protocol.Bloc bloc)
        {
            Title = bloc.Name;
        }
        #endregion

        //public void AddTrialMatrix(TrialMatrixGrid.ChannelStruct channel, TrialMatrixGrid.DataStruct data, Texture2D colormap)
        //{
        //    GameObject gameObject = null;
        //    DataInfo dataInfo = data.Dataset.Data.FirstOrDefault(d => d.Patient == channel.Patient && d.Name == data.Data);
        //    if (dataInfo != null)
        //    {
        //        HBP.Data.TrialMatrix.TrialMatrix trialMatrixData = new HBP.Data.TrialMatrix.TrialMatrix(dataInfo, channel.Channel, colormap);
        //        gameObject = Instantiate(m_TrialMatrixPrefab, transform);
        //        TrialMatrix trialMatrix = gameObject.GetComponent<TrialMatrix>();
        //        trialMatrix.Set(trialMatrixData, UsePrecalculatedLimits ? new Vector2() : Limits);
        //        trialMatrix.OnChangeUsePrecalculatedLimits.AddListener((v) => UsePrecalculatedLimits = v);
        //        trialMatrix.OnChangeLimits.AddListener((v) => Limits = v);
        //        TrialMatrixByChannel.Add(channel, trialMatrix);
        //    }
        //    else
        //    {
        //        gameObject = Instantiate(m_FillerPrefab, transform);
        //    }
        //    gameObject.name = channel.Channel + " (" + channel.Patient.Name + ") " + data.Dataset + " " + data.Data;
        //}
    }
}

