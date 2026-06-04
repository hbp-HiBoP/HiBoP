using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Data.BIDS
{
    [JsonObject(MemberSerialization.OptOut), Preserve]
    public class DatasetDescriptionFile
    {
        public string Name = "BIDS Dataset";
        public string BIDSVersion = "1.10.1";
        public string DatasetType = "derivative";
        public GeneratedByField[] GeneratedBy = new GeneratedByField[1] { new() };

        public DatasetDescriptionFile() { }
        public DatasetDescriptionFile(string name)
        {
            Name = name;
        }
    }

    [JsonObject(MemberSerialization.OptOut), Preserve]
    public class GeneratedByField
    {
        public string Name = Application.productName;
        public string Version = Application.version;
        public string CodeURL = "https://github.com/hbp-HiBoP/HiBoP";
    }

    [JsonObject(MemberSerialization.OptOut), Preserve]
    public class CoordSystemFile
    {
        public string iEEGCoordinateSystem = "MNI152Lin";
        public string iEEGCoordinateUnits = "mm";

        public CoordSystemFile() { }
        public CoordSystemFile(string coordinateSystem)
        {
            iEEGCoordinateSystem = coordinateSystem;
        }
    }

    [JsonObject(MemberSerialization.OptOut), Preserve]
    public class TaskFile
    {
        public string iEEGReference = "intracranial";
        public int SamplingFrequency = 0;
        public int PowerLineFrequency = 50;
        public string SoftwareFilters = "n/a";
        public string HardwareFilters = "n/a";
        public string ElectrodeManufacturer = "DIXI";
        public string ElectrodeManufacturersModelName = "Microdeep";
        public int ECOGChannelCount = 0;
        public int SEEGChannelCount = 0;
        public int EEGChannelCount = 0;
        public int EOGChannelCount = 0;
        public int ECGChannelCount = 0;
        public int EMGChannelCount = 0;
        public int MiscChannelCount = 0;
        public int TriggerChannelCount = 0;
        public float RecordingDuration = 0f;
        public string RecordingType = "continuous";
        public string iEEGGround = "G2";
        public string iEEGPlacementScheme = "n/a";
        public string iEEGElectrodeGroups = "n/a";
        public string SubjectArtefactDescription = "n/a";
        public string Manufacturer = "Micromed";
        public string ManufacturersModelName = "n/a";
        public string SoftwareVersions = "n/a";
        public string DeviceSerialNumber = "n/a";
        public string TaskName = "n/a";
        public string TaskDescription = "n/a";
        public string Instructions = "n/a";
        public string CogAtlasID = "n/a";
        public string CogPOID = "n/a";
        public string InstitutionName = "n/a";
        public string InstitutionAddress = "n/a";
        public string InstitutionalDepartmentName = "n/a";
    }
}