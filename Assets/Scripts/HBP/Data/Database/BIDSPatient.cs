using System;
using System.Text.RegularExpressions;
using HBP.Core.Data;

namespace HBP.Data.BIDS
{
    /// <summary>
    /// Represents a patient in BIDS format with potentially anonymized ID.
    /// </summary>
    public class BIDSPatient
    {
        #region Properties
        /// <summary>
        /// Original patient data.
        /// </summary>
        public Patient OriginalPatient { get; private set; }

        /// <summary>
        /// BIDS-formatted participant ID (e.g., "sub-001" or "sub-patient01").
        /// </summary>
        public string ParticipantId { get; private set; }

        /// <summary>
        /// The ID part without the "sub-" prefix (e.g., "001" or "patient01").
        /// </summary>
        public string SubjectId { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new BIDSPatient instance.
        /// </summary>
        /// <param name="originalPatient">Original patient data</param>
        /// <param name="subjectId">Subject ID without "sub-" prefix</param>
        public BIDSPatient(Patient originalPatient, string subjectId)
        {
            OriginalPatient = originalPatient ?? throw new ArgumentNullException(nameof(originalPatient));
            SubjectId = subjectId ?? throw new ArgumentNullException(nameof(subjectId));
            ParticipantId = $"sub-{subjectId}";
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Create a non-anonymized BIDS patient with alphanumeric-only ID.
        /// </summary>
        /// <param name="patient">Original patient</param>
        /// <returns>BIDS patient with clean alphanumeric ID</returns>
        public static BIDSPatient CreateNonAnonymized(Patient patient)
        {
            // Remove non-alphanumeric characters from patient name
            string cleanId = Regex.Replace(patient.Name, @"[^a-zA-Z0-9]", "");
            
            // Ensure the ID is not empty
            if (string.IsNullOrEmpty(cleanId))
            {
                cleanId = "patient";
            }

            return new BIDSPatient(patient, cleanId);
        }

        /// <summary>
        /// Create an anonymized BIDS patient with zero-padded numeric ID.
        /// </summary>
        /// <param name="patient">Original patient</param>
        /// <param name="anonymizedNumber">Anonymized number (1-based)</param>
        /// <returns>BIDS patient with anonymized ID</returns>
        public static BIDSPatient CreateAnonymized(Patient patient, int anonymizedNumber)
        {
            // Zero-pad to 3 digits
            string anonymizedId = anonymizedNumber.ToString("D3");
            return new BIDSPatient(patient, anonymizedId);
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