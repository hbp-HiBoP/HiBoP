using System;
using System.Linq;
using System.Collections.Generic;

namespace HBP.Data.Localizer
{
    public class Bloc
    {
        #region Properties
        public SubBloc[] SubBlocs { get; set; }
        public Dictionary<string, string> UnitBySite { get; set; }
        #endregion

        #region Constructor
        public Bloc(SubBloc[] subBlocs, Dictionary<string, string> uniteBySite)
        {
            SubBlocs = subBlocs;
            UnitBySite = uniteBySite;
        }
        public Bloc(): this(new SubBloc[0], new Dictionary<string, string>())
        {
        }
        #endregion

        #region Public static Methods
        public static Bloc Average(Bloc[] blocs, Enums.AveragingType valueAveragingMode, Enums.AveragingType eventPositionAveragingMode )
        {
            Bloc firstBloc = blocs.FirstOrDefault();
            try
            {
                int numberOfSubBlocs = firstBloc.SubBlocs.Length;
                SubBloc[] subBlocs = new SubBloc[numberOfSubBlocs];
                for (int i = 0; i < numberOfSubBlocs; i++)
                {
                    subBlocs[i] = SubBloc.Average(blocs.Select((bloc) => bloc.SubBlocs[i]).ToArray(), valueAveragingMode, eventPositionAveragingMode);
                }
                return new Bloc(subBlocs, firstBloc.UnitBySite);
            }
            catch
            {
                throw new Exception("The blocs array to averaging is empty.");
            }
		}
        #endregion
    }
} 