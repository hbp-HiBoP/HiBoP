using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HBP.Data.Experience.Protocol
{
    /// <summary>
    /// Class which define a event in a bloc.
    ///     -Label of the event.
    ///     -Codes of the event.
    /// </summary>
    [Serializable]
	public class Event : ICloneable
	{
		#region Properties
		private const char SEPARATOR = '_';

        [SerializeField]
		private string name;
		public string Name
        {
            get { return name; }
            set { name=value; }
        }
		
        [SerializeField]
		private List<int> codes;
		public ReadOnlyCollection<int> Codes
        {
            get { return new ReadOnlyCollection<int>(codes); }
        }
		public string CodesString
        {
            get { return GenerateStringFromCodes(codes.ToArray()); }
            set {codes = GenerateCodesFromString(value).ToList(); }
        }
        #endregion

        #region Constructors
        public Event(string label, int[] codes)
        {
            Name = label;
            this.codes = codes.ToList();
        }
        public Event() : this(string.Empty,new int[0])
		{
		}
        #endregion

        #region Private Methods
        private string GenerateStringFromCodes(int[] codes)
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
		private int[] GenerateCodesFromString(string codesString)
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
        public object Clone()
        {
            return new Event(Name.Clone() as string, codes.ToArray().Clone() as int[]);
        }
        #endregion
    }
}
