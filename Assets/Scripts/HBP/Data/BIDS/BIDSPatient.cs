using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HBP.Core.Data;
using HBP.Core.Database;

namespace HBP.Data.BIDS
{
    /// <summary>
    /// Represents a patient in BIDS format with potentially anonymized ID.
    /// </summary>
    public class BIDSPatient
    {
        #region Properties
        public Patient Patient { get; private set; }
        public List<IEEGDataInfo> DataInfos { get; private set; } = new();

        /// <summary>
        /// BIDS-formatted participant ID (e.g., "sub-001" or "sub-patient01").
        /// </summary>
        public string ParticipantId { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new BIDSPatient instance.
        /// </summary>
        /// <param name="patient">Original patient data</param>
        /// <param name="subjectId">Subject ID without "sub-" prefix</param>
        public BIDSPatient(Patient patient, IEnumerable<Protocol> protocols, IEnumerable<string> dataNames, string subjectId)
        {
            Patient = patient ?? throw new ArgumentNullException(nameof(patient));
            DataInfos = DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>().Where(di => di.Patient == Patient && protocols.Contains(di.Protocol) && dataNames.Contains(di.Name)).ToList();
            ParticipantId = $"sub-{subjectId}";
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Create a non-anonymized BIDS patient with alphanumeric-only ID.
        /// </summary>
        /// <param name="patient">Original patient</param>
        /// <returns>BIDS patient with clean alphanumeric ID</returns>
        public static BIDSPatient CreateNonAnonymized(Patient patient, IEnumerable<Protocol> protocols, IEnumerable<string> dataNames)
        {
            // Remove non-alphanumeric characters from patient name
            string cleanId = Regex.Replace(patient.ID, @"[^a-zA-Z0-9]", "");
            
            // Ensure the ID is not empty
            if (string.IsNullOrEmpty(cleanId))
            {
                cleanId = "patient";
            }

            return new BIDSPatient(patient, protocols, dataNames, cleanId);
        }

        /// <summary>
        /// Create an anonymized BIDS patient with zero-padded numeric ID.
        /// </summary>
        /// <param name="patient">Original patient</param>
        /// <param name="anonymizedNumber">Anonymized number (1-based)</param>
        /// <returns>BIDS patient with anonymized ID</returns>
        public static BIDSPatient CreateAnonymized(Patient patient, IEnumerable<Protocol> protocols, IEnumerable<string> dataNames, int anonymizedNumber)
        {
            // Zero-pad to 3 digits
            string anonymizedId = anonymizedNumber.ToString("D3");
            return new BIDSPatient(patient, protocols, dataNames, anonymizedId);
        }

        /// <summary>
        /// String representation showing the participant ID.
        /// </summary>
        /// <returns>Participant ID</returns>
        public override string ToString()
        {
            return ParticipantId;
        }
        #endregion
    }
}