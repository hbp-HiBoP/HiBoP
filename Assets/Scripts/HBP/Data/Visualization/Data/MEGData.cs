using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;

namespace HBP.Core.Data.Processed
{
    public class MEGItem
    {
        #region Properties
        public string Label { get; set; }
        public Patient Patient { get; set; }
        public Object3D.FMRI FMRI { get; set; } = new Object3D.FMRI();
        public Dictionary<string, float[]> ValuesByChannel { get; set; } = new Dictionary<string, float[]>();
        public Dictionary<string, string> UnitByChannel { get; set; } = new Dictionary<string, string>();
        public Tools.Frequency Frequency { get; set; } = new Tools.Frequency(0);
        public Window Window
        {
            get
            {
                if (ValuesByChannel.Count > 0)
                {
                    return new Window(0, Frequency.ConvertNumberOfSamplesToRoundedMilliseconds(ValuesByChannel.Values.Select(v => v.Length).Max()));
                }
                return new Window(0, 1);
            }
        }
        #endregion

        #region Public Methods
        public Window GetChannelWindow(string channel)
        {
            if (ValuesByChannel.TryGetValue(channel, out float[] values))
            {
                return new Window(0, Frequency.ConvertNumberOfSamplesToRoundedMilliseconds(values.Length));
            }
            return new Window(0, 0);
        }
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
                        existingItem.FMRI = new Core.Object3D.FMRI(data.FMRI);
                    }
                    else
                    {
                        MEGItem newItem = new MEGItem()
                        {
                            Label = vDataInfo.Name,
                            Patient = vDataInfo.Patient,
                            FMRI = new Core.Object3D.FMRI(data.FMRI)
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

