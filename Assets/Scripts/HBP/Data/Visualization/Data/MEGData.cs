﻿using HBP.Data.Experience.Dataset;
using HBP.Module3D;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Tools.CSharp;

namespace HBP.Data.Visualization
{
    public class MEGItem
    {
        #region Properties
        public string Label { get; set; }
        public Patient Patient { get; set; }
        public Module3D.FMRI FMRI { get; set; }
        public Dictionary<string, float[]> ValuesByChannel { get; set; }
        public Dictionary<string, string> UnitByChannel { get; set; }
        public Tools.CSharp.EEG.Frequency Frequency { get; set; }
        #endregion
    }
    public class MEGData
    {
        #region Properties
        public List<MEGItem> MEGItems { get; set; } = new List<MEGItem>();
        #endregion

        #region Public Methods
        public void Load(IEnumerable<PatientDataInfo> columnData)
        {
            foreach (PatientDataInfo dataInfo in columnData)
            {
                if (dataInfo is MEGvDataInfo vDataInfo)
                {
                    MEGvData data = DataManager.GetData(vDataInfo) as MEGvData;
                    MEGItem existingItem = MEGItems.Find(i => i.Patient == vDataInfo.Patient && i.Label == vDataInfo.Name);
                    if (existingItem != null)
                    {
                        existingItem.FMRI = new Module3D.FMRI(data.FMRI);
                    }
                    else
                    {
                        MEGItem newItem = new MEGItem()
                        {
                            Label = vDataInfo.Name,
                            Patient = vDataInfo.Patient,
                            FMRI = new Module3D.FMRI(data.FMRI)
                        };
                        MEGItems.Add(newItem);
                    }
                }
                else if (dataInfo is MEGcDataInfo cDataInfo)
                {
                    MEGcData data = DataManager.GetData(cDataInfo) as MEGcData;
                    MEGItem existingItem = MEGItems.Find(i => i.Patient == cDataInfo.Patient && i.Label == cDataInfo.Name);
                    if (existingItem != null)
                    {
                        existingItem.ValuesByChannel = data.ValuesByChannel;
                        existingItem.UnitByChannel = data.UnitByChannel;
                        existingItem.Frequency = data.Frequency;
                    }
                    else
                    {
                        MEGItem newItem = new MEGItem()
                        {
                            Label = cDataInfo.Name,
                            Patient = cDataInfo.Patient,
                            ValuesByChannel = data.ValuesByChannel,
                            UnitByChannel = data.UnitByChannel,
                            Frequency = data.Frequency
                        };
                        MEGItems.Add(newItem);
                    }
                }
            }
        }
        public void Unload()
        {
            foreach (var fmri in MEGItems)
            {
                fmri.FMRI.Clean();
            }
            MEGItems.Clear();
        }
        #endregion
    }
}

