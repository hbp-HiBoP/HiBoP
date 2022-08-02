using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class which contains all the data about a experience Event.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier</description>
    /// </item>
    /// <item>
    /// <term><b>Name</b></term> 
    /// <description>Name</description>
    /// </item>
    /// <item>
    /// <term><b>Codes</b></term> 
    /// <description>Codes of the Event</description>
    /// </item>
    /// <item>
    /// <term><b>Type</b></term> 
    /// <description>Type of the event (main or secondary)</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
	public class Event : BaseData, INameable
	{
        #region Properties
        /// <summary>
        /// Code string separator.
        /// </summary>
        private const char SEPARATOR = ',';
        /// <summary>
        /// Name.
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// Codes.
        /// </summary>
        [DataMember] public List<int> Codes { get; set; }
        /// <summary>
        /// Type (main or secondary).
        /// </summary>
        [DataMember] public Enums.MainSecondaryEnum Type { get; set; }
        /// <summary>
        /// Codes of the event in a string format with code separate with the string separator.
        /// </summary>
        [IgnoreDataMember] public string CodesString
        {
            get { return GetStringFromCodes(Codes.ToArray()); }
            set {Codes = GetCodesFromString(value).ToList(); }
        }
        /// <summary>
        /// True if is visualizable, False otherwise.
        /// </summary>
        public bool IsVisualizable
        {
            get
            {
                return Codes.Count > 0;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new Event instance.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="codes">Codes</param>
        /// <param name="type">Type (main or secondary) </param>
        /// <param name="ID">Unique identifier</param>
        public Event(string name, IEnumerable<int> codes, Enums.MainSecondaryEnum type, string ID) : base(ID)
        {
            Name = name;
            Codes = codes.ToList();
            Type = type;
        }
        /// <summary>
        /// Create a new Event instance.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="codes">Codes</param>
        /// <param name="type">Type (main or secondary)</param>
        public Event(string name, IEnumerable<int> codes, Enums.MainSecondaryEnum type) : base()
        {
            Name = name;
            Codes = codes.ToList();
            Type = type;
        }
        /// <summary>
        /// Create a new Event instance.
        /// </summary>
        /// <param name="type">Type (main or secondary)</param>
        public Event(Enums.MainSecondaryEnum type) : this("New Event",new int[0],type)
        {

        }
        /// <summary>
        /// Create a new Event instance with default parameters
        /// </summary>
        public Event() : this(Enums.MainSecondaryEnum.Main)
		{
		}
        #endregion

        #region Private Methods
        /// <summary>
        /// Get string from code array.
        /// </summary>
        /// <param name="codes">Codes to translate.</param>
        /// <returns>Codes string translated.</returns>
        public static string GetStringFromCodes(int[] codes)
		{
            string result = string.Empty;
			for(int i=0 ;i < codes.Length;i++)
			{
				result += codes[i].ToString();
				if(i < codes.Length-1)
				{
					result += SEPARATOR;
				}
			}
			return result;
		}		
        /// <summary>
        /// Get code array from string.
        /// </summary>
        /// <param name="codesString">String to translate.</param>
        /// <returns>Codes array.</returns>
		public static int[] GetCodesFromString(string codesString)
		{
			string[] codes = codesString.Split(new char[] { SEPARATOR },StringSplitOptions.RemoveEmptyEntries);
			List<int> result = new List<int>();
			for(int i=0;i<codes.Length;i++)
			{
                int code;
                if(int.TryParse(codes[i],out code))
                {
                    result.Add(code);
                }
			}
			return result.ToArray();
		}
        #endregion

        #region Operators
        /// <summary>
        /// Clone a Event instance.
        /// </summary>
        /// <returns>Event cloned.</returns>
        public override object Clone()
        {
            return new Event(Name, Codes, Type, ID);
        }
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if(obj is Event eve)
            {
                Name = eve.Name;
                Codes = eve.Codes;
                Type = eve.Type;
            }
        }
        #endregion
    }
}