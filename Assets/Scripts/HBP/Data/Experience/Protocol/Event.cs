using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Experience.Protocol
{
    /**
    * \class Event
    * \author Adrien Gannerie
    * \version 1.0
    * \date 09 janvier 2017
    * \brief Protocol Event.
    * 
    * \details Class which define a event in a protocol which contains :
    *     - \a Label.
    *     - \a Codes ( Put ',' between codes in the UI).
    */
    [DataContract]
	public class Event : BaseData
	{
        #region Properties
        /** Code separator. */
        private const char SEPARATOR = ',';
        /** Name of the code. */
        [DataMember] public string Name { get; set; }
        /// <summary>
        ///  Codes of the event.
        /// </summary>
        [DataMember] public List<int> Codes { get; set; }
        /// <summary>
        /// Type of event.
        /// </summary>
        [DataMember] public Enums.MainSecondaryEnum Type { get; set; }
        /** Codes of the event in a string with code separate with ','. */
        [IgnoreDataMember] public string CodesString
        {
            get { return GetStringFromCodes(Codes.ToArray()); }
            set {Codes = GetCodesFromString(value).ToList(); }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of a event.
        /// </summary>
        /// <param name="name">Label of the event.</param>
        /// <param name="codes">Codes of the event.</param>
        public Event(string name, int[] codes, Enums.MainSecondaryEnum type, string id) : base(id)
        {
            Name = name;
            Codes = codes.ToList();
            Type = type;
        }
        /// <summary>
        /// Create a new instance of a event.
        /// </summary>
        /// <param name="name">Label of the event.</param>
        /// <param name="codes">Codes of the event.</param>
        public Event(string name, int[] codes, Enums.MainSecondaryEnum type) : base()
        {
            Name = name;
            Codes = codes.ToList();
            Type = type;
        }
        public Event(Enums.MainSecondaryEnum type) : this("",new int[0],type)
        {

        }
        /// <summary>
        /// Create a new instance with a empty label and no codes.
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
            return new Event(Name.Clone() as string, Codes.ToArray().Clone() as int[], Type);
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